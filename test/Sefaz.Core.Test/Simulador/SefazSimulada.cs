using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sefaz.Core.Test.Simulador
{
    /// <summary>
    /// Servidor HTTPS que faz o papel dos web services da SEFAZ: exige TLS 1.2 com certificado de cliente,
    /// registra tudo o que recebe e responde com o XML que cada teste definir
    /// </summary>
    internal sealed class SefazSimulada : IAsyncDisposable
    {
        public const string NFeDistribuicaoDFe = "/NFeDistribuicaoDFe/NFeDistribuicaoDFe.asmx";
        public const string CTeDistribuicaoDFe = "/CTeDistribuicaoDFe/CTeDistribuicaoDFe.asmx";
        public const string NFeRecepcaoEvento = "/NFeRecepcaoEvento4/NFeRecepcaoEvento4.asmx";
        public const string CTeRecepcaoEvento = "/CTeRecepcaoEvento/CTeRecepcaoEvento.asmx";

        /// <summary>Como cada serviço embrulha o resultado na resposta SOAP (igual aos WSDLs em doc/wsdl)</summary>
        private static readonly Dictionary<string, Func<string, string>> EmbrulhoDaResposta = new()
        {
            [NFeDistribuicaoDFe] = resultado =>
                "<nfeDistDFeInteresseResponse xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe\">" +
                $"<nfeDistDFeInteresseResult>{resultado}</nfeDistDFeInteresseResult></nfeDistDFeInteresseResponse>",
            [CTeDistribuicaoDFe] = resultado =>
                "<cteDistDFeInteresseResponse xmlns=\"http://www.portalfiscal.inf.br/cte/wsdl/CTeDistribuicaoDFe\">" +
                $"<cteDistDFeInteresseResult>{resultado}</cteDistDFeInteresseResult></cteDistDFeInteresseResponse>",
            [NFeRecepcaoEvento] = resultado =>
                $"<nfeRecepcaoEventoNFResult xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4\">{resultado}</nfeRecepcaoEventoNFResult>",
            [CTeRecepcaoEvento] = resultado =>
                $"<cteRecepcaoEventoResult xmlns=\"http://www.portalfiscal.inf.br/cte/wsdl/CteRecepcaoEvento\">{resultado}</cteRecepcaoEventoResult>",
        };

        private readonly WebApplication _app;
        private readonly ConcurrentQueue<RequisicaoRecebida> _requisicoes = new();
        private readonly ConcurrentDictionary<string, Func<RequisicaoRecebida, string>> _respostas = new();

        private SefazSimulada(X509Certificate2 certificadoServidor)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, endereco => endereco.UseHttps(https =>
            {
                https.ServerCertificate = certificadoServidor;
                https.SslProtocols = SslProtocols.Tls12; // como a SEFAZ
                https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                https.AllowAnyClientCertificate(); // quem confere o certificado são os testes, pelo que foi registrado
            })));

            _app = builder.Build();
            _app.Run(AtenderAsync);
        }

        /// <summary>Endereço base, no formato https://127.0.0.1:porta</summary>
        public Uri Endereco { get; private set; }

        public IReadOnlyList<RequisicaoRecebida> Requisicoes => _requisicoes.ToArray();

        public static async Task<SefazSimulada> IniciarAsync(X509Certificate2 certificadoServidor)
        {
            var simulador = new SefazSimulada(certificadoServidor);
            await simulador._app.StartAsync();
            var enderecos = simulador._app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            simulador.Endereco = new Uri(enderecos.Addresses.Single());
            return simulador;
        }

        public string Url(string servico) => new Uri(Endereco, servico).ToString();

        /// <summary>Define o XML de resultado (retDistDFeInt, retEnvEvento...) que o serviço devolve a cada chamada</summary>
        public void Responder(string servico, Func<RequisicaoRecebida, string> resultado) => _respostas[servico] = resultado;

        /// <summary>Esquece as requisições e respostas do teste anterior</summary>
        public void Reiniciar()
        {
            _requisicoes.Clear();
            _respostas.Clear();
        }

        private async Task AtenderAsync(HttpContext contexto)
        {
            using var bytes = new MemoryStream();
            await contexto.Request.Body.CopyToAsync(bytes);
            var conteudo = bytes.ToArray();

            // A SEFAZ exige UTF-8 (rejeição 402): decodifica em modo estrito, sem trocar bytes inválidos em silêncio
            var bom = Encoding.UTF8.GetPreamble();
            var tinhaBom = conteudo.Length >= bom.Length && conteudo.AsSpan(0, bom.Length).SequenceEqual(bom);
            var envelope = new XmlDocument { PreserveWhitespace = true };
            string corpo = null, erroDeLeitura = null;
            try
            {
                corpo = new UTF8Encoding(false, true).GetString(conteudo, tinhaBom ? bom.Length : 0, conteudo.Length - (tinhaBom ? bom.Length : 0));
                envelope.LoadXml(corpo);
            }
            catch (DecoderFallbackException ex) { erroDeLeitura = $"não é UTF-8 válido ({ex.Message})"; }
            catch (XmlException ex) { erroDeLeitura = ex.Message; }

            var requisicao = new RequisicaoRecebida
            {
                Metodo = contexto.Request.Method,
                Caminho = contexto.Request.Path,
                ContentType = contexto.Request.ContentType,
                SoapAction = contexto.Request.Headers["SOAPAction"].FirstOrDefault(),
                ProtocoloTls = contexto.Features.Get<ITlsHandshakeFeature>()?.Protocol ?? SslProtocols.None,
                CertificadoCliente = contexto.Connection.ClientCertificate,
                Enquadramento = contexto.Request.ContentLength.HasValue ? "Content-Length" : contexto.Request.Headers.TransferEncoding.ToString(),
                TinhaBom = tinhaBom,
                Corpo = corpo,
                Envelope = envelope,
            };
            _requisicoes.Enqueue(requisicao);

            var soap12 = requisicao.VersaoSoap == "1.2";
            if (erroDeLeitura != null)
            {
                await ResponderFalhaAsync(contexto, soap12, $"O corpo da requisição não pôde ser lido: {erroDeLeitura}");
                return;
            }
            if (!EmbrulhoDaResposta.TryGetValue(requisicao.Caminho, out var embrulhar) || !_respostas.TryGetValue(requisicao.Caminho, out var gerarResultado))
            {
                await ResponderFalhaAsync(contexto, soap12, $"Nenhuma resposta configurada para {requisicao.Caminho}.");
                return;
            }

            string resultado;
            try { resultado = gerarResultado(requisicao); }
            catch (Exception ex)
            {
                await ResponderFalhaAsync(contexto, soap12, $"Erro ao montar a resposta simulada: {ex.Message}");
                return;
            }

            var namespaceSoap = soap12 ? RequisicaoRecebida.NamespaceSoap12 : RequisicaoRecebida.NamespaceSoap11;
            contexto.Response.ContentType = soap12 ? "application/soap+xml; charset=utf-8" : "text/xml; charset=utf-8";
            await contexto.Response.WriteAsync(
                $"<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"{namespaceSoap}\"><soap:Body>{embrulhar(resultado)}</soap:Body></soap:Envelope>");
        }

        private static async Task ResponderFalhaAsync(HttpContext contexto, bool soap12, string motivo)
        {
            var texto = SecurityElement.Escape(motivo);
            contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
            if (soap12)
            {
                contexto.Response.ContentType = "application/soap+xml; charset=utf-8";
                await contexto.Response.WriteAsync(
                    $"<?xml version=\"1.0\" encoding=\"utf-8\"?><env:Envelope xmlns:env=\"{RequisicaoRecebida.NamespaceSoap12}\"><env:Body><env:Fault>" +
                    $"<env:Code><env:Value>env:Sender</env:Value></env:Code><env:Reason><env:Text xml:lang=\"pt-BR\">{texto}</env:Text></env:Reason>" +
                    "</env:Fault></env:Body></env:Envelope>");
            }
            else
            {
                contexto.Response.ContentType = "text/xml; charset=utf-8";
                await contexto.Response.WriteAsync(
                    $"<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"{RequisicaoRecebida.NamespaceSoap11}\"><soap:Body><soap:Fault>" +
                    $"<faultcode>soap:Client</faultcode><faultstring>{texto}</faultstring></soap:Fault></soap:Body></soap:Envelope>");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
