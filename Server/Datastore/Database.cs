namespace GPSS_Server.Datastore
{
    using GPSS_Server.Models;
    using GPSS_Server.Utils;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Defines the <see cref="GpssDbContext" />.
    /// </summary>
    public class GpssDbContext(DbContextOptions<GpssDbContext> options) : DbContext(options)
    {
        /// <summary>
        /// Gets or sets the Pokemons.
        /// </summary>
        public DbSet<Pokemon> Pokemons { get; set; }

        /// <summary>
        /// Gets or sets the Bundles.
        /// </summary>
        public DbSet<Bundle> Bundles { get; set; }

        /// <summary>
        /// Gets or sets the BundlePokemons.
        /// </summary>
        public DbSet<BundlePokemon> BundlePokemons { get; set; }

        /// <summary>
        /// The OnModelCreating.
        /// </summary>
        /// <param name="modelBuilder">The modelBuilder<see cref="ModelBuilder"/>.</param>
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

    /// <summary>
    /// Defines the <see cref="Database" />.
    /// </summary>
    public class Database(GpssDbContext db)
    {
        /// <summary>
        /// Gets the Instance.
        /// </summary>
        public static Database? Instance { get; } = new(new GpssDbContext(new DbContextOptions<GpssDbContext>()));

        /// <summary>
        /// The CheckIfPokemonExistsAsync.
        /// </summary>
        /// <param name="base64">The base64<see cref="string"/>.</param>
        /// <param name="returnId">The returnId<see cref="bool"/>.</param>
        /// <returns>The <see cref="Task{string?}"/>.</returns>
        public async Task<string?> CheckIfPokemonExistsAsync(string base64, bool returnId = false) // UNUSED
        {
            var base64Hash = Helpers.ComputeSha256Hash(base64);
            var pokemon = await db.Pokemons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Base64Hash == base64Hash);

            if (pokemon == null)
                return null;

            return returnId ? pokemon.Id.ToString() : pokemon.DownloadCode;
        }

        /// <summary>
        /// The InsertPokemonAsync.
        /// </summary>
        /// <param name="base64">The base64<see cref="string"/>.</param>
        /// <param name="legal">The legal<see cref="bool"/>.</param>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <param name="generation">The generation<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{long}"/>.</returns>
        public async Task<long> InsertPokemonAsync(string base64, bool legal, string code, string generation)
        {
            var base64Hash = Helpers.ComputeSha256Hash(base64);

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

            db.Pokemons.Add(pokemon);
            await db.SaveChangesAsync();
            return pokemon.Id;
        }

        /// <summary>
        /// The DeleteByCodeAsync.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{bool}"/>.</returns>
        public async Task<bool> DeleteByCodeAsync(string entityType, string code)
        {
            switch (entityType.ToLowerInvariant())
            {
                case "pokemon":
                    {
                        var pokemon = await db.Pokemons.FirstOrDefaultAsync(p => p.DownloadCode == code);
                        if (pokemon == null)
                            return false;
                        db.Pokemons.Remove(pokemon);
                        await db.SaveChangesAsync();
                        return true;
                    }
                case "bundle":
                case "bundles":
                    {
                        var bundle = await db.Bundles.FirstOrDefaultAsync(b => b.DownloadCode == code);
                        if (bundle == null)
                            return false;
                        db.Bundles.Remove(bundle);
                        await db.SaveChangesAsync();
                        return true;
                    }
                default:
                    return false;
            }
        }

        /// <summary>
        /// The ListPokemonsAsync.
        /// </summary>
        /// <param name="page">The page<see cref="int"/>.</param>
        /// <param name="pageSize">The pageSize<see cref="int"/>.</param>
        /// <param name="search">The search<see cref="Search?"/>.</param>
        /// <returns>The <see cref="Task{List{GpssPokemon}}"/>.</returns>
        public async Task<List<GpssPokemon>> ListPokemonsAsync(int page = 1, int pageSize = 30, Search? search = null)
        {
            var query = db.Pokemons.AsNoTracking();

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
            return [.. pokemons.Select(p => new GpssPokemon
            {
                Legal = p.Legal,
                Base64 = p.Base64,
                DownloadCode = p.DownloadCode,
                Generation = p.Generation
            })];
        }

        /// <summary>
        /// The GetPokemonIdAsync.
        /// </summary>
        /// <param name="base64">The base64<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{long?}"/>.</returns>
        public async Task<long?> GetPokemonIdAsync(string base64)
        {
            var base64Hash = Helpers.ComputeSha256Hash(base64);
            var pokemon = await db.Pokemons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Base64Hash == base64Hash);

            return pokemon?.Id;
        }

        /// <summary>
        /// The GetPokemonDownloadCodeAsync.
        /// </summary>
        /// <param name="base64">The base64<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{string?}"/>.</returns>
        public async Task<string?> GetPokemonDownloadCodeAsync(string base64)
        {
            var base64Hash = Helpers.ComputeSha256Hash(base64);
            var pokemon = await db.Pokemons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Base64Hash == base64Hash);

            return pokemon?.DownloadCode;
        }

        /// <summary>
        /// The CheckIfBundleExistsAsync.
        /// </summary>
        /// <param name="pokemonIds">The pokemonIds<see cref="List{long}"/>.</param>
        /// <returns>The <see cref="Task{string?}"/>.</returns>
        public async Task<string?> CheckIfBundleExistsAsync(List<long> pokemonIds)
        {
            // Find bundles that contain exactly the same set of pokemonIds
            var bundleIds = await db.BundlePokemons
                .Where(bp => pokemonIds.Contains(bp.PokemonId))
                .GroupBy(bp => bp.BundleId)
                .Where(g => g.Count() == pokemonIds.Count)
                .Select(g => g.Key)
                .ToListAsync();

            foreach (var bundleId in bundleIds)
            {
                var bundlePokemonIds = await db.BundlePokemons
                    .Where(bp => bp.BundleId == bundleId)
                    .Select(bp => bp.PokemonId)
                    .ToListAsync();

                if (bundlePokemonIds.Count == pokemonIds.Count && !bundlePokemonIds.Except(pokemonIds).Any())
                {
                    var bundle = await db.Bundles.FindAsync(bundleId);
                    return bundle?.DownloadCode;
                }
            }
            return null;
        }

        /// <summary>
        /// The InsertBundleAsync.
        /// </summary>
        /// <param name="legal">The legal<see cref="bool"/>.</param>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <param name="minGen">The minGen<see cref="string"/>.</param>
        /// <param name="maxGen">The maxGen<see cref="string"/>.</param>
        /// <param name="ids">The ids<see cref="List{long}"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
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
                BundlePokemons = [.. ids.Select(id => new BundlePokemon { PokemonId = id })]
            };
            db.Bundles.Add(bundle);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// The ListBundlesAsync.
        /// </summary>
        /// <param name="page">The page<see cref="int"/>.</param>
        /// <param name="pageSize">The pageSize<see cref="int"/>.</param>
        /// <param name="search">The search<see cref="Search?"/>.</param>
        /// <returns>The <see cref="Task{List{GpssBundle}}"/>.</returns>
        public async Task<List<GpssBundle>> ListBundlesAsync(int page = 1, int pageSize = 30, Search? search = null)
        {
            var query = db.Bundles
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
            return [.. bundles.Select(b =>
            {
                var pokemons = b.BundlePokemons.Select(bp => new GpssBundlePokemon
                {
                    Legal = bp.Pokemon.Legal,
                    Base64 = bp.Pokemon.Base64,
                    Generation = bp.Pokemon.Generation
                }).ToList();

                var downloadCodes = b.BundlePokemons.Select(bp => bp.Pokemon.DownloadCode).ToList();

                return GpssBundle.Create(pokemons, downloadCodes, b.DownloadCode, b.MinGen, b.MaxGen, b.Legal);
            })];
        }

        /// <summary>
        /// The CodeExistsAsync.
        /// </summary>
        /// <param name="table">The table<see cref="string"/>.</param>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{bool}"/>.</returns>
        public async Task<bool> CodeExistsAsync(string table, string code)
        {
            return table switch
            {
                "pokemon" => await db.Pokemons.AnyAsync(p => p.DownloadCode == code),
                "bundle" => await db.Bundles.AnyAsync(b => b.DownloadCode == code),
                _ => false
            };
        }

        /// <summary>
        /// The IncrementDownloadAsync.
        /// </summary>
        /// <param name="table">The table<see cref="string"/>.</param>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task IncrementDownloadAsync(string table, string code)
        {
            if (table == "pokemon")
            {
                var pokemon = await db.Pokemons.FirstOrDefaultAsync(p => p.DownloadCode == code);
                if (pokemon != null)
                {
                    pokemon.DownloadCount++;
                    await db.SaveChangesAsync();
                }
            }
            else if (table == "bundle")
            {
                var bundle = await db.Bundles.FirstOrDefaultAsync(b => b.DownloadCode == code);
                if (bundle != null)
                {
                    bundle.DownloadCount++;
                    var bundlePokemons = await db.BundlePokemons.Where(bp => bp.BundleId == bundle.Id).ToListAsync();
                    foreach (var bp in bundlePokemons)
                    {
                        var pokemon = await db.Pokemons.FindAsync(bp.PokemonId);
                        if (pokemon != null)
                            pokemon.DownloadCount++;
                    }
                    await db.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// The CountAsync.
        /// </summary>
        /// <param name="table">The table<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{int}"/>.</returns>
        public async Task<int> CountAsync(string table)
        {
            return table switch
            {
                "pokemon" => await db.Pokemons.CountAsync(),
                "bundle" => await db.Bundles.CountAsync(),
                _ => 0
            };
        }
    }
}
