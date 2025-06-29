namespace GPSS_Client.Models
{
    /// <summary>
    /// Defines the <see cref="APIRecords" />.
    /// </summary>
    public class APIRecords
    {
        public record GpssPokemon(
        bool Legal,
        string Base64,
        string Code,
        string Generation
        );

        public record GpssBundlePokemon(
            bool Legality,
            string Base64,
            string Generation
        );

        public record GpssBundle(
            List<GpssBundlePokemon> Pokemons,
            List<string> DownloadCodes,
            string DownloadCode,
            string MinGen,
            string MaxGen,
            int Count,
            bool Legal
        );

        public record Search(
            List<string>? Generations = null,
            bool LegalOnly = false,
            bool SortDirection = false,
            string SortField = "upload_datetime",
            string? DownloadCode = null
        );
    }
}
