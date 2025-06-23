using System.Data;
using System.Text.Json;
using Models;
using Utils;
using MySqlConnector;
using PKHeX.Core;

namespace Datastore
{
    public class Database
    {
        private readonly string _connectionString;

        private Database()
        {
            var config = Helpers.Init() ?? Helpers.FirstTime();
            _connectionString = $"Server={config.MySqlHost};Port={config.MySqlPort};User={config.MySqlUser};Password={config.MySqlPassword};Database={config.MySqlDatabase};";
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            Migrate(connection);
        }

        public static Database? Instance { get; } = new();

        #region Pokemon Functions
        public dynamic? CheckIfPokemonExists(string base64, bool returnId = false)
        {
            var base64Hash = ComputeSha256Hash(base64);

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT {(returnId ? "id" : "download_code")} FROM pokemon WHERE base_64_hash = @base64_hash";
            cmd.Parameters.AddWithValue("@base64_hash", base64Hash);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return returnId ? reader.GetInt64(0) : reader.GetString(0);
        }
    
        public long InsertPokemon(string base64, bool legal, string code, string generation)
        {
            var base64Hash = ComputeSha256Hash(base64);

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"INSERT INTO pokemon (upload_datetime, download_code, download_count, generation, legal, base_64, base_64_hash) 
                  VALUES (@upload_datetime, @download_code, @download_count, @generation, @legal, @base_64, @base_64_hash); 
                  SELECT LAST_INSERT_ID();";
            cmd.Parameters.AddWithValue("@upload_datetime", DateTime.Now);
            cmd.Parameters.AddWithValue("@download_code", code);
            cmd.Parameters.AddWithValue("@download_count", 0);
            cmd.Parameters.AddWithValue("@generation", generation);
            cmd.Parameters.AddWithValue("@legal", legal);
            cmd.Parameters.AddWithValue("@base_64", base64);
            cmd.Parameters.AddWithValue("@base_64_hash", base64Hash);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        #endregion
    
        #region Bundle Functions

        public string? CheckIfBundleExists(List<long> pokemonIds)
        {
            var valuesStr = string.Join(" UNION ALL ", pokemonIds.Select(id => $"SELECT {id} AS pokemon_id"));
        
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                                   WITH input_pokemon_ids AS (
                                       {valuesStr}
                                   ), 
                                   bundles_with_matching_pokemon AS (
                                       SELECT bundle_id
                                       FROM bundle_pokemon
                                       WHERE pokemon_id IN (SELECT pokemon_id FROM input_pokemon_ids)
                                       GROUP BY bundle_id
                                       HAVING COUNT(DISTINCT pokemon_id) = (SELECT COUNT(*) FROM input_pokemon_ids)
                                   )
                                   SELECT b.download_code
                                   FROM bundles_with_matching_pokemon bp
                                   JOIN bundle b ON bp.bundle_id = b.id
                                   LIMIT 1
                               """;
            var reader = cmd.ExecuteReader();
        
            if (reader.Read())
            {
                return reader.IsDBNull(0) ? null : reader.GetString(0);
            }
            return null;
        }

    
        public void InsertBundle(bool legal, string code, string minGen, string maxGen, List<long> ids)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"INSERT INTO bundle (upload_datetime, download_code, download_count, legal, min_gen, max_gen) VALUES (@upload_datetime, @download_code, @download_count, @legal, @min_gen, @max_gen); SELECT LAST_INSERT_ID();";
            cmd.Parameters.AddWithValue("@upload_datetime", DateTime.Now);
            cmd.Parameters.AddWithValue("@download_code", code);
            cmd.Parameters.AddWithValue("@download_count", 0);
            cmd.Parameters.AddWithValue("@legal", legal);
            cmd.Parameters.AddWithValue("@min_gen", minGen);
            cmd.Parameters.AddWithValue("@max_gen", maxGen);
        
            var bundleId = Convert.ToInt64(cmd.ExecuteScalar());
        
            // Now to loop through and do a mass insert
            cmd.Parameters.Clear();

            cmd.CommandText = "INSERT INTO bundle_pokemon (pokemon_id, bundle_id) VALUES\n";
            for (var i = 0; i < ids.Count; i++)
            {
                cmd.CommandText += $"({ids[i]}, {bundleId})";
                if (i < ids.Count - 1)
                {
                    cmd.CommandText += ",\n";
                }
                else
                {
                    cmd.CommandText += ";";
                }
            
            }
        
            cmd.ExecuteNonQuery();
        }
        #endregion
    
        #region Generic Functions
        public bool CodeExists(string table, string code)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM {table} WHERE download_code = @code)";
            cmd.Parameters.AddWithValue("@code", code);

            return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
        }

        public void IncrementDownload(string table, string code)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"UPDATE {table} SET download_count = download_count + 1 WHERE download_code = @code";
            cmd.Parameters.AddWithValue("@code", code);
            cmd.ExecuteNonQuery();
            if (table == "bundle")
            {
                cmd.CommandText = "UPDATE pokemon SET download_count = download_count + 1 WHERE id in (SELECT pokemon_id from bundle_pokemon where bundle_id = (select id from bundle where download_code = @code))";
                cmd.ExecuteNonQuery();
            }
        
        }
    
        public int Count(string table, Search? search = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            var sql = GenerateBaseSelectSql(table, true, search);

            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            reader.Read();

            return reader.GetInt32(0);
        }

        public List<T> List<T>(string table, int page = 1, int pageSize = 30, Search? search = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            var sql = GenerateBaseSelectSql(table, false, search);

            sql += "LIMIT " + pageSize;
            if (page > 1) sql += " OFFSET " + page * pageSize;

            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();


            var items = new List<T>();
            var buffer1 = new List<GpssBundlePokemon>();
            var buffer2 = new List<string>();
            var buffer3 = new Dictionary<string, dynamic>();
            var currentDc = "";
            while (reader.Read())
            {
                if (table == "pokemon")
                {
                    items.Add((T)Activator.CreateInstance(typeof(T), reader));
                } else {
                    var dc = reader.GetString(reader.GetOrdinal("download_code"));
                    if (currentDc == "")
                    {
                        // get the current download code for the bundle
                        currentDc = dc;
                    }
                    else if (dc != currentDc)
                    {
                        // Lists are pass by reference, this is dumb, it means I essentially have to duplicate the list
                        // to be able to clear the buffer so that the values actually get populated >_<
                        items.Add((T)Activator.CreateInstance(typeof(T), new List<GpssBundlePokemon>(buffer1), new List<string>(buffer2), buffer3));
                        buffer1.Clear();
                        buffer2.Clear();
                        buffer3.Clear();
                        currentDc = dc;
                    }
                
                    buffer1.Add(new GpssBundlePokemon()
                    {
                        Generation = reader.GetString(reader.GetOrdinal("pg")),
                        Legal = reader.GetBoolean(reader.GetOrdinal("legality")),
                        Base64 = reader.GetString(reader.GetOrdinal("base_64")),
                    });
                    buffer2.Add(reader.GetString(reader.GetOrdinal("pdc")));
                    if (!buffer3.ContainsKey("download_count"))
                    {
                        buffer3.Add("download_count", reader.GetInt64(reader.GetOrdinal("download_count")));
                        buffer3.Add("download_code", reader.GetString(reader.GetOrdinal("download_code")));
                        buffer3.Add("min_gen", reader.GetString(reader.GetOrdinal("min_gen")));
                        buffer3.Add("max_gen", reader.GetString(reader.GetOrdinal("max_gen")));
                        buffer3.Add("legal", reader.GetBoolean(reader.GetOrdinal("legal")));
                    }
                }
            }
        
            if (table == "bundle" && buffer1.Count > 0)
            {
                items.Add((T)Activator.CreateInstance(typeof(T), new List<GpssBundlePokemon>(buffer1), new List<string>(buffer2), buffer3));
                buffer1.Clear();
                buffer2.Clear();
                buffer3.Clear();
            }

            return items;
        }
        #endregion
        private static string ComputeSha256Hash(string rawData)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexStringLower(bytes);
        }

        private static string GenerateBaseSelectSql(string table, bool count, Search? search = null)
        {
            var sql = $"SELECT {(count ? "COUNT(*)" : $"{(table == "pokemon" ? "*" : "bundle.*, pokemon.download_code as pdc, pokemon.generation as pg, pokemon.base_64, pokemon.legal as legality")}")} FROM {table} ";

            var whereClauses = new List<string>();

            if (search.HasValue)
            {
                if (!string.IsNullOrEmpty(search.Value.DownloadCode))
                {
                    whereClauses.Add($"{(table == "pokemon" ? "" : "bundle.")}download_code = '{search.Value.DownloadCode.Replace("'", "''")}'");
                }

                if (search.Value.Generations != null)
                {
                    if (table == "pokemon")
                        whereClauses.Add($"generation IN ('{string.Join("','", search.Value.Generations)}')");
                    else
                    {
                        var gens = string.Join("','", search.Value.Generations);
                        whereClauses.Add($"min_gen IN ('{gens}') AND max_gen IN ('{gens}')");
                    }
                }

                if (search.Value.LegalOnly)
                    whereClauses.Add((table == "bundle" ? "bundle.legal = 1" : "legal = 1"));
            }

            if (whereClauses.Count > 0)
                sql += "WHERE " + string.Join(" AND ", whereClauses) + " ";

            if (search.HasValue && !string.IsNullOrEmpty(search.Value.SortField))
                sql += "ORDER BY " + search.Value.SortField + (search.Value.SortDirection ? " ASC " : " DESC ");

            return sql;
        }

        private static void Migrate(MySqlConnection connection)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS pokemon (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    upload_datetime DATETIME NOT NULL,
                    download_code VARCHAR(255) UNIQUE,
                    download_count INT,
                    generation VARCHAR(32) NOT NULL,
                    legal BOOLEAN NOT NULL,
                    base_64 TEXT NOT NULL,
                    base_64_hash CHAR(64) NOT NULL,
                    UNIQUE KEY uq_base64_hash (base_64_hash)
                );
                """;
            cmd.ExecuteNonQuery();

            cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS bundle (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    download_code VARCHAR(255) UNIQUE,
                    upload_datetime DATETIME NOT NULL,
                    download_count INT,
                    legal BOOLEAN NOT NULL,
                    min_gen VARCHAR(32) NOT NULL,
                    max_gen VARCHAR(32) NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();

            cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS bundle_pokemon (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    pokemon_id INT,
                    bundle_id INT,
                    FOREIGN KEY (pokemon_id) REFERENCES pokemon(id) ON DELETE CASCADE,
                    FOREIGN KEY (bundle_id) REFERENCES bundle(id) ON DELETE CASCADE
                );
                """;
            cmd.ExecuteNonQuery();
        }
    }
}