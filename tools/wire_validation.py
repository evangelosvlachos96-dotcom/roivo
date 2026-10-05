"""Point DataAnnotations attributes at the ValidationMessages resx instead of
inline Greek literals, so validation errors follow the request culture."""
import io, re, glob

# Greek literal -> resx key
KEYS = {
 "Επίλεξε τύπο λογαριασμού": "AccountTypeRequired",
 "Επιβεβαίωσε τον κωδικό": "ConfirmPasswordRequired",
 "Η επωνυμία είναι υποχρεωτική": "OrganisationNameRequired",
 "Μη έγκυρο email": "EmailInvalid",
 "Ο ΑΦΜ είναι υποχρεωτικός": "AfmRequired",
 "Ο ΑΦΜ πρέπει να είναι 9 ψηφία": "AfmLength",
 "Ο κωδικός είναι υποχρεωτικός": "PasswordRequired",
 "Οι κωδικοί δεν ταιριάζουν": "PasswordsDoNotMatch",
 "Πρέπει να αποδεχτείς τους όρους": "TermsMustBeAccepted",
 "Το email είναι υποχρεωτικό": "EmailRequired",
 "Το ονοματεπώνυμο είναι υποχρεωτικό": "FullNameRequired",
 "Το ονοματεπώνυμο πρέπει να έχει τουλάχιστον 2 χαρακτήρες": "FullNameTooShort",
}

total = 0
for path in sorted(glob.glob('src/Roivo.Web/Areas/Account/Pages/*.cshtml.cs')):
    s = io.open(path, encoding='utf-8').read()
    original = s
    for greek, key in KEYS.items():
        # ErrorMessage = "greek"  ->  resource type + name
        s = s.replace(
            'ErrorMessage = "%s"' % greek,
            'ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.%s)' % key)
    if s != original:
        if 'using Roivo.Resources;' not in s:
            m = re.search(r'^using [^\n]+;\n', s, re.M)
            s = s[:m.end()] + 'using Roivo.Resources;\n' + s[m.end():] if m else 'using Roivo.Resources;\n' + s
        io.open(path, 'w', encoding='utf-8', newline='').write(s)
        n = sum(original.count('ErrorMessage = "%s"' % g) for g in KEYS)
        total += n
        print('  %s: %d attributes rewired' % (path.split('/')[-1], n))

print('total rewired: %d' % total)
