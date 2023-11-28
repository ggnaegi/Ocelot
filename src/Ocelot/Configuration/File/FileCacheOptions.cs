namespace Ocelot.Configuration.File
{
    public class FileCacheOptions
    {
        public FileCacheOptions()
        {
            Header = string.Empty;
            Region = string.Empty;
            TtlSeconds = 0;
            RequestBodyHashing = false;

        }

        public FileCacheOptions(FileCacheOptions from)
        {
            Header = from.Header;
            Region = from.Region;
            TtlSeconds = from.TtlSeconds;
            RequestBodyHashing = from.RequestBodyHashing;
        }

        public string Header { get; set; }
        public string Region { get; set; }
        public int TtlSeconds { get; set; }
        public bool RequestBodyHashing { get; set; }
    }
}
