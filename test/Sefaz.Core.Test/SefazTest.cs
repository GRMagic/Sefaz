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
            new Sefaz("test.pfx", "123456", Models.NFe.TAmb.Homologacao).Dispose();
        }

        [Fact(DisplayName = "Formato da UF incorreto")]
        [Trait("Categoria", "BaixarNFe")]
        public async Task FormatoUFIncorreto()
        {
            using var sefaz = new Sefaz("test.pfx", "123456", Models.NFe.TAmb.Homologacao);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sefaz.BaixarNFeAsync("4", "12345678901234", "12345678901234567890123456789012345678901234"));

            Assert.Equal("cUF", ex.ParamName);
        }

        [Fact(DisplayName = "Formato do CNPJ incorreto")]
        [Trait("Categoria", "BaixarNFe")]
        public async Task FormatoCNPJIncorreto()
        {
            using var sefaz = new Sefaz("test.pfx", "123456", Models.NFe.TAmb.Homologacao);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sefaz.BaixarNFeAsync("12", "123", "12345678901234567890123456789012345678901234"));

            Assert.Equal("cnpj", ex.ParamName);
        }

        [Fact(DisplayName = "Formato da chave incorreto")]
        [Trait("Categoria", "BaixarNFe")]
        public async Task FormatoChaveIncorreto()
        {
            using var sefaz = new Sefaz("test.pfx", "123456", Models.NFe.TAmb.Homologacao);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sefaz.BaixarNFeAsync("12", "12345678901234", "123"));

            Assert.Equal("chave", ex.ParamName);
        }

        [Theory(DisplayName = "Tamanho da observação fora do limite de 15 a 255 caracteres")]
        [Trait("Categoria", "ManifestarDesacordoCTe")]
        [InlineData(14)]
        [InlineData(256)]
        public async Task TamanhoObservacaoIncorreto(int tamanho)
        {
            using var sefaz = new Sefaz("test.pfx", "123456", Models.NFe.TAmb.Homologacao);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sefaz.ManifestarDesacordoCTeAsync("12345678901234", "12345678901234567890123456789012345678901234", new string('a', tamanho)));

            Assert.Equal("observacao", ex.ParamName);
        }

        [Theory(DisplayName = "Tamanho da observação dentro do limite de 15 a 255 caracteres")]
        [Trait("Categoria", "ManifestarDesacordoCTe")]
        [InlineData(15)]
        [InlineData(255)]
        public async Task TamanhoObservacaoCorreto(int tamanho)
        {
            using var sefaz = new Sefaz("test.pfx", "123456", Models.NFe.TAmb.Homologacao);

            // A validação dos argumentos passa e a execução falha depois, na checagem do certificado (o test.pfx está vencido)
            // ou na chamada ao webservice. O endpoint local inacessível garante que o teste nunca chame a SEFAZ de verdade.
            var ex = await Record.ExceptionAsync(() => sefaz.ManifestarDesacordoCTeAsync("12345678901234", "12345678901234567890123456789012345678901234", new string('a', tamanho), webservice: "https://127.0.0.1:1/"));

            Assert.IsNotType<ArgumentException>(ex);
            Assert.IsNotType<ArgumentNullException>(ex);
        }
    }
}
