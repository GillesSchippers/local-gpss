using System.ComponentModel.DataAnnotations;

namespace GPSS_Server.Models
{
    public class BundlePokemon
    {
        [Key]
        public long Id { get; set; }

        public long PokemonId { get; set; }
        public Pokemon Pokemon { get; set; } = null!;

        public long BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;
    }
}