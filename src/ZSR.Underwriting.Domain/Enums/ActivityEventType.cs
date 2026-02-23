namespace ZSR.Underwriting.Domain.Enums;

public enum ActivityEventType
{
    SessionStart,
    SessionEnd,
    PageView,
    SearchPerformed,
    QuickAnalysisStarted,
    WizardStarted,
    WizardCompleted,
    ReportViewed,
    PdfExported,
    DocumentUploaded,
    DealCreated,
    DealDeleted,
    DocumentAccessDenied,
    DocumentScanFailed,
    DocumentRateLimited,
    DocumentDeleted
}
