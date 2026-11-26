using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace Sefaz.Core.Test.Simulador
{
    [CollectionDefinition(Nome)]
    public sealed class ColecaoSefazSimulada : ICollectionFixture<SefazSimuladaFixture>
    {
        public const string Nome = "SEFAZ simulada";
    }

    /// <summary>
    /// Gera os certificados uma vez por execução e sobe a SEFAZ simulada. Os dois lados usam os mesmos certificados:
    /// a biblioteca envia o de cliente e confia no de servidor; o simulador apresenta o de servidor e os testes
    /// conferem se o de cliente foi o recebido.
    /// </summary>
    public sealed class SefazSimuladaFixture : IAsyncLifetime
    {
        internal X509Certificate2 CertificadoCliente { get; } = CertificadosDeTeste.CriarCliente();
        internal X509Certificate2 CertificadoServidor { get; } = CertificadosDeTeste.CriarServidor();
        internal SefazSimulada Simulador { get; private set; }

        public async Task InitializeAsync() => Simulador = await SefazSimulada.IniciarAsync(CertificadoServidor);

        /// <summary>
        /// Cria a biblioteca apontando para a SEFAZ simulada
        /// </summary>
        /// <param name="certificadoServidorConfiavel">Certificado de servidor aceito; por padrão, o do simulador</param>
        /// <param name="confiarNoSimulador">Falso para manter só a validação do sistema operacional</param>
        internal Sefaz CriarSefaz(X509Certificate2 certificadoServidorConfiavel = null, bool confiarNoSimulador = true) =>
            new Sefaz(CertificadoCliente, Models.NFe.TAmb.Homologacao,
                      endPointNFeDistribuicaoDFe: Simulador.Url(SefazSimulada.NFeDistribuicaoDFe),
                      endPointCTeDistribuicaoDFe: Simulador.Url(SefazSimulada.CTeDistribuicaoDFe),
                      endPointNFeRecepcaoEvento: Simulador.Url(SefazSimulada.NFeRecepcaoEvento))
            {
                CertificadoServidorConfiavel = confiarNoSimulador ? certificadoServidorConfiavel ?? CertificadoServidor : null
            };

        public async Task DisposeAsync()
        {
            if (Simulador != null) await Simulador.DisposeAsync();
            CertificadoCliente.Dispose();
            CertificadoServidor.Dispose();
        }
    }
}
