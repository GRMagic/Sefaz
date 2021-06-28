using Sefaz.Core.Models;
using Sefaz.WCF.NFeDistribuicaoDFe;
using Sefaz.WCF.NFeRecepcaoEvento4;
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
    /// <summary>
    /// Classe utilizada para comunicar-se com a Sefaz
    /// </summary>
    public class Sefaz : ISefaz
    {
        private const string ENDPOINTNFEDISTRIBUICAODFE = "https://www1.nfe.fazenda.gov.br/NFeDistribuicaoDFe/NFeDistribuicaoDFe.asmx";
        private const string ENDPOINTCTEDISTRIBUICAODFE = "https://www1.cte.fazenda.gov.br/CTeDistribuicaoDFe/CTeDistribuicaoDFe.asmx";
        private const string ENDPOINTNFERECEPCAOEVENTO = "https://www.nfe.fazenda.gov.br/NFeRecepcaoEvento4/NFeRecepcaoEvento4.asmx";
        private const string CUFMANIFESTOAN = "91";

        private X509Certificate2 _Certificado;
        private string _EndPointNFeDistribuicaoDFe;
        private string _EndPointCTeDistribuicaoDFe;
        private string _EndPointNFeRecepcaoEvento;
        private Models.NFe.TAmb _Ambiente;
        private bool _DisposeCertificado;

        /// <summary>
        /// Código IBGE do orgão que irá recepcionar os eventos de manifesto do destinatário
        /// </summary>
        public Models.NFe.TCOrgaoIBGE OrgaoManifesto { get; protected set; }

        /// <summary>
        /// Constrói um objeto pasando o caminho e a senha de um certificado (A1)
        /// </summary>
        /// <param name="certificado">Caminho do certificado</param>
        /// <param name="senha">Senha do certificado</param>
        /// <param name="ambiente">Ambiente de produção ou homologação</param>
        /// <param name="endPointNFeDistribuicaoDFe">Endereço do serviço de distribuição de NFe e eventos</param>
        /// <param name="endPointCTeDistribuicaoDFe">Endereço do serviço de distribuição de CTe e eventos</param>
        /// <param name="endPointNFeRecepcaoEvento">Endereço do serviço de recepção de eventos NFe</param>
        /// <param name="cUFManifesto">Código IBGE do orgão que irá recepcionar eventos de manifesto do destinatário (91 = Ambiente Nacional)</param>
        public Sefaz(string certificado, 
                     string senha = null,
                     Models.NFe.TAmb ambiente = Models.NFe.TAmb.Producao,
                     string endPointNFeDistribuicaoDFe = ENDPOINTNFEDISTRIBUICAODFE,
                     string endPointCTeDistribuicaoDFe = ENDPOINTCTEDISTRIBUICAODFE,
                     string endPointNFeRecepcaoEvento = ENDPOINTNFERECEPCAOEVENTO,
                     string cUFManifesto = CUFMANIFESTOAN)
        {
            _EndPointNFeDistribuicaoDFe = endPointNFeDistribuicaoDFe;
            _EndPointCTeDistribuicaoDFe = endPointCTeDistribuicaoDFe;
            _EndPointNFeRecepcaoEvento = endPointNFeRecepcaoEvento;
            _Ambiente = ambiente;

            Models.NFe.TCOrgaoIBGE cUF;
            if (!Models.NFe.TCOrgaoIBGE.TryParse("Item" + cUFManifesto, out cUF)) 
                throw new ArgumentException("Código IBGE inválido para recepção de manifestos do destinatário!", nameof(cUFManifesto));
            OrgaoManifesto = cUF;

            this._Certificado = new X509Certificate2(certificado, senha, X509KeyStorageFlags.MachineKeySet);
            _DisposeCertificado = true;
        }

        /// <summary>
        /// Constrói um objeto passando um certificado X509Certificate2 (A1 ou A3)
        /// </summary>
        /// <param name="certificado">Certificado</param>
        /// <param name="ambiente">Ambiente de produção ou homologação</param>
        /// <param name="endPointNFeDistribuicaoDFe">Endereço do serviço de distribuição de NFe e eventos</param>
        /// <param name="endPointCTeDistribuicaoDFe">Endereço do serviço de distribuição de CTe e eventos</param>
        /// <param name="endPointNFeRecepcaoEvento">Endereço do serviço de recepção de eventos</param>
        /// <param name="cUFManifesto">Código IBGE do orgão que irá recepcionar eventos de manifesto do destinatário (91 = Ambiente Nacional)</param>
        /// <param name="disposeCertificado">Indica se ao fazer o dispose desse objeto o dispose do certificado deve ser feito automáticamente</param>
        public Sefaz(X509Certificate2 certificado,
                     Models.NFe.TAmb ambiente = Models.NFe.TAmb.Producao,
                     string endPointNFeDistribuicaoDFe = ENDPOINTNFEDISTRIBUICAODFE,
                     string endPointCTeDistribuicaoDFe = ENDPOINTCTEDISTRIBUICAODFE,
                     string endPointNFeRecepcaoEvento = ENDPOINTNFERECEPCAOEVENTO,
                     string cUFManifesto = CUFMANIFESTOAN,
                     bool disposeCertificado = false)
        {
            _EndPointNFeDistribuicaoDFe = endPointNFeDistribuicaoDFe;
            _EndPointCTeDistribuicaoDFe = endPointCTeDistribuicaoDFe;
            _EndPointNFeRecepcaoEvento = endPointNFeRecepcaoEvento;
            _Ambiente = ambiente;

            Models.NFe.TCOrgaoIBGE cUF;
            if (!Models.NFe.TCOrgaoIBGE.TryParse("Item" + cUFManifesto, out cUF))
                throw new ArgumentException("Código IBGE inválido para recepção de manifestos do destinatário!", nameof(cUFManifesto));
            OrgaoManifesto = cUF;

            this._Certificado = certificado;
            _DisposeCertificado = disposeCertificado;
        }

        /// <summary>
        /// Chama o WS da Sefaz para baixar a NFe
        /// </summary>
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ</param>
        /// <param name="chave">Chave da nota</param>
        /// <returns>Documento retornado pela SEFAZ</returns>
        [Obsolete("O nome dessa função mudou para BaixarNFeAsync.", true)]
        public Task<Documento> DownloadNFe(string cUF, string cnpj, string chave) => BaixarNFeAsync(cUF, cnpj, chave);

        /// <summary>
        /// Chama o WS da Sefaz para baixar a NFe
        /// </summary>
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ</param>
        /// <param name="chave">Chave da nota</param>
        /// <returns>Documento retornado pela SEFAZ</returns>
        [Obsolete("O nome dessa função mudou para BaixarNFeAsync.")]
        public Task<Documento> BaixarNFe(string cUF, string cnpj, string chave) => BaixarNFeAsync(cUF, cnpj, chave);

        /// <summary>
        /// Chama o WS de distribuição de NFe da Sefaz para consultar um NSU
        /// </summary>
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ do interessado</param>
        /// <param name="nsu">Número sequencial único</param>
        /// <returns>Documento retornado pela SEFAZ, não necessariamente uma NFe, pode ser algum evento por exemplo</returns>
        [Obsolete("O nome dessa função mudou para ConsultarNFeNSUAsync.")] 
        public Task<Documento> ConsultarNFeNSU(string cUF, string cnpj, long nsu) => ConsultarNFeNSUAsync(cUF, cnpj, nsu);

        /// <summary>
        /// Chama o WS da SEFAZ para consultar as notas e eventos de notas
        /// </summary>
        /// <param name="cUF">Código IBGE da UF da empresa</param>
        /// <param name="cnpj">CNPJ do interessado</param>
        /// <param name="ultimoNSU">NSU mais recente conhecido (Quando informado 0 retorna as notas dos últimos 90 dias)</param>
        /// <param name="todosLotes">Consultar automaticamente todos os lotes até o mais atual?</param>
        /// <returns>XML retornado pela Sefaz já deserializado em um objeto</returns>
        /// <remarks>ATENÇÃO! Consultas grandes e frequentes podem causar bloqueio temporário do serviço. Evite usar ultNSU=0 mais de uma vez por hora.</remarks>
        [Obsolete("O nome dessa função mudou para ConsultarNFeCNPJAsync.")]
        public Task<ListaDocumentos> ConsultarNFeCNPJ(string cUF, string cnpj, long ultimoNSU = 0, bool todosLotes = true) => ConsultarNFeCNPJAsync(cUF, cnpj, ultimoNSU, todosLotes);

        /// <summary>
        /// Gera um evento de manifesto do destinatário
        /// </summary>
        /// <param name="cnpj">CNPJ do destinatário</param>
        /// <param name="chave">Chave da NFe</param>
        /// <param name="evento">Tipo de evento</param>
        /// <param name="sequencia">Número sequencial usado para identificar a ordem que os eventos ocorreram</param>
        /// <param name="justificativa">Justificativa caso necessária</param>
        /// <exception cref="SefazException">Pode lançar uma exceção caso o cStat tenha algum valor inesperado</exception>
        /// <remarks>O schema (xsd) não está sendo validado antes do envio</remarks>
        [Obsolete("O nome dessa função mudou para ManifestarNFeAsync.")] 
        public Task ManifestarNFe(string cnpj, string chave, Models.NFe.TEventoInfEventoDetEventoDescEvento evento, int sequencia = 1, string justificativa = null) => ManifestarNFeAsync(cnpj, chave, evento, sequencia, justificativa);

        /// <summary>
        /// Chama o WS da Sefaz para baixar a NFe
        /// </summary>
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ</param>
        /// <param name="chave">Chave da nota</param>
        /// <returns>Documento retornado pela SEFAZ</returns>
        public async Task<Documento> BaixarNFeAsync(string cUF, string cnpj, string chave)
        {
            if (string.IsNullOrWhiteSpace(cUF)) throw new ArgumentNullException(nameof(cUF));
            if (cUF.Length != 2) throw new ArgumentException("O código da UF deve ter dois algarismos.", nameof(cUF));
            if (string.IsNullOrWhiteSpace(cnpj)) throw new ArgumentNullException(nameof(cnpj));
            if (cnpj.Length != 14) throw new ArgumentException("O CNPJ deve ter 14 algarismos.", nameof(cnpj));
            if (string.IsNullOrWhiteSpace(chave)) throw new ArgumentNullException(nameof(chave));
            if (chave.Length != 44) throw new ArgumentException("A chave deve ter 44 algarismos.", nameof(chave));

            // Dados
            var dados = new Models.NFe.distDFeIntConsChNFe
            {
                chNFe = chave
            };

            // Chamada
            var retorno = await ChamarWsNFe(cUF, cnpj, dados);

            try
            {
                var nota = retorno.loteDistDFeInt.docZip.FirstOrDefault(d => d.schema.StartsWith("procNFe_"));
                return new Documento
                {
                    NSU = long.Parse(nota.NSU),
                    Schema = nota.schema,
                    Xml = nota.Decompress()
                };
            }
            catch
            {
                throw new SefazException(retorno.cStat, retorno.xMotivo);
            }
        }

        /// <summary>
        /// Chama o WS de distribuição de NFe da Sefaz para consultar um NSU
        /// </summary>
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ do interessado</param>
        /// <param name="nsu">Número sequencial único</param>
        /// <returns>Documento retornado pela SEFAZ, não necessariamente uma NFe, pode ser algum evento por exemplo</returns>
        public async Task<Documento> ConsultarNFeNSUAsync(string cUF, string cnpj, long nsu)
        {

            if (string.IsNullOrWhiteSpace(cUF)) throw new ArgumentNullException(nameof(cUF));
            if (cUF.Length != 2) throw new ArgumentException("O código da UF deve ter dois algarismos.", nameof(cUF));
            if (string.IsNullOrWhiteSpace(cnpj)) throw new ArgumentNullException(nameof(cnpj));
            if (cnpj.Length != 14) throw new ArgumentException("O CNPJ deve ter 14 algarismos.", nameof(cnpj));
            if (nsu < 0) throw new ArgumentException("O NSU deve ser um número positivo.", nameof(nsu));

            // Dados
            var dados = new Models.NFe.distDFeIntConsNSU
            {
                NSU = nsu.ToString("000000000000000")
            };

            // Chamada
            var retorno = await ChamarWsNFe(cUF, cnpj, dados);

            if (retorno.cStat != "138") throw new SefazException(retorno.cStat, retorno.xMotivo);

            var doc = retorno.loteDistDFeInt.docZip.First();

            return new Documento
            {
                NSU = long.Parse(doc.NSU),
                Schema = doc.schema,
                Xml = doc.Decompress()
            };
        }

        /// <summary>
        /// Chama o WS da SEFAZ para consultar as notas e eventos de notas
        /// </summary>
        /// <param name="cUF">Código IBGE da UF da empresa</param>
        /// <param name="cnpj">CNPJ do interessado</param>
        /// <param name="ultimoNSU">NSU mais recente conhecido (Quando informado 0 retorna as notas dos últimos 90 dias)</param>
        /// <param name="todosLotes">Consultar automaticamente todos os lotes até o mais atual?</param>
        /// <returns>XML retornado pela Sefaz já deserializado em um objeto</returns>
        /// <remarks>ATENÇÃO! Consultas grandes e frequentes podem causar bloqueio temporário do serviço. Evite usar ultNSU=0 mais de uma vez por hora.</remarks>
        public async Task<ListaDocumentos> ConsultarNFeCNPJAsync(string cUF, string cnpj, long ultimoNSU = 0, bool todosLotes = true)
        {
            if (string.IsNullOrWhiteSpace(cUF)) throw new ArgumentNullException(nameof(cUF));
            if (cUF.Length != 2) throw new ArgumentException("O código da UF deve ter dois algarismos.", nameof(cUF));
            if (string.IsNullOrWhiteSpace(cnpj)) throw new ArgumentNullException(nameof(cnpj));
            if (cnpj.Length != 14) throw new ArgumentException("O CNPJ deve ter 14 algarismos.", nameof(cnpj));
            if (ultimoNSU < 0) throw new ArgumentException("O NSU deve ser um número positivo.", nameof(ultimoNSU));

            var lista = new ListaDocumentos();
            lista.UltimoNSU = ultimoNSU;

            do
            {
                // Chamada ao serviço
                var consulta = await ChamarWsNFe(cUF, cnpj, new Models.NFe.distDFeIntDistNSU
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
                                NSU = long.Parse(docZip.NSU),
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
        /// Gera um evento de manifesto do destinatário
        /// </summary>
        /// <param name="cnpj">CNPJ do destinatário</param>
        /// <param name="chave">Chave da NFe</param>
        /// <param name="evento">Tipo de evento</param>
        /// <param name="sequencia">Número sequencial usado para identificar a ordem que os eventos ocorreram</param>
        /// <param name="justificativa">Justificativa caso necessária</param>
        /// <exception cref="SefazException">Pode lançar uma exceção caso o cStat tenha algum valor inesperado</exception>
        /// <remarks>O schema (xsd) não está sendo validado antes do envio</remarks>
        public async Task ManifestarNFeAsync(string cnpj, string chave, Models.NFe.TEventoInfEventoDetEventoDescEvento evento, int sequencia = 1, string justificativa = null)
        {
            if (string.IsNullOrWhiteSpace(cnpj)) throw new ArgumentNullException(nameof(cnpj));
            if (cnpj.Length != 14) throw new ArgumentException("O CNPJ deve ter 14 algarismos.", nameof(cnpj));
            if (string.IsNullOrWhiteSpace(chave)) throw new ArgumentNullException(nameof(chave));
            if (chave.Length != 44) throw new ArgumentException("A chave deve ter 44 algarismos.", nameof(chave));
            if (DateTime.UtcNow > _Certificado.NotAfter) throw new Exception($"O certificado venceu em {_Certificado.NotAfter}!");
            if (DateTime.UtcNow < _Certificado.NotBefore) throw new Exception("O certificado ainda não é válido!");

            // Configuração do ws
            var endpoint = new System.ServiceModel.EndpointAddress(_EndPointNFeRecepcaoEvento);
            var binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport) { MaxReceivedMessageSize = 999999999 };
            binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Certificate;

            // Instância do cliente
            var ws = new NFeRecepcaoEvento4SoapClient(binding, endpoint);

            // Definição do certificado
            ws.ClientCredentials.ClientCertificate.Certificate = _Certificado;

            // Dados
            Models.NFe.TEventoInfEventoTpEvento tpEvento;
            string id;
            string nSeqEvento;
            switch (evento)
            {
                case Models.NFe.TEventoInfEventoDetEventoDescEvento.CienciaDaOperacao:
                    tpEvento = Models.NFe.TEventoInfEventoTpEvento.CienciaDaEmissao;
                    nSeqEvento = "1"; // FIXO "1"
                    id = "ID" + "210210" + chave + nSeqEvento.PadLeft(2, '0'); // "ID" + tpEvento + chave da NF-e + nSeqEvento 
                    break;
                case Models.NFe.TEventoInfEventoDetEventoDescEvento.ConfirmacaoDaOperacao:
                    tpEvento = Models.NFe.TEventoInfEventoTpEvento.ConfirmacaoDaOperacao;
                    nSeqEvento = sequencia.ToString();
                    id = "ID" + "210200" + chave + nSeqEvento.PadLeft(2, '0');
                    break;
                case Models.NFe.TEventoInfEventoDetEventoDescEvento.DesconhecimentoDaOperacao:
                    tpEvento = Models.NFe.TEventoInfEventoTpEvento.DesconhecimentoDaOperacao;
                    nSeqEvento = sequencia.ToString();
                    id = "ID" + "210220" + chave + nSeqEvento.PadLeft(2, '0');
                    break;
                case Models.NFe.TEventoInfEventoDetEventoDescEvento.OperacaoNaoRealizada:
                    tpEvento = Models.NFe.TEventoInfEventoTpEvento.OperacaoNaoRealizada;
                    nSeqEvento = sequencia.ToString();
                    id = "ID" + "210240" + chave + nSeqEvento.PadLeft(2, '0');
                    break;
                default:
                    throw new NotImplementedException("Tipo de evento não suportado!");
            }
            var dados = new Models.NFe.TEnvEvento
            {
                versao = "1.00",
                idLote = 1.ToString("000000000000000"),
                evento = new[]{
                    new Models.NFe.TEvento {
                        versao = "1.00", // Versão do layout do evento
                        infEvento = new Models.NFe.TEventoInfEvento {
                            Id = id, // Identificador da TAG a ser assinada, a regra de formação do Id é: “ID” + tpEvento + chave da NF-e + nSeqEvento 
                            cOrgao = OrgaoManifesto, // Código do órgão de recepção do Evento. Utilizar a Tabela de UF do IBGE, utilizar 91 para identificar o Ambiente Nacional.
                            tpAmb = _Ambiente,
                            Item = cnpj,
                            ItemElementName = cnpj.Length > 11 ? Models.NFe.TipoPessoa.CNPJ : Models.NFe.TipoPessoa.CPF,
                            chNFe = chave,
                            dhEvento = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                            tpEvento = tpEvento,
                            nSeqEvento = nSeqEvento,
                            verEvento = "1.00", // Identificação da Versão do evento informado em detEvento
                            detEvento = new Models.NFe.TEventoInfEventoDetEvento
                            {
                                versao = Models.NFe.TEventoInfEventoDetEventoVersao.Item100,
                                descEvento = evento,
                                xJust = justificativa
                            }
                        }
                        //Signature = null // Assinatura Digital do documento XML, a assinatura deverá ser aplicada no elemento infEvento
                    }
                }
            };

            // Corpo
            var corpo = new XmlDocument();
            using var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "http://www.portalfiscal.inf.br/nfe");
            new XmlSerializer(typeof(Models.NFe.TEnvEvento)).Serialize(streamWriter, dados, ns);
            corpo.LoadXml(Encoding.UTF8.GetString(memoryStream.ToArray()));

            //Assinar(ref corpo);
            corpo = Util.AssinarXML(corpo, _Certificado, "infEvento");

            // Chama o web service
            var resposta = await ws.nfeRecepcaoEventoNFAsync(corpo);

            // Trabalha com a resposta
            var retorno = resposta.nfeRecepcaoEventoNFResult.DeserializeTo<Models.NFe.TRetEnvEvento>();
            if (retorno.cStat != "128") throw new SefazException(retorno.cStat, retorno.xMotivo);
            var infEvento = retorno.retEvento[0].infEvento;
            if (infEvento.cStat != "135") throw new SefazException(infEvento.cStat, infEvento.xMotivo);

        }

        /// <summary>
        /// Faz a chamada para o webservice de distribuição de NFe
        /// </summary>
        /// <param name="cUF">Código da UF (IBGE)</param>
        /// <param name="cnpj">CNPJ da empresa (14 dígitos - sem máscara)</param>
        /// <param name="dados">Dados que serão enviados para o webservice</param>
        private async Task<Models.NFe.retDistDFeInt> ChamarWsNFe(string cUF, string cnpj, object dados)
        {
            if (DateTime.UtcNow > _Certificado.NotAfter) throw new Exception($"O certificado venceu em {_Certificado.NotAfter}!");
            if (DateTime.UtcNow < _Certificado.NotBefore) throw new Exception("O certificado ainda não é válido!");

            // Configuração do ws
            var endpoint = new System.ServiceModel.EndpointAddress(_EndPointNFeDistribuicaoDFe);
            var binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport) { MaxReceivedMessageSize = 999999999 };
            binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Certificate;

            // Instância do cliente
            var ws = new NFeDistribuicaoDFeSoapClient(binding, endpoint);

            try
            {
                // Definição do certificado
                ws.ClientCredentials.ClientCertificate.Certificate = _Certificado;

                // Dados
                Models.NFe.TCodUfIBGE eUF;
                if (!Models.NFe.TCodUfIBGE.TryParse("Item" + cUF, out eUF)) throw new ArgumentException("Código IBGE da UF Inválido!", nameof(cUF));
                using var memoryStream = new MemoryStream();
                using var streamWriter = new StreamWriter(memoryStream);

                new XmlSerializer(typeof(Models.NFe.distDFeInt)).Serialize(streamWriter, new Models.NFe.distDFeInt
                {
                    tpAmb = _Ambiente,
                    cUFAutor = eUF,
                    cUFAutorSpecified = true,
                    ItemElementName = cnpj.Length > 11 ? Models.NFe.TipoPessoa.CNPJ : Models.NFe.TipoPessoa.CPF,
                    CpjCnpj = cnpj,
                    Item1 = dados,
                    versao = Models.NFe.TVerDistDFe.Item101,
                });
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(Encoding.UTF8.GetString(memoryStream.ToArray()));

                // Chama o web service
                var resposta = await ws.nfeDistDFeInteresseAsync(xmlDocument.DocumentElement);

                // Trabalha com a resposta
                return resposta.Body.nfeDistDFeInteresseResult.DeserializeTo<Models.NFe.retDistDFeInt>();

            }
            finally
            {
                await ws.CloseAsync();
            }
        }


        /// <summary>
        /// Faz a chamada para o webservice de distribuição de CTe
        /// </summary>
        /// <param name="cUF">Código da UF (IBGE)</param>
        /// <param name="cnpj">CNPJ da empresa (14 dígitos - sem máscara)</param>
        /// <param name="dados">Dados que serão enviados para o webservice</param>
        private async Task<Models.CTe.retDistDFeInt> ChamarWsCTe(string cUF, string cnpj, object dados)
        {
            if (DateTime.UtcNow > _Certificado.NotAfter) throw new Exception($"O certificado venceu em {_Certificado.NotAfter}!");
            if (DateTime.UtcNow < _Certificado.NotBefore) throw new Exception("O certificado ainda não é válido!");

            // Configuração do ws
            var endpoint = new System.ServiceModel.EndpointAddress(_EndPointCTeDistribuicaoDFe);
            var binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport) { MaxReceivedMessageSize = 999999999 };
            binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Certificate;

            // Instância do cliente
            var ws = new WCF.CTeDistribuicaoDFe.CTeDistribuicaoDFeSoapClient(binding, endpoint);

            try
            {
                // Definição do certificado
                ws.ClientCredentials.ClientCertificate.Certificate = _Certificado;

                // Dados
                Models.CTe.TCodUfIBGE eUF;
                if (!Models.CTe.TCodUfIBGE.TryParse("Item" + cUF, out eUF)) throw new ArgumentException("Código IBGE da UF Inválido!", nameof(cUF));
                using var memoryStream = new MemoryStream();
                using var streamWriter = new StreamWriter(memoryStream);

                new XmlSerializer(typeof(Models.CTe.distDFeInt)).Serialize(streamWriter, new Models.CTe.distDFeInt
                {
                    tpAmb = (Models.CTe.TAmb)_Ambiente,
                    cUFAutor = eUF,
                    ItemElementName = cnpj.Length > 11 ? Models.CTe.TipoPessoa.CNPJ : Models.CTe.TipoPessoa.CPF,
                    CpjCnpj = cnpj,
                    Item1 = dados,
                    versao = Models.CTe.TVerDistDFe.Item100,
                });
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(Encoding.UTF8.GetString(memoryStream.ToArray()));

                // Chama o web service
                var resposta = await ws.cteDistDFeInteresseAsync(xmlDocument.DocumentElement);

                // Trabalha com a resposta
                return resposta.Body.cteDistDFeInteresseResult.DeserializeTo<Models.CTe.retDistDFeInt>();

            }
            finally
            {
                await ws.CloseAsync();
            }
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            if(_DisposeCertificado)
                _Certificado?.Dispose();
        }
    }
}
