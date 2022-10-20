using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace PrismLauncherMigrator
{
    public partial class StepThree : Window
    {
        public StepThree()
        {
            InitializeComponent();
        }

        private async void ButtonInstallJava8_Click(object sender, RoutedEventArgs e)
        {
            LabelStatus.Content = "Downloading Java...";

            ((Button)e.Source).IsEnabled = false;
            string installerPath = Path.GetTempPath() + $"openjdk-8-{App.Timestamp}.msi";

            await GetAdoptium("8", installerPath);
            LabelStatus.Content = "Please follow the installation instructions...";

            await RunInstaller(installerPath);

            LabelStatus.Content = "Finished installing Java 8.";
        }
        private async void ButtonInstallJava19_Click(object sender, RoutedEventArgs e)
        {
            LabelStatus.Content = "Downloading Java...";

            ((Button)e.Source).IsEnabled = false;
            string installerPath = Path.GetTempPath() + $"openjdk-19-{App.Timestamp}.msi";

            await GetAdoptium("19", installerPath);
            LabelStatus.Content = "Please follow the installation instructions...";

            await RunInstaller(installerPath);

            LabelStatus.Content = "Finished installing Java 19.";
        }

        private async void ButtonInstallJava17_Click(object sender, RoutedEventArgs e)
        {
            LabelStatus.Content = "Downloading Java...";

            ((Button)e.Source).IsEnabled = false;
            string installerPath = Path.GetTempPath() + $"openjdk-17-{App.Timestamp}.msi";

            await GetMicrosoftOpenJDK("17", installerPath);
            LabelStatus.Content = "Please follow the installation instructions...";

            await RunInstaller(installerPath);

            LabelStatus.Content = "Finished installing Java 17.";
        }

        private async Task RunInstaller(string installerPath)
        {
            using (var process = new Process())
            {
                // msi files are weird
                process.StartInfo.FileName = "msiexec";
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(installerPath);
                process.StartInfo.Arguments = $"/i {Path.GetFileName(installerPath)}";
                process.Start();

                await process.WaitForExitAsync();
            }
        }

        private string GetArchitecture()
        {
            string architecture = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

            var architectureNames = new Dictionary<string, string>()
            {
                { "x86", "x86" },
                { "x64", "x64" },
                { "amd64", "x64" },
                { "arm64", "aarch64" },
            };
            return architectureNames[architecture.ToLower()];
        }

        private async Task GetMicrosoftOpenJDK(string version, string installerPath)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Accept", "application/json, application/octet-stream");
            client.DefaultRequestHeaders.Add("User-Agent", "PrismLauncher Migration Tool");

            string architecture = GetArchitecture();

            // fallback to adoptium on x86
            if (architecture.Equals("x86"))
            {
                await GetAdoptium(version, installerPath);
                return;
            }
            string url = $"https://aka.ms/download-jdk/microsoft-jdk-{version}-windows-{architecture}.msi";

            HttpResponseMessage exeResponse = await client.GetAsync(url);
            using (var fs = new FileStream(installerPath, FileMode.OpenOrCreate))
            {
                await exeResponse.Content.CopyToAsync(fs);
            }
        }

        private async Task GetAdoptium(string version, string installerPath)
        {
            LabelStatus.Content = $"Obtaining Adoptium OpenJDK download link...";

            string architecture = GetArchitecture();

            string url = $"https://api.adoptium.net/v3/assets/latest/{version}/hotspot?architecture={architecture}&image_type=jdk&os=windows";

            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Accept", "application/json, application/octet-stream");
            client.DefaultRequestHeaders.Add("User-Agent", "PrismLauncher Migration Tool");

            string response = await client.GetStringAsync(url);

            JsonNode value = JsonNode.Parse(response);
            string downloadUrl = (string)value[0]["binary"]["installer"]["link"];

            LabelStatus.Content = $"Downloading {downloadUrl}";

            HttpResponseMessage exeResponse = await client.GetAsync(downloadUrl);
            using (var fs = new FileStream(installerPath, FileMode.OpenOrCreate))
            {
                await exeResponse.Content.CopyToAsync(fs);
            }
        }
    }
}
