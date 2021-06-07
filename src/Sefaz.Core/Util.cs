using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Sefaz.Core
{
    /// <summary>
    /// Classe estática com algumas funções úteis para o projeto
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Usado para gerar uma string indentanda a partir de um nó do xml
        /// </summary>
        /// <param name="node">Nó do XML</param>
        /// <param name="indentation">Quandidade de espaços usado na indentação</param>
        /// <returns></returns>
        private static string ToString(this XmlNode node, int indentation)
        {
            using var sw = new StringWriter();
            using var xw = new XmlTextWriter(sw);

            xw.Formatting = Formatting.Indented;
            xw.Indentation = indentation;
            node.WriteContentTo(xw);
            return sw.ToString();
        }

        /// <summary>
        /// Deserializa um nó do xml para um objeto
        /// </summary>
        /// <typeparam name="T">Tipo do objeto</typeparam>
        /// <param name="node">Nó do XML</param>
        /// <returns>Objeto do tipo informado</returns>
        public static T DeserializeTo<T>(this XmlNode node) where T : class
        {
            using var stm = new MemoryStream();
            using var stw = new StreamWriter(stm);
        
            stw.Write(node.OuterXml);
            stw.Flush();
            stm.Position = 0;

            XmlSerializer ser = new XmlSerializer(typeof(T));
            return ser.Deserialize(stm) as T;
        }

        /// <summary>
        /// Deserializa um nó do xml para um objeto
        /// </summary>
        /// <typeparam name="T">Tipo do objeto</typeparam>
        /// <param name="element">Nó do XML</param>
        /// <returns>Objeto do tipo informado</returns>
        public static T DeserializeTo<T>(this System.Xml.Linq.XElement element) where T : class
        {
            using var stm = new MemoryStream();
            using var stw = new StreamWriter(stm);

            stw.Write(element);
            stw.Flush();
            stm.Position = 0;

            XmlSerializer ser = new XmlSerializer(typeof(T));
            return ser.Deserialize(stm) as T;
        }


        /// <summary>
        /// Le um doczip e retorna um xml
        /// </summary>
        /// <param name="docZip">Dados compactados</param>
        /// <returns>Dados descompactados</returns>
        internal static XmlDocument Decompress(this Meta.retDistDFeIntLoteDistDFeIntDocZip docZip)
        {
            using var strStream = new MemoryStream();
            strStream.Write(docZip.Value, 0, docZip.Value.Length);
            strStream.Position = 0;
            using Stream csStream = new System.IO.Compression.GZipStream(strStream, System.IO.Compression.CompressionMode.Decompress);

            var xml = new XmlDocument();
            xml.Load(csStream);
            return xml;
        }

        ///// <summary>
        ///// Le um doczip e retorna um xml
        ///// </summary>
        ///// <param name="docZip">Dados compactados</param>
        ///// <returns>Dados descompactados</returns>
        //public static XmlDocument Decompress(this Meta.CTe.retDistDFeIntLoteDistDFeIntDocZip docZip)
        //{
        //    using var strStream = new MemoryStream();
        //    strStream.Write(docZip.Value, 0, docZip.Value.Length);
        //    strStream.Position = 0;
        //    using Stream csStream = new System.IO.Compression.GZipStream(strStream, System.IO.Compression.CompressionMode.Decompress);

        //    var xml = new XmlDocument();
        //    xml.Load(csStream);
        //    return xml;
        //}

        /// <summary>
        /// Verifica se o xml está seguindo o schema xsd
        /// </summary>
        /// <param name="xml">Leitor do XML</param>
        /// <param name="xsdFile">Caminho do arquivo xsd</param>
        /// <returns>Um erro por linha</returns>
        public static string CheckXSD(XmlReader xml, string xsdFile)
        {
            // Valida o xsd
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.DtdProcessing = DtdProcessing.Prohibit; // Por padrão já é proibido, mas como é uma questão de segurança, estou deixando essa opção explícita
            var schemas = new XmlSchemaSet();
            settings.Schemas = schemas;
            // Quando carregar o eschema, especificar o namespace que ele valida e a localização do arquivo 
            schemas.Add(null, xsdFile);
            // Especifica o tratamento de evento para os erros de validacao
            string msg = null;
            settings.ValidationEventHandler += (object sender, ValidationEventArgs args) => {
                if (msg == null) msg = "";
                if (msg.Length > 0) msg += "\n";
                msg += args.Message;
            };

            // Cria um leitor para validação e faz a leitura de todos os dados XML
            using var validator = XmlReader.Create(xml, settings);
            while (validator.Read());
            
            return msg;
        }

        /// <summary>
        /// Verifica se o xml está seguindo o schema xsd
        /// </summary>
        /// <param name="xmlFile">Caminho do XML</param>
        /// <param name="xsdFile">Caminho do arquivo xsd</param>
        /// <returns>true se o xml respeita o xsd e falso se não respeita</returns>
        public static bool IsXSD(string xmlFile, string xsdFile)
        {
            // Valida o xsd
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.DtdProcessing = DtdProcessing.Prohibit; // Por padrão já é proibido, mas como é uma questão de segurança, estou deixando essa opção explícita
            var schemas = new XmlSchemaSet();
            settings.Schemas = schemas;
            // Quando carregar o eschema, especificar o namespace que ele valida e a localização do arquivo 
            schemas.Add(null, xsdFile);
            // Especifica o tratamento de evento para os erros de validacao
            bool eValido = true;
            settings.ValidationEventHandler += (object sender, ValidationEventArgs args) => {
                eValido = false;
            };
            
            // Cria um leitor para validação
            using var validator = XmlReader.Create(xmlFile, settings);
            // Faz a leitura de todos os dados XML enquanto ele ainda for válido
            while (eValido && validator.Read()) ;
            
            return eValido;
        }


        /// <summary>
        /// Coleta informações da chave da nota
        /// </summary>
        /// <param name="chNFe">Chave da nota</param>
        /// <returns>Dicionario com as informações</returns>
        public static IDictionary<string, string> InfoFromChNfe(string chNFe)
        {
            var info = new Dictionary<string, string>();
            int i = 0;
            info["cuf"] = chNFe.Substring(i, 2); i += 2;
            info["aamm"] = chNFe.Substring(i, 4); i += 4;
            info["cnpj"] = chNFe.Substring(i, 14); i += 14;
            info["modelo"] = chNFe.Substring(i, 2); i += 2;
            info["serie"] = chNFe.Substring(i, 3); i += 3;
            info["numero"] = chNFe.Substring(i, 9); i += 9;
            info["forma"] = chNFe.Substring(i, 1); i += 1;
            info["codigo"] = chNFe.Substring(i, 8); i += 8;
            info["dv"] = chNFe.Substring(i, 1); i += 1;
            return info;
        }

        /// <summary>
        /// Método responsável por assinar documentos XML. A assinatura é realizada utilizando os padrões
        /// estabelecidos para a Nota Fiscal Eletrônica.
        /// Somente é assinada a primeira TAG com seu atributo encontrada no documento XML.
        /// </summary>
        /// <param name="documentoXML">Documento XML a ser assinado</param>
        /// <param name="certificadoX509">Certificado Digital X.509 com chave privada</param>
        /// <param name="tagAAssinar">TAG do documento XML a ser assinada</param>
        /// <param name="idAtributoTag">Atributo que identifica a TAG a ser assinada</param>
        /// <returns>Documento XML assinado</returns>
        public static XmlDocument AssinarXML(XmlDocument documentoXML, X509Certificate2 certificadoX509, string tagAAssinar, string idAtributoTag = "Id")
        {
            string signatureMethod = @"http://www.w3.org/2000/09/xmldsig#rsa-sha1";
            string digestMethod = @"http://www.w3.org/2000/09/xmldsig#sha1";

            if (documentoXML == null) throw new ArgumentNullException(nameof(documentoXML));
            if (certificadoX509 == null) throw new ArgumentNullException(nameof(certificadoX509));
            if (!certificadoX509.HasPrivateKey) throw new ArgumentException("Certificado Digital informado não possui chave privada.", nameof(certificadoX509));
            if (string.IsNullOrWhiteSpace(tagAAssinar)) throw new ArgumentException("String que informa a tag XML a ser assinada está vazia,", nameof(tagAAssinar));
            if (string.IsNullOrWhiteSpace(idAtributoTag)) throw new ArgumentException("String que informa o id da tag XML a ser assinada está vazia", nameof(idAtributoTag));

            try
            {
                // Informando qual a tag será assinada
                var nodeParaAssinatura = documentoXML.GetElementsByTagName(tagAAssinar);
                var signedXml = new SignedXml((XmlElement)nodeParaAssinatura[0]);
                signedXml.SignedInfo.SignatureMethod = signatureMethod;

                // Adicionando a chave privada para assinar o documento
                signedXml.SigningKey = certificadoX509.PrivateKey;

                // Referenciando o identificador da tag que será assinada
                var reference = new Reference("#" + nodeParaAssinatura[0].Attributes[idAtributoTag].Value);
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform(false));
                reference.AddTransform(new XmlDsigC14NTransform(false));
                reference.DigestMethod = digestMethod;

                // Adicionando a referência de qual tag será assinada
                signedXml.AddReference(reference);

                // Adicionando informações do certificado na assinatura
                var keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(certificadoX509));
                signedXml.KeyInfo = keyInfo;

                // Calculando a assinatura
                signedXml.ComputeSignature();

                // Adicionando a tag de assinatura ao documento xml
                var sig = signedXml.GetXml();
                documentoXML.GetElementsByTagName(tagAAssinar)[0].ParentNode.AppendChild(sig);

                var xmlAssinado = new XmlDocument();
                xmlAssinado.PreserveWhitespace = true;
                xmlAssinado.LoadXml(documentoXML.OuterXml);
                return xmlAssinado;
            }
            catch (Exception ex)
            {
                // Falha ao assinar documento XML
                throw new Exception("Falha ao assinar documento XML.", ex);
            }
        }

    }
}
