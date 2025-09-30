using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyAndFeel.Utils
{
    public static class ShellHelper
    {
        public static async Task<string> RunCommandAsync(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C " + command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            string outp = await proc.StandardOutput.ReadToEndAsync();
            string err = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0)
                throw new Exception($"Command failed ({proc.ExitCode}): {err}");

            return outp;
        }
    }

}
