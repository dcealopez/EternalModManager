using System;
using System.IO;
using System.Windows;

namespace EternalModManager
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Expected game folder (working directory)
        /// </summary>
        public static string GameFolder = ".";

        /// <summary>
        /// Determines if the program is restoring the backups or not
        /// </summary>
        public static bool IsRestoringBackups;

        /// <summary>
        /// Fired on startup
        /// </summary>
        /// <param name="e">startup event args</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Check for game
            if (!File.Exists(Path.Combine(GameFolder, "DOOMEternalx64vk.exe")))
            {
                MessageBox.Show("Can't find DOOMEternalx64vk.exe\nThis tool needs to be placed in the game folder.", "Missing game", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            // Check for tools
            if (!File.Exists(Path.Combine(GameFolder, "EternalModInjector.bat")))
            {
                MessageBox.Show("Can't find EternalModInjector.bat\nMake sure that the modding tools are installed.", "Missing tools", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            // Create the mods folder if it doesn't exist
            if (!Directory.Exists(Path.Combine(GameFolder, "Mods")))
            {
                if (MessageBox.Show("This tool needs a mods folder in your game folder to place mods, and this folder is currently missing.\n\n" +
                    "In order to proceed the folder needs to be created, do you want to create it?", "Mods folder", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(GameFolder, "Mods"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error while creating the mods folder\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(1);
                    }
                }
                else
                {
                    Environment.Exit(0);
                }
            }

            // Create the disabled mods folder if it doesn't exist
            if (!Directory.Exists(Path.Combine(GameFolder, "DisabledMods")))
            {
                if (MessageBox.Show("This tool needs a disabled mods folder in your game folder to place disabled mods, and this folder is currently missing.\n\n" +
                    "In order to proceed the folder needs to be created, do you want to create it?", "Disabled mods folder", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(GameFolder, "DisabledMods"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error while creating the disabled mods folder\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(1);
                        return;
                    }
                }
                else
                {
                    Environment.Exit(0);
                    return;
                }
            }
        }
    }
}
