namespace Ocelot.Configuration.File
{
    public class FileCacheOptions
    {
        public FileCacheOptions()
        {
            Headers = null;
            Region = string.Empty;
            TtlSeconds = 0;
            RequestBodyHashing = false;

        }

        public FileCacheOptions(FileCacheOptions from)
        {
            Headers = from.Headers;
            Region = from.Region;
            TtlSeconds = from.TtlSeconds;
            RequestBodyHashing = from.RequestBodyHashing;
        }

        public string[] Headers { get; set; }
        public string Region { get; set; }
        public int TtlSeconds { get; set; }
        public bool RequestBodyHashing { get; set; }
    }
}
