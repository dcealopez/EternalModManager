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
        /// Whether or not the mod is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Mod constructor
        /// </summary>
        /// <param name="fileName">mod file name</param>
        /// <param name="isEnabled">is the mod enabled?</param>
        public Mod(string fileName, bool isEnabled)
        {
            FileName = fileName;
            IsEnabled = isEnabled;
        }
    }
}
