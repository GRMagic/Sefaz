using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace Sefaz.Core.Test.Simulador
{
    /// <summary>Documento que a distribuição devolve compactado (gzip + base64) num docZip</summary>
    internal sealed record DocumentoZip(long Nsu, string Schema, string Xml);

    /// <summary>
    /// Resultados no formato dos XSDs de retorno da SEFAZ (retDistDFeInt, retEnvEvento, retEventoCTe)
    /// </summary>
    internal static class RespostasSefaz
    {
        private const string NamespaceNFe = "http://www.portalfiscal.inf.br/nfe";
        private const string NamespaceCTe = "http://www.portalfiscal.inf.br/cte";
        private const string VerAplic = "SEFAZ_SIMULADA_1.0";

        /// <summary>dhResp da distribuição de NF-e, com fuso (TDateTimeUTC)</summary>
        public const string DataHoraNFe = "2026-11-26T10:00:00-03:00";

        /// <summary>dhResp da distribuição de CT-e, sem fuso (AAAA-MM-DDTHH:MM:SS)</summary>
        public const string DataHoraCTe = "2026-11-26T10:00:00";

        public static string DistribuicaoNFe(string cStat, string xMotivo, long ultNSU, long maxNSU, params DocumentoZip[] documentos) =>
            Distribuicao(NamespaceNFe, "1.01", DataHoraNFe, cStat, xMotivo, ultNSU, maxNSU, documentos);

        public static string DistribuicaoCTe(string cStat, string xMotivo, long ultNSU, long maxNSU, params DocumentoZip[] documentos) =>
            Distribuicao(NamespaceCTe, "1.00", DataHoraCTe, cStat, xMotivo, ultNSU, maxNSU, documentos);

        /// <summary>Retorno do envio de evento de NF-e: o lote e, se o lote foi processado (128), o evento</summary>
        public static string EventoNFe(string chave, string tpEvento, string cStatLote = "128", string cStatEvento = "135",
                                       string xMotivoEvento = "Evento registrado e vinculado a NF-e")
        {
            var retEvento = cStatLote != "128" ? "" :
                "<retEvento versao=\"1.00\"><infEvento>" +
                $"<tpAmb>2</tpAmb><verAplic>{VerAplic}</verAplic><cOrgao>91</cOrgao><cStat>{cStatEvento}</cStat><xMotivo>{Texto(xMotivoEvento)}</xMotivo>" +
                $"<chNFe>{chave}</chNFe><tpEvento>{tpEvento}</tpEvento><xEvento>Manifestacao do destinatario</xEvento><nSeqEvento>1</nSeqEvento>" +
                $"<dhRegEvento>{DataHoraNFe}</dhRegEvento><nProt>891260000000001</nProt>" +
                "</infEvento></retEvento>";

            return $"<retEnvEvento xmlns=\"{NamespaceNFe}\" versao=\"1.00\"><idLote>1</idLote><tpAmb>2</tpAmb><verAplic>{VerAplic}</verAplic>" +
                   $"<cOrgao>91</cOrgao><cStat>{cStatLote}</cStat><xMotivo>{(cStatLote == "128" ? "Lote de evento processado" : "Rejeicao")}</xMotivo>" +
                   $"{retEvento}</retEnvEvento>";
        }

        /// <summary>Retorno do evento de CT-e</summary>
        public static string EventoCTe(string chave, string cStat = "135", string xMotivo = "Evento registrado e vinculado ao CT-e") =>
            $"<retEventoCTe xmlns=\"{NamespaceCTe}\" versao=\"3.00\"><infEvento>" +
            $"<tpAmb>2</tpAmb><verAplic>{VerAplic}</verAplic><cOrgao>{chave[..2]}</cOrgao><cStat>{cStat}</cStat><xMotivo>{Texto(xMotivo)}</xMotivo>" +
            $"<chCTe>{chave}</chCTe><tpEvento>610110</tpEvento><xEvento>Prestacao do Servico em Desacordo</xEvento><nSeqEvento>1</nSeqEvento>" +
            $"<dhRegEvento>{DataHoraNFe}</dhRegEvento><nProt>143260000000001</nProt>" +
            "</infEvento></retEventoCTe>";

        public static string Compactar(string xml)
        {
            using var memoria = new MemoryStream();
            using (var gzip = new GZipStream(memoria, CompressionLevel.Optimal, leaveOpen: true))
            {
                var bytes = Encoding.UTF8.GetBytes(xml);
                gzip.Write(bytes, 0, bytes.Length);
            }
            return Convert.ToBase64String(memoria.ToArray());
        }

        public static string Nsu(long nsu) => nsu.ToString("000000000000000");

        private static string Distribuicao(string ns, string versao, string dhResp, string cStat, string xMotivo, long ultNSU, long maxNSU, DocumentoZip[] documentos)
        {
            var lote = documentos.Length == 0 ? "" :
                "<loteDistDFeInt>" +
                string.Concat(documentos.Select(d => $"<docZip NSU=\"{Nsu(d.Nsu)}\" schema=\"{d.Schema}\">{Compactar(d.Xml)}</docZip>")) +
                "</loteDistDFeInt>";

            return $"<retDistDFeInt xmlns=\"{ns}\" versao=\"{versao}\"><tpAmb>2</tpAmb><verAplic>{VerAplic}</verAplic>" +
                   $"<cStat>{cStat}</cStat><xMotivo>{Texto(xMotivo)}</xMotivo><dhResp>{dhResp}</dhResp>" +
                   $"<ultNSU>{Nsu(ultNSU)}</ultNSU><maxNSU>{Nsu(maxNSU)}</maxNSU>{lote}</retDistDFeInt>";
        }

        private static string Texto(string texto) => SecurityElement.Escape(texto);
    }
}
