using System;
using System.IO;
using System.Windows;

namespace EternalModManager
{
    /// <summary>
    /// Utils class
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Checks whether or not the specified mods folder exists
        /// If it doesn't, it will prompt the user to create it
        /// </summary>
        /// <param name="modsFolder">mods folder</param>
        /// <returns>true if the problem was resolved, false otherwise</returns>
        public static bool CheckModsDirectory(string modsFolder)
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
