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
using System.Collections.Generic;
using SoftwareLibrary.Controls;

namespace SoftwareLibrary
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly StorageService _storage;
        private System.Windows.Point _dragStartPoint;

        private AdornerLayer? _adornerLayer;
        private DragAdorner? _dragAdorner;
        private ContentPresenter? _currentDropTargetPresenter;

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

            // save changes to window bounds when user resizes/moves the window or changes state
            this.SizeChanged += MainWindow_SizeOrLocationChanged;
            this.LocationChanged += MainWindow_SizeOrLocationChanged;
            this.StateChanged += MainWindow_StateChanged;
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
                // do not mark handled here; selection will be applied on MouseLeftButtonUp to avoid interfering with inner buttons
                _vm.SelectedItem = item;
            }
        }

        private void Tile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // If the click originated from a Button (or inside one), do not intercept so the Button can handle it.
            if (e.OriginalSource is DependencyObject dep)
            {
                var ancestor = FindAncestor<System.Windows.Controls.Button>(dep);
                if (ancestor != null) return; // let button handle the click
            }

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

        private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T t) return t;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
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

        private void Tile_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void Tile_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
            var pos = e.GetPosition(null);
            var diff = pos - _dragStartPoint;
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                object? dataItem = null;
                ContentPresenter? sourcePresenter = null;
                if (sender is ContentPresenter cp) { dataItem = cp.Content; sourcePresenter = cp; }
                else if (sender is FrameworkElement fe) { dataItem = fe.DataContext; sourcePresenter = fe as ContentPresenter; }
                if (dataItem is SoftwareItem si && sourcePresenter != null)
                {
                    var dragData = new System.Windows.DataObject("SoftwareItem", si);

                    // create adorner
                    _adornerLayer = AdornerLayer.GetAdornerLayer(this);
                    if (_adornerLayer != null)
                    {
                        _dragAdorner = new DragAdorner(this, sourcePresenter, 0.8);
                        _adornerLayer.Add(_dragAdorner);
                    }

                    System.Windows.DragDrop.DoDragDrop(this, dragData, System.Windows.DragDropEffects.Move);

                    // clean up
                    if (_dragAdorner != null)
                    {
                        _adornerLayer?.Remove(_dragAdorner);
                        _dragAdorner = null;
                    }
                }
            }
        }

        private void Tile_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("SoftwareItem")) { e.Effects = System.Windows.DragDropEffects.None; e.Handled = true; return; }
            e.Effects = System.Windows.DragDropEffects.Move;

            // update adorner position
            if (_dragAdorner != null)
            {
                var pos = e.GetPosition(this);
                _dragAdorner.SetOffsets(pos.X + 10, pos.Y + 10);
            }

            // highlight potential target
            ContentPresenter? targetPresenter = null;
            if (sender is ContentPresenter cp) targetPresenter = cp;
            else if (sender is FrameworkElement fe) targetPresenter = fe as ContentPresenter;

            if (_currentDropTargetPresenter != targetPresenter)
            {
                // remove previous highlight
                if (_currentDropTargetPresenter != null)
                {
                    _currentDropTargetPresenter.ClearValue(Border.BorderBrushProperty);
                    _currentDropTargetPresenter.ClearValue(Border.BorderThicknessProperty);
                }

                _currentDropTargetPresenter = targetPresenter;

                if (_currentDropTargetPresenter != null)
                {
                    // set simple highlight style
                    _currentDropTargetPresenter.SetValue(Border.BorderBrushProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x90, 0xFF)));
                    _currentDropTargetPresenter.SetValue(Border.BorderThicknessProperty, new Thickness(2));
                }
            }

            e.Handled = true;
        }

        private void Tile_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("SoftwareItem")) return;
            var droppedItem = e.Data.GetData("SoftwareItem") as SoftwareItem;
            if (droppedItem == null) return;

            // cleanup highlight
            if (_currentDropTargetPresenter != null)
            {
                _currentDropTargetPresenter.ClearValue(Border.BorderBrushProperty);
                _currentDropTargetPresenter.ClearValue(Border.BorderThicknessProperty);
                _currentDropTargetPresenter = null;
            }

            // determine target
            SoftwareItem? targetItem = null;
            if (sender is ContentPresenter cp) targetItem = cp.Content as SoftwareItem;
            else if (sender is FrameworkElement fe) targetItem = fe.DataContext as SoftwareItem;

            if (targetItem == null || ReferenceEquals(droppedItem, targetItem)) return;

            var vm = DataContext as SoftwareLibrary.ViewModels.MainViewModel;
            if (vm == null) return;

            var items = vm.Items;
            var oldIndex = items.IndexOf(droppedItem);
            var newIndex = items.IndexOf(targetItem);
            if (oldIndex < 0 || newIndex < 0) return;

            items.Move(oldIndex, newIndex);
            vm.Save();

            // clean up adorner
            if (_dragAdorner != null)
            {
                _adornerLayer?.Remove(_dragAdorner);
                _dragAdorner = null;
            }

            e.Handled = true;
        }

        private void SendToPosition_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as SoftwareLibrary.ViewModels.MainViewModel;
            if (vm == null || vm.SelectedItem == null) return;

            int currentIndex = vm.Items.IndexOf(vm.SelectedItem) + 1; // 1-based
            var dlg = new SendToPositionWindow(currentIndex, vm.Items.Count) { Owner = this };
            if (dlg.ShowDialog() == true && dlg.Result.HasValue)
            {
                int target = dlg.Result.Value;
                if (target < 1) target = 1;
                if (target > vm.Items.Count) target = vm.Items.Count;

                int oldIndex = vm.Items.IndexOf(vm.SelectedItem);
                int newIndex = target - 1;
                if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                {
                    vm.Items.Move(oldIndex, newIndex);
                    vm.Save();
                }
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            try
            {
                var settings = _storage.LoadSettings();
                settings.IsMaximized = this.WindowState == WindowState.Maximized;
                if (!settings.IsMaximized)
                {
                    settings.WindowLeft = this.Left;
                    settings.WindowTop = this.Top;
                    settings.WindowWidth = this.ActualWidth;
                    settings.WindowHeight = this.ActualHeight;
                }
                _storage.SaveSettings(settings);
            }
            catch
            {
                // ignore errors saving transient state
            }
        }

        private void MainWindow_SizeOrLocationChanged(object? sender, EventArgs e)
        {
            try
            {
                if (this.WindowState == WindowState.Maximized) return;
                var settings = _storage.LoadSettings();
                settings.WindowLeft = this.Left;
                settings.WindowTop = this.Top;
                settings.WindowWidth = this.ActualWidth;
                settings.WindowHeight = this.ActualHeight;
                _storage.SaveSettings(settings);
            }
            catch
            {
                // ignore
            }
        }

        private void AddTile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Add tile clicked (debug)", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
            try
            {
                var vm = DataContext as SoftwareLibrary.ViewModels.MainViewModel;
                if (vm != null && vm.AddCommand != null && vm.AddCommand.CanExecute(null))
                {
                    vm.AddCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Add command failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTile_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.MessageBox.Show("Add tile preview mouse up (debug)", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
            try
            {
                var vm = DataContext as SoftwareLibrary.ViewModels.MainViewModel;
                if (vm != null && vm.AddCommand != null && vm.AddCommand.CanExecute(null))
                {
                    vm.AddCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Add command failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ItemsScroll_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // hit test to find if a Button inside AddTileTemplate was clicked
            var pt = e.GetPosition(this);
            var result = VisualTreeHelper.HitTest(this, pt);
            if (result == null) return;
            var dep = result.VisualHit as DependencyObject;
            while (dep != null)
            {
                if (dep is System.Windows.Controls.Button btn)
                {
                    // check tooltip or template to identify Add button
                    if (btn.ToolTip as string == "Add new software")
                    {
                        System.Windows.MessageBox.Show("Add button clicked via hit test (debug)", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }
                }
                dep = VisualTreeHelper.GetParent(dep);
            }
        }

        private void RunTile_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as System.Windows.Controls.Button;

                // Defer running until after the input event processing completes so mouse/drag logic doesn't interfere
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SoftwareItem? item = null;

                    // try CommandParameter first
                    if (btn != null && btn.CommandParameter is SoftwareItem cp)
                    {
                        item = cp;
                    }

                    // fallback to DataContext
                    if (item == null && btn != null && btn.DataContext is SoftwareItem dc)
                    {
                        item = dc;
                    }

                    if (item == null)
                    {
                        System.Windows.MessageBox.Show("Unable to determine item to run.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var vm = DataContext as SoftwareLibrary.ViewModels.MainViewModel;
                    if (vm == null)
                    {
                        System.Windows.MessageBox.Show("ViewModel not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Prefer invoking the command on the view model if available
                    if (vm.RunItemCommand != null && vm.RunItemCommand.CanExecute(item))
                    {
                        vm.RunItemCommand.Execute(item);
                        return;
                    }

                    // fallback: directly start process using the item's ExecutablePath
                    if (string.IsNullOrWhiteSpace(item.ExecutablePath))
                    {
                        System.Windows.MessageBox.Show("No executable configured for this item.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.ExecutablePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Failed to start process: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Run handler failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}