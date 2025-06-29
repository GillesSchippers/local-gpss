namespace GPSS_Server.Datastore.Checks
{
    using GPSS_Server.Datastore;
    using GPSS_Server.Utils;
    using Microsoft.EntityFrameworkCore;
    using PKHeX.Core;

    /// <summary>
    /// Defines the <see cref="IntegrityChecker" />.
    /// </summary>
    public class IntegrityChecker(IServiceProvider services, ILogger<IntegrityChecker> logger) : BackgroundService
    {
        /// <summary>
        /// Defines the Interval.
        /// </summary>
        private readonly TimeSpan Interval = TimeSpan.FromDays(1);

        /// <summary>
        /// Defines the BatchSize.
        /// </summary>
        private const int BatchSize = 1000;

        /// <summary>
        /// The ExecuteAsync.
        /// </summary>
        /// <param name="stoppingToken">The stoppingToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await CheckAllPokemonAsync(stoppingToken);
                    await Task.Delay(Interval, stoppingToken);
                }
            }
            catch (TaskCanceledException) { /* Ignore */ }
        }

        /// <summary>
        /// The CheckAllPokemonAsync.
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task CheckAllPokemonAsync(CancellationToken cancellationToken)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GpssDbContext>();
            logger.LogInformation("Daily integrity check of pokemon is starting.");

            int total = await context.Pokemons.CountAsync(cancellationToken);
            for (int skip = 0; skip < total; skip += BatchSize)
            {
                var pokemons = await context.Pokemons
                    .OrderBy(p => p.Id)
                    .Skip(skip)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken);

                foreach (var p in pokemons)
                {
                    try
                    {
                        var computedHash = Helpers.ComputeSha256Hash(p.Base64);
                        bool hashMatches = string.Equals(computedHash, p.Base64Hash, StringComparison.OrdinalIgnoreCase);

                        PKM? pkmn = null;
                        bool canParse = false;
                        try
                        {
                            var bytes = Convert.FromBase64String(p.Base64);
                            pkmn = EntityFormat.GetFromBytes(bytes, Helpers.EntityContextFromString(p.Generation));
                            canParse = pkmn != null;
                        }
                        catch
                        {
                            canParse = false;
                        }

                        if (!hashMatches)
                        {
                            if (canParse)
                            {
                                // Hash mismatch but PKHeX can parse: update hash
                                logger.LogWarning("Hash mismatch for Pokemon ID {Id}, but PKHeX can parse. Updating hash.", p.Id);
                                p.Base64Hash = computedHash;

                                // If we get here, PKHeX can parse the Pok�mon. Check generation.
                                if (pkmn != null)
                                {
                                    var parsedGen = pkmn.Context.Generation().ToString();
                                    if (!string.Equals(p.Generation, parsedGen, StringComparison.OrdinalIgnoreCase))
                                    {
                                        logger.LogWarning("Generation mismatch for Pokemon ID {Id}: stored {StoredGen}, parsed {ParsedGen}. Updating stored generation.", p.Id, p.Generation, parsedGen);
                                        p.Generation = parsedGen;
                                    }
                                }
                                // Continue to legality check below
                            }
                            else
                            {
                                // Hash mismatch and cannot parse: remove from DB and all references
                                logger.LogWarning("Hash mismatch and PKHeX cannot parse Pokemon ID {Id}. Removing from database and all references.", p.Id);
                                var bundleRefs = context.BundlePokemons?
                                    .Where(bp => bp.PokemonId == p.Id)
                                    .ToList();
                                if (bundleRefs != null && bundleRefs.Count > 0)
                                    context.BundlePokemons?.RemoveRange(bundleRefs);
                                context.Pokemons.Remove(p);
                                continue;
                            }
                        }
                        else
                        {
                            if (!canParse)
                            {
                                // Hash matches but cannot parse: remove from DB and all references
                                logger.LogWarning("PKHeX cannot parse Pokemon ID {Id} (hash ok). Removing from database and all references.", p.Id);
                                var bundleRefs = context.BundlePokemons?
                                    .Where(bp => bp.PokemonId == p.Id)
                                    .ToList();
                                if (bundleRefs != null && bundleRefs.Count > 0)
                                    context.BundlePokemons?.RemoveRange(bundleRefs);
                                context.Pokemons.Remove(p);
                                continue;
                            }
                        }

                        // If we get here, PKHeX can parse the Pok�mon. Check legality.
                        if (pkmn != null)
                        {
                            var legality = new LegalityAnalysis(pkmn);
                            if (p.Legal != legality.Valid)
                            {
                                logger.LogInformation("Updating legality for Pokemon ID {Id}: {Old} -> {New}", p.Id, p.Legal, legality.Valid);
                                p.Legal = legality.Valid;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error checking Pokemon ID {Id}", p.Id);
                    }
                }
                await context.SaveChangesAsync(cancellationToken);
            }
            logger.LogInformation("Daily integrity check of pokemon has finished.");
        }
    }
}
