using System;
using System.Windows;

namespace PrismLauncherMigrator
{
    public partial class App : Application
    {
        public static long Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

        public App()
        {
        }
    }
}
