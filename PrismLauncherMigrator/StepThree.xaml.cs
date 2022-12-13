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
            var installerPath = Path.GetTempPath() + $"openjdk-8-{App.Timestamp}.msi";

            await GetAdoptium("8", installerPath);
            LabelStatus.Content = "Please follow the installation instructions...";

            await RunInstaller(installerPath);

            LabelStatus.Content = "Finished installing Java 8.";
        }
        private async void ButtonInstallJava19_Click(object sender, RoutedEventArgs e)
        {
            LabelStatus.Content = "Downloading Java...";

            ((Button)e.Source).IsEnabled = false;
            var installerPath = Path.GetTempPath() + $"openjdk-19-{App.Timestamp}.msi";

            await GetAdoptium("19", installerPath);
            LabelStatus.Content = "Please follow the installation instructions...";

            await RunInstaller(installerPath);

            LabelStatus.Content = "Finished installing Java 19.";
        }

        private async void ButtonInstallJava17_Click(object sender, RoutedEventArgs e)
        {
            LabelStatus.Content = "Downloading Java...";

            ((Button)e.Source).IsEnabled = false;
            var installerPath = Path.GetTempPath() + $"openjdk-17-{App.Timestamp}.msi";

            await GetMicrosoftOpenJDK("17", installerPath);
            LabelStatus.Content = "Please follow the installation instructions...";

            await RunInstaller(installerPath);

            LabelStatus.Content = "Finished installing Java 17.";
        }

        private static async Task RunInstaller(string installerPath)
        {
            using var process = new Process();
            // msi files are weird
            process.StartInfo.FileName = "msiexec";
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(installerPath);
            process.StartInfo.Arguments = $"/i {Path.GetFileName(installerPath)}";
            process.Start();

            await process.WaitForExitAsync();
        }

        private static async Task GetMicrosoftOpenJDK(string version, string installerPath)
        {
            var client = new HttpClient();
            client.Timeout = System.TimeSpan.FromMinutes(10);

            client.DefaultRequestHeaders.Add("Accept", "application/json, application/octet-stream");
            client.DefaultRequestHeaders.Add("User-Agent", "PrismLauncher Migration Tool");

            var url = $"https://aka.ms/download-jdk/microsoft-jdk-{version}-windows-x64.msi";

            var exeResponse = await client.GetAsync(url);
            using var fs = new FileStream(installerPath, FileMode.OpenOrCreate);
            await exeResponse.Content.CopyToAsync(fs);
        }

        private async Task GetAdoptium(string version, string installerPath)
        {
            LabelStatus.Content = $"Obtaining Adoptium OpenJDK download link...";

            var url = $"https://api.adoptium.net/v3/assets/latest/{version}/hotspot?architecture=x64&image_type=jdk&os=windows";

            var client = new HttpClient();
            client.Timeout = System.TimeSpan.FromMinutes(10);

            client.DefaultRequestHeaders.Add("Accept", "application/json, application/octet-stream");
            client.DefaultRequestHeaders.Add("User-Agent", "PrismLauncher Migration Tool");

            var response = await client.GetStringAsync(url);

            var value = JsonNode.Parse(response);
            var downloadUrl = (string)value![0]!["binary"]!["installer"]!["link"]!;

            LabelStatus.Content = $"Downloading {downloadUrl}";

            var exeResponse = await client.GetAsync(downloadUrl);
            using var fs = new FileStream(installerPath, FileMode.OpenOrCreate);
            await exeResponse.Content.CopyToAsync(fs);
        }
    }
}
