using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EternalModManager
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Watcher for the Mods folder
        /// </summary>
        private FileSystemWatcher ModsFolderWatcher;

        /// <summary>
        /// Watcher for the disabled mods folder
        /// </summary>
        private FileSystemWatcher DisabledModsFolderWatcher;

        /// <summary>
        /// Whether or not to discard the folder watcher events
        /// </summary>
        private bool DiscardFolderWatcherEvents;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the list box
            FillModsListBox();
            InitializeFileSystemWatchers();

            // On close event, to prevent closing the application on certain ocassions
            Closing += MainWindow_Closing;
        }

        /// <summary>
        /// Main window closing event
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.IsRestoringBackups)
            {
                e.Cancel = true;
                return;
            }

            foreach (var childWindow in OwnedWindows)
            {
                (childWindow as Window).Close();
            }
        }

        /// <summary>
        /// Initializes the FS watchers for the Mods and Disabled mods folders
        /// </summary>
        public void InitializeFileSystemWatchers()
        {
            ModsFolderWatcher = new FileSystemWatcher(Path.Combine(App.GameFolder, "Mods"))
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false,
            };

            DisabledModsFolderWatcher = new FileSystemWatcher(Path.Combine(App.GameFolder, "DisabledMods"))
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            ModsFolderWatcher.Created += new FileSystemEventHandler(ModsFolderChange);
            ModsFolderWatcher.Deleted += new FileSystemEventHandler(ModsFolderChange);
            ModsFolderWatcher.Renamed += new RenamedEventHandler(ModsFolderChange);
            ModsFolderWatcher.Changed += new FileSystemEventHandler(ModsFolderChange);

            DisabledModsFolderWatcher.Created += new FileSystemEventHandler(DisabledModsFolderChange);
            DisabledModsFolderWatcher.Deleted += new FileSystemEventHandler(DisabledModsFolderChange);
            DisabledModsFolderWatcher.Renamed += new RenamedEventHandler(DisabledModsFolderChange);
            DisabledModsFolderWatcher.Changed += new FileSystemEventHandler(DisabledModsFolderChange);
        }

        /// <summary>
        /// Mods folder file change event handler
        /// </summary>
        /// <param name="fsObject">FS object</param>
        /// <param name="eventArgs">event args</param>
        protected void ModsFolderChange(object fsObject, FileSystemEventArgs eventArgs)
        {
            if (DiscardFolderWatcherEvents)
            {
                return;
            }

            FillModsListBox(eventArgs.FullPath, eventArgs.ChangeType);
        }

        /// <summary>
        /// Disabled mods folder file change event handler
        /// </summary>
        /// <param name="fsObject">FS object</param>
        /// <param name="eventArgs">event args</param>
        protected void DisabledModsFolderChange(object fsObject, FileSystemEventArgs eventArgs)
        {
            if (DiscardFolderWatcherEvents)
            {
                return;
            }

            FillModsListBox(eventArgs.FullPath, eventArgs.ChangeType);
        }

        /// <summary>
        /// Fills the specified list box with the mod files in the specified mods folder
        /// </summary>
        /// <param name="changedFile">file that was changed</param>
        /// <param name="changeType">the type of change the file went through</param>
        public void FillModsListBox(string changedFile = null, WatcherChangeTypes changeType = WatcherChangeTypes.All)
        {
            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                // Cache the multiplayer safe and valid mod checks, so that
                // we don't do it if we don't have to, to improve performance
                Dictionary<string, Tuple<bool, bool>> fileSafetyCache = new Dictionary<string, Tuple<bool, bool>>();

                if (changedFile != null)
                {
                    foreach (Mod mod in ModListBox.Items)
                    {
                        fileSafetyCache.Add(mod.FullPath, new Tuple<bool, bool>(mod.IsOnlineSafe, mod.IsValid));
                    }
                }

                ModListBox.Items.Clear();

                // Enabled mods first
                foreach (var file in Directory.EnumerateFiles(Path.Combine(App.GameFolder, "Mods"), "*.zip", SearchOption.TopDirectoryOnly))
                {
                    // Check mod multiplayer safety if necessary
                    bool isMultiplayerSafe = false;
                    bool isValid = true;

                    if (changedFile != null && changedFile != file)
                    {
                        Tuple<bool, bool> cachedResults;
                        fileSafetyCache.TryGetValue(file, out cachedResults);

                        if (cachedResults != null)
                        {
                            isMultiplayerSafe = cachedResults.Item1;
                            isValid = cachedResults.Item2;
                        }
                    }

                    if (changedFile == null
                        || (changedFile == file
                            && (changeType == WatcherChangeTypes.Changed
                            || changeType == WatcherChangeTypes.Created)))
                    {
                        try
                        {
                            using (var zipArchive = ZipFile.OpenRead(file))
                            {
                                isMultiplayerSafe = OnlineSafety.IsModSafeForOnline(zipArchive);
                            }
                        }
                        catch
                        {
                            isValid = false;
                        }
                    }

                    var fileName = Path.GetFileName(file);
                    var mod = new Mod(file, fileName, true, isMultiplayerSafe);
                    mod.IsValid = isValid;

                    ModListBox.Items.Add(mod);
                }

                // Disabled mods
                foreach (var file in Directory.EnumerateFiles(Path.Combine(App.GameFolder, "DisabledMods"), "*.zip", SearchOption.TopDirectoryOnly))
                {
                    // Check mod multiplayer safety if necessary
                    bool isMultiplayerSafe = false;
                    bool isValid = true;

                    if (changedFile != null && changedFile != file)
                    {
                        Tuple<bool, bool> cachedResults;
                        fileSafetyCache.TryGetValue(file, out cachedResults);

                        if (cachedResults != null)
                        {
                            isMultiplayerSafe = cachedResults.Item1;
                            isValid = cachedResults.Item2;
                        }
                    }

                    if (changedFile == null
                        || (changedFile == file
                            && (changeType == WatcherChangeTypes.Changed
                            || changeType == WatcherChangeTypes.Created)))
                    {
                        try
                        {
                            using (var zipArchive = ZipFile.OpenRead(file))
                            {
                                isMultiplayerSafe = OnlineSafety.IsModSafeForOnline(zipArchive);
                            }
                        }
                        catch
                        {
                            isValid = false;
                        }
                    }

                    var fileName = Path.GetFileName(file);
                    var mod = new Mod(file, fileName, false, isMultiplayerSafe);
                    mod.IsValid = isValid;

                    ModListBox.Items.Add(mod);
                }
            });
        }

        /// <summary>
        /// Mods list selection changed event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void ModListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            if (ModListBox.SelectedItem != null)
            {
                Mod mod = ModListBox.SelectedItem as Mod;
                DisplayModInformation(mod);
            }
            else
            {
                HideModInformation();
            }
        }

        /// <summary>
        /// Inject mods & play button click event handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void InjectAndPlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folder if it doesn't exist
            if (!Utils.CheckModsDirectory("Mods"))
            {
                return;
            }

            // If we can't find EternalModInjector.bat, warn the user
            if (!File.Exists(Path.Combine(App.GameFolder, "EternalModInjector.bat")))
            {
                MessageBox.Show("Can't find EternalModInjector.bat\nMake sure that the modding tools are installed.", "Missing tools", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var process = new Process())
            {
                var processStartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    WorkingDirectory = App.GameFolder,
                    FileName = "cmd.exe",
                    Arguments = "/c " + Path.Combine(App.GameFolder, "EternalModInjector.bat")
                };

                process.StartInfo = processStartInfo;
                process.Start();
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Hides the mod information section
        /// </summary>
        private void HideModInformation()
        {
            ModNameTextBlock.Text = "-";
            ModAuthorsTextBlock.Text = "-";
            ModDescriptionTextBlock.Text = "-";
            ModVersionTextBlock.Text = "-";
            ModLoaderVersionTextBlock.Text = "-";
            ModLoadPriorityTextBlock.Text = "-";
            ModMultiplayerSafeLabel.Content = "";
        }

        /// <summary>
        /// Displays the mod information section, filling it with the specified mod information
        /// </summary>
        /// <param name="modFilePath">mod file path</param>
        private void DisplayModInformation(Mod mod)
        {
            var modFilePath = Path.Combine(App.GameFolder, mod.IsEnabled ? "Mods" : "DisabledMods", mod.FileName);

            if (!File.Exists(modFilePath))
            {
                return;
            }

            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (!mod.IsValid)
                    {
                        ModMultiplayerSafeLabel.Content = "Invalid .zip file.";
                        ModMultiplayerSafeLabel.Foreground = Brushes.Red;
                    }
                    else
                    {
                        if (mod.IsOnlineSafe)
                        {
                            ModMultiplayerSafeLabel.Content = "This mod is safe for multiplayer.";
                            ModMultiplayerSafeLabel.Foreground = Brushes.Green;
                        }
                        else
                        {
                            if (mod.IsGoingToBeLoaded)
                            {
                                ModMultiplayerSafeLabel.Content = "This mod is not safe for multiplayer. Battlemode will be disabled if this mod is enabled.";
                                ModMultiplayerSafeLabel.Foreground = Brushes.Red;
                            }
                            else
                            {
                                ModMultiplayerSafeLabel.Content = "This mod is not safe for multiplayer. It will not be loaded.";
                                ModMultiplayerSafeLabel.Foreground = Brushes.Orange;
                            }
                        }
                    }
                });

                try
                {
                    using (var zipArchive = ZipFile.OpenRead(modFilePath))
                    {
                        var eternalModJsonFile = zipArchive.GetEntry("EternalMod.json");

                        if (eternalModJsonFile == null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ModNameTextBlock.Text = Path.GetFileName(modFilePath);
                                ModAuthorsTextBlock.Text = "Unknown.";
                                ModDescriptionTextBlock.Text = "Not specified.";
                                ModVersionTextBlock.Text = "Not specified.";
                                ModLoaderVersionTextBlock.Text = "Unknown.";
                                ModLoadPriorityTextBlock.Text = "0";
                            });
                        }
                        else
                        {
                            ModInfo modInfo = null;

                            try
                            {
                                var stream = eternalModJsonFile.Open();
                                byte[] eternalModJsonFileBytes = null;

                                using (var memoryStream = new MemoryStream())
                                {
                                    stream.CopyTo(memoryStream);
                                    eternalModJsonFileBytes = memoryStream.ToArray();
                                }

                                // Parse the JSON
                                var serializerSettings = new JsonSerializerSettings();
                                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                modInfo = JsonConvert.DeserializeObject<ModInfo>(Encoding.UTF8.GetString(eternalModJsonFileBytes), serializerSettings);
                            }
                            catch (Exception)
                            {
                                // Don't do anything, will be handled by the finally block
                            }
                            finally
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ModNameTextBlock.Text = modInfo == null || modInfo.Name == null ? Path.GetFileName(modFilePath) : modInfo.Name;
                                    ModAuthorsTextBlock.Text = modInfo == null || modInfo.Author == null ? "Unknown." : modInfo.Author;
                                    ModDescriptionTextBlock.Text = modInfo == null || modInfo.Description == null ? "Not specified." : modInfo.Description;
                                    ModVersionTextBlock.Text = modInfo == null || modInfo.Version == null ? "Not specified." : modInfo.Version;
                                    ModLoaderVersionTextBlock.Text = modInfo == null || modInfo.RequiredVersion == 0 ? "Unknown." : modInfo.RequiredVersion.ToString();
                                    ModLoadPriorityTextBlock.Text = modInfo == null || modInfo.LoadPriority == 0 ? "0" : modInfo.LoadPriority.ToString();
                                });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // No need to do anything
                }
            });
        }

        /// <summary>
        /// Fired when a mod's checkbox is checked/unchecked
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void ModCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            // Determine the source and destination directories depending on
            // the checked / unchecked mods that are selected
            // And move them to the corresponding directories
            string sourceDir = "Mods";
            string destDir = "DisabledMods";

            if (ModListBox.SelectedItems != null && ModListBox.SelectedItems.Count > 1)
            {
                ((sender as CheckBox).DataContext as Mod).IsEnabled = !((sender as CheckBox).DataContext as Mod).IsEnabled;

                foreach (var selectedMod in ModListBox.SelectedItems)
                {
                    if (!(selectedMod as Mod).IsEnabled)
                    {
                        sourceDir = "DisabledMods";
                        destDir = "Mods";
                        break;
                    }
                }

                foreach (var selectedMod in ModListBox.SelectedItems)
                {
                    var filePath = Path.Combine(App.GameFolder, sourceDir, (selectedMod as Mod).FileName);

                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    try
                    {
                        File.Move(filePath, Path.Combine(App.GameFolder, destDir, (selectedMod as Mod).FileName));
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error while moving mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }
            }
            else
            {
                var mod = (sender as CheckBox).DataContext as Mod;
                sourceDir = mod.IsEnabled ? "DisabledMods" : "Mods";
                destDir = mod.IsEnabled ? "Mods" : "DisabledMods";
                var filePath = Path.Combine(App.GameFolder, sourceDir, mod.FileName);

                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"The selected mod doesn't exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    File.Move(filePath, Path.Combine(App.GameFolder, destDir, mod.FileName));
                }
                catch(Exception)
                {
                    MessageBox.Show("Error while moving mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Fired when the advanced options button is clicked
        /// Opens the advanced options menu
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void AdvancedOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            var advancedOptionsWindow = new AdvancedOptionsWindow();
            advancedOptionsWindow.Owner = this;
            advancedOptionsWindow.Show();
            advancedOptionsWindow.Focus();
        }

        /// <summary>
        /// Fired when entering the drag area in the list box
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void ModListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
        }

        /// <summary>
        /// Fired when files are dropped in the list box
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void ModListBox_Drop(object sender, DragEventArgs e)
        {
            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            // Add all the mod files, if valid
            foreach (var file in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                var modFileName = Path.GetFileName(file);

                if (Path.GetExtension(file) != ".zip")
                {
                    MessageBox.Show($"Skipping \"{modFileName}\", unsupported format.", "Add mods", MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                if (File.Exists(Path.Combine(App.GameFolder, "Mods", modFileName)))
                {
                    if (MessageBox.Show($"There is a mod with the name \"{modFileName}\" already installed, do you want to overwrite it?", "Add mods", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                    {
                        continue;
                    }
                }

                try
                {
                    File.Move(file, Path.Combine(App.GameFolder, "Mods", modFileName));
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while adding mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }
        }

        /// <summary>
        /// Fired when the install mods context menu button is clicked
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void InstallModButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            // Open a file dialog to let the user search for zipped mods
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Zip Files (*.zip)|*.zip",
                FilterIndex = 1,
                Multiselect = true,
                Title = "Add a mod from a Zip file"
            };

            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            // Attempt to add them
            foreach (var file in openFileDialog.FileNames)
            {
                var modFileName = Path.GetFileName(file);

                if (File.Exists(Path.Combine(App.GameFolder, "Mods", modFileName)))
                {
                    if (MessageBox.Show($"There is a mod with the name \"{modFileName}\" already installed, do you want to overwrite it?", "Add mods", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                    {
                        continue;
                    }
                }

                try
                {
                    File.Move(file, Path.Combine(App.GameFolder, "Mods", modFileName));
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while adding mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }
        }

        /// <summary>
        /// Fired when the delete selected mods context menu button is clicked
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            if (ModListBox.SelectedItems == null || ModListBox.SelectedItems.Count == 0)
            {
                return;
            }

            // Warn the user first
            if (ModListBox.SelectedItems.Count == 1)
            {
                if (MessageBox.Show($"Are you sure you want to delete the selected mod?\n\n{(ModListBox.SelectedItem as Mod).FileName}", "Delete mods", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                {
                    return;
                }
            }
            else
            {
                if (MessageBox.Show($"Are you sure you want to delete the selected mods?", "Delete mods", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                {
                    return;
                }
            }

            // Build the mods to delete list and then delete them
            List<string> modsToDelete = new List<string>();

            foreach (var item in ModListBox.SelectedItems)
            {
                var mod = (item as Mod);
                var path = Path.Combine(App.GameFolder, mod.IsEnabled ? "Mods" : "DisabledMods", mod.FileName);

                if (!File.Exists(path))
                {
                    MessageBox.Show($"{mod.FileName} doesn't exist.", "Delete mods", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                modsToDelete.Add(path);
            }

            foreach (var modPath in modsToDelete)
            {
                try
                {
                    File.Delete(modPath);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while deleting mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }
        }

        /// <summary>
        /// Fired when the list box context menu is opened
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (ModListBox.SelectedItems == null || ModListBox.SelectedItems.Count == 0)
            {
                DeleteSelectedButton.IsEnabled = false;
                EnableDisableSelectedButton.IsEnabled = false;
                OpenFileLocationButton.IsEnabled = false;
                return;
            }

            OpenFileLocationButton.IsEnabled = ModListBox.SelectedItems.Count == 1;
            DeleteSelectedButton.IsEnabled = true;
            EnableDisableSelectedButton.IsEnabled = true;

            if (ModListBox.SelectedItems != null)
            {
                EnableDisableSelectedButton.Header = "Disable selected";

                foreach (var selectedMod in ModListBox.SelectedItems)
                {
                    if (!(selectedMod as Mod).IsEnabled)
                    {
                        EnableDisableSelectedButton.Header = "Enable selected";
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Fired when the enable/disable context menu button is clicked
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void EnableDisableSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            // Discard the folder watcher events for now, to avoid multiple unnecessary mod .zip file reads
            DiscardFolderWatcherEvents = true;

            // Determine the source and destination directories depending on the
            // mods that are checked / unchecked, and then enable or disable them
            string sourceDir = "Mods";
            string destDir = "DisabledMods";

            if (ModListBox.SelectedItems != null)
            {
                foreach (var selectedMod in ModListBox.SelectedItems)
                {
                    if (!(selectedMod as Mod).IsEnabled)
                    {
                        sourceDir = "DisabledMods";
                        destDir = "Mods";
                        break;
                    }
                }

                foreach (var selectedMod in ModListBox.SelectedItems)
                {
                    var filePath = Path.Combine(App.GameFolder, sourceDir, (selectedMod as Mod).FileName);

                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    try
                    {
                        File.Move(filePath, Path.Combine(App.GameFolder, destDir, (selectedMod as Mod).FileName));
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error while moving mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }
            }

            // Refill the mods list box and re-enable the folder watcher events
            FillModsListBox();
            DiscardFolderWatcherEvents = false;
        }

        /// <summary>
        /// Fired when the enable / disable all checkbox is checked/unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableDisableAllCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            // Discard the folder watcher events for now, to avoid multiple unnecessary mod .zip file reads
            DiscardFolderWatcherEvents = true;

            // Determine the source and destination directories and enable/disable all the mods
            string sourceDir = (sender as CheckBox).IsChecked == true ? "DisabledMods" : "Mods";
            string destDir = (sender as CheckBox).IsChecked == true ? "Mods" : "DisabledMods";

            foreach (var selectedMod in ModListBox.Items)
            {
                var filePath = Path.Combine(App.GameFolder, sourceDir, (selectedMod as Mod).FileName);

                if (!File.Exists(filePath))
                {
                    continue;
                }

                try
                {
                    File.Move(filePath, Path.Combine(App.GameFolder, destDir, (selectedMod as Mod).FileName));
                }
                catch (Exception)
                {
                    MessageBox.Show("Error while moving mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }

            // Refill the mods list box and re-enable the folder watcher events
            FillModsListBox();
            DiscardFolderWatcherEvents = false;
        }

        /// <summary>
        /// Fired when a key is pressed while the list box has focus
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void ModListBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Only handle the space key, ignore if key modifiers are being applied
            if ((e.Key != Key.Space && e.Key != Key.Delete) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                return;
            }

            // Prompt to create the folders if they don't exist
            if (!Utils.CheckModsDirectory("Mods") || !Utils.CheckModsDirectory("DisabledMods"))
            {
                return;
            }

            if (ModListBox.SelectedItems == null)
            {
                return;
            }

            e.Handled = true;

            // Discard the folder watcher events for now, to avoid multiple unnecessary mod .zip file reads
            DiscardFolderWatcherEvents = true;

            // Determine the source and destination directories and enable / disable the mods
            string sourceDir = "Mods";
            string destDir = "DisabledMods";

            if (ModListBox.SelectedItems != null)
            {
                foreach (var selectedMod in ModListBox.SelectedItems)
                {
                    if (!(selectedMod as Mod).IsEnabled)
                    {
                        sourceDir = "DisabledMods";
                        destDir = "Mods";
                        break;
                    }
                }

                foreach (var selectedMod in ModListBox.SelectedItems)
                {
                    var filePath = Path.Combine(App.GameFolder, sourceDir, (selectedMod as Mod).FileName);

                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    if (e.Key == Key.Space)
                    {
                        try
                        {
                            File.Move(filePath, Path.Combine(App.GameFolder, destDir, (selectedMod as Mod).FileName));
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Error while moving mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }
                    }
                    else if (e.Key == Key.Delete)
                    {
                        // Warn the user first
                        if (ModListBox.SelectedItems.Count == 1)
                        {
                            if (MessageBox.Show($"Are you sure you want to delete the selected mod?\n\n{(ModListBox.SelectedItem as Mod).FileName}", "Delete mods", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                            {
                                DiscardFolderWatcherEvents = false;
                                return;
                            }
                        }
                        else
                        {
                            if (MessageBox.Show($"Are you sure you want to delete the selected mods?", "Delete mods", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                            {
                                DiscardFolderWatcherEvents = false;
                                return;
                            }
                        }

                        // Build the mods to delete list and then delete them
                        List<string> modsToDelete = new List<string>();

                        foreach (var item in ModListBox.SelectedItems)
                        {
                            var mod = (item as Mod);
                            var path = Path.Combine(App.GameFolder, mod.IsEnabled ? "Mods" : "DisabledMods", mod.FileName);

                            if (!File.Exists(path))
                            {
                                MessageBox.Show($"{mod.FileName} doesn't exist.", "Delete mods", MessageBoxButton.OK, MessageBoxImage.Error);
                                continue;
                            }

                            modsToDelete.Add(path);
                        }

                        foreach (var modPath in modsToDelete)
                        {
                            try
                            {
                                File.Delete(modPath);
                            }
                            catch (Exception)
                            {
                                MessageBox.Show("Error while deleting mod file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                continue;
                            }
                        }
                    }
                }

                // Refill the mods list box and re-enable the folder watcher events
                FillModsListBox();
                DiscardFolderWatcherEvents = false;
            }
        }

        /// <summary>
        /// Open file location context menu button click
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModListBox.SelectedItem == null)
            {
                return;
            }

            Process.Start(Path.GetDirectoryName((ModListBox.SelectedItem as Mod).FullPath));
        }
    }
}
