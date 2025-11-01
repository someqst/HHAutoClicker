using System.Diagnostics;

using HHAutoClicker.Services;


namespace HHAutoClicker
{
    internal class Program
    {

        static bool RunPowerShell(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };
                using var p = Process.Start(psi);
                p.WaitForExit();

                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout);
                if (!string.IsNullOrWhiteSpace(stderr)) Console.WriteLine(stderr);

                return p.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка (PowerShell): " + ex.Message);
                return false;
            }
        }

        public static async Task Main()
        {

            if (!RunPowerShell("./playwright.ps1 --version"))
            {
                Console.WriteLine("Устанавливаю playwright");
                RunPowerShell("./playwright.ps1 --install");
            }
            var app = new App();
            await app.RunAsync();
        }
    }
}
