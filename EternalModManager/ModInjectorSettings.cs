namespace EternalModManager
{
    /// <summary>
    /// Mod injector settings class
    /// </summary>
    public class ModInjectorSettings
    {
        /// <summary>
        /// Launch the game automatically after injecting mods
        /// </summary>
        public bool AutomaticGameLaunch { get; set; } = false;

        /// <summary>
        /// Reset backups before injecting mods
        /// </summary>
        public bool ResetBackups { get; set; } = false;

        /// <summary>
        /// Slow mod loading mode (mod loader)
        /// </summary>
        public bool SlowMode { get; set; } = false;

        /// <summary>
        /// Compress textures (mod loader)
        /// </summary>
        public bool CompressTextures { get; set; } = false;

        /// <summary>
        /// Load online-safe mods only (mod loader)
        /// </summary>
        public bool OnlineSafe { get; set; } = false;

        /// <summary>
        /// Verbose logging (mod loader)
        /// </summary>
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Disable multi-threading (mod loader)
        /// </summary>
        public bool DisableMultiThreading { get; set; } = false;

        /// <summary>
        /// Game parameters
        /// </summary>
        public string GameParameters { get; set; } = string.Empty;

        /// <summary>
        /// Returns true if the two injector settings objects are equal, false otherwise
        /// </summary>
        /// <param name="injectorSettings">injector settings object to compare against</param>
        /// <returns>true if the two injector settings objects are equal, false otherwise</returns>
        public bool IsEqualTo(ModInjectorSettings injectorSettings)
        {
            if (AutomaticGameLaunch == injectorSettings.AutomaticGameLaunch
                && ResetBackups == injectorSettings.ResetBackups
                && SlowMode == injectorSettings.SlowMode
                && CompressTextures == injectorSettings.CompressTextures
                && OnlineSafe == injectorSettings.OnlineSafe
                && Verbose == injectorSettings.Verbose
                && DisableMultiThreading == injectorSettings.DisableMultiThreading
                && GameParameters == injectorSettings.GameParameters)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clones the mod injector settings object
        /// </summary>
        /// <returns>A clone of the mod injector settings object</returns>
        public ModInjectorSettings Clone()
        {
            var clone = new ModInjectorSettings()
            {
                AutomaticGameLaunch = AutomaticGameLaunch,
                ResetBackups = ResetBackups,
                SlowMode = SlowMode,
                CompressTextures = CompressTextures,
                OnlineSafe = OnlineSafe,
                Verbose = Verbose,
                DisableMultiThreading = DisableMultiThreading,
                GameParameters = GameParameters
            };

            return clone;
        }
    }
}
