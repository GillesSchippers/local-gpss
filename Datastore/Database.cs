using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Models;
using Utils;
using PKHeX.Core;
using System.Linq;

namespace Datastore
{
    public class GpssDbContext : DbContext
    {
        public DbSet<Pokemon> Pokemons { get; set; }
        public DbSet<Bundle> Bundles { get; set; }
        public DbSet<BundlePokemon> BundlePokemons { get; set; }

        public GpssDbContext(DbContextOptions<GpssDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pokemon>()
                .HasIndex(p => p.DownloadCode)
                .IsUnique();

            modelBuilder.Entity<Pokemon>()
                .HasIndex(p => p.Base64Hash)
                .IsUnique();

            modelBuilder.Entity<Bundle>()
                .HasIndex(b => b.DownloadCode)
                .IsUnique();

            modelBuilder.Entity<BundlePokemon>()
                .HasOne(bp => bp.Pokemon)
                .WithMany()
                .HasForeignKey(bp => bp.PokemonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BundlePokemon>()
                .HasOne(bp => bp.Bundle)
                .WithMany(b => b.BundlePokemons)
                .HasForeignKey(bp => bp.BundleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class Database
    {
        private readonly GpssDbContext _db;

        public Database(GpssDbContext db)
        {
            _db = db;
        }

        public static Database? Instance { get; } = new(new GpssDbContext(new DbContextOptions<GpssDbContext>()));

        #region Pokemon Functions
        public async Task<string?> CheckIfPokemonExistsAsync(string base64, bool returnId = false) // UNUSED
        {
            var base64Hash = ComputeSha256Hash(base64);
            var pokemon = await _db.Pokemons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Base64Hash == base64Hash);

            if (pokemon == null)
                return null;

            return returnId ? pokemon.Id.ToString() : pokemon.DownloadCode;
        }

        public async Task<long> InsertPokemonAsync(string base64, bool legal, string code, string generation)
        {
            var base64Hash = ComputeSha256Hash(base64);

            var pokemon = new Pokemon
            {
                UploadDateTime = DateTime.Now,
                DownloadCode = code,
                DownloadCount = 0,
                Generation = generation,
                Legal = legal,
                Base64 = base64,
                Base64Hash = base64Hash
            };

            _db.Pokemons.Add(pokemon);
            await _db.SaveChangesAsync();
            return pokemon.Id;
        }

        public async Task<List<GpssPokemon>> ListPokemonsAsync(int page = 1, int pageSize = 30, Search? search = null)
        {
            var query = _db.Pokemons.AsNoTracking();

            // Filtering
            if (search.HasValue)
            {
                if (!string.IsNullOrEmpty(search.Value.DownloadCode))
                    query = query.Where(p => p.DownloadCode == search.Value.DownloadCode);

                if (search.Value.Generations is { Count: > 0 })
                    query = query.Where(p => search.Value.Generations.Contains(p.Generation));

                if (search.Value.LegalOnly)
                    query = query.Where(p => p.Legal);

                // Sorting
                if (!string.IsNullOrEmpty(search.Value.SortField))
                {
                    if (search.Value.SortField == "download_count")
                        query = search.Value.SortDirection ? query.OrderBy(p => p.DownloadCount) : query.OrderByDescending(p => p.DownloadCount);
                    else // default to upload_datetime
                        query = search.Value.SortDirection ? query.OrderBy(p => p.UploadDateTime) : query.OrderByDescending(p => p.UploadDateTime);
                }
                else
                {
                    query = query.OrderByDescending(p => p.UploadDateTime);
                }
            }
            else
            {
                query = query.OrderByDescending(p => p.UploadDateTime);
            }

            // Pagination
            if (page > 1)
                query = query.Skip((page - 1) * pageSize);
            query = query.Take(pageSize);

            var pokemons = await query.ToListAsync();

            // Map to GpssPokemon DTO
            return pokemons.Select(p => new GpssPokemon
            {
                Legal = p.Legal,
                Base64 = p.Base64,
                DownloadCode = p.DownloadCode,
                Generation = p.Generation
            }).ToList();
        }

        public async Task<long?> GetPokemonIdAsync(string base64)
        {
            var base64Hash = ComputeSha256Hash(base64);
            var pokemon = await _db.Pokemons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Base64Hash == base64Hash);

            return pokemon?.Id;
        }

        public async Task<string?> GetPokemonDownloadCodeAsync(string base64)
        {
            var base64Hash = ComputeSha256Hash(base64);
            var pokemon = await _db.Pokemons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Base64Hash == base64Hash);

            return pokemon?.DownloadCode;
        }
        #endregion

        #region Bundle Functions

        public async Task<string?> CheckIfBundleExistsAsync(List<long> pokemonIds)
        {
            // Find bundles that contain exactly the same set of pokemonIds
            var bundleIds = await _db.BundlePokemons
                .Where(bp => pokemonIds.Contains(bp.PokemonId))
                .GroupBy(bp => bp.BundleId)
                .Where(g => g.Count() == pokemonIds.Count)
                .Select(g => g.Key)
                .ToListAsync();

            foreach (var bundleId in bundleIds)
            {
                var bundlePokemonIds = await _db.BundlePokemons
                    .Where(bp => bp.BundleId == bundleId)
                    .Select(bp => bp.PokemonId)
                    .ToListAsync();

                if (bundlePokemonIds.Count == pokemonIds.Count && !bundlePokemonIds.Except(pokemonIds).Any())
                {
                    var bundle = await _db.Bundles.FindAsync(bundleId);
                    return bundle?.DownloadCode;
                }
            }
            return null;
        }

        public async Task InsertBundleAsync(bool legal, string code, string minGen, string maxGen, List<long> ids)
        {
            var bundle = new Bundle
            {
                UploadDateTime = DateTime.Now,
                DownloadCode = code,
                DownloadCount = 0,
                Legal = legal,
                MinGen = minGen,
                MaxGen = maxGen,
                BundlePokemons = ids.Select(id => new BundlePokemon { PokemonId = id }).ToList()
            };
            _db.Bundles.Add(bundle);
            await _db.SaveChangesAsync();
        }

        public async Task<List<GpssBundle>> ListBundlesAsync(int page = 1, int pageSize = 30, Search? search = null)
        {
            var query = _db.Bundles
                .Include(b => b.BundlePokemons)
                    .ThenInclude(bp => bp.Pokemon)
                .AsNoTracking();

            // Filtering
            if (search.HasValue)
            {
                if (!string.IsNullOrEmpty(search.Value.DownloadCode))
                    query = query.Where(b => b.DownloadCode == search.Value.DownloadCode);

                if (search.Value.Generations is { Count: > 0 })
                    query = query.Where(b => search.Value.Generations.Contains(b.MinGen) && search.Value.Generations.Contains(b.MaxGen));

                if (search.Value.LegalOnly)
                    query = query.Where(b => b.Legal);

                // Sorting
                if (!string.IsNullOrEmpty(search.Value.SortField))
                {
                    if (search.Value.SortField == "download_count")
                        query = search.Value.SortDirection ? query.OrderBy(b => b.DownloadCount) : query.OrderByDescending(b => b.DownloadCount);
                    else // default to upload_datetime
                        query = search.Value.SortDirection ? query.OrderBy(b => b.UploadDateTime) : query.OrderByDescending(b => b.UploadDateTime);
                }
                else
                {
                    query = query.OrderByDescending(b => b.UploadDateTime);
                }
            }
            else
            {
                query = query.OrderByDescending(b => b.UploadDateTime);
            }

            // Pagination
            if (page > 1)
                query = query.Skip((page - 1) * pageSize);
            query = query.Take(pageSize);

            var bundles = await query.ToListAsync();

            // Map to GpssBundle DTO
            return bundles.Select(b =>
            {
                var pokemons = b.BundlePokemons.Select(bp => new GpssBundlePokemon
                {
                    Legal = bp.Pokemon.Legal,
                    Base64 = bp.Pokemon.Base64,
                    Generation = bp.Pokemon.Generation
                }).ToList();

                var downloadCodes = b.BundlePokemons.Select(bp => bp.Pokemon.DownloadCode).ToList();

                var data = new Dictionary<string, dynamic>
                {
                    { "legal", b.Legal },
                    { "download_count", b.DownloadCount },
                    { "download_code", b.DownloadCode },
                    { "min_gen", b.MinGen },
                    { "max_gen", b.MaxGen }
                };

                return new GpssBundle(pokemons, downloadCodes, data);
            }).ToList();
        }
        #endregion

        #region Generic Functions
        public async Task<bool> CodeExistsAsync(string table, string code)
        {
            return table switch
            {
                "pokemon" => await _db.Pokemons.AnyAsync(p => p.DownloadCode == code),
                "bundle" => await _db.Bundles.AnyAsync(b => b.DownloadCode == code),
                _ => false
            };
        }

        public async Task IncrementDownloadAsync(string table, string code)
        {
            if (table == "pokemon")
            {
                var pokemon = await _db.Pokemons.FirstOrDefaultAsync(p => p.DownloadCode == code);
                if (pokemon != null)
                {
                    pokemon.DownloadCount++;
                    await _db.SaveChangesAsync();
                }
            }
            else if (table == "bundle")
            {
                var bundle = await _db.Bundles.FirstOrDefaultAsync(b => b.DownloadCode == code);
                if (bundle != null)
                {
                    bundle.DownloadCount++;
                    var bundlePokemons = await _db.BundlePokemons.Where(bp => bp.BundleId == bundle.Id).ToListAsync();
                    foreach (var bp in bundlePokemons)
                    {
                        var pokemon = await _db.Pokemons.FindAsync(bp.PokemonId);
                        if (pokemon != null)
                            pokemon.DownloadCount++;
                    }
                    await _db.SaveChangesAsync();
                }
            }
        }

        public async Task<int> CountAsync(string table)
        {
            return table switch
            {
                "pokemon" => await _db.Pokemons.CountAsync(),
                "bundle" => await _db.Bundles.CountAsync(),
                _ => 0
            };
        }
        #endregion

        private static string ComputeSha256Hash(string rawData)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}