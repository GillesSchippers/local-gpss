namespace GPSS_Client.Utils
{
    public static class Helpers
    {
        public static string? GetGenerationFromFilename(string filename)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            if (ext.StartsWith(".pk") && ext.Length > 3 && int.TryParse(ext.AsSpan(3), out var gen))
                return gen.ToString();
            return null;
        }
    }
}
