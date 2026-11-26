using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Xunit;

namespace Sefaz.Core.Test.Simulador
{
    /// <summary>
    /// Conferências do que a SEFAZ simulada recebeu: transporte, schema, assinatura e snapshot
    /// </summary>
    internal static class Verificacoes
    {
        private static readonly string PastaXsd = Path.Combine(AppContext.BaseDirectory, "XSDs");
        private static readonly ConcurrentDictionary<string, XmlSchemaSet> Schemas = new();

        /// <summary>
        /// Elementos cujo conteúdo muda a cada execução e fica fora do snapshot. O formato do dhEvento é conferido
        /// na normalização; os valores da assinatura, em <see cref="ConferirAssinatura"/>.
        /// </summary>
        private static readonly HashSet<string> ConteudoVariavel = new() { "dhEvento", "DigestValue", "SignatureValue", "X509Certificate" };

        /// <summary>Formato AAAA-MM-DDThh:mm:ss±hh:mm (TDateTimeUTC)</summary>
        private static readonly Regex FormatoDataHora = new(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$");

        /// <summary>TLS 1.2, certificado de cliente, método HTTP, UTF-8 sem BOM, versão do SOAP e action</summary>
        /// <param name="actionOpcional">
        /// No SOAP 1.2, aceita também o Content-Type sem o parâmetro action. Use só onde a versão do WCF muda isso:
        /// uma implementação nova deve enviar a action do WSDL.
        /// </param>
        public static void ConferirTransporte(RequisicaoRecebida requisicao, X509Certificate2 certificadoCliente, string versaoSoap, string action,
                                              bool actionOpcional = false)
        {
            Assert.Equal("POST", requisicao.Metodo);
            Assert.Equal(SslProtocols.Tls12, requisicao.ProtocoloTls);
            Assert.NotNull(requisicao.CertificadoCliente);
            Assert.Equal(certificadoCliente.Thumbprint, requisicao.CertificadoCliente.Thumbprint);
            Assert.False(requisicao.TinhaBom, "O corpo começa com o BOM do UTF-8.");
            Assert.Equal(versaoSoap, requisicao.VersaoSoap);

            var (tipo, parametros) = LerContentType(requisicao.ContentType);
            Assert.Equal("utf-8", parametros.GetValueOrDefault("charset")?.ToLowerInvariant());
            if (versaoSoap == "1.1")
            {
                Assert.Equal("text/xml", tipo);
                Assert.Equal(action, requisicao.SoapAction?.Trim('"'));
            }
            else
            {
                Assert.Equal("application/soap+xml", tipo);
                var recebida = parametros.GetValueOrDefault("action");
                Assert.True(recebida == action || (actionOpcional && recebida == null),
                            $"Action esperada no Content-Type: {action}{Environment.NewLine}Content-Type recebido: {requisicao.ContentType}");
            }
        }

        /// <summary>
        /// Na área de dados (o conteúdo de nfeDadosMsg/cteDadosMsg, como chegou), a SEFAZ rejeita prefixo de namespace
        /// (rejeição 404) e caracteres de edição entre as tags (588). O snapshot normaliza esses detalhes, então
        /// eles são conferidos aqui.
        /// </summary>
        public static void ConferirFormaDaMensagem(RequisicaoRecebida requisicao)
        {
            var areaDeDados = requisicao.AreaDeDados;
            Assert.NotNull(areaDeDados);

            var problemas = new List<string>();
            void Conferir(XmlNode no)
            {
                switch (no)
                {
                    case XmlWhitespace or XmlSignificantWhitespace:
                        problemas.Add($"espaço ou quebra de linha entre as tags, dentro de <{no.ParentNode?.LocalName}>");
                        break;
                    case XmlElement elemento:
                        if (elemento.Prefix != "") problemas.Add($"prefixo de namespace no elemento <{elemento.Name}>");
                        foreach (XmlAttribute atributo in elemento.Attributes)
                            if (atributo.Prefix != "" && atributo.Prefix != "xmlns")
                                problemas.Add($"atributo com prefixo {atributo.Name} em <{elemento.LocalName}>");
                        foreach (XmlNode filho in elemento.ChildNodes) Conferir(filho);
                        break;
                }
            }
            foreach (XmlNode no in areaDeDados.ChildNodes) Conferir(no);

            Assert.True(problemas.Count == 0, $"A área de dados tem o que a SEFAZ rejeita:{Environment.NewLine}{string.Join(Environment.NewLine, problemas.Distinct())}");
        }

        /// <summary>Valida o XML contra um XSD oficial de doc/XSDs (caminho relativo a essa pasta)</summary>
        public static void ConferirXsd(XmlDocument xml, string xsd)
        {
            Assert.NotNull(xml);
            var schemas = Schemas.GetOrAdd(xsd, arquivo =>
            {
                var conjunto = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
                conjunto.Add(null, Path.Combine(PastaXsd, arquivo));
                conjunto.Compile();
                return conjunto;
            });

            var copia = new XmlDocument { Schemas = schemas };
            copia.LoadXml(xml.OuterXml);
            var erros = new List<string>();
            copia.Validate((_, e) => erros.Add($"{e.Severity}: {e.Message}"));
            Assert.True(erros.Count == 0, $"O XML não segue {xsd}:{Environment.NewLine}{string.Join(Environment.NewLine, erros)}{Environment.NewLine}{xml.OuterXml}");
        }

        /// <summary>
        /// A assinatura tem de ser válida e seguir o padrão da SEFAZ: RSA-SHA1, C14N, referência ao Id do infEvento
        /// e certificado igual ao do TLS
        /// </summary>
        public static void ConferirAssinatura(XmlDocument mensagem, X509Certificate2 certificadoEsperado)
        {
            var assinaturas = mensagem.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
            Assert.Equal(1, assinaturas.Count);

            var assinatura = new SignedXml(mensagem);
            assinatura.LoadXml((XmlElement)assinaturas[0]);

            var certificadoNaAssinatura = Assert.Single(assinatura.KeyInfo.OfType<KeyInfoX509Data>()
                .SelectMany(dados => dados.Certificates.Cast<X509Certificate2>()));
            Assert.Equal(certificadoEsperado.Thumbprint, certificadoNaAssinatura.Thumbprint);
            Assert.True(assinatura.CheckSignature(certificadoNaAssinatura, true), "A assinatura não confere com o conteúdo assinado.");

            Assert.Equal(SignedXml.XmlDsigRSASHA1Url, assinatura.SignedInfo.SignatureMethod);
            Assert.Equal(SignedXml.XmlDsigC14NTransformUrl, assinatura.SignedInfo.CanonicalizationMethod);

            var referencia = Assert.Single(assinatura.SignedInfo.References.Cast<Reference>());
            var infEvento = (XmlElement)mensagem.GetElementsByTagName("infEvento", mensagem.DocumentElement.NamespaceURI)[0];
            Assert.Equal("#" + infEvento.GetAttribute("Id"), referencia.Uri);
            Assert.Equal(SignedXml.XmlDsigSHA1Url, referencia.DigestMethod);
            var transformacoes = referencia.TransformChain;
            Assert.Equal(new[] { SignedXml.XmlDsigEnvelopedSignatureTransformUrl, SignedXml.XmlDsigC14NTransformUrl },
                         Enumerable.Range(0, transformacoes.Count).Select(i => transformacoes[i].Algorithm));
        }

        /// <summary>
        /// Confere a forma da área de dados (<see cref="ConferirFormaDaMensagem"/>) e compara o que o servidor recebeu
        /// com o snapshot versionado em Snapshots/{nome}.txt. Se mudou, grava Snapshots/{nome}.recebido.txt ao lado
        /// para comparação; se o snapshot não existe, cria e falha para revisão.
        /// </summary>
        public static void ConferirSnapshot(RequisicaoRecebida requisicao, string nome, [CallerFilePath] string arquivoDoTeste = "")
        {
            ConferirFormaDaMensagem(requisicao);

            var atual = Descrever(requisicao);
            var pasta = Path.Combine(Path.GetDirectoryName(arquivoDoTeste), "Snapshots");
            var esperado = Path.Combine(pasta, nome + ".txt");
            var recebido = Path.Combine(pasta, nome + ".recebido.txt");

            if (!File.Exists(esperado))
            {
                Directory.CreateDirectory(pasta);
                File.WriteAllText(esperado, atual, new UTF8Encoding(false));
                Assert.Fail($"Snapshot criado em {esperado}. Revise o conteúdo e rode o teste de novo.");
            }

            if (File.ReadAllText(esperado).Replace("\r\n", "\n") == atual)
            {
                File.Delete(recebido);
                return;
            }

            File.WriteAllText(recebido, atual, new UTF8Encoding(false));
            Assert.Fail($"O que a SEFAZ recebe mudou em relação a {esperado}.{Environment.NewLine}" +
                        $"Compare com {recebido}; se a mudança for intencional, substitua o snapshot por ele.");
        }

        /// <summary>
        /// Descrição estável da requisição: o que importa para o servidor, sem o que varia entre execuções (data e
        /// assinatura) nem entre formas equivalentes de escrever o mesmo XML (ordem de atributos, declarações de
        /// namespace). Prefixos e espaços proibidos pela SEFAZ são barrados antes, em <see cref="ConferirFormaDaMensagem"/>.
        /// A action do SOAP 1.2 fica de fora, porque varia com a versão do WCF; ela é conferida em <see cref="ConferirTransporte"/>.
        /// </summary>
        public static string Descrever(RequisicaoRecebida requisicao)
        {
            var (tipo, parametros) = LerContentType(requisicao.ContentType);
            var parametrosEstaveis = parametros.Where(p => p.Key != "action").OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}");
            var texto = new StringBuilder();
            texto.AppendLine($"{requisicao.Metodo} {requisicao.Caminho}");
            texto.AppendLine($"Enquadramento HTTP: {requisicao.Enquadramento}");
            texto.AppendLine($"SOAP {requisicao.VersaoSoap}");
            texto.AppendLine($"Content-Type: {string.Join("; ", new[] { tipo }.Concat(parametrosEstaveis))}");
            texto.AppendLine($"SOAPAction: {requisicao.SoapAction?.Trim('"') ?? "(ausente)"}");
            texto.AppendLine();
            texto.AppendLine("Cabeçalho SOAP:");
            DescreverFilhos(texto, requisicao.Cabecalho);
            texto.AppendLine();
            texto.AppendLine("Corpo SOAP:");
            DescreverFilhos(texto, requisicao.CorpoSoap);
            return texto.ToString().Replace("\r\n", "\n");
        }

        public static string Valor(XmlDocument xml, string nomeLocal) =>
            xml.GetElementsByTagName("*").OfType<XmlElement>().FirstOrDefault(e => e.LocalName == nomeLocal)?.InnerText;

        private static void DescreverFilhos(StringBuilder texto, XmlElement pai)
        {
            var filhos = pai?.ChildNodes.OfType<XmlElement>().ToList() ?? new List<XmlElement>();
            if (filhos.Count == 0) texto.AppendLine("(vazio)");
            foreach (var filho in filhos)
            {
                using var leitor = new XmlNodeReader(filho);
                texto.AppendLine(Normalizar(XElement.Load(leitor)).ToString());
            }
        }

        /// <summary>Recria o elemento sem declarações de namespace (o LINQ to XML gera as necessárias) e sem espaços</summary>
        private static XElement Normalizar(XElement elemento)
        {
            var atributos = elemento.Attributes()
                .Where(a => !a.IsNamespaceDeclaration)
                .OrderBy(a => a.Name.NamespaceName, StringComparer.Ordinal)
                .ThenBy(a => a.Name.LocalName, StringComparer.Ordinal)
                .Select(a => new XAttribute(a.Name, a.Value));

            // Valor variável vira marcador; um dhEvento fora do formato fica literal, para o snapshot acusar
            if (ConteudoVariavel.Contains(elemento.Name.LocalName) &&
                (elemento.Name.LocalName != "dhEvento" || FormatoDataHora.IsMatch(elemento.Value)))
                return new XElement(elemento.Name, atributos, $"{{{elemento.Name.LocalName}}}");

            var conteudo = elemento.Nodes().Select(no => no switch
            {
                XElement filho => (object)Normalizar(filho),
                XText texto when !string.IsNullOrWhiteSpace(texto.Value) => new XText(texto.Value),
                _ => null
            });
            return new XElement(elemento.Name, atributos, conteudo);
        }

        private static (string Tipo, Dictionary<string, string> Parametros) LerContentType(string contentType)
        {
            var partes = (contentType ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var parametros = partes.Skip(1)
                .Select(p => p.Split('=', 2))
                .ToDictionary(p => p[0].Trim().ToLowerInvariant(), p => p.Length > 1 ? p[1].Trim().Trim('"') : "");
            return (partes.FirstOrDefault()?.ToLowerInvariant() ?? "", parametros);
        }
    }
}
