using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PrismLauncherMigrator
{
    public partial class StepOne : Window
    {
        string ExistingPrismFolder = null;

        public StepOne()
        {
            InitializeComponent();
            PrepareLists();
        }

        private async Task PrepareLists()
        {
            List<string> dataFolders = new List<string>();
            List<string> installations = new List<string>();

            List<string> folders = new List<string>();

            await Task.Run(() =>
            {
                folders = WalkDirectoryTree(folders);
                foreach (var folder in folders)
                {
                    if (folder.EndsWith("PrismLauncher"))
                    {
                        if (Directory.Exists(folder + @"\instances"))
                        {
                            ExistingPrismFolder = folder;
                        }
                    }
                    else if (Directory.Exists(folder + @"\instances"))
                    {
                        dataFolders.Add(folder);
                    }
                    else if (File.Exists(folder + @"\MultiMC.exe") || File.Exists(folder + @"\PolyMC.exe") || File.Exists(folder + @"\ManyMC.exe"))
                    {
                        installations.Add(folder);
                    }
                }
            });

            ListDataFolders.Items.Add("[ Do not migrate anything ]");
            foreach (var folder in dataFolders)
            {
                ListDataFolders.Items.Add(folder);
            }
            foreach (var folder in installations)
            {
                ListInstallations.Items.Add(folder);
            }

            ListInstallations.SelectAll();
            ListDataFolders.SelectedIndex = 0;

            ButtonMigrate.Content = "Migrate to PrismLauncher";
            ButtonMigrate.IsEnabled = true;
        }

        private List<string> WalkDirectoryTree(List<string> results)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                WalkDirectoryTree(new DirectoryInfo(drive.Name), results);
            }
            return results;
        }

        private void WalkDirectoryTree(System.IO.DirectoryInfo root, List<string> results)
        {
            System.IO.DirectoryInfo[] subDirs;
            try
            {
                subDirs = root.GetDirectories();
            }
            catch
            {
                return;
            }

            foreach (System.IO.DirectoryInfo dirInfo in subDirs)
            {
                string[] knownFolders = { "PolyMC", "ManyMC", "MultiMC", "PrismLauncher" };
                if (knownFolders.Any(s => dirInfo.Name.Equals(s)))
                {
                    results.Add(dirInfo.FullName);
                }

                WalkDirectoryTree(dirInfo, results);
            }
        }

        private void ListDataFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selection = ListDataFolders.Items[ListDataFolders.SelectedIndex].ToString();
            if (!selection.Equals("[ Do not migrate anything ]") && ExistingPrismFolder != null)
            {
                LabelStatus.Content = $"Warning: Will backup existing Prism data folder\n{ExistingPrismFolder}\nto\n{ExistingPrismFolder}.{App.Timestamp}.bak";
            }
            else
            {
                LabelStatus.Content = "";
            }
        }

        private void ButtonMigrate_Click(object sender, RoutedEventArgs e)
        {
            string dataFolder = ListDataFolders.Items[ListDataFolders.SelectedIndex].ToString();
            if (dataFolder.Equals("[ Do not migrate anything ]")) dataFolder = null;
            string[] installations = ListInstallations.SelectedItems.Cast<string>().ToArray();

            StepTwo stepTwo = new StepTwo(installations, dataFolder);
            Close();
            stepTwo.Show();
        }
    }
}
