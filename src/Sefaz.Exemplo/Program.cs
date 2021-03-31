using Sefaz.Core;
using System;
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

            // -------------- Baixar uma NFe pela Chave --------------

            // Chave da nota que vamos baixar. A nota precisa ter pelo menos manifesto de ciência da emissão.
            var chaveNFe = "123456789901234567889012345678901234";

            // Local onde vamos salvar a nota
            var pasta = @"D:\Temp\";

            // Download do xml assinado (com valor fiscal)
            var doc = await sefaz.DownloadNFe(cUF, cnpj, chaveNFe);

            doc.SalvarArquivo($@"{pasta}{chaveNFe}.xml");

            // -------------- Consultar as notas e eventos de um cnpj --------------

            // Busca os útimos 90 dias
            var documentos = await sefaz.ConsultarNFeCNPJ(cUF, cnpj);
            
            foreach(var documento in documentos)
            {
                documento.SalvarArquivo($@"{pasta}{documento.NSU}.xml");
            }
        }
    }
}
