using System.Threading.Tasks;

namespace Sefaz.Exemplo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Dados da empresa
            var cUF = "12";
            var cnpj = "12345678901234";

            // Dados do certificado
            var caminhoCertificado = @"D:\Temp\certificado.pfx";
            var senhaCertificado = @"123456";

            // Criando uma instância para uso em PRODUÇÃO
            using var sefaz = new Sefaz.Core.Sefaz(caminhoCertificado, senhaCertificado);

            // Chave da nota que vamos baixar
            var chaveNFe = "12345678901234567890123456789012345678901234";

            // -------------- Manifestar ciência da emissão por parte do destinatário --------------

            await sefaz.ManifestarNFeAsync(cnpj, chaveNFe, Core.Models.NFe.TEventoInfEventoDetEventoDescEvento.CienciaDaOperacao);
            // OBS.: É comum a SEFAZ demorar alguns segundos para liberar a NFe para download

            // -------------- Baixar uma NFe pela Chave --------------

            // Local onde vamos salvar a nota
            var pasta = @"D:\Temp\";

            // Download do xml assinado (com valor fiscal)
            var doc = await sefaz.BaixarNFeAsync(cUF, cnpj, chaveNFe);

            doc.SalvarArquivo($@"{pasta}{chaveNFe}.xml");

            // -------------- Consultar as notas e eventos de um cnpj --------------

            // Busca os útimos 90 dias
            var documentos = await sefaz.ConsultarNFeCNPJAsync(cUF, cnpj);

            foreach (var documento in documentos)
            {
                documento.SalvarArquivo($@"{pasta}{documento.NSU}.xml");
            }

            // -------------- Consultar XML conhecendo o NSU --------------

            doc = await sefaz.ConsultarNFeNSUAsync(cUF, cnpj, 1234);

            // -------------- Consultar os conhecimentos de transporte e eventos relacionados de um cnpj --------------

            // Busca os útimos 90 dias
            documentos = await sefaz.ConsultarCTeCNPJAsync(cUF, cnpj);

            // -------------- Manifestar desacordo por parte do destinatário --------------

            var chaveCTe = "12345678901234567890123456789012345678901234";
            await sefaz.ManifestarDesacordoCTeAsync(cnpj, chaveCTe, "Alguma justificativa");
        }
    }
}
