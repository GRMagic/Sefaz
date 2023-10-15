using Sefaz.Core.Models.NFe;
using System;
using System.Threading.Tasks;

namespace Sefaz.Core
{
    /// <summary>
    /// Interface utilizada para comunicar-se com a Sefaz
    /// </summary>
    public interface ISefaz : IDisposable
    {
        /// <summary>
        /// Chama o WS da Sefaz para baixar a NFe
        /// </summary>
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ</param>
        /// <param name="chave">Chave da nota</param>
        /// <returns>Documento retornado pela SEFAZ</returns>
        Task<Documento> BaixarNFeAsync(string cUF, string cnpj, string chave);

        /// <summary>
        /// Chama o WS de distribuição de NFe da Sefaz para consultar um NSU
        /// </summary>
        /// <param name="cUF">Código IBGE da UF</param>
        /// <param name="cnpj">CNPJ do interessado</param>
        /// <param name="nsu">Número sequencial único</param>
        /// <returns>Documento retornado pela SEFAZ, não necessariamente uma NFe, pode ser algum evento por exemplo</returns>
        public Task<Documento> ConsultarNFeNSUAsync(string cUF, string cnpj, long nsu);

        /// <summary>
        /// Chama o WS da SEFAZ para consultar as notas e eventos de notas
        /// </summary>
        /// <param name="cUF">Código IBGE da UF da empresa</param>
        /// <param name="cnpj">CNPJ do interessado</param>
        /// <param name="ultimoNSU">NSU mais recente conhecido (Quando informado 0 retorna as notas dos últimos 90 dias)</param>
        /// <param name="todosLotes">Consultar automaticamente todos os lotes até o mais atual?</param>
        /// <returns>XML retornado pela Sefaz já deserializado em um objeto</returns>
        /// <remarks>ATENÇÃO! Consultas grandes e frequentes podem causar bloqueio temporário do serviço. Evite usar ultNSU=0 mais de uma vez por hora.</remarks>
        public Task<ListaDocumentos> ConsultarNFeCNPJAsync(string cUF, string cnpj, long ultimoNSU = 0, bool todosLotes = true);

        /// <summary>
        /// Gera um evento de manifesto do destinatário para a NFe
        /// </summary>
        /// <param name="cnpj">CNPJ do destinatário</param>
        /// <param name="chave">Chave da NFe</param>
        /// <param name="evento">Tipo de evento</param>
        /// <param name="sequencia">Número sequencial usado para identificar a ordem que os eventos ocorreram</param>
        /// <param name="justificativa">Justificativa caso necessária</param>
        /// <exception cref="SefazException">Pode lançar uma exceção caso o cStat tenha algum valor inesperado</exception>
        /// <remarks>O schema (xsd) não está sendo validado antes do envio</remarks>
        public Task ManifestarNFeAsync(string cnpj, string chave, TEventoInfEventoDetEventoDescEvento evento, int sequencia = 1, string justificativa = null);

        /// <summary>
        /// Chama o WS da SEFAZ para consultar os conhecimentos de transporte e eventos
        /// </summary>
        /// <param name="cUF">Código IBGE da UF da empresa</param>
        /// <param name="cnpj">CNPJ do interessado</param>
        /// <param name="ultimoNSU">NSU mais recente conhecido (Quando informado 0 retorna os conhecimentos dos últimos 90 dias)</param>
        /// <param name="todosLotes">Consultar automaticamente todos os lotes até o mais atual?</param>
        /// <returns>XML retornado pela Sefaz já deserializado em um objeto</returns>
        /// <remarks>ATENÇÃO! Consultas grandes e frequentes podem causar bloqueio temporário do serviço. Evite usar ultimoNSU=0 mais de uma vez por hora.</remarks>
        Task<ListaDocumentos> ConsultarCTeCNPJAsync(string cUF, string cnpj, long ultimoNSU = 0, bool todosLotes = true);

        /// <summary>
        /// Gera um evento de 'Prestação do Serviço em Desacordo' para a CTe
        /// </summary>
        /// <param name="cnpj">CNPJ do destinatário</param>
        /// <param name="chave">Chave da Cte</param>
        /// <param name="sequencia">Número sequencial usado para identificar a ordem que os eventos ocorreram</param>
        /// <param name="observacao">Justificativa caso necessária</param>
        /// <param name="webservice">Endereço do webservice CteRecepcaoEvento</param>
        /// <exception cref="SefazException">Pode lançar uma exceção caso o cStat tenha algum valor inesperado</exception>
        /// <remarks>O schema (xsd) não está sendo validado antes do envio</remarks>
        public Task ManifestarDesacordoCTeAsync(string cnpj, string chave, string observacao, int sequencia = 1, string webservice = null);

    }
}
