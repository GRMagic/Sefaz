using System;
using System.Collections;
using System.Collections.Generic;

namespace Sefaz.Core
{
    /// <summary>
    /// Lista de documentos
    /// </summary>
    public class ListaDocumentos : IEnumerable<Documento>
    {
        /// <summary>
        /// Data e hora que a sefaz deu o retorno
        /// </summary>
        public DateTime DataHoraResposta;

        /// <summary>
        /// Último NSU retornado pela consulta
        /// </summary>
        public long UltimoNSU;

        /// <summary>
        /// Maior NSU existente no ambiente nacional da SEFAZ
        /// </summary>
        public long NSUMaximo;

        /// <summary>
        /// Lista de documentos
        /// </summary>
        public List<Documento> Documentos { get; set; } = new List<Documento>();

        /// <summary>
        /// Permite percorrer a lista com foreach
        /// </summary>
        public IEnumerator<Documento> GetEnumerator() => Documentos.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Documentos.GetEnumerator();

        /// <summary>
        /// Cast para uma List
        /// </summary>
        /// <param name="lista">Objeto que será convertido</param>
        public static explicit operator List<Documento>(ListaDocumentos lista) => lista.Documentos;
    }
}
