using System;
using System.Globalization;
using System.Windows.Data;

namespace SuikaTextExpander.Views
{
    public class FolderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SuikaTextExpander.Models.SnippetNode node)
            {
                if (node.IsDummy) return "";
                if (node.IsFolder) return "📁";
                if (node.IsSeparator) return "➖";
                return "📄";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
