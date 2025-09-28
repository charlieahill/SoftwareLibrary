using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SoftwareLibrary.Views
{
    public partial class AddTile : System.Windows.Controls.UserControl
    {
        public AddTile()
        {
            InitializeComponent();
            RootBorderInternal.MouseLeftButtonUp += (s, e) =>
            {
                if (Command != null && Command.CanExecute(CommandParameter))
                {
                    Command.Execute(CommandParameter);
                }
                AddClicked?.Invoke(this, new RoutedEventArgs());
            };
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command), typeof(ICommand), typeof(AddTile), new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(AddTile), new PropertyMetadata(null));

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public event RoutedEventHandler? AddClicked;

        public Border RootBorder => RootBorderInternal;
    }
}
