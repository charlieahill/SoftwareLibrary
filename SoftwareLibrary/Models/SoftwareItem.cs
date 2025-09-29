using System;
using System.ComponentModel;

namespace SoftwareLibrary
{
    public class SoftwareItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        public string Id
        {
            get => _id;
            set {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        private string _title = "New Software";
        public string Title
        {
            get => _title;
            set {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set {
                if (_description == value) return;
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set {
                if (_notes == value) return;
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        private string _imagePath = string.Empty; // can be a file path or empty
        public string ImagePath
        {
            get => _imagePath;
            set {
                if (_imagePath == value) return;
                _imagePath = value;
                OnPropertyChanged(nameof(ImagePath));
            }
        }

        private string _executablePath = string.Empty;
        public string ExecutablePath
        {
            get => _executablePath;
            set {
                if (_executablePath == value) return;
                _executablePath = value;
                OnPropertyChanged(nameof(ExecutablePath));
            }
        }

        private string _buildFolder = string.Empty; // AppData
        public string BuildFolder
        {
            get => _buildFolder;
            set {
                if (_buildFolder == value) return;
                _buildFolder = value;
                OnPropertyChanged(nameof(BuildFolder));
            }
        }

        private string _dataFolder = string.Empty; // UserData
        public string DataFolder
        {
            get => _dataFolder;
            set {
                if (_dataFolder == value) return;
                _dataFolder = value;
                OnPropertyChanged(nameof(DataFolder));
            }
        }

        private string _status = "In development";
        public string Status
        {
            get => _status;
            set {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}