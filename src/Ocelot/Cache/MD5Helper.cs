using System.Security.Cryptography;

namespace Ocelot.Cache;

public static class MD5Helper
{
    public static string GenerateMd5(byte[] contentBytes)
    {
        var hash = MD5.HashData(contentBytes);
        var sb = new StringBuilder(32);

        foreach (var byteValue in hash)
        {
            sb.Append(byteValue.ToString("X2"));
        }

        return sb.ToString();
    }
}
