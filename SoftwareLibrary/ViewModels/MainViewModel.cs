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
using System.Collections.Generic;

namespace SoftwareLibrary.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SoftwareItem> Items { get; } = new ObservableCollection<SoftwareItem>();
        public ObservableCollection<object> ViewItems { get; } = new ObservableCollection<object>();

        // expose status options for binding
        public IEnumerable<string> StatusOptions { get; } = new List<string>
        {
            "In development",
            "In testing",
            "Deployed",
            "Archived"
        };

        public int ItemsCount => Items.Count;

        private SoftwareItem? _selectedItem;
        public SoftwareItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                // update command states when selection changes
                RaiseCanExecuteChangedForSelection();
            }
        }

        // Search text used to filter visible items
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value ?? string.Empty;
                OnPropertyChanged(nameof(SearchText));
                RebuildViewItems();
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
        public ICommand OpenBackupAppDataLocationCommand { get; }
        public ICommand OpenBackupUserDataLocationCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ReorderCommand { get; }
        public ICommand OpenExecutableLocationCommand { get; }
        public ICommand OpenBuildFolderLocationCommand { get; }
        public ICommand OpenDataFolderLocationCommand { get; }

        public MainViewModel() : this(new StorageService()) { }

        public MainViewModel(StorageService storage)
        {
            _storage = storage;

            foreach (var it in _storage.LoadItems())
            {
                Items.Add(it);
                HookItemPropertyChanged(it);
            }

            Items.CollectionChanged += Items_CollectionChanged;
            RebuildViewItems();

            AddCommand = new RelayCommand(_ => AddNew());
            PasteImageCommand = new RelayCommand(_ => PasteImage(), _ => SelectedItem != null);
            ChooseImageCommand = new RelayCommand(_ => ChooseImage(), _ => SelectedItem != null);
            ChooseExecutableCommand = new RelayCommand(_ => ChooseExecutable(), _ => SelectedItem != null);
            RunExecutableCommand = new RelayCommand(_ => RunExecutable(), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.ExecutablePath));
            OpenExecutableLocationCommand = new RelayCommand(_ => OpenExecutableLocation(), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.ExecutablePath));
            RunItemCommand = new RelayCommand(p => RunItem(p), p => p is SoftwareItem si && !string.IsNullOrWhiteSpace(si.ExecutablePath));
            ChooseBuildFolderCommand = new RelayCommand(_ => ChooseBuildFolder(), _ => SelectedItem != null);
            ChooseDataFolderCommand = new RelayCommand(_ => ChooseDataFolder(), _ => SelectedItem != null);
            OpenBuildFolderLocationCommand = new RelayCommand(_ => OpenFolder(SelectedItem?.BuildFolder), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.BuildFolder) && Directory.Exists(SelectedItem?.BuildFolder ?? string.Empty));
            OpenDataFolderLocationCommand = new RelayCommand(_ => OpenFolder(SelectedItem?.DataFolder), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.DataFolder) && Directory.Exists(SelectedItem?.DataFolder ?? string.Empty));

            BackupAppDataCommand = new RelayCommand(_ => BackupFolder(SelectedItem?.BuildFolder, "AppData"), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.BuildFolder));
            BackupUserDataCommand = new RelayCommand(_ => BackupFolder(SelectedItem?.DataFolder, "UserData"), _ => SelectedItem != null && !string.IsNullOrWhiteSpace(SelectedItem?.DataFolder));

            OpenBackupAppDataLocationCommand = new RelayCommand(_ => OpenBackupFolderLocation("AppData"), _ => SelectedItem != null);
            OpenBackupUserDataLocationCommand = new RelayCommand(_ => OpenBackupFolderLocation("UserData"), _ => SelectedItem != null);

            SaveCommand = new RelayCommand(_ => Save(), _ => SelectedItem != null);
            CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => SelectedItem != null);
            ReorderCommand = new RelayCommand(_ => OpenReorder());
        }

        private void HookItemPropertyChanged(SoftwareItem item)
        {
            if (item is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += Item_PropertyChanged;
            }
        }

        private void UnhookItemPropertyChanged(SoftwareItem item)
        {
            if (item is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged -= Item_PropertyChanged;
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // When any SoftwareItem property changes, reevaluate command CanExecute so buttons enable immediately
            RaiseCanExecuteChangedForSelection();

            // Also persist changes immediately where appropriate
            if (e.PropertyName == nameof(SoftwareItem.ImagePath) || e.PropertyName == nameof(SoftwareItem.Title) || e.PropertyName == nameof(SoftwareItem.Description) || e.PropertyName == nameof(SoftwareItem.Notes) || e.PropertyName == nameof(SoftwareItem.ExecutablePath) || e.PropertyName == nameof(SoftwareItem.BuildFolder) || e.PropertyName == nameof(SoftwareItem.DataFolder) || e.PropertyName == nameof(SoftwareItem.Status))
            {
                SaveItems();
            }
        }

        private void RebuildViewItems()
        {
            ViewItems.Clear();

            var filter = (SearchText ?? string.Empty).Trim();
            IEnumerable<SoftwareItem> source = Items;
            if (!string.IsNullOrEmpty(filter))
            {
                source = Items.Where(it =>
                    (!string.IsNullOrEmpty(it.Title) && it.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(it.Description) && it.Description.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(it.Notes) && it.Notes.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(it.ExecutablePath) && it.ExecutablePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            foreach (var it in source)
            {
                ViewItems.Add(it);
            }
            // keep Add button visible even when filtering
            ViewItems.Add(new AddButtonPlaceholder());
        }

        private void AddNew()
        {
            var s = new SoftwareItem { Title = "New Software" };
            // always default new items to In development so they don't inherit UI control state
            s.Status = "In development";
            Items.Add(s);
            HookItemPropertyChanged(s);
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
            (OpenExecutableLocationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RunItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ChooseBuildFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ChooseDataFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenBuildFolderLocationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenDataFolderLocationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (BackupAppDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (BackupUserDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenBackupAppDataLocationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenBackupUserDataLocationCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

                var canceled = !CopyDirectory(srcFolder, cloneDest);
                if (canceled)
                {
                    Wpf.MessageBox.Show("Backup canceled by user.");
                    return;
                }

                Wpf.MessageBox.Show("Backup completed to: " + cloneDest);
            }
            catch (Exception ex)
            {
                Wpf.MessageBox.Show("Backup failed: " + ex.Message);
            }
        }

        private bool CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException();
            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                try
                {
                    file.CopyTo(Path.Combine(destinationDir, file.Name));
                }
                catch (Exception ex)
                {
                    // ask user whether to skip this file or cancel entire backup
                    var msg = $"Failed to copy file:\n{file.FullName}\n\nError: {ex.Message}\n\nSkip this file? (Yes = Skip, No = Cancel)";
                    var res = Wpf.MessageBox.Show(msg, "Copy Error", Wpf.MessageBoxButton.YesNo, Wpf.MessageBoxImage.Warning);
                    if (res == Wpf.MessageBoxResult.Yes)
                    {
                        // skip this file and continue
                        continue;
                    }
                    else
                    {
                        // user chose to cancel
                        return false;
                    }
                }
            }

            foreach (var subDir in dir.GetDirectories())
            {
                var destSub = Path.Combine(destinationDir, subDir.Name);
                var cont = true;
                try
                {
                    cont = CopyDirectory(subDir.FullName, destSub);
                }
                catch (Exception ex)
                {
                    var msg = $"Failed to copy folder:\n{subDir.FullName}\n\nError: {ex.Message}\n\nSkip this folder? (Yes = Skip, No = Cancel)";
                    var res = Wpf.MessageBox.Show(msg, "Copy Error", Wpf.MessageBoxButton.YesNo, Wpf.MessageBoxImage.Warning);
                    if (res == Wpf.MessageBoxResult.Yes)
                    {
                        cont = true; // skip this folder
                    }
                    else
                    {
                        return false;
                    }
                }

                if (!cont) return false;
            }

            return true;
        }

        private void OpenBackupFolderLocation(string type)
        {
            if (SelectedItem == null) return;
            try
            {
                var destRoot = _storage.GetBackupBaseFolder(SelectedItem);
                var dest = Path.Combine(destRoot, type);
                Directory.CreateDirectory(dest);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dest}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Wpf.MessageBox.Show("Failed to open backup folder: " + ex.Message);
            }
        }

        // Public Save wrapper used by MainWindow
        public void Save()
        {
            SaveItems();
        }

        private void BackupAppData()
        {
            BackupFolder(SelectedItem?.BuildFolder, "AppData");
        }

        private void BackupUserData()
        {
            BackupFolder(SelectedItem?.DataFolder, "UserData");
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
            // hook/unhook property changed for new/old items so commands update when item fields change
            if (e.NewItems != null)
            {
                foreach (var ni in e.NewItems.OfType<SoftwareItem>()) HookItemPropertyChanged(ni);
            }
            if (e.OldItems != null)
            {
                foreach (var oi in e.OldItems.OfType<SoftwareItem>()) UnhookItemPropertyChanged(oi);
            }
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

        private void OpenExecutableLocation()
        {
            if (SelectedItem == null || string.IsNullOrWhiteSpace(SelectedItem.ExecutablePath)) return;
            try
            {
                var path = SelectedItem.ExecutablePath;
                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                }
                else if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
                }
                else
                {
                    Wpf.MessageBox.Show("Path does not exist: " + path);
                }
            }
            catch (Exception ex)
            {
                Wpf.MessageBox.Show("Failed to open location: " + ex.Message);
            }
        }

        private void OpenFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return;
            try
            {
                if (Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
                }
                else
                {
                    Wpf.MessageBox.Show("Folder does not exist: " + folder);
                }
            }
            catch (Exception ex)
            {
                Wpf.MessageBox.Show("Failed to open folder: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}