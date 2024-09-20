using System.IO;
using System.Xml;

namespace Sefaz.Core
{
    /// <summary>
    /// Dados do documento conforme o ambiente nacional da SEFAZ
    /// </summary>
    public class Documento
    {
        /// <summary>
        /// Sequencial de controle utilizado pela SEFAZ para identificar o documento
        /// </summary>
        public long? NSU { get; set; }

        /// <summary>
        /// Schema do XML
        /// </summary>
        /// <remarks>Útil para identificar o tipo de documento e validar o XML</remarks>
        public string Schema { get; set; }

        /// <summary>
        /// XML
        /// </summary>
        public XmlDocument Xml { get; set; }

        /// <summary>
        /// Pega o conteúdo completo do XML
        /// </summary>
        /// <returns>Conteúdo do XML</returns>
        public override string ToString() => Xml?.OuterXml;

        /// <summary>
        /// Salva o XML como UTF8 sem BOM
        /// </summary>
        /// <param name="caminho">Local onde o arquivo deve ser salvo</param>
        /// <remarks>Igual quando baixa pelo site da fazenda</remarks>
        public void SalvarArquivo(string caminho) => File.WriteAllText(caminho, Xml?.OuterXml, new System.Text.UTF8Encoding(false));
    }
}
