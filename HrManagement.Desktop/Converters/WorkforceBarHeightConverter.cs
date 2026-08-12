using System.Globalization;
using System.Windows.Data;

namespace HrManagement.Desktop.Converters;

public sealed class WorkforceBarHeightConverter
    : IMultiValueConverter
{
    public object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (values.Length < 2
            || values[0] is not int value
            || values[1] is not int maximum
            || maximum <= 0)
        {
            return 0d;
        }

        double maximumHeight = 180d;

        if (parameter is string text
            && double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsedHeight))
        {
            maximumHeight = parsedHeight;
        }

        if (value <= 0)
        {
            return 0d;
        }

        return value
            / (double)maximum
            * maximumHeight;
    }

    public object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
