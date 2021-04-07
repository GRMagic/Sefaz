using System;
using System.Threading.Tasks;
using Xunit;

namespace Sefaz.Core.Test
{
    public class SefazTest
    {
        [Fact(DisplayName = "Criar classe Sefaz com sucesso")]
        [Trait("Categoria", "Sefaz")]
        public void ConstruirClasse()
        {
            // Act
            new Sefaz("cert.pfx", "123456", Meta.TAmb.Homologacao).Dispose();
        }

        [Fact(DisplayName = "Formato da UF incorreto")]
        [Trait("Categoria", "BaixarNFe")]
        public async Task FormatoUFIncorreto()
        {
            using var sefaz = new Sefaz("cert.pfx", "123456", Meta.TAmb.Homologacao);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sefaz.BaixarNFe("4", "12345678901234", "12345678901234567890123456789012345678901234"));

            Assert.Equal("cUF", ex.ParamName);
        }

        [Fact(DisplayName = "Formato do CNPJ incorreto")]
        [Trait("Categoria", "BaixarNFe")]
        public async Task FormatoCNPJIncorreto()
        {
            using var sefaz = new Sefaz("cert.pfx", "123456", Meta.TAmb.Homologacao);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sefaz.BaixarNFe("12", "123", "12345678901234567890123456789012345678901234"));

            Assert.Equal("cnpj", ex.ParamName);
        }

        [Fact(DisplayName = "Formato da chave incorreto")]
        [Trait("Categoria", "BaixarNFe")]
        public async Task FormatoChaveIncorreto()
        {
            using var sefaz = new Sefaz("cert.pfx", "123456", Meta.TAmb.Homologacao);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sefaz.BaixarNFe("12", "12345678901234", "123"));

            Assert.Equal("chave", ex.ParamName);
        }
    }
}
