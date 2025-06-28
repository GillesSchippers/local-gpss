namespace GPSS_Server.Utils
{
    using GPSS_Server.Datastore;
    using GPSS_Server.Models;
    using MessagePack;
    using MessagePack.Resolvers;
    using Microsoft.Extensions.Caching.Memory;
    using PKHeX.Core;
    using PKHeX.Core.AutoMod;
    using System.Collections.Concurrent;
    using System.Dynamic;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Text;
    using System.Text.Json;

    /// <summary>
    /// Defines the <see cref="Helpers" />.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Defines the SearchCacheKeys.
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> SearchCacheKeys = new();

        /// <summary>
        /// The Init.
        /// </summary>
        public static void Init()
        {
            if (IsRunningAsAdminOrRoot())
            {
                Console.WriteLine("Error: Running this application as administrator or root is not supported. Please run as a standard user.");
                Environment.Exit(1);
            }

            EncounterEvent.RefreshMGDB(string.Empty);
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
            Legalizer.EnableEasterEggs = false;

            return;
        }

        /// <summary>
        /// The EntityContextFromString.
        /// </summary>
        /// <param name="generation">The generation<see cref="string"/>.</param>
        /// <returns>The <see cref="EntityContext"/>.</returns>
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

        /// <summary>
        /// The GameVersionFromString.
        /// </summary>
        /// <param name="version">The version<see cref="string"/>.</param>
        /// <returns>The <see cref="GameVersion"/>.</returns>
        public static GameVersion GameVersionFromString(string version)
        {
            if (!Enum.TryParse(version, out GameVersion gameVersion)) return GameVersion.Any;

            return gameVersion;
        }

        /// <summary>
        /// The PokemonAndBase64FromForm.
        /// </summary>
        /// <param name="pokemon">The pokemon<see cref="IFormFile"/>.</param>
        /// <param name="context">The context<see cref="EntityContext"/>.</param>
        /// <returns>The <see cref="dynamic"/>.</returns>
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

        /// <summary>
        /// The PokemonFromForm.
        /// </summary>
        /// <param name="pokemon">The pokemon<see cref="IFormFile"/>.</param>
        /// <param name="context">The context<see cref="EntityContext"/>.</param>
        /// <returns>The <see cref="PKM?"/>.</returns>
        public static PKM? PokemonFromForm(IFormFile pokemon, EntityContext context = EntityContext.None)
        {
            using var memoryStream = new MemoryStream();
            pokemon.CopyTo(memoryStream);

            return EntityFormat.GetFromBytes(memoryStream.ToArray(), context);
        }

        // This essentially takes in the search format that the FlagBrew website would've looked for
        // and re-shapes it in a way that the SQL query can use.

        /// <summary>
        /// The SearchTranslation.
        /// </summary>
        /// <param name="query">The query<see cref="JsonElement"/>.</param>
        /// <returns>The <see cref="Search"/>.</returns>
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

        /// <summary>
        /// The GenerateDownloadCodeAsync.
        /// </summary>
        /// <param name="db">The db<see cref="Database"/>.</param>
        /// <param name="table">The table<see cref="string"/>.</param>
        /// <param name="length">The length<see cref="int"/>.</param>
        /// <returns>The <see cref="Task{string}"/>.</returns>
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

        /// <summary>
        /// The DoesPropertyExist.
        /// </summary>
        /// <param name="obj">The obj<see cref="dynamic"/>.</param>
        /// <param name="name">The name<see cref="string"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool DoesPropertyExist(dynamic obj, string name)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);

            return obj.GetType().GetProperty(name) != null;
        }

        /// <summary>
        /// The GetAddressFromString.
        /// </summary>
        /// <param name="address">The address<see cref="string"/>.</param>
        /// <returns>The <see cref="IPAddress?"/>.</returns>
        public static IPAddress? GetAddressFromString(string address)
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

        /// <summary>
        /// The IsRunningAsAdminOrRoot.
        /// </summary>
        /// <returns>The <see cref="bool"/>.</returns>
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

        /// <summary>
        /// The ComputeSha256Hash.
        /// </summary>
        /// <param name="rawData">The rawData<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string ComputeSha256Hash(string rawData)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>
        /// The ComputeSha256Hash.
        /// </summary>
        /// <param name="file">The file<see cref="IFormFile"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string ComputeSha256Hash(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var hash = SHA256.HashData(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// The GetObjectSizeInBytes.
        /// </summary>
        /// <param name="obj">The obj<see cref="object"/>.</param>
        /// <returns>The <see cref="int"/>.</returns>
        public static int GetObjectSizeInBytes(object obj) =>
            obj is null ? 0 :
            MessagePackSerializer.Serialize(obj, MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance)).Length;

        /// <summary>
        /// The SetAndTrackSearchCache.
        /// </summary>
        /// <param name="cache">The cache<see cref="IMemoryCache"/>.</param>
        /// <param name="key">The key<see cref="string"/>.</param>
        /// <param name="value">The value<see cref="object"/>.</param>
        /// <param name="options">The options<see cref="MemoryCacheEntryOptions"/>.</param>
        public static void SetAndTrackSearchCache(
            IMemoryCache cache,
            string key,
            object value,
            MemoryCacheEntryOptions options)
        {
            cache.Set(key, value, options);
            AddSearchCacheKey(key);
        }

        /// <summary>
        /// The AddSearchCacheKey.
        /// </summary>
        /// <param name="key">The key<see cref="string"/>.</param>
        private static void AddSearchCacheKey(string key)
        {
            SearchCacheKeys.TryAdd(key, 0);
        }

        /// <summary>
        /// The RemoveSearchCacheKey.
        /// </summary>
        /// <param name="key">The key<see cref="string"/>.</param>
        private static void RemoveSearchCacheKey(string key)
        {
            SearchCacheKeys.TryRemove(key, out _);
        }

        /// <summary>
        /// The InvalidateSearchCache.
        /// </summary>
        /// <param name="cache">The cache<see cref="IMemoryCache"/>.</param>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        public static void InvalidateSearchCache(IMemoryCache cache, string entityType)
        {
            var keysToRemove = SearchCacheKeys.Keys
                .Where(k => k.StartsWith(entityType + ":", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
                SearchCacheKeys.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// The InvalidateSearchCacheAsync.
        /// </summary>
        /// <param name="cache">The cache<see cref="IMemoryCache"/>.</param>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        public static void InvalidateSearchCacheAsync(IMemoryCache cache, string entityType)
        {
            Task.Run(() => InvalidateSearchCache(cache, entityType));
        }
    }
}
