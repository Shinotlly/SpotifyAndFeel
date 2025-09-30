using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyAndFeel.Utils
{
    public static class PkceUtil
    {
        public static string CreateCodeVerifier()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static string CreateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            return Convert.ToBase64String(hash)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }

}
