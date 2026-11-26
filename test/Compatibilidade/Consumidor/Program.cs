using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Xml;
using Sefaz.Core.Models.NFe;

// Consumidor mínimo do pacote rmdev.sefaz. Exercita em tempo de execução as dependências do pacote
// (System.Security.Cryptography.Xml e WCF) sem chamar a SEFAZ: os endpoints apontam para uma porta local fechada,
// então cada chamada deve chegar até a conexão e falhar nela, e não antes (por assembly ou tipo não encontrado).

const string EnderecoInacessivel = "https://127.0.0.1:1/";
const string Cnpj = "12345678901234";
const string Chave = "43123456789012345678901234567890123456789012";

var falhas = 0;
Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.OSDescription})");

// Certificado autoassinado e válido, para passar pelas checagens de validade da biblioteca
using var rsa = RSA.Create(2048);
var requisicao = new CertificateRequest("CN=Teste de compatibilidade", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
using var certificado = requisicao.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

Verificar("Assinatura de XML (System.Security.Cryptography.Xml)", () =>
{
    var xml = new XmlDocument();
    xml.LoadXml("<evento><infEvento Id=\"ID1\"><valor>teste</valor></infEvento></evento>");
    var assinado = Sefaz.Core.Util.AssinarXML(xml, certificado, "infEvento");

    var signedXml = new SignedXml(assinado);
    signedXml.LoadXml((XmlElement)assinado.GetElementsByTagName("Signature")[0]);
    if (!signedXml.CheckSignature(certificado, true)) throw new Exception("A assinatura gerada não é válida.");
});

using (var sefaz = new Sefaz.Core.Sefaz(certificado, TAmb.Homologacao,
                                        endPointNFeDistribuicaoDFe: EnderecoInacessivel,
                                        endPointCTeDistribuicaoDFe: EnderecoInacessivel,
                                        endPointNFeRecepcaoEvento: EnderecoInacessivel))
{
    await VerificarFalhaDeConexao("Distribuição de NF-e (WCF)", () => sefaz.ConsultarNFeCNPJAsync("43", Cnpj));
    await VerificarFalhaDeConexao("Distribuição de CT-e (WCF)", () => sefaz.ConsultarCTeCNPJAsync("43", Cnpj));
    await VerificarFalhaDeConexao("Manifesto de NF-e (assinatura + WCF)",
        () => sefaz.ManifestarNFeAsync(Cnpj, Chave, TEventoInfEventoDetEventoDescEvento.CienciaDaOperacao));
    await VerificarFalhaDeConexao("Desacordo de CT-e (assinatura + WCF SOAP 1.2)",
        () => sefaz.ManifestarDesacordoCTeAsync(Cnpj, Chave, "Observação do teste de compatibilidade", webservice: EnderecoInacessivel));
}

Console.WriteLine(falhas == 0 ? "Todas as verificações passaram." : $"{falhas} verificação(ões) falharam.");
return falhas == 0 ? 0 : 1;

void Verificar(string nome, Action acao)
{
    try
    {
        acao();
        Console.WriteLine($"[OK]    {nome}");
    }
    catch (Exception ex)
    {
        falhas++;
        Console.WriteLine($"[FALHA] {nome}: {ex}");
    }
}

async Task VerificarFalhaDeConexao(string nome, Func<Task> acao)
{
    try
    {
        await acao();
        falhas++;
        Console.WriteLine($"[FALHA] {nome}: a chamada deveria ter falhado ao conectar em {EnderecoInacessivel}");
    }
    catch (Exception ex) when (EhFalhaDeConexao(ex))
    {
        Console.WriteLine($"[OK]    {nome} (falhou ao conectar, como esperado: {ex.GetType().Name})");
    }
    catch (Exception ex)
    {
        falhas++;
        Console.WriteLine($"[FALHA] {nome}: {ex}");
    }
}

static bool EhFalhaDeConexao(Exception ex)
{
    for (var atual = ex; atual != null; atual = atual.InnerException)
        if (atual is SocketException || atual is HttpRequestException) return true;
    return false;
}
