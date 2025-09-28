using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SoftwareLibrary.Services
{
    public class StorageService
    {
        private readonly string _baseFolder;
        private readonly string _itemsFile;
        private readonly string _settingsFile;

        public StorageService(string? baseFolder = null)
        {
            _baseFolder = !string.IsNullOrWhiteSpace(baseFolder)
                ? baseFolder!
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CHillSW", "SoftwareLibrary");

            Directory.CreateDirectory(_baseFolder);
            _itemsFile = Path.Combine(_baseFolder, "items.json");
            _settingsFile = Path.Combine(_baseFolder, "settings.json");
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFile))
                {
                    var s = new AppSettings();
                    SaveSettings(s);
                    return s;
                }

                var json = File.ReadAllText(_settingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings s)
        {
            // sanitize floating point values to avoid serializing NaN/Infinity which JSON doesn't support by default
            s.LeftColumnWidth = Sanitize(s.LeftColumnWidth, 360.0);
            s.WindowWidth = Sanitize(s.WindowWidth, 1400.0);
            s.WindowHeight = Sanitize(s.WindowHeight, 900.0);
            s.WindowLeft = Sanitize(s.WindowLeft, 100.0);
            s.WindowTop = Sanitize(s.WindowTop, 100.0);

            var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
        }

        private static double Sanitize(double value, double defaultValue)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return defaultValue;
            return value;
        }

        public List<SoftwareItem> LoadItems()
        {
            try
            {
                if (!File.Exists(_itemsFile))
                {
                    var empty = new List<SoftwareItem>();
                    SaveItems(empty);
                    return empty;
                }

                var json = File.ReadAllText(_itemsFile);
                return JsonSerializer.Deserialize<List<SoftwareItem>>(json) ?? new List<SoftwareItem>();
            }
            catch
            {
                return new List<SoftwareItem>();
            }
        }

        public void SaveItems(List<SoftwareItem> items)
        {
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_itemsFile, json);
        }

        public string GetBackupBaseFolder(SoftwareItem item)
        {
            var settings = LoadSettings();
            var root = !string.IsNullOrWhiteSpace(settings.BackupsRoot) ? settings.BackupsRoot : _baseFolder;
            var dest = Path.Combine(root, item.Title, "Backups");
            Directory.CreateDirectory(dest);
            return dest;
        }
    }
}