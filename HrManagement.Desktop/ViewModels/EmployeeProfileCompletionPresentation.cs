using HrManagement.Application.Employees.Profiles.Completion;

namespace HrManagement.Desktop.ViewModels;

public static class EmployeeProfileCompletionPresentation
{
    public static string BuildWarningText(
    EmployeeProfileCompletionResult result)
    {
        ArgumentNullException.ThrowIfNull(
            result);

        return result.RequiresCompletion
            ? BuildMissingText(result)
            : string.Empty;
    }

    public static string BuildMissingText(
        EmployeeProfileCompletionResult result)
    {
        ArgumentNullException.ThrowIfNull(
            result);

        if (result.MissingRequirements.Count == 0)
        {
            return string.Empty;
        }

        IEnumerable<string> labels =
            result.MissingRequirements
                .Select(GetLabel);

        return $"Thiếu: {string.Join(", ", labels)}";
    }

    public static string BuildStatusText(
        EmployeeProfileCompletionResult result)
    {
        ArgumentNullException.ThrowIfNull(
            result);

        if (result.IsComplete)
        {
            return "Hồ sơ đã đầy đủ";
        }

        if (result.RequiresCompletion)
        {
            return "Hồ sơ cần bổ sung";
        }

        return "Hồ sơ còn thiếu thông tin";
    }

    public static string GetLabel(
        EmployeeProfileRequirement requirement)
    {
        return requirement switch
        {
            EmployeeProfileRequirement.Email =>
                "Email",

            EmployeeProfileRequirement.PhoneNumber =>
                "Số điện thoại",

            EmployeeProfileRequirement.DateOfBirth =>
                "Ngày sinh",

            EmployeeProfileRequirement.Gender =>
                "Giới tính",

            EmployeeProfileRequirement.Nationality =>
                "Quốc tịch",

            EmployeeProfileRequirement.PlaceOfBirth =>
                "Nơi sinh",

            EmployeeProfileRequirement.PermanentAddress =>
                "Địa chỉ thường trú",

            EmployeeProfileRequirement.EmergencyContact =>
                "Liên hệ khẩn cấp",

            EmployeeProfileRequirement.IdentificationRecord =>
                "Giấy tờ định danh",

            _ =>
                requirement.ToString()
        };
    }
}
