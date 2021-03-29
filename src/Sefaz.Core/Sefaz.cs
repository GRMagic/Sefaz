using Sefaz.Core.Meta.NFeDistDFe;
using Sefaz.WCF;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Sefaz.Core
{
    public class Sefaz : IDisposable
    {
        private X509Certificate2 Certificado;
        private string _EndPointNFeDistribuicaoDFe;
        private TAmb _Ambiente;

        public Sefaz(string certificado, 
                     string senha = null, 
                     TAmb ambiente = TAmb.Producao,
                     string endPointNFeDistribuicaoDFe = "https://www1.nfe.fazenda.gov.br/NFeDistribuicaoDFe/NFeDistribuicaoDFe.asmx")
        {
            _EndPointNFeDistribuicaoDFe = endPointNFeDistribuicaoDFe;
            _Ambiente = ambiente;
            this.Certificado = new X509Certificate2(certificado, senha, X509KeyStorageFlags.MachineKeySet);
        }

        /// <summary>
        /// Faz a chamada para o webservice de distribuição de NFe
        /// </summary>
        /// <param name="cUF">Código da UF (IBGE)</param>
        /// <param name="cnpj">CNPJ da empresa (14 dígitos - sem máscara)</param>
        /// <param name="dados">Dados que serão enviados para o webservice</param>
        private async Task<retDistDFeInt> ChamarWsNfe(string cUF, string cnpj, object dados)
        {
            if (DateTime.UtcNow > Certificado.NotAfter) throw new Exception($"O certificado venceu em {Certificado.NotAfter}!");
            if (DateTime.UtcNow < Certificado.NotBefore) throw new Exception("O certificado ainda não é válido!");

            // Configuração do ws
            var endpoint = new System.ServiceModel.EndpointAddress(_EndPointNFeDistribuicaoDFe);
            var binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport) { MaxReceivedMessageSize = 999999999 };
            binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Certificate;

            // Instância do cliente
            var ws = new NFeDistribuicaoDFeSoapClient(binding, endpoint);

            try
            {
                // Definição do certificado
                ws.ClientCredentials.ClientCertificate.Certificate = Certificado;

                // Dados
                TCodUfIBGE eUF;
                 if (!TCodUfIBGE.TryParse("Item" + cUF, out eUF)) throw new Exception("Código IBGE da UF Inválido!");
                using var memoryStream = new MemoryStream();
                using var streamWriter = new StreamWriter(memoryStream);

                new XmlSerializer(typeof(distDFeInt)).Serialize(streamWriter, new distDFeInt
                {
                    tpAmb = _Ambiente,
                    cUFAutor = eUF,
                    cUFAutorSpecified = true,
                    ItemElementName = cnpj.Length > 11 ? CpfCnpjChoiceType.CNPJ : CpfCnpjChoiceType.CPF,
                    CpjCnpj = cnpj,
                    Item1 = dados,
                    versao = TVerDistDFe.Item101,
                });
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(Encoding.UTF8.GetString(memoryStream.ToArray()));
                
                // Chama o web service
                var resposta = await ws.nfeDistDFeInteresseAsync(xmlDocument.DocumentElement);
                
                // Trabalha com a resposta
                return resposta.Body.nfeDistDFeInteresseResult.DeserializeTo<retDistDFeInt>();
                
            }
            finally
            {
                await ws.CloseAsync();
            }
        }

        /// <summary>
        /// Chama o WS da Sefaz para baixar a NFe
        /// </summary>
        /// <param name="cUF">Código IBGE da UF do destinatário</param>
        /// <param name="cnpj">CNPJ do destinatário</param>
        /// <param name="chave">Chave da nota</param>
        /// <param name="caminhoXML">Quando informado tenta salvar o XML no caminho específicado</param>
        /// <returns>XML retornado pela Sefaz já deserializado em um objeto</returns>
        public async Task<retDistDFeInt> DownloadNFe(string cUF, string cnpj, string chave, string caminhoXML = null)
        {

            // Dados
            var dados = new distDFeIntConsChNFe
            {
                chNFe = chave
            };

            // Chamada
            var retorno = await ChamarWsNfe(cUF, cnpj, dados);

            // Salvar o arquivo
            if (caminhoXML != null)
            {
                string retNFe = null;
                try
                {
                    var nota = retorno.loteDistDFeInt.docZip.Where(d => d.schema.StartsWith("procNFe_")).FirstOrDefault();
                    var xml = nota.Decompress();
                    retNFe = xml.OuterXml;
                }
                catch { }
                if (retNFe != null) File.WriteAllText(caminhoXML, retNFe, new UTF8Encoding(false)); // Salva como UTF8 sem BOM (igual quando baixa pelo site da fazenda)
            }

            return retorno;
        }

        public void Dispose() => Certificado?.Dispose();
    }
}
