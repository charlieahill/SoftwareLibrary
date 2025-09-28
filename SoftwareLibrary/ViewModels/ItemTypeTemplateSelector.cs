using System.Windows;
using System.Windows.Controls;

namespace SoftwareLibrary.ViewModels
{
    public class ItemTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? SoftwareTemplate { get; set; }
        public DataTemplate? AddTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is SoftwareLibrary.SoftwareItem) return SoftwareTemplate;
            if (item is SoftwareLibrary.AddButtonPlaceholder) return AddTemplate;
            return base.SelectTemplate(item, container);
        }
    }
}
