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
        /// Mod injector settings
        /// </summary>
        public static ModInjectorSettings ModInjectorSettings { get; set; }

        /// <summary>
        /// Original mod injector settings object, to track setting changes
        /// </summary>
        private static ModInjectorSettings OriginalModInjectorSettings;

        /// <summary>
        /// Advanced Options Window constructor
        /// </summary>
        public AdvancedOptionsWindow()
        {
            InitializeComponent();

            // Set the data context for the injector settings controls to this window
            // so that they can be bound to the injector settings object properties
            AutoLaunchGameCheckBox.DataContext = this;
            ResetBackupsCheckBox.DataContext = this;
            OnlineSafeCheckbox.DataContext = this;
            SlowModeCheckBox.DataContext = this;
            TextureCompressionCheckBox.DataContext = this;
            VerboseCheckBox.DataContext = this;
            MultiThreadingCheckBox.DataContext = this;
            GameParametersTextBox.DataContext = this;

            // Load the current mod injector settings file
            var settingsFilePath = Path.Combine(App.GameFolder, "EternalModInjector Settings.txt");

            if (!File.Exists(settingsFilePath))
            {
                InjectorSettingsGrid.IsEnabled = false;
                MessageBox.Show("Mod injector settings file not found.\n\nThe mod injector settings section will not be available until the mod injector is run at least once.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadModInjectorSettingsFile();

            // Bind the events now
            AutoLaunchGameCheckBox.Checked += InjectorSettingsControlChanged;
            AutoLaunchGameCheckBox.Unchecked += InjectorSettingsControlChanged;
            ResetBackupsCheckBox.Checked += InjectorSettingsControlChanged;
            ResetBackupsCheckBox.Unchecked += InjectorSettingsControlChanged;
            OnlineSafeCheckbox.Checked += InjectorSettingsControlChanged;
            OnlineSafeCheckbox.Unchecked += InjectorSettingsControlChanged;
            SlowModeCheckBox.Checked += InjectorSettingsControlChanged;
            SlowModeCheckBox.Unchecked += InjectorSettingsControlChanged;
            TextureCompressionCheckBox.Checked += InjectorSettingsControlChanged;
            TextureCompressionCheckBox.Unchecked += InjectorSettingsControlChanged;
            VerboseCheckBox.Checked += InjectorSettingsControlChanged;
            VerboseCheckBox.Unchecked += InjectorSettingsControlChanged;
            MultiThreadingCheckBox.Checked += InjectorSettingsControlChanged;
            MultiThreadingCheckBox.Unchecked += InjectorSettingsControlChanged;
            GameParametersTextBox.TextChanged += InjectorSettingsControlChanged;

            Loaded += AdvancedOptionsWindow_Loaded;
            Closing += AdvancedOptionsWindow_Closing;
        }

        /// <summary>
        /// Loads the mod injector settings file
        /// </summary>
        public static void LoadModInjectorSettingsFile()
        {
            var settingsFilePath = Path.Combine(App.GameFolder, "EternalModInjector Settings.txt");

            if (!File.Exists(settingsFilePath))
            {
                return;
            }

            // Load the settings from the file now
            ModInjectorSettings = new ModInjectorSettings();
            var injectorSettings = File.ReadAllLines(settingsFilePath).Where(line => line.StartsWith(":"));

            // Currently, all the settings are booleans or strings
            foreach (var setting in injectorSettings)
            {
                var settingData = setting.Split('=');

                if (settingData == null || settingData.Length == 0)
                {
                    continue;
                }

                bool settingValue = false;

                if (settingData.Length > 1)
                {
                    if (settingData[0] != ":GAME_PARAMETERS")
                    {
                        int value = 0;
                        int.TryParse(settingData[1], out value);

                        settingValue = value == 0 ? false : true;
                    }
                }

                switch (settingData[0])
                {
                    case ":AUTO_LAUNCH_GAME":
                        ModInjectorSettings.AutomaticGameLaunch = settingValue;
                        break;
                    case ":RESET_BACKUPS":
                        ModInjectorSettings.ResetBackups = settingValue;
                        break;
                    case ":VERBOSE":
                        ModInjectorSettings.Verbose = settingValue;
                        break;
                    case ":SLOW":
                        ModInjectorSettings.SlowMode = settingValue;
                        break;
                    case ":COMPRESS_TEXTURES":
                        ModInjectorSettings.CompressTextures = settingValue;
                        break;
                    case ":ONLINE_SAFE":
                        ModInjectorSettings.OnlineSafe = settingValue;
                        break;
                    case ":DISABLE_MULTITHREADING":
                        ModInjectorSettings.DisableMultiThreading = settingValue;
                        break;
                    case ":GAME_PARAMETERS":
                        {
                            if (settingData.Length > 1)
                            {
                                ModInjectorSettings.GameParameters = settingData[1];
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            OriginalModInjectorSettings = ModInjectorSettings.Clone();
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
            MainGrid.IsEnabled = false;
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
                    MainGrid.IsEnabled = true;
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

        /// <summary>
        /// Fired when an injector settings control has changed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void InjectorSettingsControlChanged(object sender, RoutedEventArgs e)
        {
            SaveInjectorSettingsButton.IsEnabled = !ModInjectorSettings.IsEqualTo(OriginalModInjectorSettings);
        }

        /// <summary>
        /// Fired when the "Save mod injector settings" button is clicked
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void SaveInjectorSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsFilePath = Path.Combine(App.GameFolder, "EternalModInjector Settings.txt");

            if (!File.Exists(settingsFilePath))
            {
                InjectorSettingsGrid.IsEnabled = false;
                SaveInjectorSettingsButton.IsEnabled = false;
                MessageBox.Show("Mod injector settings file not found.\n\nThe mod injector settings section will not be available until the mod injector is run at least once.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Load the settings from the file now
            string[] injectorSettings = null;

            try
            {
                injectorSettings = File.ReadAllLines(settingsFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while saving the mod injector settings\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (injectorSettings == null)
            {
                return;
            }

            // Change the settings
            for (int i = 0; i < injectorSettings.Length; i++)
            {
                // If we found an empty line, this means we reached the end of the setting sectiont
                if (string.IsNullOrEmpty(injectorSettings[i].Trim()))
                {
                    break;
                }

                var settingData = injectorSettings[i].Split('=');

                if (settingData == null || settingData.Length == 0)
                {
                    continue;
                }

                switch (settingData[0])
                {
                    case ":AUTO_LAUNCH_GAME":
                        injectorSettings[i] = $":AUTO_LAUNCH_GAME={(ModInjectorSettings.AutomaticGameLaunch ? "1" : "0")}";
                        break;
                    case ":RESET_BACKUPS":
                        injectorSettings[i] = $":RESET_BACKUPS={(ModInjectorSettings.ResetBackups ? "1" : "0")}";
                        break;
                    case ":VERBOSE":
                        injectorSettings[i] = $":VERBOSE={(ModInjectorSettings.Verbose ? "1" : "0")}";
                        break;
                    case ":DISABLE_MULTITHREADING":
                        injectorSettings[i] = $":DISABLE_MULTITHREADING={(ModInjectorSettings.DisableMultiThreading ? "1" : "0")}";
                        break;
                    case ":SLOW":
                        injectorSettings[i] = $":SLOW={(ModInjectorSettings.SlowMode ? "1" : "0")}";
                        break;
                    case ":COMPRESS_TEXTURES":
                        injectorSettings[i] = $":COMPRESS_TEXTURES={(ModInjectorSettings.CompressTextures ? "1" : "0")}";
                        break;
                    case ":ONLINE_SAFE":
                        injectorSettings[i] = $":ONLINE_SAFE={(ModInjectorSettings.OnlineSafe ? "1" : "0")}";

                        // Refresh the mod list box
                        (Owner as MainWindow).FillModsListBox();
                        break;
                    case ":GAME_PARAMETERS":
                        injectorSettings[i] = $":GAME_PARAMETERS={ModInjectorSettings.GameParameters.Trim()}";
                        break;
                    default:
                        break;
                }
            }

            try
            {
                // Write the settings file file
                File.WriteAllLines(settingsFilePath, injectorSettings);
                SaveInjectorSettingsButton.IsEnabled = false;
                OriginalModInjectorSettings = ModInjectorSettings.Clone();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while saving the mod injector settings\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// Fired when the "Copy EternalMod.json template" button is clicked
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void CopyTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("{\r\n\t\"name\":\"\",\r\n\t\"author\":\"\",\r\n\t\"description\":\"\",\r\n\t\"version\":\"\",\r\n\t\"loadPriority\":0,\r\n\t\"requiredVersion\":8\r\n}");
            MessageBox.Show("EternalMod.json template copied to your clipboard.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
