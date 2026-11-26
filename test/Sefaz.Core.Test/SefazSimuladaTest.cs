using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Sefaz.Core.Models.NFe;
using Sefaz.Core.Test.Simulador;
using Xunit;

namespace Sefaz.Core.Test
{
    /// <summary>
    /// Chamadas de ponta a ponta contra a SEFAZ simulada: conferem o que chega do outro lado da conexão
    /// (TLS, certificado, SOAP, schema, assinatura) e como a biblioteca interpreta as respostas.
    /// Os snapshots em Snapshots/ registram exatamente o que o servidor recebe, para que uma troca de
    /// implementação (por exemplo, do WCF para HttpClient) prove que nada mudou para a SEFAZ.
    /// </summary>
    [Collection(ColecaoSefazSimulada.Nome)]
    public class SefazSimuladaTest
    {
        private const string Categoria = "SEFAZ simulada";
        private const string Cnpj = "12345678000195";
        private const string ChaveNFe = "43261112345678000195550010000000011000000014";
        private const string ChaveCTe = "43261112345678000195570010000000011000000017";

        private const string ActionNFeDistribuicao = "http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe/nfeDistDFeInteresse";
        private const string ActionCTeDistribuicao = "http://www.portalfiscal.inf.br/cte/wsdl/CTeDistribuicaoDFe/cteDistDFeInteresse";
        private const string ActionNFeEvento = "http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4/nfeRecepcaoEventoNF";
        private const string ActionCTeEvento = "http://www.portalfiscal.inf.br/cte/wsdl/CteRecepcaoEvento/cteRecepcaoEvento";

        // Textos com acento, para conferir que chegam em UTF-8 do outro lado
        private const string ObservacaoDesacordo = "Mercadoria entregue com avaria, não conforme o pedido";

        private const string XsdNFeDistribuicao = "PL_NFeDistDFe_102/distDFeInt_v1.01.xsd";
        private const string XsdCTeDistribuicao = "PL_CTeDistDFe_100/distDFeInt_v1.00.xsd";
        private const string XsdNFeEvento = "Evento_ManifestaDest_PL_v1.01/envConfRecebto_v1.00.xsd";

        private readonly SefazSimuladaFixture _fixture;

        public SefazSimuladaTest(SefazSimuladaFixture fixture)
        {
            _fixture = fixture;
            _fixture.Simulador.Reiniciar();
        }

        private SefazSimulada Simulador => _fixture.Simulador;

        [Fact(DisplayName = "Consulta de NF-e por CNPJ percorre os lotes até o maxNSU")]
        [Trait("Categoria", Categoria)]
        public async Task ConsultarNFeCNPJ()
        {
            Simulador.Responder(SefazSimulada.NFeDistribuicaoDFe, requisicao => Verificacoes.Valor(requisicao.Mensagem, "ultNSU") switch
            {
                "000000000000000" => RespostasSefaz.DistribuicaoNFe("138", "Documento localizado", 2, 3, ResumoNFe(1), ResumoNFe(2)),
                "000000000000002" => RespostasSefaz.DistribuicaoNFe("138", "Documento localizado", 3, 3, ResumoNFe(3)),
                var ultNSU => throw new InvalidOperationException($"ultNSU inesperado: {ultNSU}")
            });

            using var sefaz = _fixture.CriarSefaz();
            var lista = await sefaz.ConsultarNFeCNPJAsync("43", Cnpj);

            Assert.Equal(new long?[] { 1, 2, 3 }, lista.Select(d => d.NSU));
            Assert.All(lista, d => Assert.Equal("resNFe_v1.01.xsd", d.Schema));
            Assert.Equal(ResumoNFe(2).Xml, lista.Documentos[1].Xml.OuterXml);
            Assert.Equal(3, lista.UltimoNSU);
            Assert.Equal(3, lista.NSUMaximo);
            Assert.Equal(DateTimeOffset.Parse(RespostasSefaz.DataHoraNFe).LocalDateTime, lista.DataHoraResposta);

            Assert.Collection(Simulador.Requisicoes,
                primeira => ConferirDistribuicaoNFe(primeira, "ultNSU", "000000000000000"),
                segunda => ConferirDistribuicaoNFe(segunda, "ultNSU", "000000000000002"));
            Verificacoes.ConferirSnapshot(Simulador.Requisicoes[0], "NFeDistribuicaoDFe-distNSU");
        }

        [Fact(DisplayName = "Download de NF-e pela chave devolve o procNFe")]
        [Trait("Categoria", Categoria)]
        public async Task BaixarNFe()
        {
            var nota = new DocumentoZip(10, "procNFe_v4.00.xsd",
                $"<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\"><NFe><infNFe Id=\"NFe{ChaveNFe}\" versao=\"4.00\" /></NFe></nfeProc>");
            Simulador.Responder(SefazSimulada.NFeDistribuicaoDFe, _ => RespostasSefaz.DistribuicaoNFe("138", "Documento localizado", 10, 10, nota));

            using var sefaz = _fixture.CriarSefaz();
            var documento = await sefaz.BaixarNFeAsync("43", Cnpj, ChaveNFe);

            Assert.Equal(10, documento.NSU);
            Assert.Equal("procNFe_v4.00.xsd", documento.Schema);
            Assert.Equal(nota.Xml, documento.Xml.OuterXml);

            var requisicao = Assert.Single(Simulador.Requisicoes);
            ConferirDistribuicaoNFe(requisicao, "chNFe", ChaveNFe);
            Verificacoes.ConferirSnapshot(requisicao, "NFeDistribuicaoDFe-consChNFe");
        }

        [Fact(DisplayName = "Consulta de NF-e por NSU envia o NSU com 15 dígitos")]
        [Trait("Categoria", Categoria)]
        public async Task ConsultarNFeNSU()
        {
            Simulador.Responder(SefazSimulada.NFeDistribuicaoDFe, _ => RespostasSefaz.DistribuicaoNFe("138", "Documento localizado", 1234, 2000, ResumoNFe(1234)));

            using var sefaz = _fixture.CriarSefaz();
            var documento = await sefaz.ConsultarNFeNSUAsync("43", Cnpj, 1234);

            Assert.Equal(1234, documento.NSU);
            Assert.Equal(ResumoNFe(1234).Xml, documento.Xml.OuterXml);

            var requisicao = Assert.Single(Simulador.Requisicoes);
            ConferirDistribuicaoNFe(requisicao, "NSU", "000000000001234");
            Verificacoes.ConferirSnapshot(requisicao, "NFeDistribuicaoDFe-consNSU");
        }

        [Fact(DisplayName = "Rejeição na distribuição de NF-e vira SefazException com o cStat")]
        [Trait("Categoria", Categoria)]
        public async Task ConsultarNFeCNPJRejeitado()
        {
            Simulador.Responder(SefazSimulada.NFeDistribuicaoDFe, _ => RespostasSefaz.DistribuicaoNFe("656", "Rejeicao: Consumo Indevido", 0, 0));

            using var sefaz = _fixture.CriarSefaz();
            var erro = await Assert.ThrowsAsync<SefazException>(() => sefaz.ConsultarNFeCNPJAsync("43", Cnpj));

            Assert.Equal("656", erro.cStat);
            Assert.Equal("Rejeicao: Consumo Indevido", erro.xMotivo);
        }

        [Fact(DisplayName = "Consulta de CT-e por CNPJ segue o XSD e devolve os documentos")]
        [Trait("Categoria", Categoria)]
        public async Task ConsultarCTeCNPJ()
        {
            var conhecimento = new DocumentoZip(7, "procCTe_v4.00.xsd",
                $"<cteProc xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"4.00\"><CTe><infCte Id=\"CTe{ChaveCTe}\" versao=\"4.00\" /></CTe></cteProc>");
            Simulador.Responder(SefazSimulada.CTeDistribuicaoDFe, _ => RespostasSefaz.DistribuicaoCTe("138", "Documento localizado", 7, 7, conhecimento));

            using var sefaz = _fixture.CriarSefaz();
            var lista = await sefaz.ConsultarCTeCNPJAsync("43", Cnpj);

            var documento = Assert.Single(lista);
            Assert.Equal(7, documento.NSU);
            Assert.Equal(conhecimento.Xml, documento.Xml.OuterXml);
            Assert.Equal(DateTime.Parse(RespostasSefaz.DataHoraCTe), lista.DataHoraResposta);

            var requisicao = Assert.Single(Simulador.Requisicoes);
            Verificacoes.ConferirTransporte(requisicao, _fixture.CertificadoCliente, "1.1", ActionCTeDistribuicao);
            Verificacoes.ConferirXsd(requisicao.Mensagem, XsdCTeDistribuicao);
            Assert.Equal("000000000000000", Verificacoes.Valor(requisicao.Mensagem, "ultNSU"));
            Verificacoes.ConferirSnapshot(requisicao, "CTeDistribuicaoDFe-distNSU");
        }

        [Theory(DisplayName = "Manifesto do destinatário chega assinado e segue o XSD")]
        [Trait("Categoria", Categoria)]
        [InlineData(TEventoInfEventoDetEventoDescEvento.CienciaDaOperacao, "210210", null)]
        [InlineData(TEventoInfEventoDetEventoDescEvento.ConfirmacaoDaOperacao, "210200", null)]
        [InlineData(TEventoInfEventoDetEventoDescEvento.DesconhecimentoDaOperacao, "210220", null)]
        [InlineData(TEventoInfEventoDetEventoDescEvento.OperacaoNaoRealizada, "210240", "Operação não realizada: pedido cancelado pelo fornecedor")]
        public async Task ManifestarNFe(TEventoInfEventoDetEventoDescEvento evento, string tpEvento, string justificativa)
        {
            Simulador.Responder(SefazSimulada.NFeRecepcaoEvento, _ => RespostasSefaz.EventoNFe(ChaveNFe, tpEvento));

            using var sefaz = _fixture.CriarSefaz();
            await sefaz.ManifestarNFeAsync(Cnpj, ChaveNFe, evento, justificativa: justificativa);

            var requisicao = Assert.Single(Simulador.Requisicoes);
            Verificacoes.ConferirTransporte(requisicao, _fixture.CertificadoCliente, "1.1", ActionNFeEvento);
            Verificacoes.ConferirXsd(requisicao.Mensagem, XsdNFeEvento);
            Verificacoes.ConferirAssinatura(requisicao.Mensagem, _fixture.CertificadoCliente);
            Assert.Equal(tpEvento, Verificacoes.Valor(requisicao.Mensagem, "tpEvento"));
            Assert.Equal(justificativa, Verificacoes.Valor(requisicao.Mensagem, "xJust"));
            Verificacoes.ConferirSnapshot(requisicao, $"NFeRecepcaoEvento-{tpEvento}");
        }

        [Fact(DisplayName = "Evento de NF-e rejeitado vira SefazException com o cStat do evento")]
        [Trait("Categoria", Categoria)]
        public async Task ManifestarNFeRejeitado()
        {
            Simulador.Responder(SefazSimulada.NFeRecepcaoEvento,
                _ => RespostasSefaz.EventoNFe(ChaveNFe, "210210", cStatEvento: "573", xMotivoEvento: "Rejeicao: Duplicidade de Evento"));

            using var sefaz = _fixture.CriarSefaz();
            var erro = await Assert.ThrowsAsync<SefazException>(
                () => sefaz.ManifestarNFeAsync(Cnpj, ChaveNFe, TEventoInfEventoDetEventoDescEvento.CienciaDaOperacao));

            Assert.Equal("573", erro.cStat);
        }

        [Fact(DisplayName = "Desacordo de CT-e chega assinado, em SOAP 1.2 e com o cabeçalho cteCabecMsg")]
        [Trait("Categoria", Categoria)]
        public async Task ManifestarDesacordoCTe()
        {
            Simulador.Responder(SefazSimulada.CTeRecepcaoEvento, _ => RespostasSefaz.EventoCTe(ChaveCTe));

            using var sefaz = _fixture.CriarSefaz();
            await sefaz.ManifestarDesacordoCTeAsync(Cnpj, ChaveCTe, ObservacaoDesacordo,
                                                     webservice: Simulador.Url(SefazSimulada.CTeRecepcaoEvento));

            var requisicao = Assert.Single(Simulador.Requisicoes);
            // Com o WCF 4.5.3 (o mínimo do pacote), a action não vai no Content-Type; com o WCF 8.x/10.x, o mesmo
            // binding envia a do WSDL. O teste aceita as duas formas, mas uma implementação nova deve enviar a do WSDL.
            Verificacoes.ConferirTransporte(requisicao, _fixture.CertificadoCliente, "1.2", ActionCTeEvento, actionOpcional: true);
            Verificacoes.ConferirAssinatura(requisicao.Mensagem, _fixture.CertificadoCliente);
            Assert.Equal(ObservacaoDesacordo, Verificacoes.Valor(requisicao.Mensagem, "xObs"));

            var cabecalho = requisicao.Cabecalho;
            Assert.NotNull(cabecalho);
            Assert.Equal("43", cabecalho.GetElementsByTagName("cUF")[0]?.InnerText);
            Assert.Equal("3.00", cabecalho.GetElementsByTagName("versaoDados")[0]?.InnerText);
            Verificacoes.ConferirSnapshot(requisicao, "CTeRecepcaoEvento-610110");
        }

        [Fact(DisplayName = "Desacordo de CT-e segue o XSD do CT-e 4.00",
              Skip = "A biblioteca ainda envia o evento de CT-e na versão 3.00, pelo web service CTeRecepcaoEvento. " +
                     "A migração para a 4.00 (CTeRecepcaoEventoV4, sem o cabeçalho cteCabecMsg) foi adiada.")]
        [Trait("Categoria", Categoria)]
        public async Task ManifestarDesacordoCTeSegueXsd400()
        {
            Simulador.Responder(SefazSimulada.CTeRecepcaoEvento, _ => RespostasSefaz.EventoCTe(ChaveCTe));

            using var sefaz = _fixture.CriarSefaz();
            await sefaz.ManifestarDesacordoCTeAsync(Cnpj, ChaveCTe, ObservacaoDesacordo,
                                                     webservice: Simulador.Url(SefazSimulada.CTeRecepcaoEvento));

            Verificacoes.ConferirXsd(Assert.Single(Simulador.Requisicoes).Mensagem, "PL_CTe_400/eventoCTe_v4.00.xsd");
        }

        [Fact(DisplayName = "Certificado de servidor diferente do confiável é recusado antes de enviar a mensagem")]
        [Trait("Categoria", Categoria)]
        public async Task CertificadoServidorDiferente()
        {
            using var outroCertificado = CertificadosDeTeste.CriarServidor();
            using var sefaz = _fixture.CriarSefaz(certificadoServidorConfiavel: outroCertificado);

            await Assert.ThrowsAnyAsync<CommunicationException>(() => sefaz.ConsultarNFeCNPJAsync("43", Cnpj));
            Assert.Empty(Simulador.Requisicoes);
        }

        [Fact(DisplayName = "Sem certificado confiável configurado, vale a validação do sistema operacional")]
        [Trait("Categoria", Categoria)]
        public async Task ValidacaoDoSistemaOperacional()
        {
            using var sefaz = _fixture.CriarSefaz(confiarNoSimulador: false);

            await Assert.ThrowsAnyAsync<CommunicationException>(() => sefaz.ConsultarNFeCNPJAsync("43", Cnpj));
            Assert.Empty(Simulador.Requisicoes);
        }

        [Fact(DisplayName = "As respostas da SEFAZ simulada seguem os XSDs de retorno")]
        [Trait("Categoria", Categoria)]
        public void RespostasSeguemXsd()
        {
            Verificacoes.ConferirXsd(Xml(RespostasSefaz.DistribuicaoNFe("138", "Documento localizado", 2, 3, ResumoNFe(1), ResumoNFe(2))),
                                     "PL_NFeDistDFe_102/retDistDFeInt_v1.01.xsd");
            Verificacoes.ConferirXsd(Xml(RespostasSefaz.DistribuicaoNFe("656", "Rejeicao: Consumo Indevido", 0, 0)),
                                     "PL_NFeDistDFe_102/retDistDFeInt_v1.01.xsd");
            Verificacoes.ConferirXsd(Xml(RespostasSefaz.DistribuicaoCTe("138", "Documento localizado", 7, 7, ResumoNFe(7))),
                                     "PL_CTeDistDFe_100/retDistDFeInt_v1.00.xsd");
            Verificacoes.ConferirXsd(Xml(RespostasSefaz.EventoNFe(ChaveNFe, "210210")),
                                     "Evento_ManifestaDest_PL_v1.01/retEnvConfRecebto_v1.00.xsd");
        }

        private void ConferirDistribuicaoNFe(RequisicaoRecebida requisicao, string campo, string valorEsperado)
        {
            Verificacoes.ConferirTransporte(requisicao, _fixture.CertificadoCliente, "1.1", ActionNFeDistribuicao);
            Verificacoes.ConferirXsd(requisicao.Mensagem, XsdNFeDistribuicao);
            Assert.Equal(valorEsperado, Verificacoes.Valor(requisicao.Mensagem, campo));
        }

        private static DocumentoZip ResumoNFe(long nsu) => new(nsu, "resNFe_v1.01.xsd",
            $"<resNFe xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"1.01\"><chNFe>{ChaveNFe}</chNFe><xNome>FORNECEDOR {nsu}</xNome></resNFe>");

        private static System.Xml.XmlDocument Xml(string conteudo)
        {
            var documento = new System.Xml.XmlDocument();
            documento.LoadXml(conteudo);
            return documento;
        }
    }
}
