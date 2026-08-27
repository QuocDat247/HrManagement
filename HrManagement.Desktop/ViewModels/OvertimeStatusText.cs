using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Desktop.ViewModels;

public static class OvertimeStatusText
{
    public static string Get(
        OvertimeRequestStatus status)
    {
        return status switch
        {
            OvertimeRequestStatus.Pending =>
                "Chờ duyệt",

            OvertimeRequestStatus.Approved =>
                "Đã duyệt",

            OvertimeRequestStatus.Rejected =>
                "Từ chối",

            OvertimeRequestStatus.Cancelled =>
                "Đã hủy",

            _ =>
                "Không xác định"
        };
    }
}
