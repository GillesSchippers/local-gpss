namespace GPSS_Server.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Defines the <see cref="Bundle" />.
    /// </summary>
    public class Bundle
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the DownloadCode.
        /// </summary>
        [MaxLength(255)]
        public string DownloadCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UploadDateTime.
        /// </summary>
        public DateTime UploadDateTime { get; set; }

        /// <summary>
        /// Gets or sets the DownloadCount.
        /// </summary>
        public int DownloadCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        public bool Legal { get; set; }

        /// <summary>
        /// Gets or sets the MinGen.
        /// </summary>
        [MaxLength(32)]
        public string MinGen { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MaxGen.
        /// </summary>
        [MaxLength(32)]
        public string MaxGen { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the BundlePokemons.
        /// </summary>
        public List<BundlePokemon> BundlePokemons { get; set; } = [];
    }
}
