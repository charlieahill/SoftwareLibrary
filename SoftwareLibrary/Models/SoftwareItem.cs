using System;

namespace SoftwareLibrary
{
    public class SoftwareItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "New Software";
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty; // can be a file path or empty
        public string ExecutablePath { get; set; } = string.Empty;
        public string BuildFolder { get; set; } = string.Empty; // AppData
        public string DataFolder { get; set; } = string.Empty; // UserData
    }
}