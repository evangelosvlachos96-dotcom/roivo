namespace Roivo.Resources;

/// <summary>User-facing strings for authentication pages.</summary>
public static class Auth
{
    // Login
    public const string Login_PageTitle = "Σύνδεση";
    public const string Login_EmailLabel = "Email";
    public const string Login_PasswordLabel = "Κωδικός";
    public const string Login_RememberMe = "Να με θυμάσαι";
    public const string Login_SignInButton = "Σύνδεση";
    public const string Login_ForgotPasswordLink = "Ξεχάσατε τον κωδικό σας;";
    public const string Login_NoAccountLink = "Δεν έχετε λογαριασμό; Εγγραφή";
    public const string Login_InvalidCredentials = "Λάθος email ή κωδικός.";
    public const string Login_EmailNotConfirmed = "Πρέπει να επιβεβαιώσετε το email σας πριν συνδεθείτε.";
    public const string Login_AccountLocked = "Ο λογαριασμός σας έχει κλειδωθεί προσωρινά λόγω πολλών αποτυχημένων προσπαθειών. Δοκιμάστε ξανά αργότερα.";

    // Register
    public const string Register_PageTitle = "Εγγραφή";
    public const string Register_FullNameLabel = "Πλήρες όνομα";
    public const string Register_EmailLabel = "Email";
    public const string Register_PasswordLabel = "Κωδικός";
    public const string Register_ConfirmPasswordLabel = "Επιβεβαίωση κωδικού";
    public const string Register_TenantTypeLabel = "Τύπος λογαριασμού";
    public const string Register_TenantType_Accountant = "Λογιστής";
    public const string Register_TenantType_BusinessOwner = "Ιδιοκτήτης επιχείρησης";
    public const string Register_AfmLabel = "ΑΦΜ";
    public const string Register_BusinessNameLabel = "Όνομα επιχείρησης";
    public const string Register_SubmitButton = "Εγγραφή";
    public const string Register_HaveAccountLink = "Έχετε ήδη λογαριασμό; Σύνδεση";
    public const string Register_DuplicateEmail = "Υπάρχει ήδη λογαριασμός με αυτό το email.";
    public const string Register_PasswordTooWeak = "Ο κωδικός δεν πληροί τις απαιτήσεις ασφαλείας.";
    public const string Register_PasswordMismatch = "Οι κωδικοί δεν ταιριάζουν.";

    // Confirm email
    public const string ConfirmEmail_PageTitle = "Επιβεβαίωση email";
    public const string ConfirmEmail_Pending_Heading = "Ελέγξτε το email σας";
    public const string ConfirmEmail_Pending_Body = "Στείλαμε email στο {0}. Κάντε κλικ στον σύνδεσμο μέσα στο email για να ολοκληρώσετε την εγγραφή.";
    public const string ConfirmEmail_Pending_ResendButton = "Επαναποστολή email";
    public const string ConfirmEmail_Success_Heading = "Το email επιβεβαιώθηκε";
    public const string ConfirmEmail_Success_Body = "Μπορείτε τώρα να συνδεθείτε στον λογαριασμό σας.";
    public const string ConfirmEmail_TokenInvalid = "Ο σύνδεσμος επιβεβαίωσης δεν είναι έγκυρος ή έχει λήξει.";

    // Forgot password
    public const string ForgotPassword_PageTitle = "Ανάκτηση κωδικού";
    public const string ForgotPassword_EmailLabel = "Email";
    public const string ForgotPassword_SubmitButton = "Αποστολή συνδέσμου ανάκτησης";
    public const string ForgotPassword_Confirmation = "Αν υπάρχει λογαριασμός με αυτό το email, θα λάβετε σύνδεσμο για επαναφορά κωδικού.";
    public const string ForgotPassword_BackToLogin = "Πίσω στη σύνδεση";

    // Reset password
    public const string ResetPassword_PageTitle = "Επαναφορά κωδικού";
    public const string ResetPassword_NewPasswordLabel = "Νέος κωδικός";
    public const string ResetPassword_ConfirmPasswordLabel = "Επιβεβαίωση νέου κωδικού";
    public const string ResetPassword_SubmitButton = "Αλλαγή κωδικού";
    public const string ResetPassword_Success = "Ο κωδικός σας άλλαξε. Μπορείτε τώρα να συνδεθείτε.";
    public const string ResetPassword_TokenInvalid = "Ο σύνδεσμος επαναφοράς δεν είναι έγκυρος ή έχει λήξει.";

    // Logout
    public const string LogoutMenuItem = "Έξοδος";
}
