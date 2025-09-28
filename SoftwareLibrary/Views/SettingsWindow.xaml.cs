using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using SoftwareLibrary.Services;

namespace SoftwareLibrary.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly StorageService _storage;
        private AppSettings _settings;

        public SettingsWindow(StorageService storage)
        {
            InitializeComponent();
            _storage = storage;
            _settings = _storage.LoadSettings();
            BackupRootText.Text = _settings.BackupsRoot ?? string.Empty;
            LeftWidthText.Text = _settings.LeftColumnWidth.ToString();
        }

        private void BrowseBackupRoot_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            var res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                BackupRootText.Text = dlg.SelectedPath;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _settings.BackupsRoot = string.IsNullOrWhiteSpace(BackupRootText.Text) ? string.Empty : BackupRootText.Text;
            if (double.TryParse(LeftWidthText.Text, out var w) && w > 0)
            {
                _settings.LeftColumnWidth = w;
            }
            _storage.SaveSettings(_settings);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}