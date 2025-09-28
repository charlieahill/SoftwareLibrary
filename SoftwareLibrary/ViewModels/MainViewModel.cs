using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using System;
using System.Windows.Media.Imaging;
using SoftwareLibrary.Services;
using SoftwareLibrary.Views;
using Forms = System.Windows.Forms;
using Wpf = System.Windows;
using System.Collections.Specialized;

namespace SoftwareLibrary.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SoftwareItem> Items { get; } = new ObservableCollection<SoftwareItem>();
        public ObservableCollection<object> ViewItems { get; } = new ObservableCollection<object>();

        public int ItemsCount => Items.Count;

        private SoftwareItem? _selectedItem;
        public SoftwareItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                // update command states when selection changes
                RaiseCanExecuteChangedForSelection();
            }
        }

        public bool IsReadOnly { get; private set; } = false;

        private readonly StorageService _storage;

        public ICommand AddCommand { get; }
        public ICommand PasteImageCommand { get; }
        public ICommand ChooseImageCommand { get; }
        public ICommand ChooseExecutableCommand { get; }
        public ICommand RunExecutableCommand { get; }
        public ICommand RunItemCommand { get; }
        public ICommand ChooseBuildFolderCommand { get; }
        public ICommand ChooseDataFolderCommand { get; }
        public ICommand BackupAppDataCommand { get; }
        public ICommand BackupUserDataCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ReorderCommand { get; }

        public MainViewModel() : this(new StorageService()) { }

        public MainViewModel(StorageService storage)
        {
            _storage = storage;

            foreach (var it in _storage.LoadItems()) Items.Add(it);
            Items.CollectionChanged += Items_CollectionChanged;
            RebuildViewItems();

            AddCommand = new RelayCommand(_ => AddNew());
            PasteImageCommand = new RelayCommand(_ => PasteImage(), _ => SelectedItem != null);
            ChooseImageCommand = new RelayCommand(_ => ChooseImage(), _ => SelectedItem != null);
            ChooseExecutableCommand = new RelayCommand(_ => ChooseExecutable(), _ => SelectedItem != null);
            RunExecutableCommand = new RelayCommand(_ => RunExecutable(), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.ExecutablePath));
            RunItemCommand = new RelayCommand(p => RunItem(p), p => p is SoftwareItem si && !string.IsNullOrWhiteSpace(si.ExecutablePath));
            ChooseBuildFolderCommand = new RelayCommand(_ => ChooseBuildFolder(), _ => SelectedItem != null);
            ChooseDataFolderCommand = new RelayCommand(_ => ChooseDataFolder(), _ => SelectedItem != null);
            BackupAppDataCommand = new RelayCommand(_ => BackupFolder(SelectedItem?.BuildFolder, "AppData"), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.BuildFolder));
            BackupUserDataCommand = new RelayCommand(_ => BackupFolder(SelectedItem?.DataFolder, "UserData"), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.DataFolder));
            SaveCommand = new RelayCommand(_ => Save(), _ => SelectedItem != null);
            CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => SelectedItem != null);
            ReorderCommand = new RelayCommand(_ => OpenReorder());
        }

        private void RebuildViewItems()
        {
            ViewItems.Clear();
            foreach (var it in Items) ViewItems.Add(it);
            ViewItems.Add(new AddButtonPlaceholder());
        }

        private void AddNew()
        {
            var s = new SoftwareItem { Title = "New Software" };
            Items.Add(s);
            RebuildViewItems();
            SelectedItem = s;
            IsReadOnly = false;
            OnPropertyChanged(nameof(IsReadOnly));
            SaveItems();
            // update commands (selection has changed)
            RaiseCanExecuteChangedForSelection();
        }

        private void RaiseCanExecuteChangedForSelection()
        {
            (PasteImageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ChooseImageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ChooseExecutableCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RunExecutableCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RunItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ChooseBuildFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ChooseDataFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (BackupAppDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (BackupUserDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelEditCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void PasteImage()
        {
            if (Wpf.Clipboard.ContainsImage() && SelectedItem != null)
            {
                var img = Wpf.Clipboard.GetImage();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(img));
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CHillSW", "SoftwareLibrary", "Images");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, SelectedItem.Id + ".png");
                using (var fs = new FileStream(path, FileMode.Create)) encoder.Save(fs);
                SelectedItem.ImagePath = path;
                OnPropertyChanged(nameof(SelectedItem));
                SaveItems();
            }
            else
            {
                Wpf.MessageBox.Show("No image in clipboard.");
            }
        }

        private void ChooseImage()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif" };
            if (dlg.ShowDialog() == true && SelectedItem != null)
            {
                SelectedItem.ImagePath = dlg.FileName;
                OnPropertyChanged(nameof(SelectedItem));
                SaveItems();
            }
        }

        private void ChooseExecutable()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Executables|*.exe;*.bat;*.cmd|All Files|*.*" };
            if (dlg.ShowDialog() == true && SelectedItem != null)
            {
                SelectedItem.ExecutablePath = dlg.FileName;
                OnPropertyChanged(nameof(SelectedItem));
                SaveItems();
            }
        }

        private void RunExecutable()
        {
            if (SelectedItem == null || string.IsNullOrWhiteSpace(SelectedItem.ExecutablePath)) return;
            try
            {
                Process.Start(new ProcessStartInfo(SelectedItem.ExecutablePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Wpf.MessageBox.Show("Failed to start process: " + ex.Message);
            }
        }

        private void RunItem(object? parameter)
        {
            if (parameter is not SoftwareItem si) return;
            if (string.IsNullOrWhiteSpace(si.ExecutablePath))
            {
                Wpf.MessageBox.Show("No executable configured for this item.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(si.ExecutablePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Wpf.MessageBox.Show("Failed to start process: " + ex.Message);
            }
        }

        private void ChooseBuildFolder()
        {
            using var dlg = new Forms.FolderBrowserDialog();
            var res = dlg.ShowDialog();
            if (res == Forms.DialogResult.OK && SelectedItem != null)
            {
                SelectedItem.BuildFolder = dlg.SelectedPath;
                OnPropertyChanged(nameof(SelectedItem));
                SaveItems();
            }
        }

        private void ChooseDataFolder()
        {
            using var dlg = new Forms.FolderBrowserDialog();
            var res = dlg.ShowDialog();
            if (res == Forms.DialogResult.OK && SelectedItem != null)
            {
                SelectedItem.DataFolder = dlg.SelectedPath;
                OnPropertyChanged(nameof(SelectedItem));
                SaveItems();
            }
        }

        private void BackupFolder(string? srcFolder, string type)
        {
            if (SelectedItem == null || string.IsNullOrWhiteSpace(srcFolder) || !Directory.Exists(srcFolder))
            {
                Wpf.MessageBox.Show("Source folder does not exist.");
                return;
            }

            try
            {
                var destRoot = _storage.GetBackupBaseFolder(SelectedItem);
                var dest = Path.Combine(destRoot, type);
                Directory.CreateDirectory(dest);
                var name = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var cloneDest = Path.Combine(dest, name);
                CopyDirectory(srcFolder, cloneDest);
                Wpf.MessageBox.Show("Backup completed to: " + cloneDest);
            }
            catch (Exception ex)
            {
                Wpf.MessageBox.Show("Backup failed: " + ex.Message);
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException();
            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                file.CopyTo(Path.Combine(destinationDir, file.Name));
            }

            foreach (var subDir in dir.GetDirectories())
            {
                CopyDirectory(subDir.FullName, Path.Combine(destinationDir, subDir.Name));
            }
        }

        public void Save()
        {
            SaveItems();
        }

        private void CancelEdit()
        {
            // reload from storage
            var loaded = _storage.LoadItems();
            Items.Clear();
            foreach (var it in loaded) Items.Add(it);
            SelectedItem = Items.FirstOrDefault();
            OnPropertyChanged(nameof(IsReadOnly));
        }

        private void SaveItems()
        {
            _storage.SaveItems(Items.ToList());
            RebuildViewItems();
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ItemsCount));
            RebuildViewItems();
        }

        private void OpenReorder()
        {
            var dialog = new ReorderWindow(new ObservableCollection<SoftwareItem>(Items)) { Owner = Wpf.Application.Current?.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                // replace items with new order
                Items.Clear();
                foreach (var it in dialog.Result) Items.Add(it);
                SaveItems();
                OnPropertyChanged(nameof(Items));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}