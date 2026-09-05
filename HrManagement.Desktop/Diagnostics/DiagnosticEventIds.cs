using Microsoft.Extensions.Logging;

namespace HrManagement.Desktop.Diagnostics;

public static class DiagnosticEventIds
{
    public static readonly EventId ApplicationStarted =
        new(
            1000,
            nameof(ApplicationStarted));

    public static readonly EventId DatabaseInitialized =
        new(
            1001,
            nameof(DatabaseInitialized));

    public static readonly EventId DatabaseInitializationFailed =
        new(
            1002,
            nameof(DatabaseInitializationFailed));

    public static readonly EventId LoginWindowOpened =
        new(
            1003,
            nameof(LoginWindowOpened));

    public static readonly EventId LoginCancelled =
        new(
            1004,
            nameof(LoginCancelled));

    public static readonly EventId MainWindowOpened =
        new(
            1005,
            nameof(MainWindowOpened));

    public static readonly EventId ApplicationExited =
        new(
            1006,
            nameof(ApplicationExited));

    public static readonly EventId
        DispatcherUnhandledException =
            new(
                9000,
                nameof(DispatcherUnhandledException));

    public static readonly EventId
        AppDomainUnhandledException =
            new(
                9001,
                nameof(AppDomainUnhandledException));

    public static readonly EventId
        UnobservedTaskException =
            new(
                9002,
                nameof(UnobservedTaskException));

    public static readonly EventId
        DiagnosticQueued =
            new(
                9100,
                nameof(DiagnosticQueued));

    public static readonly EventId
        DiagnosticQueueFailed =
            new(
                9101,
                nameof(DiagnosticQueueFailed));

    public static readonly EventId
        DiagnosticDeliverySent =
            new(
                9200,
                nameof(DiagnosticDeliverySent));

    public static readonly EventId
        DiagnosticDeliveryDeferred =
            new(
                9201,
                nameof(DiagnosticDeliveryDeferred));

    public static readonly EventId
        DiagnosticDeliveryRejected =
            new(
                9202,
                nameof(DiagnosticDeliveryRejected));

    public static readonly EventId
        DiagnosticDeliveryQuarantineFailed =
            new(
                9203,
                nameof(DiagnosticDeliveryQuarantineFailed));

    public static readonly EventId
        DiagnosticDeliveryDeleteFailed =
            new(
                9204,
                nameof(DiagnosticDeliveryDeleteFailed));

    public static readonly EventId
        DiagnosticProcessorFailed =
            new(
                9205,
                nameof(DiagnosticProcessorFailed));

    public static readonly EventId
        DiagnosticDeliveryNotAuthorized =
            new(
                9206,
                nameof(DiagnosticDeliveryNotAuthorized));
}
