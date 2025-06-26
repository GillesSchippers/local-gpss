namespace GPSS_Server.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Defines the <see cref="BundlePokemon" />.
    /// </summary>
    public class BundlePokemon
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the PokemonId.
        /// </summary>
        public long PokemonId { get; set; }

        /// <summary>
        /// Gets or sets the Pokemon.
        /// </summary>
        public Pokemon Pokemon { get; set; } = null!;

        /// <summary>
        /// Gets or sets the BundleId.
        /// </summary>
        public long BundleId { get; set; }

        /// <summary>
        /// Gets or sets the Bundle.
        /// </summary>
        public Bundle Bundle { get; set; } = null!;
    }
}
