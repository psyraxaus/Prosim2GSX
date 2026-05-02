using System.Windows;
using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.Checklists
{
    public class ChecklistItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SeparatorTemplate { get; set; }
        public DataTemplate NoteTemplate { get; set; }
        public DataTemplate CheckedTemplate { get; set; }
        public DataTemplate CurrentTemplate { get; set; }
        public DataTemplate PendingTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not ChecklistItemView v) return base.SelectTemplate(item, container);
            if (v.IsSeparator) return SeparatorTemplate;
            if (v.IsNote) return NoteTemplate;
            if (v.IsChecked) return CheckedTemplate;
            if (v.IsCurrentItem) return CurrentTemplate;
            return PendingTemplate;
        }
    }
}
