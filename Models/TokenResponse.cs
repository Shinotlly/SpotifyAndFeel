using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace SpotifyAndFeel.Models
{
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
        public string Scope { get; set; }
    }

}
