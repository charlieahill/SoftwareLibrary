using System.Text;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SoftwareLibrary.Services;
using SoftwareLibrary.ViewModels;
using SoftwareLibrary.Views;

namespace SoftwareLibrary
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly StorageService _storage;

        public MainWindow()
        {
            InitializeComponent();
            _storage = new StorageService();
            _vm = new MainViewModel(_storage);
            DataContext = _vm;

            // register lost focus preview to auto-save
            this.PreviewLostKeyboardFocus += MainWindow_PreviewLostKeyboardFocus;

            // Load saved left column width and window bounds
            var settings = _storage.LoadSettings();
            if (settings.LeftColumnWidth > 0)
            {
                LeftColumn.Width = new GridLength(settings.LeftColumnWidth);
            }

            // Apply saved size first
            if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
            }

            // Apply saved position if present
            if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
            {
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }

            // Validate that the window is visible on at least one screen; if not, center on primary screen and clamp size
            if (!IsVisibleOnAnyScreen(Left, Top, Width, Height))
            {
                try
                {
                    var wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                    // clamp size
                    if (Width > wa.Width) Width = Math.Max(600, wa.Width * 0.9);
                    if (Height > wa.Height) Height = Math.Max(400, wa.Height * 0.9);

                    Left = wa.Left + (wa.Width - Width) / 2.0;
                    Top = wa.Top + (wa.Height - Height) / 2.0;
                }
                catch
                {
                    // ignore and use default WPF placement
                }
            }

            if (settings.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }

            if (_vm is INotifyPropertyChanged pc)
            {
                pc.PropertyChanged += ViewModel_PropertyChanged;
            }

            this.Closing += MainWindow_Closing;
        }

        private bool IsVisibleOnAnyScreen(double left, double top, double width, double height)
        {
            try
            {
                if (double.IsNaN(left) || double.IsNaN(top)) return false;
                var rect = new System.Drawing.Rectangle((int)left, (int)top, (int)Math.Max(1, width), (int)Math.Max(1, height));
                foreach (var s in System.Windows.Forms.Screen.AllScreens)
                {
                    if (s.WorkingArea.IntersectsWith(rect)) return true;
                }
            }
            catch
            {
                // if anything goes wrong, be permissive
                return true;
            }

            return false;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // Handle input focus loss to auto-save via view model Save()
            _vm?.Save();

            var settings = _storage.LoadSettings();
            // only store bounds if not maximized
            settings.IsMaximized = this.WindowState == WindowState.Maximized;
            if (!settings.IsMaximized)
            {
                settings.WindowLeft = this.Left;
                settings.WindowTop = this.Top;
                settings.WindowWidth = this.ActualWidth;
                settings.WindowHeight = this.ActualHeight;
            }

            // also persist left column width
            settings.LeftColumnWidth = LeftColumn.Width.Value;
            _storage.SaveSettings(settings);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedItem))
            {
                // Stop using the slide animation — immediately show the details panel
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        DetailsTransform.X = 0; // ensure details panel is in place
                    }
                    catch
                    {
                        // ignore if transform not available yet
                    }
                });
            }
        }

        private void Tile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // find the data item of the clicked tile. The sender can be a Border, ContentPresenter, or inner element.
            object? data = null;

            if (sender is ContentPresenter cp)
            {
                data = cp.Content;
            }
            else if (sender is FrameworkElement fe && fe.DataContext != null)
            {
                data = fe.DataContext;
            }
            else if (e.OriginalSource is FrameworkElement oe && oe.DataContext != null)
            {
                data = oe.DataContext;
            }

            if (data is SoftwareItem item)
            {
                _vm.SelectedItem = item;
                e.Handled = true;
            }
        }

        private void GridSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            try
            {
                var settings = _storage.LoadSettings();
                // Use ActualWidth which reflects the rendered size and is always a finite number
                var leftWidth = LeftColumn.ActualWidth;
                if (double.IsNaN(leftWidth) || leftWidth <= 0) leftWidth = LeftColumn.Width.Value;
                if (!double.IsNaN(leftWidth) && leftWidth > 0)
                {
                    settings.LeftColumnWidth = leftWidth;
                    _storage.SaveSettings(settings);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Unable to save layout: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsWindow(_storage) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var settings = _storage.LoadSettings();
                if (settings.LeftColumnWidth > 0)
                {
                    LeftColumn.Width = new GridLength(settings.LeftColumnWidth);
                }

                if (settings.IsMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
            }
        }

        private void MainWindow_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                _vm?.Save();
            }
            catch
            {
                // ignore
            }
        }
    }
}