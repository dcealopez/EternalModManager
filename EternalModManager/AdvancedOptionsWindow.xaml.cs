using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EternalModManager
{
    /// <summary>
    /// Lógica de interacción para AdvancedOptionsWindow.xaml
    /// </summary>
    public partial class AdvancedOptionsWindow : Window
    {
        /// <summary>
        /// Advanced Options Window constructor
        /// </summary>
        public AdvancedOptionsWindow()
        {
            InitializeComponent();

            Loaded += AdvancedOptionsWindow_Loaded;
            Closing += AdvancedOptionsWindow_Closing;
        }

        /// <summary>
        /// Fired when the window is loaded
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void AdvancedOptionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Disable the parent window main grid
            (Owner as MainWindow).MainGrid.IsEnabled = false;
        }

        /// <summary>
        /// Fired when attempting to close the window
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void AdvancedOptionsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.IsRestoringBackups)
            {
                MessageBox.Show("Please wait until the backup restore process is finished.", "Restoring backups", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            (Owner as MainWindow).MainGrid.IsEnabled = true;
            Owner = null;
        }

        /// <summary>
        /// Open enabled mods folder button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void OpenEnabledModsButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folder if it doesn't exist
            if (!CheckModsDirectory("Mods"))
            {
                return;
            }

            Process.Start(System.IO.Path.Combine(App.GameFolder, "Mods"));
        }

        /// <summary>
        /// Open disabled mods folder button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void OpenDisabledModsButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folder if it doesn't exist
            if (!CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            Process.Start(System.IO.Path.Combine(App.GameFolder, "DisabledMods"));
        }

        /// <summary>
        /// Open game folder button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void OpenGameFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(System.IO.Path.Combine(App.GameFolder));
        }

        /// <summary>
        /// Restore backups button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void RestoreBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"This will restore your game to vanilla state by restoring the unmodded backed up game files.\n\nThis process might take a while depending on the speed of your disk, so please be patient.\n\nAre you sure you want to continue?", "Restore backups", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
            {
                return;
            }

            // Disable the UI
            MainStackPanel.IsEnabled = false;
            RestoreBackupsButton.Content = "Restoring backups...";
            App.IsRestoringBackups = true;

            // Restore the backups asynchronously
            Task.Run(() =>
            {
                int restoredCount = 0;

                // Restore packagemapspec.json
                var packageMapSpecJsonBackupFilePath = System.IO.Path.Combine(App.GameFolder, "base", "packagemapspec.json.backup");

                try
                {
                    if (File.Exists(packageMapSpecJsonBackupFilePath))
                    {
                        File.Copy(packageMapSpecJsonBackupFilePath, packageMapSpecJsonBackupFilePath.Replace(".backup", ""), true);
                        restoredCount++;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while restoring backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Restore backups in the "base" directory
                foreach (var file in Directory.EnumerateFiles(System.IO.Path.Combine(App.GameFolder, "base"), "*.resources.backup", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        File.Copy(file, file.Replace(".backup", ""), true);
                        restoredCount++;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error while restoring backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }

                // Restore backups in the "base/game" directory (all directories)
                foreach (var file in Directory.EnumerateFiles(System.IO.Path.Combine(App.GameFolder, "base", "game"), "*.resources.backup", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Copy(file, file.Replace(".backup", ""), true);
                        restoredCount++;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error while restoring backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }

                // Restore backups in the "base/sound/soundbanks/pc" directory
                foreach (var file in Directory.EnumerateFiles(System.IO.Path.Combine(App.GameFolder, "base", "sound", "soundbanks", "pc"), "*.snd.backup", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        File.Copy(file, file.Replace(".backup", ""), true);
                        restoredCount++;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error while restoring backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }

                // Re-enable the UI
                Dispatcher.Invoke(() =>
                {
                    MainStackPanel.IsEnabled = true;
                    RestoreBackupsButton.Content = "Restore backups";
                    MessageBox.Show($"{restoredCount} backups were restored.", "Restore backups", MessageBoxButton.OK, MessageBoxImage.Information);
                });

                App.IsRestoringBackups = false;
            });
        }

        /// <summary>
        /// Reset backups button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void ResetBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"This will delete your backed up game files. Normally this should only be done if you accidentally backed up modded game files, or after a game update.\n\nThe next time mods are injected the backups will be re-created using the game files installed at that moment, so make sure to verify your game files before or after doing this.\n\nAre you sure you want to continue?", "Reset backups", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            int deletedCount = 0;

            // Delete packagemapspec.json.backup
            var packageMapSpecJsonBackupFilePath = System.IO.Path.Combine(App.GameFolder, "base", "packagemapspec.json.backup");

            try
            {
                if (File.Exists(packageMapSpecJsonBackupFilePath))
                {
                    File.Delete(packageMapSpecJsonBackupFilePath);
                    deletedCount++;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Delete backups in the "base" directory
            foreach (var file in Directory.EnumerateFiles(System.IO.Path.Combine(App.GameFolder, "base"), "*.resources.backup", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }

            // Delete backups in the "base/game" directory (all directories)
            foreach (var file in Directory.EnumerateFiles(System.IO.Path.Combine(App.GameFolder, "base", "game"), "*.resources.backup", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }

            // Delete backups in the "base/sound/soundbanks/pc" directory
            foreach (var file in Directory.EnumerateFiles(System.IO.Path.Combine(App.GameFolder, "base", "sound", "soundbanks", "pc"), "*.snd.backup", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while deleting backup file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }

            // Remove the resource references from EternalModInjector Settings.txt
            var settingsPath = System.IO.Path.Combine(App.GameFolder, "EternalModInjector Settings.txt");

            try
            {
                if (File.Exists(settingsPath))
                {
                    var linesToKeep = File.ReadAllLines(settingsPath).Where(line => line.StartsWith(":"));
                    File.Delete(settingsPath);
                    File.WriteAllLines(settingsPath, linesToKeep);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error while removing backup files from EternalModInjector Settings.txt", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            MessageBox.Show($"{deletedCount} backups were deleted.", "Reset backups", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Checks whether or not the specified mods folder exists
        /// If it doesn't, it will prompt the user to create it
        /// </summary>
        /// <param name="modsFolder">mods folder</param>
        /// <returns>true if the problem was resolved, false otherwise</returns>
        private bool CheckModsDirectory(string modsFolder)
        {
            // Prompt to create the folder if it doesn't exist
            if (!Directory.Exists(Path.Combine(App.GameFolder, modsFolder)))
            {
                if (MessageBox.Show($"The {modsFolder} folder has been deleted. In order for this application to function correctly, the folder must exist. Please restore the folder.\n\n" +
                    "Do you want to create it instead?", "Missing folder", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(App.GameFolder, modsFolder));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error while creating {modsFolder} folder\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
