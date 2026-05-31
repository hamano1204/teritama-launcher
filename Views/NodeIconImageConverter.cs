using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TeritamaLauncher.Models;
using TeritamaLauncher.Services;

namespace TeritamaLauncher.Views
{
    public class NodeIconImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SnippetNode node)
            {
                if (node.IsDummy || node.IsSeparator)
                {
                    return null;
                }
                return SystemIconService.GetIcon(node.Content, node.IsFolder);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
