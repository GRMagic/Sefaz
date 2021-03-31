using System;

namespace Sefaz.Core
{
    /// <summary>
    /// Exceção com o cStat e xMotivo informado pela SEFAZ
    /// </summary>
    public class SefazException : Exception
    {
        internal SefazException(string cStat, string xMotivo) : base($"{cStat} - {xMotivo}")
        {
            this.cStat = cStat;
            this.xMotivo = xMotivo;
        }


        /// <summary>
        /// Código de erro da SEFAZ
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Manter o nome usado pela SEFAZ ajuda o usuário a relacionar o código apresentado com a documentação da SEFAZ")]
        public string cStat { get; private set; }

        /// <summary>
        /// Descrição do erro da SEFAZ
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Manter o nome usado pela SEFAZ ajuda o usuário a relacionar o código apresentado com a documentação da SEFAZ")] 
        public string xMotivo { get; private set; }
    }
}
