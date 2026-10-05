namespace Roivo.Resources;

/// <summary>
/// Public, pre-login marketing copy: the landing page plus the privacy and
/// terms pages.
/// </summary>
/// <remarks>
/// These strings are read by anonymous visitors, so they are the only copy in
/// the product that doubles as positioning. Keep them aligned with
/// <c>marketing/positioning.md</c>, which is the canonical source.
///
/// The legal pages are TEMPLATES pending review by a Greek lawyer. Every value
/// that must not be invented — company name, address, VAT and registry
/// numbers, contact addresses, retention periods, jurisdiction — is left as a
/// bracketed placeholder such as <c>[COMPANY_ADDRESS]</c> so that nothing
/// fictional can ship by accident. Search this file for <c>[</c> to find them
/// all.
/// </remarks>
public static class Marketing
{
    // ---------------------------------------------------------------- brand

    public static string BrandName => Strings.Get("Marketing.BrandName", "Roivo");
    public static string Tagline => Strings.Get("Marketing.Tagline", "Cashflow Intelligence");

    // ------------------------------------------------------- landing / SEO

    public static string Landing_PageTitle =>
        Strings.Get("Marketing.Landing_PageTitle", "Ταμειακές ροές χωρίς εκπλήξεις");

    public static string Landing_MetaDescription => Strings.Get("Marketing.Landing_MetaDescription",
        "Το Roivo συνδέει τους τραπεζικούς λογαριασμούς με το myDATA, συμφωνεί αυτόματα κινήσεις και παραστατικά και προβλέπει την ταμειακή ροή 90 ημερών μπροστά.");

    public static string Landing_SkipToContent =>
        Strings.Get("Marketing.Landing_SkipToContent", "Μετάβαση στο περιεχόμενο");

    public static string Landing_LogoAlt =>
        Strings.Get("Marketing.Landing_LogoAlt", "Roivo — Cashflow Intelligence");

    // ------------------------------------------------------------ nav / top

    public static string Nav_Features => Strings.Get("Marketing.Nav_Features", "Λειτουργίες");
    public static string Nav_HowItWorks => Strings.Get("Marketing.Nav_HowItWorks", "Πώς δουλεύει");
    public static string Nav_Accountants => Strings.Get("Marketing.Nav_Accountants", "Για λογιστές");
    public static string Nav_Pricing => Strings.Get("Marketing.Nav_Pricing", "Τιμές");

    // ------------------------------------------------------------ hero

    public static string Hero_Headline =>
        Strings.Get("Marketing.Hero_Headline", "Ταμειακές ροές χωρίς εκπλήξεις");

    public static string Hero_SubHeadline => Strings.Get("Marketing.Hero_SubHeadline",
        "Το Roivo συμφωνεί αυτόματα τις τραπεζικές κινήσεις με τα παραστατικά του myDATA, προβλέπει την ταμειακή ροή 90 ημερών μπροστά και σας θυμίζει κάθε φορολογική υποχρέωση πριν τη λήξη της — όχι μετά.");

    public static string Hero_PrimaryCta => Strings.Get("Marketing.Hero_PrimaryCta", "Ξεκινήστε δωρεάν");
    public static string Hero_SecondaryCta => Strings.Get("Marketing.Hero_SecondaryCta", "Σύνδεση");

    public static string Hero_Reassurance => Strings.Get("Marketing.Hero_Reassurance",
        "Χωρίς κάρτα. Χωρίς εγκατάσταση. Δεδομένα σε servers εντός ΕΕ.");

    // ------------------------------------------------------------ features

    public static string Features_Title => Strings.Get("Marketing.Features_Title", "Τι κάνει το Roivo");

    public static string Features_Subtitle => Strings.Get("Marketing.Features_Subtitle",
        "Δύο πηγές δεδομένων που μέχρι σήμερα δούλευαν χωριστά, σε μία εικόνα.");

    public static string Feature_Psd2_Title => Strings.Get("Marketing.Feature_Psd2_Title", "Σύνδεση τραπεζών (PSD2)");

    public static string Feature_Psd2_Desc => Strings.Get("Marketing.Feature_Psd2_Desc",
        "Συνδέετε τους λογαριασμούς σας μέσω Open Banking. Οι κινήσεις ενημερώνονται αυτόματα, χωρίς να μπαίνετε σε δέκα διαφορετικά portals τραπεζών.");

    public static string Feature_MyData_Title => Strings.Get("Marketing.Feature_MyData_Title", "Συγχρονισμός myDATA");

    public static string Feature_MyData_Desc => Strings.Get("Marketing.Feature_MyData_Desc",
        "Τα παραστατικά εσόδων και εξόδων κατεβαίνουν απευθείας από την πλατφόρμα myDATA της ΑΑΔΕ, χωρίς αντιγραφή στο χέρι.");

    public static string Feature_Reconciliation_Title =>
        Strings.Get("Marketing.Feature_Reconciliation_Title", "Αυτόματη συμφωνία");

    public static string Feature_Reconciliation_Desc => Strings.Get("Marketing.Feature_Reconciliation_Desc",
        "Το Roivo αναγνωρίζει ποιο τιμολόγιο πληρώθηκε από ποια κατάθεση και σας αφήνει να δείτε μόνο ό,τι πραγματικά χρειάζεται έλεγχο.");

    public static string Feature_Forecast_Title => Strings.Get("Marketing.Feature_Forecast_Title", "Πρόβλεψη 90 ημερών");

    public static string Feature_Forecast_Desc => Strings.Get("Marketing.Feature_Forecast_Desc",
        "Δείτε πού πάει το ταμείο σας τους επόμενους τρεις μήνες, με βάση τις τάσεις εσόδων-εξόδων, τα ανοιχτά τιμολόγια και τις γνωστές περιοδικές υποχρεώσεις.");

    public static string Feature_Tax_Title => Strings.Get("Marketing.Feature_Tax_Title", "Φορολογικές υποχρεώσεις");

    public static string Feature_Tax_Desc => Strings.Get("Marketing.Feature_Tax_Desc",
        "ΦΠΑ, ΕΦΚΑ, παρακρατούμενοι φόροι, προκαταβολή φόρου: ειδοποίηση έγκαιρα, με το ποσό και την ημερομηνία λήξης.");

    // ------------------------------------------------------- how it works

    public static string How_Title => Strings.Get("Marketing.How_Title", "Πώς δουλεύει");
    public static string How_Subtitle => Strings.Get("Marketing.How_Subtitle", "Τρία βήματα. Μία φορά.");

    public static string How_Step1_Label => Strings.Get("Marketing.How_Step1_Label", "Σύνδεση");

    public static string How_Step1_Desc => Strings.Get("Marketing.How_Step1_Desc",
        "Συνδέετε τράπεζα και myDATA μία φορά, με ασφαλή εξουσιοδότηση που μπορείτε να ανακαλέσετε όποτε θέλετε.");

    public static string How_Step2_Label => Strings.Get("Marketing.How_Step2_Label", "Συγχρονισμός");

    public static string How_Step2_Desc => Strings.Get("Marketing.How_Step2_Desc",
        "Το Roivo κατεβάζει κινήσεις και παραστατικά κάθε μέρα και τα συμφωνεί αυτόματα μεταξύ τους.");

    public static string How_Step3_Label => Strings.Get("Marketing.How_Step3_Label", "Πρόβλεψη");

    public static string How_Step3_Desc => Strings.Get("Marketing.How_Step3_Desc",
        "Βλέπετε την ταμειακή ροή 90 ημερών μπροστά και τις υποχρεώσεις που έρχονται, πριν γίνουν πρόβλημα.");

    // --------------------------------------------------------- accountants

    public static string Accountants_Eyebrow =>
        Strings.Get("Marketing.Accountants_Eyebrow", "Για λογιστικά γραφεία");

    public static string Accountants_Title =>
        Strings.Get("Marketing.Accountants_Title", "Όλοι οι πελάτες σας σε μία οθόνη");

    public static string Accountants_Lead => Strings.Get("Marketing.Accountants_Lead",
        "Αν διαχειρίζεστε από 20 έως 500 πελάτες, το Roivo σας δείχνει ποιος οδεύει σε ταμειακό πρόβλημα πριν σας τηλεφωνήσει για να σας το πει.");

    public static string Accountants_Point1_Title =>
        Strings.Get("Marketing.Accountants_Point1_Title", "Πίνακας πολλών πελατών");

    public static string Accountants_Point1_Desc => Strings.Get("Marketing.Accountants_Point1_Desc",
        "Μία οθόνη με όλους τους πελάτες σας, με χρωματική ένδειξη ρευστότητας για τον καθένα.");

    public static string Accountants_Point2_Title =>
        Strings.Get("Marketing.Accountants_Point2_Title", "Ειδοποιήσεις ρευστότητας");

    public static string Accountants_Point2_Desc => Strings.Get("Marketing.Accountants_Point2_Desc",
        "Ειδοποίηση όταν μια επερχόμενη υποχρέωση δεν καλύπτεται από τα τρέχοντα διαθέσιμα του πελάτη.");

    public static string Accountants_Point3_Title =>
        Strings.Get("Marketing.Accountants_Point3_Title", "Ενοποιημένες αναφορές");

    public static string Accountants_Point3_Desc => Strings.Get("Marketing.Accountants_Point3_Desc",
        "Συγκεντρωτικές αναφορές για όλο το πελατολόγιο, έτοιμες για εξαγωγή και αποστολή.");

    public static string Accountants_Cta =>
        Strings.Get("Marketing.Accountants_Cta", "Δοκιμάστε το στο γραφείο σας");

    public static string Accountants_Mock_Title => Strings.Get("Marketing.Accountants_Mock_Title", "Πελάτες");

    public static string Accountants_Mock_ColumnBalance =>
        Strings.Get("Marketing.Accountants_Mock_ColumnBalance", "Διαθέσιμα");

    public static string Accountants_Mock_ColumnForecast =>
        Strings.Get("Marketing.Accountants_Mock_ColumnForecast", "Πρόβλεψη 30 ημ.");

    public static string Accountants_Mock_Client1 => Strings.Get("Marketing.Accountants_Mock_Client1", "Πελάτης Α");
    public static string Accountants_Mock_Client2 => Strings.Get("Marketing.Accountants_Mock_Client2", "Πελάτης Β");
    public static string Accountants_Mock_Client3 => Strings.Get("Marketing.Accountants_Mock_Client3", "Πελάτης Γ");

    // Fictional figures for the dashboard mock-up. They live here rather than
    // in the markup so the thousands separator can follow the language.
    public static string Accountants_Mock_Value1 => Strings.Get("Marketing.Accountants_Mock_Value1", "42.300 €");
    public static string Accountants_Mock_Value2 => Strings.Get("Marketing.Accountants_Mock_Value2", "8.150 €");
    public static string Accountants_Mock_Value3 => Strings.Get("Marketing.Accountants_Mock_Value3", "1.470 €");

    public static string Accountants_Mock_StatusOk => Strings.Get("Marketing.Accountants_Mock_StatusOk", "Εντάξει");

    public static string Accountants_Mock_StatusWatch =>
        Strings.Get("Marketing.Accountants_Mock_StatusWatch", "Παρακολούθηση");

    public static string Accountants_Mock_StatusRisk =>
        Strings.Get("Marketing.Accountants_Mock_StatusRisk", "Κίνδυνος");

    public static string Accountants_Mock_Caption => Strings.Get("Marketing.Accountants_Mock_Caption",
        "Ενδεικτική απεικόνιση. Τα νούμερα είναι παράδειγμα.");

    // ------------------------------------------------------------- pricing

    public static string Pricing_Title => Strings.Get("Marketing.Pricing_Title", "Τιμολόγηση");

    public static string Pricing_Subtitle => Strings.Get("Marketing.Pricing_Subtitle",
        "Ξεκινήστε δωρεάν με μία επιχείρηση. Αναβαθμίστε όταν χρειαστείτε περισσότερες.");

    public static string Pricing_Recommended => Strings.Get("Marketing.Pricing_Recommended", "Προτεινόμενο");

    public static string Pricing_Free_Name => Strings.Get("Marketing.Pricing_Free_Name", "Δωρεάν");
    public static string Pricing_Free_Price => Strings.Get("Marketing.Pricing_Free_Price", "0 €");
    public static string Pricing_Free_Period => Strings.Get("Marketing.Pricing_Free_Period", "/μήνα");

    public static string Pricing_Free_Desc =>
        Strings.Get("Marketing.Pricing_Free_Desc", "Για μία επιχείρηση που θέλει να δει την εικόνα της.");

    public static string Pricing_Free_Feature1 => Strings.Get("Marketing.Pricing_Free_Feature1", "1 επιχείρηση");

    public static string Pricing_Free_Feature2 =>
        Strings.Get("Marketing.Pricing_Free_Feature2", "Σύνδεση τράπεζας και myDATA");

    public static string Pricing_Free_Feature3 =>
        Strings.Get("Marketing.Pricing_Free_Feature3", "Πρόβλεψη 90 ημερών και φορολογικό ημερολόγιο");

    public static string Pricing_Pro_Name => Strings.Get("Marketing.Pricing_Pro_Name", "Pro");
    public static string Pricing_Pro_Price => Strings.Get("Marketing.Pricing_Pro_Price", "29 €");
    public static string Pricing_Pro_Period => Strings.Get("Marketing.Pricing_Pro_Period", "/μήνα");

    public static string Pricing_Pro_Desc =>
        Strings.Get("Marketing.Pricing_Pro_Desc", "Για λογιστικά γραφεία και πολλαπλές επιχειρήσεις.");

    public static string Pricing_Pro_Feature1 =>
        Strings.Get("Marketing.Pricing_Pro_Feature1", "Απεριόριστες επιχειρήσεις");

    public static string Pricing_Pro_Feature2 =>
        Strings.Get("Marketing.Pricing_Pro_Feature2", "Πίνακας λογιστή και ειδοποιήσεις ρευστότητας");

    public static string Pricing_Pro_Feature3 =>
        Strings.Get("Marketing.Pricing_Pro_Feature3", "Ενοποιημένες αναφορές πελατολογίου");

    public static string Pricing_Enterprise_Name => Strings.Get("Marketing.Pricing_Enterprise_Name", "Enterprise");

    public static string Pricing_Enterprise_Price =>
        Strings.Get("Marketing.Pricing_Enterprise_Price", "Κατόπιν επικοινωνίας");

    public static string Pricing_Enterprise_Period => Strings.Get("Marketing.Pricing_Enterprise_Period", "");

    public static string Pricing_Enterprise_Desc =>
        Strings.Get("Marketing.Pricing_Enterprise_Desc", "Για μεγάλα γραφεία με ειδικές απαιτήσεις.");

    public static string Pricing_Enterprise_Feature1 =>
        Strings.Get("Marketing.Pricing_Enterprise_Feature1", "Όλα τα χαρακτηριστικά του Pro");

    public static string Pricing_Enterprise_Feature2 =>
        Strings.Get("Marketing.Pricing_Enterprise_Feature2", "Προτεραιότητα υποστήριξης και onboarding");

    public static string Pricing_Enterprise_Feature3 =>
        Strings.Get("Marketing.Pricing_Enterprise_Feature3", "Συμφωνία επεξεργασίας δεδομένων κατά παραγγελία");

    public static string Pricing_Note => Strings.Get("Marketing.Pricing_Note",
        "Οι τιμές είναι ενδεικτικές και δεν περιλαμβάνουν ΦΠΑ. Σε αυτή τη φάση δεν γίνεται online πληρωμή μέσω της πλατφόρμας.");

    // -------------------------------------------------------------- footer

    public static string Footer_Privacy => Strings.Get("Marketing.Footer_Privacy", "Πολιτική απορρήτου");
    public static string Footer_Terms => Strings.Get("Marketing.Footer_Terms", "Όροι χρήσης");

    public static string Footer_Copyright =>
        Strings.Get("Marketing.Footer_Copyright", "© 2026 Roivo — Cashflow Intelligence");

    public static string Footer_Blurb => Strings.Get("Marketing.Footer_Blurb",
        "Ταμειακή ροή για ελληνικές επιχειρήσεις και τους λογιστές τους.");

    // ------------------------------------------------- legal, shared parts

    /// <summary>
    /// Deliberately rendered in BOTH languages at once on the legal pages: a
    /// reader must see the "not reviewed yet" warning whichever language they
    /// landed in, and a Greek lawyer reviewing the page must see it too.
    /// </summary>
    public static string Legal_TemplateNotice_Heading_El => "Υπόδειγμα — εκκρεμεί νομικός έλεγχος";

    public static string Legal_TemplateNotice_Body_El =>
        "Το κείμενο που ακολουθεί είναι πρότυπο και δεν έχει ελεγχθεί από δικηγόρο. Δεν αποτελεί νομική συμβουλή και δεν δεσμεύει κανέναν. Τα πεδία σε αγκύλες, π.χ. [COMPANY_ADDRESS], πρέπει να συμπληρωθούν με πραγματικά στοιχεία πριν τη δημοσίευση.";

    public static string Legal_TemplateNotice_Heading_En => "Template — pending legal review";

    public static string Legal_TemplateNotice_Body_En =>
        "The text below is a template and has not been reviewed by a lawyer. It is not legal advice and binds no one. The bracketed fields, for example [COMPANY_ADDRESS], must be replaced with real values before publication.";

    public static string Legal_LastUpdated =>
        Strings.Get("Marketing.Legal_LastUpdated", "Τελευταία ενημέρωση: [LAST_UPDATED_DATE]");

    public static string Legal_BackToHome => Strings.Get("Marketing.Legal_BackToHome", "Επιστροφή στην αρχική");

    public static string Legal_TableOfContents => Strings.Get("Marketing.Legal_TableOfContents", "Περιεχόμενα");

    // -------------------------------------------------------------- privacy

    public static string Privacy_PageTitle =>
        Strings.Get("Marketing.Privacy_PageTitle", "Πολιτική απορρήτου");

    public static string Privacy_MetaDescription => Strings.Get("Marketing.Privacy_MetaDescription",
        "Πώς το Roivo συλλέγει, χρησιμοποιεί και προστατεύει τα δεδομένα σας, σύμφωνα με τον GDPR.");

    public static string Privacy_Title => Strings.Get("Marketing.Privacy_Title", "Πολιτική απορρήτου");

    public static string Privacy_Intro => Strings.Get("Marketing.Privacy_Intro",
        "Η παρούσα πολιτική εξηγεί ποια προσωπικά δεδομένα επεξεργάζεται το Roivo, για ποιον σκοπό, σε ποιους τα διαβιβάζει και ποια δικαιώματα έχετε βάσει του Κανονισμού (ΕΕ) 2016/679 (GDPR) και του ν. 4624/2019.");

    public static string Privacy_S1_Title => Strings.Get("Marketing.Privacy_S1_Title", "1. Υπεύθυνος επεξεργασίας");

    public static string Privacy_S1_Body => Strings.Get("Marketing.Privacy_S1_Body",
        "Υπεύθυνος επεξεργασίας είναι η [COMPANY_LEGAL_NAME], με έδρα [COMPANY_ADDRESS], ΑΦΜ [COMPANY_VAT_NUMBER], αριθμός ΓΕΜΗ [COMPANY_REGISTRY_NUMBER]. Για κάθε θέμα προστασίας δεδομένων μπορείτε να επικοινωνείτε στο [PRIVACY_CONTACT_EMAIL].");

    public static string Privacy_S2_Title => Strings.Get("Marketing.Privacy_S2_Title", "2. Δεδομένα που συλλέγουμε");

    public static string Privacy_S2_Body => Strings.Get("Marketing.Privacy_S2_Body",
        "Συλλέγουμε μόνο όσα δεδομένα χρειάζονται για να λειτουργήσει η υπηρεσία:");

    public static string Privacy_S2_Item1 => Strings.Get("Marketing.Privacy_S2_Item1",
        "Στοιχεία λογαριασμού: ονοματεπώνυμο, διεύθυνση email, κρυπτογραφημένος κωδικός πρόσβασης, τύπος λογαριασμού (επιχείρηση ή λογιστικό γραφείο).");

    public static string Privacy_S2_Item2 => Strings.Get("Marketing.Privacy_S2_Item2",
        "Στοιχεία επιχείρησης: επωνυμία, ΑΦΜ, στοιχεία επικοινωνίας, και — για λογιστικά γραφεία — τα αντίστοιχα στοιχεία των πελατών που εσείς καταχωρείτε.");

    public static string Privacy_S2_Item3 => Strings.Get("Marketing.Privacy_S2_Item3",
        "Τραπεζικά δεδομένα: IBAN, υπόλοιπα και κινήσεις λογαριασμών, τα οποία αντλούνται μέσω PSD2 και μόνο αφού δώσετε ρητή εξουσιοδότηση στην τράπεζά σας.");

    public static string Privacy_S2_Item4 => Strings.Get("Marketing.Privacy_S2_Item4",
        "Φορολογικά παραστατικά: στοιχεία τιμολογίων εσόδων και εξόδων που έχουν διαβιβαστεί στο myDATA (ΑΦΜ αντισυμβαλλομένου, ποσά, ημερομηνίες, σειρές και αριθμοί παραστατικών).");

    public static string Privacy_S2_Item5 => Strings.Get("Marketing.Privacy_S2_Item5",
        "Τεχνικά δεδομένα: διεύθυνση IP, τύπος προγράμματος περιήγησης, χρόνοι πρόσβασης και αρχεία καταγραφής ενεργειών για λόγους ασφαλείας και ελέγχου.");

    public static string Privacy_S2_Note => Strings.Get("Marketing.Privacy_S2_Note",
        "Δεν ζητάμε ποτέ τους κωδικούς e-banking σας. Η πρόσβαση στα τραπεζικά δεδομένα γίνεται αποκλειστικά μέσω των επίσημων διεπαφών PSD2.");

    public static string Privacy_S3_Title => Strings.Get("Marketing.Privacy_S3_Title", "3. Σκοποί και νομική βάση");

    public static string Privacy_S3_Body => Strings.Get("Marketing.Privacy_S3_Body",
        "Επεξεργαζόμαστε τα δεδομένα σας για τους εξής σκοπούς και με τις εξής νομικές βάσεις:");

    public static string Privacy_S3_Item1 => Strings.Get("Marketing.Privacy_S3_Item1",
        "Παροχή της υπηρεσίας — συμφωνία κινήσεων και παραστατικών, πρόβλεψη ταμειακής ροής, ειδοποιήσεις υποχρεώσεων: εκτέλεση της σύμβασης (άρθρο 6 παρ. 1 στοιχ. β GDPR).");

    public static string Privacy_S3_Item2 => Strings.Get("Marketing.Privacy_S3_Item2",
        "Άντληση τραπεζικών δεδομένων και παραστατικών myDATA: η ρητή σας συγκατάθεση και εξουσιοδότηση, την οποία μπορείτε να ανακαλέσετε οποτεδήποτε (άρθρο 6 παρ. 1 στοιχ. α GDPR).");

    public static string Privacy_S3_Item3 => Strings.Get("Marketing.Privacy_S3_Item3",
        "Ασφάλεια, πρόληψη απάτης και τεχνική υποστήριξη: έννομο συμφέρον (άρθρο 6 παρ. 1 στοιχ. στ GDPR).");

    public static string Privacy_S3_Item4 => Strings.Get("Marketing.Privacy_S3_Item4",
        "Τήρηση φορολογικών και λογιστικών υποχρεώσεων της εταιρείας μας: συμμόρφωση με νομική υποχρέωση (άρθρο 6 παρ. 1 στοιχ. γ GDPR).");

    public static string Privacy_S4_Title =>
        Strings.Get("Marketing.Privacy_S4_Title", "4. Τρίτοι αποδέκτες και εκτελούντες την επεξεργασία");

    public static string Privacy_S4_Body => Strings.Get("Marketing.Privacy_S4_Body",
        "Για να λειτουργήσει η υπηρεσία, δεδομένα σας διαβιβάζονται στους εξής παρόχους:");

    public static string Privacy_S4_Item1 => Strings.Get("Marketing.Privacy_S4_Item1",
        "Enable Banking — αδειοδοτημένος πάροχος υπηρεσιών πληροφόρησης λογαριασμού (AISP) βάσει PSD2. Μέσω αυτού πραγματοποιείται η σύνδεση με την τράπεζά σας και η άντληση υπολοίπων και κινήσεων.");

    public static string Privacy_S4_Item2 => Strings.Get("Marketing.Privacy_S4_Item2",
        "ΑΑΔΕ — πλατφόρμα myDATA. Από εκεί αντλούμε τα παραστατικά σας, χρησιμοποιώντας τα διαπιστευτήρια χρήστη myDATA που εσείς καταχωρείτε στο Roivo.");

    public static string Privacy_S4_Item3 => Strings.Get("Marketing.Privacy_S4_Item3",
        "[HOSTING_PROVIDER] — φιλοξενία της εφαρμογής και της βάσης δεδομένων, σε κέντρα δεδομένων εντός Ευρωπαϊκής Ένωσης.");

    public static string Privacy_S4_Item4 => Strings.Get("Marketing.Privacy_S4_Item4",
        "[EMAIL_PROVIDER] — αποστολή email ειδοποιήσεων και διαχείρισης λογαριασμού.");

    public static string Privacy_S4_Note => Strings.Get("Marketing.Privacy_S4_Note",
        "Δεν πωλούμε, δεν ενοικιάζουμε και δεν ανταλλάσσουμε προσωπικά δεδομένα. Με κάθε εκτελούντα την επεξεργασία έχει υπογραφεί σύμβαση κατά το άρθρο 28 GDPR.");

    public static string Privacy_S5_Title => Strings.Get("Marketing.Privacy_S5_Title", "5. Cookies");

    public static string Privacy_S5_Body => Strings.Get("Marketing.Privacy_S5_Body",
        "Χρησιμοποιούμε αυστηρά απαραίτητα cookies για τη σύνδεση, τη διατήρηση της συνεδρίας και την προστασία από επιθέσεις CSRF. Δεν χρησιμοποιούμε cookies διαφήμισης ή παρακολούθησης τρίτων. Τυχόν cookies στατιστικής ανάλυσης θα ενεργοποιηθούν μόνο μετά από ρητή συγκατάθεσή σας.");

    public static string Privacy_S6_Title => Strings.Get("Marketing.Privacy_S6_Title", "6. Τα δικαιώματά σας");

    public static string Privacy_S6_Body => Strings.Get("Marketing.Privacy_S6_Body",
        "Ως υποκείμενο των δεδομένων έχετε δικαίωμα:");

    public static string Privacy_S6_Item1 =>
        Strings.Get("Marketing.Privacy_S6_Item1", "πρόσβασης στα δεδομένα που τηρούμε για εσάς,");

    public static string Privacy_S6_Item2 =>
        Strings.Get("Marketing.Privacy_S6_Item2", "διόρθωσης ανακριβών ή ελλιπών δεδομένων,");

    public static string Privacy_S6_Item3 =>
        Strings.Get("Marketing.Privacy_S6_Item3", "διαγραφής («δικαίωμα στη λήθη»), εφόσον δεν υπάρχει αντίθετη νομική υποχρέωση,");

    public static string Privacy_S6_Item4 =>
        Strings.Get("Marketing.Privacy_S6_Item4", "περιορισμού της επεξεργασίας,");

    public static string Privacy_S6_Item5 =>
        Strings.Get("Marketing.Privacy_S6_Item5", "φορητότητας των δεδομένων σε δομημένο, κοινώς χρησιμοποιούμενο μορφότυπο,");

    public static string Privacy_S6_Item6 =>
        Strings.Get("Marketing.Privacy_S6_Item6", "εναντίωσης στην επεξεργασία που βασίζεται σε έννομο συμφέρον,");

    public static string Privacy_S6_Item7 =>
        Strings.Get("Marketing.Privacy_S6_Item7", "ανάκλησης της συγκατάθεσής σας για τη σύνδεση τράπεζας ή myDATA, οποτεδήποτε και χωρίς αιτιολογία.");

    public static string Privacy_S6_Response => Strings.Get("Marketing.Privacy_S6_Response",
        "Απαντάμε σε κάθε αίτημα εντός ενός μηνός. Υποβάλετε το αίτημά σας στο [PRIVACY_CONTACT_EMAIL].");

    public static string Privacy_S6_Complaint => Strings.Get("Marketing.Privacy_S6_Complaint",
        "Έχετε επίσης δικαίωμα καταγγελίας στην Αρχή Προστασίας Δεδομένων Προσωπικού Χαρακτήρα (www.dpa.gr).");

    public static string Privacy_S7_Title => Strings.Get("Marketing.Privacy_S7_Title", "7. Χρόνος διατήρησης");

    public static string Privacy_S7_Body => Strings.Get("Marketing.Privacy_S7_Body",
        "Τα στοιχεία λογαριασμού διατηρούνται όσο ο λογαριασμός σας είναι ενεργός και για [ACCOUNT_RETENTION_PERIOD] μετά τη διαγραφή του. Τα τραπεζικά δεδομένα και τα παραστατικά διατηρούνται για [FINANCIAL_DATA_RETENTION_PERIOD]. Τα αρχεία καταγραφής ασφαλείας διατηρούνται για [LOG_RETENTION_PERIOD]. Μετά τη λήξη των διαστημάτων αυτών τα δεδομένα διαγράφονται ή ανωνυμοποιούνται.");

    public static string Privacy_S8_Title => Strings.Get("Marketing.Privacy_S8_Title", "8. Ασφάλεια");

    public static string Privacy_S8_Body => Strings.Get("Marketing.Privacy_S8_Body",
        "Τα δεδομένα μεταφέρονται κρυπτογραφημένα (TLS) και αποθηκεύονται κρυπτογραφημένα. Τα διαπιστευτήρια και τα tokens πρόσβασης τηρούνται κρυπτογραφημένα χωριστά από τα υπόλοιπα δεδομένα. Η πρόσβαση του προσωπικού είναι περιορισμένη και καταγράφεται. Σε περίπτωση παραβίασης δεδομένων ενημερώνουμε την Αρχή και, όπου απαιτείται, εσάς, εντός 72 ωρών.");

    public static string Privacy_S9_Title => Strings.Get("Marketing.Privacy_S9_Title", "9. Μεταφορές εκτός ΕΕ");

    public static string Privacy_S9_Body => Strings.Get("Marketing.Privacy_S9_Body",
        "Τα δεδομένα σας φυλάσσονται σε κέντρα δεδομένων εντός Ευρωπαϊκής Ένωσης. Αν σε κάποια λειτουργία απαιτηθεί μεταφορά σε τρίτη χώρα, αυτή γίνεται μόνο με επαρκείς εγγυήσεις (απόφαση επάρκειας ή τυποποιημένες συμβατικές ρήτρες) και αναφέρεται ρητά εδώ.");

    public static string Privacy_S10_Title => Strings.Get("Marketing.Privacy_S10_Title", "10. Αλλαγές στην πολιτική");

    public static string Privacy_S10_Body => Strings.Get("Marketing.Privacy_S10_Body",
        "Μπορούμε να επικαιροποιήσουμε την παρούσα πολιτική. Ουσιώδεις αλλαγές ανακοινώνονται με email ή με ειδοποίηση εντός της εφαρμογής, τουλάχιστον 30 ημέρες πριν τεθούν σε ισχύ.");

    public static string Privacy_S11_Title => Strings.Get("Marketing.Privacy_S11_Title", "11. Επικοινωνία");

    public static string Privacy_S11_Body => Strings.Get("Marketing.Privacy_S11_Body",
        "[COMPANY_LEGAL_NAME], [COMPANY_ADDRESS]. Email: [PRIVACY_CONTACT_EMAIL]. Υπεύθυνος Προστασίας Δεδομένων (DPO): [DPO_CONTACT].");

    // ---------------------------------------------------------------- terms

    public static string Terms_PageTitle => Strings.Get("Marketing.Terms_PageTitle", "Όροι χρήσης");

    public static string Terms_MetaDescription => Strings.Get("Marketing.Terms_MetaDescription",
        "Οι όροι υπό τους οποίους παρέχεται και χρησιμοποιείται η πλατφόρμα Roivo.");

    public static string Terms_Title => Strings.Get("Marketing.Terms_Title", "Όροι χρήσης");

    public static string Terms_Intro => Strings.Get("Marketing.Terms_Intro",
        "Οι παρόντες όροι ρυθμίζουν τη χρήση της πλατφόρμας Roivo, που παρέχεται από την [COMPANY_LEGAL_NAME].");

    public static string Terms_S1_Title => Strings.Get("Marketing.Terms_S1_Title", "1. Αποδοχή των όρων");

    public static string Terms_S1_Body => Strings.Get("Marketing.Terms_S1_Body",
        "Με τη δημιουργία λογαριασμού ή τη χρήση της πλατφόρμας αποδέχεστε τους παρόντες όρους και την Πολιτική απορρήτου. Αν δεν τους αποδέχεστε, δεν επιτρέπεται να χρησιμοποιείτε την υπηρεσία. Αν ενεργείτε για λογαριασμό νομικού προσώπου, δηλώνετε ότι έχετε την εξουσία να το δεσμεύετε.");

    public static string Terms_S2_Title => Strings.Get("Marketing.Terms_S2_Title", "2. Περιγραφή της υπηρεσίας");

    public static string Terms_S2_Body => Strings.Get("Marketing.Terms_S2_Body",
        "Το Roivo είναι διαδικτυακή πλατφόρμα που συνδέει τραπεζικούς λογαριασμούς (μέσω PSD2) και παραστατικά myDATA, πραγματοποιεί αυτόματη συμφωνία μεταξύ τους, προβλέπει την ταμειακή ροή και ειδοποιεί για επερχόμενες φορολογικές και ασφαλιστικές υποχρεώσεις.");

    public static string Terms_S2_NotProvided => Strings.Get("Marketing.Terms_S2_NotProvided",
        "Το Roivo δεν εκδίδει παραστατικά, δεν τηρεί λογιστικά βιβλία, δεν υποβάλλει φορολογικές δηλώσεις και δεν εκτελεί πληρωμές. Οι προβλέψεις είναι στατιστικές εκτιμήσεις βάσει ιστορικών δεδομένων και δεν αποτελούν λογιστική, φορολογική, νομική ή επενδυτική συμβουλή. Οι αποφάσεις σας παραμένουν αποκλειστικά δική σας ευθύνη.");

    public static string Terms_S3_Title => Strings.Get("Marketing.Terms_S3_Title", "3. Λογαριασμοί και υποχρεώσεις χρήστη");

    public static string Terms_S3_Body => Strings.Get("Marketing.Terms_S3_Body",
        "Αναλαμβάνετε την υποχρέωση:");

    public static string Terms_S3_Item1 => Strings.Get("Marketing.Terms_S3_Item1",
        "να δηλώνετε αληθή και επικαιροποιημένα στοιχεία,");

    public static string Terms_S3_Item2 => Strings.Get("Marketing.Terms_S3_Item2",
        "να διαφυλάσσετε τα διαπιστευτήρια πρόσβασής σας και να μην τα μοιράζεστε,");

    public static string Terms_S3_Item3 => Strings.Get("Marketing.Terms_S3_Item3",
        "να έχετε τη νόμιμη εξουσιοδότηση για κάθε λογαριασμό, ΑΦΜ ή πελάτη που συνδέετε στην πλατφόρμα,");

    public static string Terms_S3_Item4 => Strings.Get("Marketing.Terms_S3_Item4",
        "να μην επιχειρείτε μη εξουσιοδοτημένη πρόσβαση, αντίστροφη μηχανίκευση ή αυτοματοποιημένη μαζική άντληση δεδομένων.");

    public static string Terms_S4_Title => Strings.Get("Marketing.Terms_S4_Title", "4. Συνδρομές και πληρωμές");

    public static string Terms_S4_Body => Strings.Get("Marketing.Terms_S4_Body",
        "Το πακέτο Δωρεάν παρέχεται χωρίς χρέωση για μία επιχείρηση. Οι τιμές που εμφανίζονται στον ιστότοπο είναι ενδεικτικές και δεν περιλαμβάνουν ΦΠΑ. Σε αυτή τη φάση δεν πραγματοποιείται πληρωμή μέσω της πλατφόρμας: η τιμολόγηση και η εξόφληση γίνονται εκτός πλατφόρμας, κατόπιν συμφωνίας. [BILLING_TERMS]");

    public static string Terms_S5_Title => Strings.Get("Marketing.Terms_S5_Title", "5. Δεδομένα και πνευματική ιδιοκτησία");

    public static string Terms_S5_Body => Strings.Get("Marketing.Terms_S5_Body",
        "Τα δεδομένα που εισάγετε ή συνδέετε παραμένουν δικά σας. Μας χορηγείτε περιορισμένη άδεια να τα επεξεργαζόμαστε αποκλειστικά για την παροχή της υπηρεσίας. Το λογισμικό, ο σχεδιασμός, το λογότυπο και το όνομα Roivo ανήκουν στην [COMPANY_LEGAL_NAME] και δεν επιτρέπεται η χρήση τους χωρίς έγγραφη άδεια.");

    public static string Terms_S6_Title => Strings.Get("Marketing.Terms_S6_Title", "6. Διαθεσιμότητα και περιορισμός ευθύνης");

    public static string Terms_S6_Body => Strings.Get("Marketing.Terms_S6_Body",
        "Η υπηρεσία παρέχεται «ως έχει». Προσπαθούμε για αδιάλειπτη λειτουργία, αλλά δεν εγγυόμαστε συγκεκριμένο ποσοστό διαθεσιμότητας, ούτε την ακρίβεια, πληρότητα ή επικαιρότητα των δεδομένων που αντλούνται από τρίτες πηγές (τράπεζες, myDATA). Στο μέγιστο βαθμό που επιτρέπει το εφαρμοστέο δίκαιο, η ευθύνη μας περιορίζεται στο ποσό των συνδρομών που καταβάλατε τους τελευταίους δώδεκα μήνες και δεν καλύπτει έμμεσες ζημίες, απώλεια κερδών ή απώλεια δεδομένων. Ο περιορισμός δεν ισχύει σε περίπτωση δόλου ή βαριάς αμέλειας.");

    public static string Terms_S7_Title => Strings.Get("Marketing.Terms_S7_Title", "7. Διάρκεια και λήξη");

    public static string Terms_S7_Body => Strings.Get("Marketing.Terms_S7_Body",
        "Μπορείτε να διαγράψετε τον λογαριασμό σας οποτεδήποτε. Μπορούμε να αναστείλουμε ή να τερματίσουμε τον λογαριασμό σας σε περίπτωση παράβασης των όρων, μη πληρωμής ή παράνομης χρήσης, κατόπιν ειδοποίησης όπου αυτό είναι εφικτό. Μετά τη λήξη, τα δεδομένα σας διαγράφονται σύμφωνα με τους χρόνους διατήρησης της Πολιτικής απορρήτου.");

    public static string Terms_S8_Title => Strings.Get("Marketing.Terms_S8_Title", "8. Τροποποιήσεις των όρων");

    public static string Terms_S8_Body => Strings.Get("Marketing.Terms_S8_Body",
        "Μπορούμε να τροποποιήσουμε τους παρόντες όρους. Ουσιώδεις αλλαγές ανακοινώνονται τουλάχιστον 30 ημέρες πριν τεθούν σε ισχύ. Η συνέχιση της χρήσης μετά την έναρξη ισχύος συνιστά αποδοχή.");

    public static string Terms_S9_Title => Strings.Get("Marketing.Terms_S9_Title", "9. Εφαρμοστέο δίκαιο και δικαιοδοσία");

    public static string Terms_S9_Body => Strings.Get("Marketing.Terms_S9_Body",
        "Οι παρόντες όροι διέπονται από το ελληνικό δίκαιο. Για κάθε διαφορά που προκύπτει από αυτούς αποκλειστικά αρμόδια είναι τα δικαστήρια [JURISDICTION_CITY], Ελλάδα. Τα δικαιώματα των καταναλωτών βάσει αναγκαστικού δικαίου δεν περιορίζονται.");

    public static string Terms_S10_Title => Strings.Get("Marketing.Terms_S10_Title", "10. Επικοινωνία");

    public static string Terms_S10_Body => Strings.Get("Marketing.Terms_S10_Body",
        "[COMPANY_LEGAL_NAME], [COMPANY_ADDRESS]. Email: [SUPPORT_CONTACT_EMAIL].");

    public static string MetaDescription => Strings.Get("Marketing.MetaDescription", "Roivo — ταμειακές ροές χωρίς εκπλήξεις για ελληνικές επιχειρήσεις. Αυτόματη αντιστοίχιση τιμολογίων, πρόβλεψη 90 ημερών, φορολογικό ημερολόγιο.");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Marketing() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Marketing.BrandName"] = "Roivo",
        ["Marketing.Tagline"] = "Cashflow Intelligence",

        ["Marketing.Landing_PageTitle"] = "Cashflow without surprises",
        ["Marketing.Landing_MetaDescription"] =
            "Roivo connects your bank accounts with AADE myDATA, reconciles transactions against invoices automatically and forecasts your cashflow 90 days ahead.",
        ["Marketing.Landing_SkipToContent"] = "Skip to content",
        ["Marketing.Landing_LogoAlt"] = "Roivo — Cashflow Intelligence",

        ["Marketing.Nav_Features"] = "Features",
        ["Marketing.Nav_HowItWorks"] = "How it works",
        ["Marketing.Nav_Accountants"] = "For accountants",
        ["Marketing.Nav_Pricing"] = "Pricing",

        ["Marketing.Hero_Headline"] = "Cashflow without surprises",
        ["Marketing.Hero_SubHeadline"] =
            "Roivo matches your bank transactions against your myDATA invoices automatically, forecasts your cashflow 90 days ahead, and reminds you of every tax obligation before it falls due — not after.",
        ["Marketing.Hero_PrimaryCta"] = "Start for free",
        ["Marketing.Hero_SecondaryCta"] = "Sign in",
        ["Marketing.Hero_Reassurance"] = "No card. No install. Data hosted inside the EU.",

        ["Marketing.Features_Title"] = "What Roivo does",
        ["Marketing.Features_Subtitle"] = "Two data sources that never spoke to each other, in one picture.",
        ["Marketing.Feature_Psd2_Title"] = "Bank connection (PSD2)",
        ["Marketing.Feature_Psd2_Desc"] =
            "Connect your accounts through Open Banking. Transactions update automatically, with no logging into ten different bank portals.",
        ["Marketing.Feature_MyData_Title"] = "myDATA sync",
        ["Marketing.Feature_MyData_Desc"] =
            "Sales and purchase invoices come straight from AADE's myDATA platform — nothing retyped by hand.",
        ["Marketing.Feature_Reconciliation_Title"] = "Automatic reconciliation",
        ["Marketing.Feature_Reconciliation_Desc"] =
            "Roivo works out which invoice was paid by which deposit, and leaves you looking only at what genuinely needs a decision.",
        ["Marketing.Feature_Forecast_Title"] = "90-day forecast",
        ["Marketing.Feature_Forecast_Desc"] =
            "See where your cash is heading over the next three months, based on income and expense trends, open invoices and known recurring obligations.",
        ["Marketing.Feature_Tax_Title"] = "Tax obligations",
        ["Marketing.Feature_Tax_Desc"] =
            "VAT, EFKA, withholding tax, tax prepayment: an alert in good time, with the amount and the deadline.",

        ["Marketing.How_Title"] = "How it works",
        ["Marketing.How_Subtitle"] = "Three steps. Once.",
        ["Marketing.How_Step1_Label"] = "Connect",
        ["Marketing.How_Step1_Desc"] =
            "Link your bank and myDATA once, through a secure authorisation you can revoke whenever you like.",
        ["Marketing.How_Step2_Label"] = "Sync",
        ["Marketing.How_Step2_Desc"] =
            "Roivo pulls transactions and invoices every day and reconciles them against each other automatically.",
        ["Marketing.How_Step3_Label"] = "Forecast",
        ["Marketing.How_Step3_Desc"] =
            "You see 90 days of cashflow ahead, and the obligations coming at you, before they become a problem.",

        ["Marketing.Accountants_Eyebrow"] = "For accounting practices",
        ["Marketing.Accountants_Title"] = "Every client on one screen",
        ["Marketing.Accountants_Lead"] =
            "If you handle 20 to 500 clients, Roivo shows you which of them is heading for a cash problem before they ring you to say so.",
        ["Marketing.Accountants_Point1_Title"] = "Multi-client dashboard",
        ["Marketing.Accountants_Point1_Desc"] =
            "One screen with all of your clients, each with a colour-coded liquidity signal.",
        ["Marketing.Accountants_Point2_Title"] = "Liquidity alerts",
        ["Marketing.Accountants_Point2_Desc"] =
            "An alert when an upcoming obligation is not covered by a client's available cash.",
        ["Marketing.Accountants_Point3_Title"] = "Consolidated reports",
        ["Marketing.Accountants_Point3_Desc"] =
            "Portfolio-wide reports across your whole client book, ready to export and send.",
        ["Marketing.Accountants_Cta"] = "Try it for your practice",
        ["Marketing.Accountants_Mock_Title"] = "Clients",
        ["Marketing.Accountants_Mock_Value1"] = "€42,300",
        ["Marketing.Accountants_Mock_Value2"] = "€8,150",
        ["Marketing.Accountants_Mock_Value3"] = "€1,470",
        ["Marketing.Accountants_Mock_ColumnBalance"] = "Balance",
        ["Marketing.Accountants_Mock_ColumnForecast"] = "30-day forecast",
        ["Marketing.Accountants_Mock_Client1"] = "Client A",
        ["Marketing.Accountants_Mock_Client2"] = "Client B",
        ["Marketing.Accountants_Mock_Client3"] = "Client C",
        ["Marketing.Accountants_Mock_StatusOk"] = "Healthy",
        ["Marketing.Accountants_Mock_StatusWatch"] = "Watch",
        ["Marketing.Accountants_Mock_StatusRisk"] = "At risk",
        ["Marketing.Accountants_Mock_Caption"] = "Illustration only. The figures are an example.",

        ["Marketing.Pricing_Title"] = "Pricing",
        ["Marketing.Pricing_Subtitle"] =
            "Start free with one business. Move up when you need more.",
        ["Marketing.Pricing_Recommended"] = "Recommended",
        ["Marketing.Pricing_Free_Name"] = "Free",
        ["Marketing.Pricing_Free_Price"] = "€0",
        ["Marketing.Pricing_Free_Period"] = "/month",
        ["Marketing.Pricing_Free_Desc"] = "For a single business that wants the full picture.",
        ["Marketing.Pricing_Free_Feature1"] = "1 business",
        ["Marketing.Pricing_Free_Feature2"] = "Bank and myDATA connection",
        ["Marketing.Pricing_Free_Feature3"] = "90-day forecast and tax calendar",
        ["Marketing.Pricing_Pro_Name"] = "Pro",
        ["Marketing.Pricing_Pro_Price"] = "€29",
        ["Marketing.Pricing_Pro_Period"] = "/month",
        ["Marketing.Pricing_Pro_Desc"] = "For accounting practices and multiple businesses.",
        ["Marketing.Pricing_Pro_Feature1"] = "Unlimited businesses",
        ["Marketing.Pricing_Pro_Feature2"] = "Accountant dashboard and liquidity alerts",
        ["Marketing.Pricing_Pro_Feature3"] = "Consolidated portfolio reports",
        ["Marketing.Pricing_Enterprise_Name"] = "Enterprise",
        ["Marketing.Pricing_Enterprise_Price"] = "Talk to us",
        ["Marketing.Pricing_Enterprise_Period"] = "",
        ["Marketing.Pricing_Enterprise_Desc"] = "For larger practices with specific requirements.",
        ["Marketing.Pricing_Enterprise_Feature1"] = "Everything in Pro",
        ["Marketing.Pricing_Enterprise_Feature2"] = "Priority support and onboarding",
        ["Marketing.Pricing_Enterprise_Feature3"] = "Data processing agreement on request",
        ["Marketing.Pricing_Note"] =
            "Prices are indicative and exclude VAT. No online payment is taken through the platform at this stage.",

        ["Marketing.Footer_Privacy"] = "Privacy policy",
        ["Marketing.Footer_Terms"] = "Terms of use",
        ["Marketing.Footer_Copyright"] = "© 2026 Roivo — Cashflow Intelligence",
        ["Marketing.Footer_Blurb"] = "Cashflow for Greek businesses and the accountants who look after them.",

        ["Marketing.Legal_LastUpdated"] = "Last updated: [LAST_UPDATED_DATE]",
        ["Marketing.Legal_BackToHome"] = "Back to home",
        ["Marketing.Legal_TableOfContents"] = "Contents",

        ["Marketing.Privacy_PageTitle"] = "Privacy policy",
        ["Marketing.Privacy_MetaDescription"] =
            "How Roivo collects, uses and protects your data, in line with the GDPR.",
        ["Marketing.Privacy_Title"] = "Privacy policy",
        ["Marketing.Privacy_Intro"] =
            "This policy explains which personal data Roivo processes, for what purpose, who it is shared with, and what rights you have under Regulation (EU) 2016/679 (GDPR) and Greek Law 4624/2019.",
        ["Marketing.Privacy_S1_Title"] = "1. Data controller",
        ["Marketing.Privacy_S1_Body"] =
            "The data controller is [COMPANY_LEGAL_NAME], registered at [COMPANY_ADDRESS], VAT number [COMPANY_VAT_NUMBER], commercial registry (GEMI) number [COMPANY_REGISTRY_NUMBER]. For any data protection matter, contact [PRIVACY_CONTACT_EMAIL].",
        ["Marketing.Privacy_S2_Title"] = "2. Data we collect",
        ["Marketing.Privacy_S2_Body"] = "We collect only the data the service needs in order to work:",
        ["Marketing.Privacy_S2_Item1"] =
            "Account details: name, email address, hashed password, account type (business or accounting practice).",
        ["Marketing.Privacy_S2_Item2"] =
            "Business details: legal name, VAT number, contact details, and — for accounting practices — the same details for the clients you add.",
        ["Marketing.Privacy_S2_Item3"] =
            "Banking data: IBANs, account balances and transactions, retrieved through PSD2 and only after you have explicitly authorised your bank to share them.",
        ["Marketing.Privacy_S2_Item4"] =
            "Tax documents: sales and purchase invoice data transmitted to myDATA (counterparty VAT number, amounts, dates, document series and numbers).",
        ["Marketing.Privacy_S2_Item5"] =
            "Technical data: IP address, browser type, access times and action logs, kept for security and audit purposes.",
        ["Marketing.Privacy_S2_Note"] =
            "We never ask for your e-banking passwords. Banking data is accessed exclusively through the official PSD2 interfaces.",
        ["Marketing.Privacy_S3_Title"] = "3. Purposes and legal bases",
        ["Marketing.Privacy_S3_Body"] = "We process your data for the following purposes, on the following legal bases:",
        ["Marketing.Privacy_S3_Item1"] =
            "Providing the service — reconciling transactions and invoices, forecasting cashflow, alerting on obligations: performance of the contract (Article 6(1)(b) GDPR).",
        ["Marketing.Privacy_S3_Item2"] =
            "Retrieving banking data and myDATA invoices: your explicit consent and authorisation, which you may withdraw at any time (Article 6(1)(a) GDPR).",
        ["Marketing.Privacy_S3_Item3"] =
            "Security, fraud prevention and technical support: legitimate interest (Article 6(1)(f) GDPR).",
        ["Marketing.Privacy_S3_Item4"] =
            "Meeting our own tax and accounting obligations: compliance with a legal obligation (Article 6(1)(c) GDPR).",
        ["Marketing.Privacy_S4_Title"] = "4. Third parties and processors",
        ["Marketing.Privacy_S4_Body"] = "To deliver the service, your data is shared with the following providers:",
        ["Marketing.Privacy_S4_Item1"] =
            "Enable Banking — a licensed account information service provider (AISP) under PSD2. The connection to your bank and the retrieval of balances and transactions happen through it.",
        ["Marketing.Privacy_S4_Item2"] =
            "AADE — the myDATA platform. We retrieve your invoice data from it using the myDATA user credentials you enter into Roivo.",
        ["Marketing.Privacy_S4_Item3"] =
            "[HOSTING_PROVIDER] — hosting for the application and database, in data centres inside the European Union.",
        ["Marketing.Privacy_S4_Item4"] = "[EMAIL_PROVIDER] — delivery of notification and account emails.",
        ["Marketing.Privacy_S4_Note"] =
            "We do not sell, rent or trade personal data. A processing agreement under Article 28 GDPR is in place with every processor.",
        ["Marketing.Privacy_S5_Title"] = "5. Cookies",
        ["Marketing.Privacy_S5_Body"] =
            "We use strictly necessary cookies for sign-in, session persistence and CSRF protection. We use no advertising or third-party tracking cookies. Any analytics cookies will be set only after your explicit consent.",
        ["Marketing.Privacy_S6_Title"] = "6. Your rights",
        ["Marketing.Privacy_S6_Body"] = "As a data subject you have the right to:",
        ["Marketing.Privacy_S6_Item1"] = "access the data we hold about you,",
        ["Marketing.Privacy_S6_Item2"] = "have inaccurate or incomplete data corrected,",
        ["Marketing.Privacy_S6_Item3"] = "erasure (the “right to be forgotten”), where no legal obligation requires us to keep the data,",
        ["Marketing.Privacy_S6_Item4"] = "restriction of processing,",
        ["Marketing.Privacy_S6_Item5"] = "data portability in a structured, commonly used format,",
        ["Marketing.Privacy_S6_Item6"] = "object to processing based on legitimate interest,",
        ["Marketing.Privacy_S6_Item7"] = "withdraw your consent for the bank or myDATA connection, at any time and without giving a reason.",
        ["Marketing.Privacy_S6_Response"] =
            "We answer every request within one month. Send your request to [PRIVACY_CONTACT_EMAIL].",
        ["Marketing.Privacy_S6_Complaint"] =
            "You also have the right to lodge a complaint with the Hellenic Data Protection Authority (www.dpa.gr).",
        ["Marketing.Privacy_S7_Title"] = "7. Retention",
        ["Marketing.Privacy_S7_Body"] =
            "Account details are kept for as long as your account is active and for [ACCOUNT_RETENTION_PERIOD] after it is deleted. Banking data and invoices are kept for [FINANCIAL_DATA_RETENTION_PERIOD]. Security logs are kept for [LOG_RETENTION_PERIOD]. After those periods the data is deleted or anonymised.",
        ["Marketing.Privacy_S8_Title"] = "8. Security",
        ["Marketing.Privacy_S8_Body"] =
            "Data is transmitted encrypted (TLS) and stored encrypted. Credentials and access tokens are held encrypted, separately from the rest of the data. Staff access is restricted and logged. In the event of a data breach we notify the authority and, where required, you, within 72 hours.",
        ["Marketing.Privacy_S9_Title"] = "9. Transfers outside the EU",
        ["Marketing.Privacy_S9_Body"] =
            "Your data is stored in data centres inside the European Union. If any feature requires a transfer to a third country, it takes place only with adequate safeguards (an adequacy decision or standard contractual clauses) and is stated explicitly here.",
        ["Marketing.Privacy_S10_Title"] = "10. Changes to this policy",
        ["Marketing.Privacy_S10_Body"] =
            "We may update this policy. Material changes are announced by email or by an in-app notice at least 30 days before they take effect.",
        ["Marketing.Privacy_S11_Title"] = "11. Contact",
        ["Marketing.Privacy_S11_Body"] =
            "[COMPANY_LEGAL_NAME], [COMPANY_ADDRESS]. Email: [PRIVACY_CONTACT_EMAIL]. Data Protection Officer (DPO): [DPO_CONTACT].",

        ["Marketing.Terms_PageTitle"] = "Terms of use",
        ["Marketing.Terms_MetaDescription"] = "The terms on which the Roivo platform is provided and used.",
        ["Marketing.Terms_Title"] = "Terms of use",
        ["Marketing.Terms_Intro"] =
            "These terms govern your use of the Roivo platform, provided by [COMPANY_LEGAL_NAME].",
        ["Marketing.Terms_S1_Title"] = "1. Acceptance of the terms",
        ["Marketing.Terms_S1_Body"] =
            "By creating an account or using the platform you accept these terms and the Privacy policy. If you do not accept them, you may not use the service. If you are acting on behalf of a legal entity, you confirm that you are authorised to bind it.",
        ["Marketing.Terms_S2_Title"] = "2. What the service is",
        ["Marketing.Terms_S2_Body"] =
            "Roivo is a web platform that connects bank accounts (via PSD2) with myDATA invoice data, reconciles the two automatically, forecasts cashflow, and alerts on upcoming tax and social security obligations.",
        ["Marketing.Terms_S2_NotProvided"] =
            "Roivo does not issue invoices, does not keep accounting books, does not file tax returns and does not process payments. Forecasts are statistical estimates based on historical data and are not accounting, tax, legal or investment advice. Your decisions remain entirely your own responsibility.",
        ["Marketing.Terms_S3_Title"] = "3. Accounts and your obligations",
        ["Marketing.Terms_S3_Body"] = "You undertake:",
        ["Marketing.Terms_S3_Item1"] = "to give true and up-to-date information,",
        ["Marketing.Terms_S3_Item2"] = "to keep your access credentials safe and not to share them,",
        ["Marketing.Terms_S3_Item3"] =
            "to hold the lawful authorisation for every account, VAT number or client you connect to the platform,",
        ["Marketing.Terms_S3_Item4"] =
            "not to attempt unauthorised access, reverse engineering or automated bulk extraction of data.",
        ["Marketing.Terms_S4_Title"] = "4. Subscriptions and payment",
        ["Marketing.Terms_S4_Body"] =
            "The Free plan is provided at no charge for one business. The prices shown on this site are indicative and exclude VAT. At this stage no payment is taken through the platform: invoicing and settlement happen off-platform, by agreement. [BILLING_TERMS]",
        ["Marketing.Terms_S5_Title"] = "5. Data and intellectual property",
        ["Marketing.Terms_S5_Body"] =
            "The data you enter or connect remains yours. You grant us a limited licence to process it solely in order to provide the service. The software, design, logo and the Roivo name belong to [COMPANY_LEGAL_NAME] and may not be used without written permission.",
        ["Marketing.Terms_S6_Title"] = "6. Availability and limitation of liability",
        ["Marketing.Terms_S6_Body"] =
            "The service is provided “as is”. We aim for uninterrupted operation but do not guarantee any particular level of availability, nor the accuracy, completeness or timeliness of data retrieved from third-party sources (banks, myDATA). To the fullest extent permitted by applicable law, our liability is limited to the subscription fees you paid over the preceding twelve months and excludes indirect damages, loss of profit and loss of data. This limitation does not apply in cases of wilful misconduct or gross negligence.",
        ["Marketing.Terms_S7_Title"] = "7. Term and termination",
        ["Marketing.Terms_S7_Body"] =
            "You may delete your account at any time. We may suspend or terminate your account for breach of these terms, non-payment or unlawful use, with notice where that is practicable. After termination your data is deleted in line with the retention periods in the Privacy policy.",
        ["Marketing.Terms_S8_Title"] = "8. Changes to these terms",
        ["Marketing.Terms_S8_Body"] =
            "We may amend these terms. Material changes are announced at least 30 days before they take effect. Continuing to use the service after that date constitutes acceptance.",
        ["Marketing.Terms_S9_Title"] = "9. Governing law and jurisdiction",
        ["Marketing.Terms_S9_Body"] =
            "These terms are governed by Greek law. The courts of [JURISDICTION_CITY], Greece have exclusive jurisdiction over any dispute arising from them. Consumer rights under mandatory law are not restricted.",
        ["Marketing.Terms_S10_Title"] = "10. Contact",
        ["Marketing.Terms_S10_Body"] = "[COMPANY_LEGAL_NAME], [COMPANY_ADDRESS]. Email: [SUPPORT_CONTACT_EMAIL].",
        ["Marketing.MetaDescription"] = "Roivo — cashflow without surprises for Greek businesses. Automatic invoice reconciliation, 90-day forecasting and the Greek tax calendar.",
    });
}
