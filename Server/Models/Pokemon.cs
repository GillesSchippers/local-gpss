namespace GPSS_Server.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Defines the <see cref="Pokemon" />.
    /// </summary>
    public class Pokemon
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the UploadDateTime.
        /// </summary>
        public DateTime UploadDateTime { get; set; }

        /// <summary>
        /// Gets or sets the DownloadCode.
        /// </summary>
        [MaxLength(255)]
        public string DownloadCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the DownloadCount.
        /// </summary>
        public int DownloadCount { get; set; }

        /// <summary>
        /// Gets or sets the Generation.
        /// </summary>
        [MaxLength(32)]
        public string Generation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        public bool Legal { get; set; }

        /// <summary>
        /// Gets or sets the Base64.
        /// </summary>
        public string Base64 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Base64Hash.
        /// </summary>
        [MaxLength(64)]
        public string Base64Hash { get; set; } = string.Empty;
    }
}
