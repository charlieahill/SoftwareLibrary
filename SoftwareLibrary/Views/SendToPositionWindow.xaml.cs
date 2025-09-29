using System;
using System.Windows;

namespace SoftwareLibrary.Views
{
    public partial class SendToPositionWindow : Window
    {
        public int? Result { get; private set; }

        public SendToPositionWindow(int currentPosition, int max)
        {
            InitializeComponent();
            PositionText.Text = currentPosition.ToString();
            PositionText.SelectAll();
            PositionText.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PositionText.Text?.Trim(), out int v) && v >= 1)
            {
                Result = v;
                DialogResult = true;
                Close();
                return;
            }
            System.Windows.MessageBox.Show("Please enter a valid positive integer.", "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
