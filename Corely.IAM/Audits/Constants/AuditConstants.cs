namespace Corely.IAM.Audits.Constants;

public static class AuditConstants
{
    public const string AUDIT_RESOURCE_TYPE = "audit";
    public const string AUDIT_SETTINGS_RESOURCE_TYPE = "audit_settings";
    public const string PLATFORM_SETTINGS_RESOURCE_TYPE = "platform_settings";

    public const string AUDIT_ENTRIES_TABLE_NAME = "AuditEntries";

    public const string FAULT_RESULT_CODE = "Fault";
    public const string SUCCESS_RESULT_CODE = "Success";
    public const string FAILED_RESULT_CODE = "Failed";
    public const string UNKNOWN_SOURCE = "unknown";

    public const int SOURCE_MAX_LENGTH = 100;
    public const int SERVICE_MAX_LENGTH = 100;
    public const int OPERATION_MAX_LENGTH = 100;
    public const int RESOURCE_TYPE_MAX_LENGTH = 100;
    public const int RESULT_CODE_MAX_LENGTH = 100;
    public const int DETAILS_MAX_LENGTH = 500;

    public const int DEFAULT_MAX_RETENTION_DAYS = 365;
    public const int DEFAULT_RETENTION_DAYS = 90;

    public const int EXPORT_MAX_ENTRIES = 10_000;
}
