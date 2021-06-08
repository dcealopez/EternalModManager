using System.Windows.Data;

namespace EternalModManager
{
    public class Mod
    {
        public string FileName { get; set; }

        public bool IsEnabled { get; set; }

        public Mod(string fileName, bool isEnabled)
        {
            FileName = fileName;
            IsEnabled = isEnabled;
        }
    }
}
