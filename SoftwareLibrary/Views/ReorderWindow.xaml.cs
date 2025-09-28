using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SoftwareLibrary.Views
{
    public partial class ReorderWindow : Window
    {
        private ObservableCollection<SoftwareItem> _items;
        private readonly ObservableCollection<SoftwareItem> _original;

        public ObservableCollection<SoftwareItem> Result { get; private set; }

        public ReorderWindow(ObservableCollection<SoftwareItem> items)
        {
            InitializeComponent();
            _original = new ObservableCollection<SoftwareItem>(items);
            _items = new ObservableCollection<SoftwareItem>(_original);
            ItemsList.ItemsSource = _items;
            if (_items.Any()) ItemsList.SelectedIndex = 0;
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            var i = ItemsList.SelectedIndex;
            if (i > 0)
            {
                var it = _items[i];
                _items.RemoveAt(i);
                _items.Insert(i - 1, it);
                ItemsList.SelectedIndex = i - 1;
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            var i = ItemsList.SelectedIndex;
            if (i >= 0 && i < _items.Count - 1)
            {
                var it = _items[i];
                _items.RemoveAt(i);
                _items.Insert(i + 1, it);
                ItemsList.SelectedIndex = i + 1;
            }
        }

        private void MoveTop_Click(object sender, RoutedEventArgs e)
        {
            var i = ItemsList.SelectedIndex;
            if (i > 0)
            {
                var it = _items[i];
                _items.RemoveAt(i);
                _items.Insert(0, it);
                ItemsList.SelectedIndex = 0;
            }
        }

        private void MoveBottom_Click(object sender, RoutedEventArgs e)
        {
            var i = ItemsList.SelectedIndex;
            if (i >= 0 && i < _items.Count - 1)
            {
                var it = _items[i];
                _items.RemoveAt(i);
                _items.Add(it);
                ItemsList.SelectedIndex = _items.Count - 1;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _items = new ObservableCollection<SoftwareItem>(_original);
            ItemsList.ItemsSource = _items;
            if (_items.Any()) ItemsList.SelectedIndex = 0;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = new ObservableCollection<SoftwareItem>(_items);
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
