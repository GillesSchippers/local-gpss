using System.Text.Json.Serialization;

namespace GPSS_Client.Models
{
    public class Pokemon
    {
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }
        [JsonPropertyName("base_64")]
        public string Base64 { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("generation")]
        public string Generation { get; set; }
    }

    public partial class PokemonResult : Pokemon
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
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
        public new string Language { get; set; }

        public bool Legal { get; set; }
        public string Code { get; set; }
    }
}
