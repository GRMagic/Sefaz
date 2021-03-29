using System.Threading.Tasks;

namespace Sefaz.Exemplo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Dados do certificado
            var caminhoCertificado = @"D:\Temp\certificado.p12";
            var senhaCertificado = @"1234";

            // Dados da empresa
            var cUF = "12";
            var cnpj = "12345678901234";

            // Chave da nota que vamos baixar. A nota precisa ter pelo menos manifesto de ciência da emissão.
            var chaveNFe = "12345678901234567890123456789012345678901234";

            // Local onde vamos salvar a nota
            var pasta = @"D:\Temp\";

            // Criando uma instância para uso em PRODUÇÃO
            using var sefaz = new Sefaz.Core.Sefaz(caminhoCertificado, senhaCertificado);

            // Download do xml assinado (com valor fiscal)
            var consulta = await sefaz.DownloadNFe(cUF, cnpj, chaveNFe, $@"{pasta}{chaveNFe}.xml");
        }
    }
}
