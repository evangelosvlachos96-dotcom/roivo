namespace Roivo.Resources;

/// <summary>User-facing strings for authentication pages.</summary>
public static class Auth
{
    // Login
    public static string Login_PageTitle => Strings.Get("Auth.Login_PageTitle", "Σύνδεση");
    public static string Login_EmailLabel => Strings.Get("Auth.Login_EmailLabel", "Email");
    public static string Login_PasswordLabel => Strings.Get("Auth.Login_PasswordLabel", "Κωδικός");
    public static string Login_RememberMe => Strings.Get("Auth.Login_RememberMe", "Να με θυμάσαι");
    public static string Login_SignInButton => Strings.Get("Auth.Login_SignInButton", "Σύνδεση");
    public static string Login_ForgotPasswordLink => Strings.Get("Auth.Login_ForgotPasswordLink", "Ξεχάσατε τον κωδικό σας;");
    public static string Login_NoAccountLink => Strings.Get("Auth.Login_NoAccountLink", "Δεν έχετε λογαριασμό; Εγγραφή");
    public static string Login_InvalidCredentials => Strings.Get("Auth.Login_InvalidCredentials", "Λάθος email ή κωδικός.");
    public static string Login_EmailNotConfirmed => Strings.Get("Auth.Login_EmailNotConfirmed", "Πρέπει να επιβεβαιώσετε το email σας πριν συνδεθείτε.");
    public static string Login_AccountLocked => Strings.Get("Auth.Login_AccountLocked", "Ο λογαριασμός σας έχει κλειδωθεί προσωρινά λόγω πολλών αποτυχημένων προσπαθειών. Δοκιμάστε ξανά αργότερα.");

    // Register
    public static string Register_PageTitle => Strings.Get("Auth.Register_PageTitle", "Εγγραφή");
    public static string Register_Heading => Strings.Get("Auth.Register_Heading", "Δημιουργία λογαριασμού");
    public static string Register_Subtitle => Strings.Get("Auth.Register_Subtitle", "Ξεκίνα με Roivo σε λιγότερο από ένα λεπτό.");
    public static string Register_FullNameLabel => Strings.Get("Auth.Register_FullNameLabel", "Πλήρες όνομα");
    public static string Register_EmailLabel => Strings.Get("Auth.Register_EmailLabel", "Email");
    public static string Register_PasswordLabel => Strings.Get("Auth.Register_PasswordLabel", "Κωδικός");
    public static string Register_ConfirmPasswordLabel => Strings.Get("Auth.Register_ConfirmPasswordLabel", "Επιβεβαίωση κωδικού");
    public static string Register_TenantTypeLabel => Strings.Get("Auth.Register_TenantTypeLabel", "Τύπος λογαριασμού");
    public static string Register_TenantType_Accountant => Strings.Get("Auth.Register_TenantType_Accountant", "Λογιστικό γραφείο");
    public static string Register_TenantType_BusinessOwner => Strings.Get("Auth.Register_TenantType_BusinessOwner", "Επιχείρηση");
    public static string Register_AfmLabel => Strings.Get("Auth.Register_AfmLabel", "ΑΦΜ");
    public static string Register_BusinessNameLabel => Strings.Get("Auth.Register_BusinessNameLabel", "Όνομα επιχείρησης");
    public static string Register_OrganizationNameLabel => Strings.Get("Auth.Register_OrganizationNameLabel", "Επωνυμία οργανισμού");
    public static string Register_OrganizationNamePlaceholder => Strings.Get("Auth.Register_OrganizationNamePlaceholder", "π.χ. Παπαδόπουλος Λογ. Γραφείο");
    public static string Register_AcceptTermsLabel => Strings.Get("Auth.Register_AcceptTermsLabel", "Συμφωνώ με τους όρους χρήσης και την πολιτική απορρήτου.");
    public static string Register_SubmitButton => Strings.Get("Auth.Register_SubmitButton", "Εγγραφή");
    public static string Register_HaveAccountLink => Strings.Get("Auth.Register_HaveAccountLink", "Έχετε ήδη λογαριασμό; Σύνδεση");
    public static string Register_HaveAccountPrompt => Strings.Get("Auth.Register_HaveAccountPrompt", "Έχεις ήδη λογαριασμό;");
    public static string Register_DuplicateEmail => Strings.Get("Auth.Register_DuplicateEmail", "Υπάρχει ήδη λογαριασμός με αυτό το email.");
    public static string Register_DuplicateAfmWarning => Strings.Get("Auth.Register_DuplicateAfmWarning", "Υπάρχει ήδη λογαριασμός με αυτό το ΑΦΜ.");
    public static string Register_ConfirmDuplicateLabel => Strings.Get("Auth.Register_ConfirmDuplicateLabel", "Καταλαβαίνω και θέλω να συνεχίσω με νέο λογαριασμό.");
    public static string Register_PasswordTooWeak => Strings.Get("Auth.Register_PasswordTooWeak", "Ο κωδικός δεν πληροί τις απαιτήσεις ασφαλείας.");
    public static string Register_PasswordMismatch => Strings.Get("Auth.Register_PasswordMismatch", "Οι κωδικοί δεν ταιριάζουν.");

    /// <summary>Shared by the registration and password-reset forms.</summary>
    public static string PasswordRequirementsHint => Strings.Get("Auth.PasswordRequirementsHint", "Τουλάχιστον 12 χαρακτήρες, με κεφαλαίο, αριθμό και σύμβολο.");

    /// <summary>Shared by the confirm-email failure and pending pages.</summary>
    public static string RegisterAgainLink => Strings.Get("Auth.RegisterAgainLink", "Εγγραφή ξανά");

    // Confirm email
    public static string ConfirmEmail_PageTitle => Strings.Get("Auth.ConfirmEmail_PageTitle", "Επιβεβαίωση email");
    public static string ConfirmEmail_Pending_Heading => Strings.Get("Auth.ConfirmEmail_Pending_Heading", "Ελέγξτε το email σας");
    public static string ConfirmEmail_Pending_Body => Strings.Get("Auth.ConfirmEmail_Pending_Body", "Στείλαμε email στο {0}. Κάντε κλικ στον σύνδεσμο μέσα στο email για να ολοκληρώσετε την εγγραφή.");
    public static string ConfirmEmail_Pending_ResendButton => Strings.Get("Auth.ConfirmEmail_Pending_ResendButton", "Επαναποστολή email");
    public static string ConfirmEmail_Pending_SentToPrefix => Strings.Get("Auth.ConfirmEmail_Pending_SentToPrefix", "Στείλαμε ένα link επιβεβαίωσης στο");
    public static string ConfirmEmail_Pending_YourEmailFallback => Strings.Get("Auth.ConfirmEmail_Pending_YourEmailFallback", "email σου");
    public static string ConfirmEmail_Pending_Instructions => Strings.Get("Auth.ConfirmEmail_Pending_Instructions", "Πάτησε το link στο email για να ενεργοποιήσεις τον λογαριασμό σου. Μπορεί να χρειαστούν μερικά λεπτά να φτάσει.");
    public static string ConfirmEmail_Pending_NotReceived => Strings.Get("Auth.ConfirmEmail_Pending_NotReceived", "Δεν λάβατε το email;");
    public static string ConfirmEmail_Success_Heading => Strings.Get("Auth.ConfirmEmail_Success_Heading", "Το email επιβεβαιώθηκε");
    public static string ConfirmEmail_Success_Body => Strings.Get("Auth.ConfirmEmail_Success_Body", "Μπορείτε τώρα να συνδεθείτε στον λογαριασμό σας.");
    public static string ConfirmEmail_Failure_Heading => Strings.Get("Auth.ConfirmEmail_Failure_Heading", "Αποτυχία επιβεβαίωσης");
    public static string ConfirmEmail_TokenInvalid => Strings.Get("Auth.ConfirmEmail_TokenInvalid", "Ο σύνδεσμος επιβεβαίωσης δεν είναι έγκυρος ή έχει λήξει.");

    // Forgot password
    public static string ForgotPassword_PageTitle => Strings.Get("Auth.ForgotPassword_PageTitle", "Ανάκτηση κωδικού");
    public static string ForgotPassword_Subtitle => Strings.Get("Auth.ForgotPassword_Subtitle", "Δώσε το email σου και θα σου στείλουμε ένα link για να ορίσεις νέο κωδικό.");
    public static string ForgotPassword_EmailLabel => Strings.Get("Auth.ForgotPassword_EmailLabel", "Email");
    public static string ForgotPassword_SubmitButton => Strings.Get("Auth.ForgotPassword_SubmitButton", "Αποστολή συνδέσμου ανάκτησης");
    public static string ForgotPassword_Confirmation => Strings.Get("Auth.ForgotPassword_Confirmation", "Αν υπάρχει λογαριασμός με αυτό το email, θα λάβετε σύνδεσμο για επαναφορά κωδικού.");
    public static string ForgotPassword_BackToLogin => Strings.Get("Auth.ForgotPassword_BackToLogin", "Πίσω στη σύνδεση");

    // Reset password
    public static string ResetPassword_PageTitle => Strings.Get("Auth.ResetPassword_PageTitle", "Επαναφορά κωδικού");
    public static string ResetPassword_NewPasswordLabel => Strings.Get("Auth.ResetPassword_NewPasswordLabel", "Νέος κωδικός");
    public static string ResetPassword_ConfirmPasswordLabel => Strings.Get("Auth.ResetPassword_ConfirmPasswordLabel", "Επιβεβαίωση νέου κωδικού");
    public static string ResetPassword_SubmitButton => Strings.Get("Auth.ResetPassword_SubmitButton", "Αλλαγή κωδικού");
    public static string ResetPassword_Success => Strings.Get("Auth.ResetPassword_Success", "Ο κωδικός σας άλλαξε. Μπορείτε τώρα να συνδεθείτε.");
    public static string ResetPassword_Success_Heading => Strings.Get("Auth.ResetPassword_Success_Heading", "Ο κωδικός άλλαξε");
    public static string ResetPassword_Success_Body => Strings.Get("Auth.ResetPassword_Success_Body", "Μπορείς τώρα να συνδεθείς με τον νέο σου κωδικό.");
    public static string ResetPassword_TokenInvalid => Strings.Get("Auth.ResetPassword_TokenInvalid", "Ο σύνδεσμος επαναφοράς δεν είναι έγκυρος ή έχει λήξει.");

    // Access denied
    public static string AccessDenied_PageTitle => Strings.Get("Auth.AccessDenied_PageTitle", "Δεν έχεις πρόσβαση");
    public static string AccessDenied_Body => Strings.Get("Auth.AccessDenied_Body", "Δεν έχεις δικαίωμα να δεις αυτή τη σελίδα.");
    public static string AccessDenied_BackHome => Strings.Get("Auth.AccessDenied_BackHome", "Επιστροφή στην αρχική");

    // Logout
    public static string LogoutMenuItem => Strings.Get("Auth.LogoutMenuItem", "Έξοδος");

    public static string ForgotPassword_CheckInbox =>
        Strings.Get("Auth.ForgotPassword_CheckInbox", "Έλεγξε το email σου");

    public static string Register_AcceptTerms =>
        Strings.Get("Auth.Register_AcceptTerms", "Συμφωνώ με τους όρους χρήσης και την πολιτική απορρήτου.");

    public static string Register_ContinueAnyway =>
        Strings.Get("Auth.Register_ContinueAnyway", "Καταλαβαίνω και θέλω να συνεχίσω με νέο λογαριασμό.");

    public static string RegisterConfirmation_SentTo =>
        Strings.Get("Auth.RegisterConfirmation_SentTo", "Στείλαμε ένα link επιβεβαίωσης στο");

    public static string RegisterConfirmation_Instructions =>
        Strings.Get("Auth.RegisterConfirmation_Instructions", "Πάτησε το link στο email για να ενεργοποιήσεις τον λογαριασμό σου. Μπορεί να χρειαστούν μερικά λεπτά να φτάσει.");

    public static string RegisterConfirmation_NotReceived =>
        Strings.Get("Auth.RegisterConfirmation_NotReceived", "Δεν λάβατε το email;");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Auth() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Auth.Login_PageTitle"] = "Sign in",
        ["Auth.Login_EmailLabel"] = "Email",
        ["Auth.Login_PasswordLabel"] = "Password",
        ["Auth.Login_RememberMe"] = "Remember me",
        ["Auth.Login_SignInButton"] = "Sign in",
        ["Auth.Login_ForgotPasswordLink"] = "Forgot your password?",
        ["Auth.Login_NoAccountLink"] = "No account? Sign up",
        ["Auth.Login_InvalidCredentials"] = "Incorrect email or password.",
        ["Auth.Login_EmailNotConfirmed"] = "You must confirm your email address before signing in.",
        ["Auth.Login_AccountLocked"] = "Your account has been temporarily locked after too many failed attempts. Please try again later.",
        ["Auth.Register_PageTitle"] = "Sign up",
        ["Auth.Register_Heading"] = "Create your account",
        ["Auth.Register_Subtitle"] = "Get started with Roivo in under a minute.",
        ["Auth.Register_FullNameLabel"] = "Full name",
        ["Auth.Register_EmailLabel"] = "Email",
        ["Auth.Register_PasswordLabel"] = "Password",
        ["Auth.Register_ConfirmPasswordLabel"] = "Confirm password",
        ["Auth.Register_TenantTypeLabel"] = "Account type",
        ["Auth.Register_TenantType_Accountant"] = "Accounting firm",
        ["Auth.Register_TenantType_BusinessOwner"] = "Business",
        ["Auth.Register_AfmLabel"] = "VAT number",
        ["Auth.Register_BusinessNameLabel"] = "Business name",
        ["Auth.Register_OrganizationNameLabel"] = "Organisation name",
        ["Auth.Register_OrganizationNamePlaceholder"] = "e.g. Papadopoulos Accounting",
        ["Auth.Register_AcceptTermsLabel"] = "I agree to the terms of service and the privacy policy.",
        ["Auth.Register_SubmitButton"] = "Sign up",
        ["Auth.Register_HaveAccountLink"] = "Already have an account? Sign in",
        ["Auth.Register_HaveAccountPrompt"] = "Already have an account?",
        ["Auth.Register_DuplicateEmail"] = "An account with this email already exists.",
        ["Auth.Register_DuplicateAfmWarning"] = "An account with this VAT number already exists.",
        ["Auth.Register_ConfirmDuplicateLabel"] = "I understand and want to continue with a new account.",
        ["Auth.Register_PasswordTooWeak"] = "The password does not meet the security requirements.",
        ["Auth.Register_PasswordMismatch"] = "The passwords do not match.",
        ["Auth.PasswordRequirementsHint"] = "At least 12 characters, with an uppercase letter, a number and a symbol.",
        ["Auth.RegisterAgainLink"] = "Sign up again",
        ["Auth.ConfirmEmail_PageTitle"] = "Confirm email",
        ["Auth.ConfirmEmail_Pending_Heading"] = "Check your email",
        ["Auth.ConfirmEmail_Pending_Body"] = "We sent an email to {0}. Click the link inside it to complete your registration.",
        ["Auth.ConfirmEmail_Pending_ResendButton"] = "Resend email",
        ["Auth.ConfirmEmail_Pending_SentToPrefix"] = "We sent a confirmation link to",
        ["Auth.ConfirmEmail_Pending_YourEmailFallback"] = "your email",
        ["Auth.ConfirmEmail_Pending_Instructions"] = "Click the link in the email to activate your account. It may take a few minutes to arrive.",
        ["Auth.ConfirmEmail_Pending_NotReceived"] = "Didn't receive the email?",
        ["Auth.ConfirmEmail_Success_Heading"] = "Email confirmed",
        ["Auth.ConfirmEmail_Success_Body"] = "You can now sign in to your account.",
        ["Auth.ConfirmEmail_Failure_Heading"] = "Confirmation failed",
        ["Auth.ConfirmEmail_TokenInvalid"] = "This confirmation link is invalid or has expired.",
        ["Auth.ForgotPassword_PageTitle"] = "Password recovery",
        ["Auth.ForgotPassword_Subtitle"] = "Enter your email and we'll send you a link to set a new password.",
        ["Auth.ForgotPassword_EmailLabel"] = "Email",
        ["Auth.ForgotPassword_SubmitButton"] = "Send recovery link",
        ["Auth.ForgotPassword_Confirmation"] = "If an account exists for this email, you will receive a link to reset your password.",
        ["Auth.ForgotPassword_BackToLogin"] = "Back to sign in",
        ["Auth.ResetPassword_PageTitle"] = "Reset password",
        ["Auth.ResetPassword_NewPasswordLabel"] = "New password",
        ["Auth.ResetPassword_ConfirmPasswordLabel"] = "Confirm new password",
        ["Auth.ResetPassword_SubmitButton"] = "Change password",
        ["Auth.ResetPassword_Success"] = "Your password has been changed. You can now sign in.",
        ["Auth.ResetPassword_Success_Heading"] = "Password changed",
        ["Auth.ResetPassword_Success_Body"] = "You can now sign in with your new password.",
        ["Auth.ResetPassword_TokenInvalid"] = "This reset link is invalid or has expired.",
        ["Auth.AccessDenied_PageTitle"] = "Access denied",
        ["Auth.AccessDenied_Body"] = "You do not have permission to view this page.",
        ["Auth.AccessDenied_BackHome"] = "Back to home",
        ["Auth.LogoutMenuItem"] = "Sign out",
        ["Auth.ForgotPassword_CheckInbox"] = "Check your email",
        ["Auth.Register_AcceptTerms"] = "I agree to the terms of use and the privacy policy.",
        ["Auth.Register_ContinueAnyway"] = "I understand and want to continue with a new account.",
        ["Auth.RegisterConfirmation_SentTo"] = "We sent a confirmation link to",
        ["Auth.RegisterConfirmation_Instructions"] = "Click the link in the email to activate your account. It may take a few minutes to arrive.",
        ["Auth.RegisterConfirmation_NotReceived"] = "Did not receive the email?",
    });
}
