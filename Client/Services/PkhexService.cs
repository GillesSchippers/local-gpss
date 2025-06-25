using PKHeX.Core;

namespace GPSS_Client.Services
{
    public class PkhexService
    {
        public static PKM? GetPkm(Stream pkmStream) => ParsePkm(pkmStream);
        public static PKM? GetPkm(string base64Pkm) => ParsePkm(base64Pkm);
        public static PokemonInfo? GetPokemonInfo(Stream pkmStream) => ParsePokemonInfo(pkmStream);
        public static PokemonInfo? GetPokemonInfo(string base64Pkm) => ParsePokemonInfo(base64Pkm);

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

    public class PokemonInfo
    {
        public ushort Species { get; set; }
        public string Nickname { get; set; }
        public string OT { get; set; }
        public int Gender { get; set; }
        public int Level { get; set; }
        public int Language { get; set; }
        public int Ability { get; set; }
        public int TID { get; set; }
        public int SID { get; set; }
        public bool IsShiny { get; set; }
        public byte Generation { get; set; }
    }

    public partial class PokemonInfoDisplay : PokemonInfo
    {
        public new string Species { get; set; }
        public new string Ability { get; set; }
        public new string Generation { get; set; }
        public new string language { get; set; }

        public bool Legal { get; set; }
        public string Code { get; set; }
    }
}