namespace GPSS_Client.Services
{
    using GPSS_Client.Models;
    using PKHeX.Core;

    /// <summary>
    /// Defines the <see cref="PkhexService" />.
    /// </summary>
    public class PKHeXService
    {
        /// <summary>
        /// The GetPkm.
        /// </summary>
        /// <param name="pkmStream">The pkmStream<see cref="Stream"/>.</param>
        /// <returns>The <see cref="PKM?"/>.</returns>
        public static PKM? GetPkm(Stream pkmStream) => ParsePkm(pkmStream);

        /// <summary>
        /// The GetPkm.
        /// </summary>
        /// <param name="base64Pkm">The base64Pkm<see cref="string"/>.</param>
        /// <returns>The <see cref="PKM?"/>.</returns>
        public static PKM? GetPkm(string base64Pkm) => ParsePkm(base64Pkm);

        /// <summary>
        /// The GetPokemonInfo.
        /// </summary>
        /// <param name="pkmStream">The pkmStream<see cref="Stream"/>.</param>
        /// <returns>The <see cref="PokemonInfo?"/>.</returns>
        public static PokemonInfo? GetPokemonInfo(Stream pkmStream) => ParsePokemonInfo(pkmStream);

        /// <summary>
        /// The GetPokemonInfo.
        /// </summary>
        /// <param name="base64Pkm">The base64Pkm<see cref="string"/>.</param>
        /// <returns>The <see cref="PokemonInfo?"/>.</returns>
        public static PokemonInfo? GetPokemonInfo(string base64Pkm) => ParsePokemonInfo(base64Pkm);

        /// <summary>
        /// The ParsePokemonInfo.
        /// </summary>
        /// <param name="pkmStream">The pkmStream<see cref="Stream"/>.</param>
        /// <returns>The <see cref="PokemonInfo?"/>.</returns>
        private static PokemonInfo? ParsePokemonInfo(Stream pkmStream)
        {
            var pkm = ParsePkm(pkmStream);
            if (pkm == null)
                return null;

            return new PokemonInfo
            {
                Species = pkm.Species,
                Nickname = pkm.Nickname,
                OT = pkm.OriginalTrainerName,
                Gender = pkm.Gender,
                Level = pkm.CurrentLevel,
                Language = pkm.Language,
                Ability = pkm.Ability,
                TID = pkm.TID16,
                SID = pkm.SID16,
                IsShiny = pkm.IsShiny,
                Generation = pkm.Context.Generation()
            };
        }

        /// <summary>
        /// The ParsePokemonInfo.
        /// </summary>
        /// <param name="base64Pkm">The base64Pkm<see cref="string"/>.</param>
        /// <returns>The <see cref="PokemonInfo?"/>.</returns>
        private static PokemonInfo? ParsePokemonInfo(string base64Pkm)
        {
            var pkm = ParsePkm(base64Pkm);
            if (pkm == null)
                return null;

            return new PokemonInfo
            {
                Species = pkm.Species,
                Nickname = pkm.Nickname,
                OT = pkm.OriginalTrainerName,
                Gender = pkm.Gender,
                Level = pkm.CurrentLevel,
                Language = pkm.Language,
                Ability = pkm.Ability,
                TID = pkm.TID16,
                SID = pkm.SID16,
                IsShiny = pkm.IsShiny,
                Generation = pkm.Context.Generation()
            };
        }

        /// <summary>
        /// The ParsePkm.
        /// </summary>
        /// <param name="pkmStream">The pkmStream<see cref="Stream"/>.</param>
        /// <returns>The <see cref="PKM?"/>.</returns>
        private static PKM? ParsePkm(Stream pkmStream)
        {
            if (pkmStream == null)
                return null;

            try
            {
                using var ms = new MemoryStream();
                pkmStream.CopyTo(ms);
                var data = ms.ToArray();
                return EntityFormat.GetFromBytes(data);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The ParsePkm.
        /// </summary>
        /// <param name="base64Pkm">The base64Pkm<see cref="string"/>.</param>
        /// <returns>The <see cref="PKM?"/>.</returns>
        private static PKM? ParsePkm(string base64Pkm)
        {
            if (string.IsNullOrWhiteSpace(base64Pkm))
                return null;

            try
            {
                var data = Convert.FromBase64String(base64Pkm);
                return EntityFormat.GetFromBytes(data);
            }
            catch
            {
                return null;
            }
        }
    }
}
