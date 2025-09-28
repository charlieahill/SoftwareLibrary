using System.Collections.Generic;

namespace SoftwareLibrary
{
    public class AppSettings
    {
        public string StorageFolder { get; set; } = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "CHillSW", "SoftwareLibrary");
        public string BackupsRoot { get; set; } = ""; // optional override
        public double LeftColumnWidth { get; set; } = 360.0; // persisted splitter width
        public List<string> BackupTargets { get; set; } = new List<string> { "Backups" };

        // Window bounds
        public double WindowLeft { get; set; } = double.NaN;
        public double WindowTop { get; set; } = double.NaN;
        public double WindowWidth { get; set; } = 1400;
        public double WindowHeight { get; set; } = 900;
        public bool IsMaximized { get; set; } = false;
    }
}