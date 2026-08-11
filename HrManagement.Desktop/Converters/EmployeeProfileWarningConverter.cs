using System.Globalization;
using System.Windows.Data;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.Converters;

public sealed class EmployeeProfileWarningConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not Employee employee
            || !employee.RequiresProfileCompletion)
        {
            return string.Empty;
        }

        var missingFields = new List<string>();

        if (employee.Email is null)
        {
            missingFields.Add("Email");
        }

        if (employee.PhoneNumber is null)
        {
            missingFields.Add("Số điện thoại");
        }

        if (employee.DateOfBirth is null)
        {
            missingFields.Add("Ngày sinh");
        }

        return $"Thiếu: {string.Join(", ", missingFields)}";
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
