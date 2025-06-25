using GPSS_Server.Models;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace GPSS_Server.Utils
{
    public class Pkhex
    {
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

        private static LegalityAnalysis CheckLegality(PKM pokemon)
        {
            return new LegalityAnalysis(pokemon);
        }

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