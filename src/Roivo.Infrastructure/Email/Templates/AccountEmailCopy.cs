namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Greek and English copy for the two account emails — registration
/// confirmation and password reset.
/// </summary>
/// <remarks>
/// <para>
/// CODING_STANDARDS § user-facing strings puts copy in <c>Roivo.Resources</c>,
/// and this belongs in <c>Auth.cs</c> next to the rest of the sign-up strings.
/// It is parked here because these strings did not exist anywhere before — they
/// were inline literals in the two Razor page models — and the resources
/// project is being edited concurrently in this change. Moving these eleven
/// pairs into <c>Auth.cs</c> and switching the call sites to
/// <c>Strings.Get</c> is a mechanical follow-up.
/// </para>
/// <para>
/// Language is an explicit argument, matching the <c>english</c> parameter the
/// templates take, rather than a <see cref="Roivo.Resources.Strings"/> lookup
/// off the ambient culture: these two emails are the ones that must never go
/// out in the wrong language.
/// </para>
/// </remarks>
internal static class AccountEmailCopy
{
    internal static string ConfirmSubject(bool english)
        => english ? "Confirm your email — Roivo" : "Επιβεβαίωση email — Roivo";

    internal static string ConfirmHeading(bool english)
        => english ? "Welcome to Roivo" : "Καλωσόρισες στο Roivo";

    /// <summary><c>{0}</c> is the recipient's full name.</summary>
    internal static string ConfirmGreeting(bool english)
        => english ? "Hi {0}," : "Γεια σου {0},";

    internal static string ConfirmBody(bool english)
        => english
            ? "One step left. Confirm your email address to activate your account and start connecting your invoices and bank transactions."
            : "Έμεινε ένα βήμα. Επιβεβαίωσε το email σου για να ενεργοποιηθεί ο λογαριασμός σου και να αρχίσεις να συνδέεις παραστατικά και τραπεζικές κινήσεις.";

    internal static string ConfirmCta(bool english)
        => english ? "Confirm email" : "Επιβεβαίωση email";

    internal static string ConfirmFootnote(bool english)
        => english
            ? "If you did not sign up for Roivo, you can ignore this message — no account will be activated."
            : "Αν δεν εγγράφηκες στο Roivo, αγνόησε αυτό το μήνυμα — κανένας λογαριασμός δεν θα ενεργοποιηθεί.";

    internal static string ConfirmFooterIntro(bool english)
        => english
            ? "You are receiving this message because a Roivo account was created with this email address."
            : "Λαμβάνεις αυτό το μήνυμα επειδή δημιουργήθηκε λογαριασμός Roivo με αυτή τη διεύθυνση email.";

    internal static string ResetSubject(bool english)
        => english ? "Reset your password — Roivo" : "Επαναφορά κωδικού — Roivo";

    internal static string ResetHeading(bool english)
        => english ? "Reset your password" : "Επαναφορά κωδικού";

    internal static string ResetBody(bool english)
        => english
            ? "We received a request to reset the password for your Roivo account. Use the button below to choose a new one."
            : "Λάβαμε αίτημα για επαναφορά του κωδικού του λογαριασμού σου στο Roivo. Πάτησε το κουμπί για να ορίσεις νέο κωδικό.";

    internal static string ResetCta(bool english)
        => english ? "Reset password" : "Επαναφορά κωδικού";

    internal static string ResetFootnote(bool english)
        => english
            ? "If you did not ask for a password reset, ignore this message. Your password has not changed and the link will expire on its own."
            : "Αν δεν ζήτησες επαναφορά κωδικού, αγνόησε αυτό το μήνυμα. Ο κωδικός σου δεν άλλαξε και ο σύνδεσμος θα λήξει από μόνος του.";

    internal static string ResetFooterIntro(bool english)
        => english
            ? "You are receiving this message because a password reset was requested for your Roivo account."
            : "Λαμβάνεις αυτό το μήνυμα επειδή ζητήθηκε επαναφορά κωδικού για τον λογαριασμό σου στο Roivo.";

    /// <summary>Label above the copy-me URL under the button.</summary>
    internal static string LinkFallbackLabel(bool english)
        => english
            ? "If the button does not work, copy this link into your browser:"
            : "Αν το κουμπί δεν λειτουργεί, αντίγραψε αυτόν τον σύνδεσμο στον browser σου:";

    /// <summary>Footer link text; the account emails have no notification preference to manage.</summary>
    internal static string FooterLinkLabel(bool english)
        => english ? "Open Roivo" : "Άνοιγμα Roivo";
}
