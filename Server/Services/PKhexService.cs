namespace GPSS_Server.Services
{
    using GPSS_Server.Models;
    using GPSS_Server.Utils;
    using PKHeX.Core;
    using PKHeX.Core.AutoMod;

    /// <summary>
    /// Defines the <see cref="PKhexService" />.
    /// </summary>
    public class PKhexService
    {
        /// <summary>
        /// The LegalityCheck.
        /// </summary>
        /// <param name="pokemon">The pokemon<see cref="IFormFile"/>.</param>
        /// <param name="context">The context<see cref="EntityContext?"/>.</param>
        /// <returns>The <see cref="dynamic"/>.</returns>
        public static dynamic LegalityCheck(IFormFile pokemon, EntityContext? context)
        {
            var pkmn = Helpers.PokemonFromForm(pokemon, context ?? EntityContext.None);
            if (pkmn == null)
                return new
                {
                    error = "not a pokemon!"
                };

            return new LegalityCheckReport(CheckLegality(pkmn));
        }

        /// <summary>
        /// The Legalize.
        /// </summary>
        /// <param name="pokemon">The pokemon<see cref="IFormFile"/>.</param>
        /// <param name="context">The context<see cref="EntityContext?"/>.</param>
        /// <param name="version">The version<see cref="GameVersion?"/>.</param>
        /// <returns>The <see cref="dynamic"/>.</returns>
        public static dynamic Legalize(IFormFile pokemon, EntityContext? context, GameVersion? version)
        {
            var pkmn = Helpers.PokemonFromForm(pokemon, context ?? EntityContext.None);
            if (pkmn == null)
                return new
                {
                    error = "not a pokemon!"
                };

            var report = CheckLegality(pkmn);
            if (report.Valid)
                return new AutoLegalizationResult(report, null, false);
            ;

            var result = AutoLegalize(pkmn, version);
            if (result != null) report = CheckLegality(result);

            return new AutoLegalizationResult(report, result, true);
        }

        /// <summary>
        /// The CheckLegality.
        /// </summary>
        /// <param name="pokemon">The pokemon<see cref="PKM"/>.</param>
        /// <returns>The <see cref="LegalityAnalysis"/>.</returns>
        private static LegalityAnalysis CheckLegality(PKM pokemon)
        {
            return new LegalityAnalysis(pokemon);
        }

        /// <summary>
        /// The AutoLegalize.
        /// </summary>
        /// <param name="pokemon">The pokemon<see cref="PKM"/>.</param>
        /// <param name="overriddenVersion">The overriddenVersion<see cref="GameVersion?"/>.</param>
        /// <returns>The <see cref="PKM?"/>.</returns>
        private static PKM? AutoLegalize(PKM pokemon,
            GameVersion? overriddenVersion = null)
        {
            var version = overriddenVersion ?? (Enum.TryParse(pokemon.Version.ToString(), out GameVersion parsedVersion)
                ? parsedVersion
                : null);
            var info = GetTrainerInfoWrapper(pokemon, version);
            var pk = info.Legalize(pokemon);

            // copy the new info so we can restore it if legality isn't happy

            var backup = GetTrainerInfoWrapper(pk, version);

            //var pk = pokemon.Legalize();
            pk.SetTrainerData(info);
            if (!CheckLegality(pk).Valid)
            {
                pk.SetTrainerData(backup);
            }
            else
            {
                var htn = pk.HandlingTrainerName;
                var htg = pk.HandlingTrainerGender;
                var htf = pk.HandlingTrainerFriendship;

                pk.HandlingTrainerName = pokemon.HandlingTrainerName;
                pk.HandlingTrainerGender = pokemon.HandlingTrainerGender;
                pk.HandlingTrainerFriendship = pokemon.HandlingTrainerFriendship;
                if (!CheckLegality(pk).Valid)
                {
                    pk.HandlingTrainerName = htn;
                    pk.HandlingTrainerGender = htg;
                    pk.HandlingTrainerFriendship = htf;
                }
            }

            return !CheckLegality(pk).Valid ? null : pk;
        }

        /// <summary>
        /// The GetTrainerInfoWrapper.
        /// </summary>
        /// <param name="pokemon">The pokemon<see cref="PKM"/>.</param>
        /// <param name="version">The version<see cref="GameVersion?"/>.</param>
        /// <returns>The <see cref="SimpleTrainerInfo"/>.</returns>
        private static SimpleTrainerInfo GetTrainerInfoWrapper(PKM pokemon, GameVersion? version)
        {
            return new SimpleTrainerInfo(version ?? GameVersion.SL)
            {
                OT = pokemon.OriginalTrainerName,
                SID16 = pokemon.SID16,
                TID16 = pokemon.TID16,
                Language = pokemon.Language,
                Gender = pokemon.OriginalTrainerGender,
                Generation = pokemon.Context.Generation()
            };
        }
    }
}
