using System.ComponentModel.DataAnnotations;

namespace GPSS_Server.Models
{
    public class Pokemon
    {
        [Key]
        public long Id { get; set; }

        public DateTime UploadDateTime { get; set; }
        [MaxLength(255)]
        public string DownloadCode { get; set; } = string.Empty;
        public int DownloadCount { get; set; }
        [MaxLength(32)]
        public string Generation { get; set; } = string.Empty;
        public bool Legal { get; set; }
        public string Base64 { get; set; } = string.Empty;
        [MaxLength(64)]
        public string Base64Hash { get; set; } = string.Empty;
    }
}