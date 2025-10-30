namespace TheBuryProject.ViewModels.Base
{
    /// <summary>
    /// Resultado paginado gen�rico para listados.
    /// Contiene los items de la p�gina actual y metadatos de paginaci�n.
    /// </summary>
    /// <typeparam name="T">Tipo de elemento en la colecci�n</typeparam>
    public sealed class PageResult<T>
    {
        /// <summary>
        /// Items de la p�gina actual
        /// </summary>
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

        /// <summary>
        /// Total de items en todas las p�ginas
        /// </summary>
        public int Total { get; init; }

        /// <summary>
        /// N�mero de p�gina actual (base 1)
        /// </summary>
        public int Page { get; init; }

        /// <summary>
        /// Cantidad de items por p�gina
        /// </summary>
        public int PageSize { get; init; }

        /// <summary>
        /// Total de p�ginas calculado
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;

        /// <summary>
        /// Indica si hay p�gina anterior
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Indica si hay p�gina siguiente
        /// </summary>
        public bool HasNextPage => Page < TotalPages;
    }
}