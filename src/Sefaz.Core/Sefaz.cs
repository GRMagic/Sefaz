using Meta.EventoManifestaDest;
using Sefaz.Core.Meta;
using Sefaz.WCF.NFeDistribuicaoDFe;
using Sefaz.WCF.NFeRecepcaoEvento4;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
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
        private X509Certificate2 _Certificado;
        private string _EndPointNFeDistribuicaoDFe;
        private string _EndPointNFeRecepcaoEvento;
        private TAmb _Ambiente;

        /// <summary>
        /// Código IBGE do orgão que irá recepcionar os eventos de manifesto do destinatário
        /// </summary>
        public TCOrgaoIBGE OrgaoManifesto { get; protected set; }

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="certificado">Caminho do certificado</param>
        /// <param name="senha">Senha do certificado</param>
        /// <param name="ambiente">Ambiente de produção ou homologação</param>
        /// <param name="endPointNFeDistribuicaoDFe">Endereço do serviço de distribuição de NFe e eventos</param>
        /// <param name="endPointNFeRecepcaoEvento">Endereço do serviço de recepção de eventos</param>
        /// <param name="cUFManifesto">Código IBGE do orgão que irá recepcionar eventos de manifesto do destinatário (91 = Ambiente Nacional)</param>
        public Sefaz(string certificado, 
                     string senha = null, 
                     TAmb ambiente = TAmb.Producao,
                     string endPointNFeDistribuicaoDFe = "https://www1.nfe.fazenda.gov.br/NFeDistribuicaoDFe/NFeDistribuicaoDFe.asmx",
                     string endPointNFeRecepcaoEvento = "https://www.nfe.fazenda.gov.br/NFeRecepcaoEvento4/NFeRecepcaoEvento4.asmx",
                     string cUFManifesto = "91")
        {
            _EndPointNFeDistribuicaoDFe = endPointNFeDistribuicaoDFe;
            _EndPointNFeRecepcaoEvento = endPointNFeRecepcaoEvento;
            _Ambiente = ambiente;

            TCOrgaoIBGE cUF;
            if (!TCOrgaoIBGE.TryParse("Item" + cUFManifesto, out cUF)) throw new ArgumentException("Código IBGE inválido para recepção de manifestos do destinatário!", nameof(cUFManifesto));
            OrgaoManifesto = cUF;

            this._Certificado = new X509Certificate2(certificado, senha, X509KeyStorageFlags.MachineKeySet);
        }

        /// <summary>
        /// Faz a chamada para o webservice de distribuição de NFe
        /// </summary>
        /// <param name="cUF">Código da UF (IBGE)</param>
        /// <param name="cnpj">CNPJ da empresa (14 dígitos - sem máscara)</param>
        /// <param name="dados">Dados que serão enviados para o webservice</param>
        private async Task<retDistDFeInt> ChamarWsNFe(string cUF, string cnpj, object dados)
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
                TCodUfIBGE eUF;
                 if (!TCodUfIBGE.TryParse("Item" + cUF, out eUF)) throw new ArgumentException("Código IBGE da UF Inválido!", nameof(cUF));
                using var memoryStream = new MemoryStream();
                using var streamWriter = new StreamWriter(memoryStream);

                new XmlSerializer(typeof(distDFeInt)).Serialize(streamWriter, new distDFeInt
                {
                    tpAmb = _Ambiente,
                    cUFAutor = eUF,
                    cUFAutorSpecified = true,
                    ItemElementName = cnpj.Length > 11 ? TipoPessoa.CNPJ : TipoPessoa.CPF,
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
        [Obsolete("O nome dessa função mudou para BaixarNFe.")]
        public async Task<Documento> DownloadNFe(string cUF, string cnpj, string chave) => await BaixarNFe(cUF, cnpj, chave);


        /// <summary>
        /// Chama o WS da Sefaz para baixar a NFe
        /// </summary>
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ</param>
        /// <param name="chave">Chave da nota</param>
        /// <returns>Documento retornado pela SEFAZ</returns>
        public async Task<Documento> BaixarNFe(string cUF, string cnpj, string chave)
        {
            if (string.IsNullOrWhiteSpace(cUF)) throw new ArgumentNullException(nameof(cUF));
            if (cUF.Length != 2) throw new ArgumentException("O código da UF deve ter dois algarismos.", nameof(cUF));
            if (string.IsNullOrWhiteSpace(cnpj)) throw new ArgumentNullException(nameof(cnpj));
            if (cnpj.Length != 14) throw new ArgumentException("O CNPJ deve ter 14 algarismos.", nameof(cnpj));
            if (string.IsNullOrWhiteSpace(chave)) throw new ArgumentNullException(nameof(chave));
            if (chave.Length != 44) throw new ArgumentException("A chave deve ter 44 algarismos.", nameof(chave));

            // Dados
            var dados = new distDFeIntConsChNFe
            {
                chNFe = chave
            };

            // Chamada
            var retorno = await ChamarWsNFe(cUF, cnpj, dados);

            try
            {
                var nota = retorno.loteDistDFeInt.docZip.Where(d => d.schema.StartsWith("procNFe_")).FirstOrDefault();
                return new Documento
                {
                    NSU = long.Parse(nota.NSU),
                    Schema = nota.schema,
                    Xml = nota.Decompress()
                };
            }
            catch {
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
        public async Task<Documento> ConsultarNFeNSU(string cUF, string cnpj, long nsu)
        {

            if (string.IsNullOrWhiteSpace(cUF)) throw new ArgumentNullException(nameof(cUF));
            if (cUF.Length != 2) throw new ArgumentException("O código da UF deve ter dois algarismos.", nameof(cUF));
            if (string.IsNullOrWhiteSpace(cnpj)) throw new ArgumentNullException(nameof(cnpj));
            if (cnpj.Length != 14) throw new ArgumentException("O CNPJ deve ter 14 algarismos.", nameof(cnpj));
            if (nsu < 0) throw new ArgumentException("O NSU deve ser um número positivo.", nameof(nsu));

            // Dados
            var dados = new distDFeIntConsNSU
            {
                NSU = nsu.ToString("000000000000000")
            };

            // Chamada
            var retorno = await ChamarWsNFe(cUF, cnpj, dados);

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
        public async Task<ListaDocumentos> ConsultarNFeCNPJ(string cUF, string cnpj, long ultimoNSU = 0, bool todosLotes = true)
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
                var consulta = await ChamarWsNFe(cUF, cnpj, new distDFeIntDistNSU
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
        public async Task ManifestarNFe(string cnpj, string chave, TEventoInfEventoDetEventoDescEvento evento, int sequencia = 1, string justificativa = null)
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
            TEventoInfEventoTpEvento tpEvento;
            string id;
            string nSeqEvento;
            switch (evento)
            {
                case TEventoInfEventoDetEventoDescEvento.CienciaDaOperacao:
                    tpEvento = TEventoInfEventoTpEvento.CienciaDaEmissao;
                    nSeqEvento = "1"; // FIXO "1"
                    id = "ID" + "210210" + chave + nSeqEvento.PadLeft(2, '0'); // "ID" + tpEvento + chave da NF-e + nSeqEvento 
                    break;
                case TEventoInfEventoDetEventoDescEvento.ConfirmacaoDaOperacao:
                    tpEvento = TEventoInfEventoTpEvento.ConfirmacaoDaOperacao;
                    nSeqEvento = sequencia.ToString();
                    id = "ID" + "210200" + chave + nSeqEvento.PadLeft(2, '0');
                    break;
                case TEventoInfEventoDetEventoDescEvento.DesconhecimentoDaOperacao:
                    tpEvento = TEventoInfEventoTpEvento.DesconhecimentoDaOperacao;
                    nSeqEvento = sequencia.ToString();
                    id = "ID" + "210220" + chave + nSeqEvento.PadLeft(2, '0');
                    break;
                case TEventoInfEventoDetEventoDescEvento.OperacaoNaoRealizada:
                    tpEvento = TEventoInfEventoTpEvento.OperacaoNaoRealizada;
                    nSeqEvento = sequencia.ToString();
                    id = "ID" + "210240" + chave + nSeqEvento.PadLeft(2, '0');
                    break;
                default:
                    throw new NotImplementedException("Tipo de evento não suportado!");
            }
            var dados = new TEnvEvento
            {
                versao = "1.00",
                idLote = 1.ToString("000000000000000"),
                evento = new[]{
                    new TEvento {
                        versao = "1.00", // Versão do layout do evento
                        infEvento = new TEventoInfEvento {
                            Id = id, // Identificador da TAG a ser assinada, a regra de formação do Id é: “ID” + tpEvento + chave da NF-e + nSeqEvento 
                            cOrgao = OrgaoManifesto, // Código do órgão de recepção do Evento. Utilizar a Tabela de UF do IBGE, utilizar 91 para identificar o Ambiente Nacional.
                            tpAmb = _Ambiente,
                            Item = cnpj,
                            ItemElementName = cnpj.Length > 11 ? TipoPessoa.CNPJ : TipoPessoa.CPF,
                            chNFe = chave,
                            dhEvento = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                            tpEvento = tpEvento,
                            nSeqEvento = nSeqEvento,
                            verEvento = "1.00", // Identificação da Versão do evento informado em detEvento
                            detEvento = new TEventoInfEventoDetEvento
                            {
                                versao = TEventoInfEventoDetEventoVersao.Item100,
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
            new XmlSerializer(typeof(TEnvEvento)).Serialize(streamWriter, dados, ns);
            corpo.LoadXml(Encoding.UTF8.GetString(memoryStream.ToArray()));
            
            //Assinar(ref corpo);
            corpo = AssinarXML(corpo, _Certificado, "infEvento");

            // Chama o web service
            var resposta = await ws.nfeRecepcaoEventoNFAsync(corpo);

            // Trabalha com a resposta
            var retorno = resposta.nfeRecepcaoEventoNFResult.DeserializeTo<TRetEnvEvento>();
            if (retorno.cStat != "128") throw new SefazException(retorno.cStat, retorno.xMotivo);
            var infEvento = retorno.retEvento[0].infEvento;
            if (infEvento.cStat != "135") throw new SefazException(infEvento.cStat, infEvento.xMotivo);

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
        public XmlDocument AssinarXML(XmlDocument documentoXML, X509Certificate2 certificadoX509, string tagAAssinar, string idAtributoTag = "Id")
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

                // Adicionando a referencia de qual tag será assinada
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

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose() => _Certificado?.Dispose();
    }
}
