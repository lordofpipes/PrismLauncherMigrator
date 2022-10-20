using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using Path = System.IO.Path;

namespace PrismLauncherMigrator
{
    public partial class StepTwo : Window
    {
        string DataFolder;
        string[] InstallationsToRemove;

        public StepTwo(string[] installationsToRemove, string dataFolder)
        {
            InstallationsToRemove = installationsToRemove;
            DataFolder = dataFolder;
            InitializeComponent();
            Start();
        }

        private async void Start()
        {
            foreach (var installationPath in InstallationsToRemove)
            {
                string fileName = installationPath + @"\uninstall.exe";
                if (!File.Exists(fileName))
                {
                    continue;
                }
                LabelStatus.Content = $"Please follow the steps for uninstallation...";

                using (var process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.Start();

                    // uninstaller files will copy themselves to a temp folder
                    // so we can't just wait until the process has ended
                    // we actually have to check if the uninstallation has finished
                    while (File.Exists(fileName))
                    {
                        await Task.Delay(500);
                    }
                }
            }

            LabelStatus.Content = $"Obtaining PrismLauncher download link...";

            string url = "https://api.github.com/repos/PrismLauncher/PrismLauncher/releases";

            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json, application/octet-stream");
            client.DefaultRequestHeaders.Add("User-Agent", "PrismLauncher Migration Tool");

            string response = await client.GetStringAsync(url);

            JsonNode value = JsonNode.Parse(response);
            JsonArray assets = value[0]["assets"].AsArray();
            JsonNode asset = assets.First(asset =>
            {
                string filename = (string)asset["name"];
                return filename.EndsWith(".exe") && filename.Contains("-Windows-Setup-") && !filename.Contains("Legacy");
            });

            string downloadUrl = (string)asset["browser_download_url"];

            LabelStatus.Content = $"Downloading {downloadUrl}";

            string installerPath = Path.GetTempPath() + $"prism-installer-{App.Timestamp}.exe";

            HttpResponseMessage exeResponse = await client.GetAsync(downloadUrl);
            using (var fs = new FileStream(installerPath, FileMode.OpenOrCreate))
            {
                await exeResponse.Content.CopyToAsync(fs);
            }

            LabelStatus.Content = "Migrating your folders...";

            if (DataFolder != null)
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string newPath = appdata + @"\PrismLauncher";
                if (Directory.Exists(newPath))
                {
                    Directory.Move(newPath, newPath + $".{App.Timestamp}.bak");
                }
                Directory.Move(DataFolder, newPath);
            }

            LabelStatus.Content = "Please follow the installation instructions...";

            using (var process = new Process())
            {
                process.StartInfo.FileName = installerPath;
                process.Start();

                await process.WaitForExitAsync();
            }

            StepThree stepThree = new StepThree();
            Close();
            stepThree.Show();
        }
    }
}
