namespace Roivo.Core.Domain.Auditing;

/// <summary>
/// All audit-loggable actions across the application. Each value maps to a
/// stable string stored in the AuditLogs table. Add new values at the end of
/// the appropriate range; never reorder or rename existing ones — historical
/// audit data references the string form via <c>Enum.ToString()</c>.
/// </summary>
public enum AuditAction
{
    // Authentication (0–99)
    UserRegistered = 0,
    EmailConfirmed = 1,
    LoginSucceeded = 2,
    LoginFailed = 3,
    LoginBlockedNotAllowed = 4,
    AccountLockedOut = 5,
    PasswordResetRequested = 6,
    PasswordResetCompleted = 7,
    LoggedOut = 8,
    RegistrationAfmDuplicateWarningShown = 9,

    // Businesses (100–199)
    BusinessCreated = 100,
    BusinessUpdated = 101,
    BusinessDeactivated = 102,
    BusinessReactivated = 103,

    // AADE (200–299)
    AadeConnected = 200,
    AadeDisconnected = 201,
    AadeSyncCompleted = 202,
    AadeSyncFailed = 203,
    AadeConnectionAfmMismatch = 204,
}
