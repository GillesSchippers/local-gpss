using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Bundle
    {
        [Key]
        public long Id { get; set; }

        [MaxLength(255)]
        public string DownloadCode { get; set; } = string.Empty;
        public DateTime UploadDateTime { get; set; }
        public int DownloadCount { get; set; }
        public bool Legal { get; set; }
        [MaxLength(32)]
        public string MinGen { get; set; } = string.Empty;
        [MaxLength(32)]
        public string MaxGen { get; set; } = string.Empty;

        public List<BundlePokemon> BundlePokemons { get; set; } = [];
    }
}