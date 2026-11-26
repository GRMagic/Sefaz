using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Sefaz.Core.Test.Simulador
{
    /// <summary>
    /// Certificados gerados em memória a cada execução: o de cliente faz o papel do e-CNPJ, e o de servidor é o da SEFAZ simulada
    /// </summary>
    internal static class CertificadosDeTeste
    {
        private const string OidAutenticacaoServidor = "1.3.6.1.5.5.7.3.1";
        private const string OidAutenticacaoCliente = "1.3.6.1.5.5.7.3.2";

        public static X509Certificate2 CriarCliente() =>
            Criar("CN=EMPRESA DE TESTE LTDA:12345678000195, OU=Certificado de teste, C=BR", OidAutenticacaoCliente, servidor: false);

        public static X509Certificate2 CriarServidor() =>
            Criar("CN=localhost, OU=SEFAZ simulada, C=BR", OidAutenticacaoServidor, servidor: true);

        private static X509Certificate2 Criar(string nome, string oidUso, bool servidor)
        {
            using var rsa = RSA.Create(2048);
            var requisicao = new CertificateRequest(nome, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            requisicao.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            requisicao.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            requisicao.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid(oidUso) }, false));
            if (servidor)
            {
                var nomesAlternativos = new SubjectAlternativeNameBuilder();
                nomesAlternativos.AddDnsName("localhost");
                nomesAlternativos.AddIpAddress(IPAddress.Loopback);
                requisicao.CertificateExtensions.Add(nomesAlternativos.Build());
            }

            using var temporario = requisicao.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));

            // No Windows, o TLS (SChannel) não usa chaves efêmeras; exportar e reimportar como PFX resolve
            return X509CertificateLoader.LoadPkcs12(temporario.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);
        }
    }
}
