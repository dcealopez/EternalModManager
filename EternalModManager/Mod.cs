using System.IO;
using System.IO.Compression;

namespace EternalModManager
{
    /// <summary>
    /// Mod class
    /// </summary>
    public class Mod
    {
        /// <summary>
        /// Mod file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Full path to the mod file
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Whether or not the mod is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Whether or not the mod is safe for multiplayer
        /// </summary>
        public bool IsOnlineSafe { get; set; }

        /// <summary>
        /// Whether or not the mod is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Whether or not this mod is going to be loaded by the mod loader
        /// </summary>
        public bool IsGoingToBeLoaded
        {
            get
            {
                if (AdvancedOptionsWindow.ModInjectorSettings == null)
                {
                    AdvancedOptionsWindow.LoadModInjectorSettingsFile();
                }

                if (!IsOnlineSafe && AdvancedOptionsWindow.ModInjectorSettings != null && AdvancedOptionsWindow.ModInjectorSettings.OnlineSafe)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Mod constructor
        /// </summary>
        /// <param name="fileName">mod file name</param>
        /// <param name="isEnabled">is the mod enabled?</param>
        public Mod(string fullPath, string fileName, bool isEnabled, bool isOnlineSafe)
        {
            FullPath = fullPath;
            FileName = fileName;
            IsEnabled = isEnabled;
            IsOnlineSafe = isOnlineSafe;
        }
    }
}
