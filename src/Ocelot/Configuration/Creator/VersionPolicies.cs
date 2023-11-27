namespace Ocelot.Configuration.Creator
{
    public class VersionPolicies
    {
        public const string RequestVersionExact = nameof(HttpVersionPolicy.RequestVersionExact);
        public const string RequestVersionOrLower = nameof(HttpVersionPolicy.RequestVersionOrLower);
        public const string RequestVersionOrHigher = nameof(HttpVersionPolicy.RequestVersionOrHigher);
    }
}
