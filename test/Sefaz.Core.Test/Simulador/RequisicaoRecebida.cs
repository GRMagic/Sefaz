using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace Sefaz.Core.Test.Simulador
{
    /// <summary>
    /// O que a SEFAZ simulada recebeu numa chamada, do jeito que chegou do outro lado da conexão
    /// </summary>
    internal sealed class RequisicaoRecebida
    {
        public const string NamespaceSoap11 = "http://schemas.xmlsoap.org/soap/envelope/";
        public const string NamespaceSoap12 = "http://www.w3.org/2003/05/soap-envelope";

        public string Metodo { get; init; }
        public string Caminho { get; init; }
        public string ContentType { get; init; }

        /// <summary>Cabeçalho HTTP SOAPAction (só existe no SOAP 1.1)</summary>
        public string SoapAction { get; init; }

        public SslProtocols ProtocoloTls { get; init; }
        public X509Certificate2 CertificadoCliente { get; init; }

        /// <summary>"Content-Length" ou o valor de Transfer-Encoding (por exemplo, "chunked")</summary>
        public string Enquadramento { get; init; }

        /// <summary>Se o corpo começava com o BOM do UTF-8</summary>
        public bool TinhaBom { get; init; }

        /// <summary>Corpo HTTP decodificado como UTF-8 estrito, sem o BOM</summary>
        public string Corpo { get; init; }

        public XmlDocument Envelope { get; init; }

        /// <summary>"1.1" ou "1.2", pelo namespace do envelope</summary>
        public string VersaoSoap => Envelope.DocumentElement?.NamespaceURI switch
        {
            NamespaceSoap11 => "1.1",
            NamespaceSoap12 => "1.2",
            var outro => $"desconhecida ({outro})"
        };

        public XmlElement Cabecalho => Filho(Envelope.DocumentElement, "Header");
        public XmlElement CorpoSoap => Filho(Envelope.DocumentElement, "Body");

        /// <summary>Elemento nfeDadosMsg/cteDadosMsg do envelope, como chegou (com os espaços preservados)</summary>
        public XmlElement AreaDeDados => CorpoSoap?.GetElementsByTagName("*").OfType<XmlElement>()
            .FirstOrDefault(e => e.LocalName == "nfeDadosMsg" || e.LocalName == "cteDadosMsg");

        /// <summary>
        /// A mensagem da SEFAZ dentro do envelope (distDFeInt, envEvento, eventoCTe...), como um documento à parte.
        /// É assim que a SEFAZ valida o schema e a assinatura: fora do envelope SOAP.
        /// </summary>
        public XmlDocument Mensagem
        {
            get
            {
                var mensagem = AreaDeDados?.ChildNodes.OfType<XmlElement>().FirstOrDefault();
                if (mensagem == null) return null;

                var documento = new XmlDocument { PreserveWhitespace = true };
                documento.AppendChild(documento.ImportNode(mensagem, true));
                return documento;
            }
        }

        private static XmlElement Filho(XmlElement pai, string nomeLocal) =>
            pai?.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName == nomeLocal && e.NamespaceURI == pai.NamespaceURI);
    }
}
