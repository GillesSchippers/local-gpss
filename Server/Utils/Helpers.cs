using GPSS_Server.Datastore;
using GPSS_Server.Models;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System.Dynamic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace GPSS_Server.Utils
{
    public static class Helpers
    {
        public static bool Init()
        {
            if (IsRunningAsAdminOrRoot())
            {
                Console.WriteLine("Error: Running this application as administrator or root is not supported. Please run as a standard user.");
                Environment.Exit(1);
            }

            EncounterEvent.RefreshMGDB(string.Empty);
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
            Legalizer.EnableEasterEggs = false;

            return true;
        }

        public static EntityContext EntityContextFromString(string generation)
        {
            return generation switch
            {
                "1" => EntityContext.Gen1,
                "2" => EntityContext.Gen2,
                "3" => EntityContext.Gen3,
                "4" => EntityContext.Gen4,
                "5" => EntityContext.Gen5,
                "6" => EntityContext.Gen6,
                "7" => EntityContext.Gen7,
                "8" => EntityContext.Gen8,
                "9" => EntityContext.Gen9,
                "BDSP" => EntityContext.Gen8b,
                "PLA" => EntityContext.Gen8a,
                _ => EntityContext.None,
            };
        }

        public static GameVersion GameVersionFromString(string version)
        {
            if (!Enum.TryParse(version, out GameVersion gameVersion)) return GameVersion.Any;

            return gameVersion;
        }

        public static dynamic PokemonAndBase64FromForm(IFormFile pokemon, EntityContext context = EntityContext.None)
        {
            using var memoryStream = new MemoryStream();
            pokemon.CopyTo(memoryStream);

            return new
            {
                pokemon = EntityFormat.GetFromBytes(memoryStream.ToArray(), context),
                base64 = Convert.ToBase64String(memoryStream.ToArray())
            };
        }

        public static PKM? PokemonFromForm(IFormFile pokemon, EntityContext context = EntityContext.None)
        {
            using var memoryStream = new MemoryStream();
            pokemon.CopyTo(memoryStream);

            return EntityFormat.GetFromBytes(memoryStream.ToArray(), context);
        }

        // This essentially takes in the search format that the FlagBrew website would've looked for
        // and re-shapes it in a way that the SQL query can use.
        public static Search SearchTranslation(JsonElement query)
        {
            var search = new Search();

            var hasGens = query.TryGetProperty("generations", out var generations);
            if (hasGens)
            {
                List<string> gens = [];

                for (var i = 0; i < generations.GetArrayLength(); i++)
                    switch (generations[i].GetString())
                    {
                        case "LGPE":
                            gens.Add("7.1");
                            break;
                        case "BDSP":
                            gens.Add("8.2");
                            break;
                        case "PLA":
                            gens.Add("8.1");
                            break;
                        case null:
                            break;
                        default:
                            gens.Add(generations[i].GetString()!);
                            break;
                    }

                search.Generations = gens;
            }

            var hasLegal = query.TryGetProperty("legal", out var legal);
            if (hasLegal) search.LegalOnly = legal.GetBoolean();

            var hasSortDirection = query.TryGetProperty("sort_direction", out var sort);
            if (hasSortDirection) search.SortDirection = sort.GetBoolean();

            var hasSortField = query.TryGetProperty("sort_field", out var sortField);
            if (hasSortField)
            {
                search.SortField = sortField.GetString() switch
                {
                    "latest" => "upload_datetime",
                    "popularity" => "download_count",
                    _ => "upload_datetime",
                };
            }

            var hasDownloadCode = query.TryGetProperty("download_code", out var downloadCode);
            if (hasDownloadCode)
                search.DownloadCode = downloadCode.GetString();

            return search;
        }

        public static async Task<string> GenerateDownloadCodeAsync(Database db, string table, int length = 10)
        {
            string code;
            do
            {
                code = "";
                for (int i = 0; i < length; i++)
                    code = string.Concat(code, Random.Shared.Next(10).ToString());
            } while (await db.CodeExistsAsync(table, code));
            return code;
        }

        // Credit: https://stackoverflow.com/a/9956981
        public static bool DoesPropertyExist(dynamic obj, string name)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);

            return obj.GetType().GetProperty(name) != null;
        }

        public static IPAddress? GetAdressFromString(string address)
        {
            try
            {
                IPAddress[] resolvedAddresses = Dns.GetHostAddresses(address);
                return resolvedAddresses.Length > 0 ? resolvedAddresses[0] : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool IsRunningAsAdminOrRoot()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var user = Environment.GetEnvironmentVariable("USER") ?? Environment.GetEnvironmentVariable("LOGNAME");
                return string.Equals(user, "root", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return false; // Unsupported OS or unable to determine admin/root status
            }
        }

        public static string ComputeSha256Hash(string rawData)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}