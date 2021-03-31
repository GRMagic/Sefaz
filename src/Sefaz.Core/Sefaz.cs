using Sefaz.Core.Meta.NFeDistDFe;
using Sefaz.WCF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Sefaz.Core
{
    /// <summary>
    /// Classe utilizada para comunicar-se com a Sefaz
    /// </summary>
    public class Sefaz : IDisposable
    {
        private X509Certificate2 Certificado;
        private string _EndPointNFeDistribuicaoDFe;
        private TAmb _Ambiente;

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="certificado">Caminho do certificado</param>
        /// <param name="senha">Senha do certificado</param>
        /// <param name="ambiente">Ambiente de produção ou homologação</param>
        /// <param name="endPointNFeDistribuicaoDFe">Endereço do serviço</param>
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
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ</param>
        /// <param name="chave">Chave da nota</param>
        /// <returns>Documento retornado pela SEFAZ</returns>
        public async Task<Documento> DownloadNFe(string cUF, string cnpj, string chave)
        {

            // Dados
            var dados = new distDFeIntConsChNFe
            {
                chNFe = chave
            };

            // Chamada
            var retorno = await ChamarWsNfe(cUF, cnpj, dados);

            try
            {
                var nota = retorno.loteDistDFeInt.docZip.Where(d => d.schema.StartsWith("procNFe_")).FirstOrDefault();
                return new Documento
                {
                    NSU = nota.NSU,
                    Schema = nota.schema,
                    Xml = nota.Decompress()
                };
            }
            catch {
                throw new SefazException(retorno.cStat, retorno.xMotivo);
            }

        }

        /// <summary>
        /// Chama o WS da Sefaz para consultar as notas
        /// </summary>
        /// <param name="cUF">Código IBGE da UF da empresa</param>
        /// <param name="cnpj">CNPJ do interessado</param>
        /// <param name="ultimoNSU">NSU mais recente conhecido (Quando informado 0 retorna as notas dos últimos 90 dias)</param>
        /// <param name="todosLotes">Consultar automaticamente todos os lotes até o mais atual?</param>
        /// <returns>XML retornado pela Sefaz já deserializado em um objeto</returns>
        /// <remarks>ATENÇÃO! Consultas grandes e frequentes podem causar bloqueio temporário do serviço. Evite usar ultNSU=0 mais de uma vez por hora.</remarks>
        public async Task<ListaDocumentos> ConsultarNFeCNPJ(string cUF, string cnpj, long ultimoNSU = 0, bool todosLotes = true)
        {
            var lista = new ListaDocumentos();
            lista.UltimoNSU = ultimoNSU;

            do
            {
                // Chamada ao serviço
                var consulta = await ChamarWsNfe(cUF, cnpj, new distDFeIntDistNSU
                {
                    ultNSU = lista.UltimoNSU.ToString("000000000000000") // 15 algarismos!
                });

                // Tratamento do retorno
                long.TryParse(consulta.ultNSU, out lista.UltimoNSU); // A SEFAZ manda os registros em lotes. Guardamos o ultNSU para saber onde continuar a busca
                long.TryParse(consulta.maxNSU, out lista.NSUMaximo);
                lista.DataHoraResposta = DateTime.ParseExact(consulta.dhResp, "yyyy-MM-ddTHH:mm:sszzz", null);

                switch (consulta.cStat)
                {
                    case "137": // Nenhum documento localizado para o interessado
                        break;

                    case "138": // Documento(s) localizado(s) para o interessado 
                        foreach (var docZip in consulta.loteDistDFeInt.docZip)
                        {
                            var documento = new Documento()
                            {
                                NSU = docZip.NSU,
                                Schema = docZip.schema,
                                Xml = docZip.Decompress()
                            };
                            lista.Documentos.Add(documento);
                        }
                        break;

                    default:
                        throw new SefazException(consulta.cStat, consulta.xMotivo);
                }

            } while (todosLotes && lista.UltimoNSU < lista.NSUMaximo);

            return lista;
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose() => Certificado?.Dispose();
    }
}
