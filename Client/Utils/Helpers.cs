namespace GPSS_Client.Utils
{
    /// <summary>
    /// Defines the <see cref="Helpers" />.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// The GetGenerationFromFilename.
        /// </summary>
        /// <param name="filename">The filename<see cref="string"/>.</param>
        /// <returns>The <see cref="string?"/>.</returns>
        public static string? GetGenerationFromFilename(string filename)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            if (ext.StartsWith(".pk") && ext.Length > 3 && int.TryParse(ext.AsSpan(3), out var gen))
                return gen.ToString();
            return null;
        }
    }
}
