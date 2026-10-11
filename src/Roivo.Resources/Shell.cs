namespace Roivo.Resources;

/// <summary>
/// Chrome for the product shell: sidebar navigation, header, theme switch.
/// Separate from <see cref="Common"/> because these strings belong to one
/// layout rather than being shared vocabulary.
/// </summary>
public static class Shell
{
    // Sidebar section headers. Uppercase in the design, and written uppercase
    // here rather than via text-transform so the Greek accents are dropped the
    // way Greek typography expects (ΠΑΡΑΚΟΛΟΥΘΗΣΗ, not ΠΑΡΑΚΟΛΟΎΘΗΣΗ).
    public static string Group_Workspace => Strings.Get("Shell.Group_Workspace", "ΧΩΡΟΣ ΕΡΓΑΣΙΑΣ");
    public static string Group_Monitoring => Strings.Get("Shell.Group_Monitoring", "ΠΑΡΑΚΟΛΟΥΘΗΣΗ");
    public static string Group_Tasks => Strings.Get("Shell.Group_Tasks", "ΕΡΓΑΣΙΕΣ");
    public static string Group_Management => Strings.Get("Shell.Group_Management", "ΔΙΑΧΕΙΡΙΣΗ");

    public static string Nav_Overview => Strings.Get("Shell.Nav_Overview", "Επισκόπηση");
    public static string Nav_Cashflow => Strings.Get("Shell.Nav_Cashflow", "Ταμειακές ροές");
    public static string Nav_Reconciliation => Strings.Get("Shell.Nav_Reconciliation", "Συμφωνίες");
    public static string Nav_Obligations => Strings.Get("Shell.Nav_Obligations", "Υποχρεώσεις");
    public static string Nav_Reports => Strings.Get("Shell.Nav_Reports", "Αναφορές");
    public static string Nav_Connections => Strings.Get("Shell.Nav_Connections", "Συνδέσεις");
    public static string Nav_Help => Strings.Get("Shell.Nav_Help", "Βοήθεια");
    public static string Nav_Settings => Strings.Get("Shell.Nav_Settings", "Ρυθμίσεις");

    public static string Nav_Label => Strings.Get("Shell.Nav_Label", "Κύρια πλοήγηση");
    public static string AccountantWorkspace => Strings.Get("Shell.AccountantWorkspace", "Λογιστικό γραφείο");

    public static string BrandTagline => Strings.Get("Shell.BrandTagline", "Cashflow intelligence");

    // Header
    public static string SearchPlaceholder => Strings.Get("Shell.SearchPlaceholder", "Αναζήτηση...");
    public static string SearchLabel => Strings.Get("Shell.SearchLabel", "Καθολική αναζήτηση");
    public static string Notifications => Strings.Get("Shell.Notifications", "Ειδοποιήσεις");
    public static string Theme_ToDark => Strings.Get("Shell.Theme_ToDark", "Ενεργοποίηση σκοτεινού θέματος");
    public static string Theme_ToLight => Strings.Get("Shell.Theme_ToLight", "Ενεργοποίηση φωτεινού θέματος");
    public static string OpenMenu => Strings.Get("Shell.OpenMenu", "Άνοιγμα μενού");

    public static string Placeholder_Heading => Strings.Get("Shell.Placeholder_Heading", "Σε εξέλιξη");
    public static string Placeholder_Body => Strings.Get("Shell.Placeholder_Body", "Αυτή η ενότητα σχεδιάζεται αυτή τη στιγμή. Τα δεδομένα σας είναι ασφαλή και διαθέσιμα στις υπάρχουσες σελίδες.");
    public static string Placeholder_Cashflow => Strings.Get("Shell.Placeholder_Cashflow", "Εισροές, εκροές και πρόβλεψη 90 ημερών.");
    public static string Placeholder_Reconciliation => Strings.Get("Shell.Placeholder_Reconciliation", "Αντιστοίχιση παραστατικών με τραπεζικές κινήσεις.");
    public static string Placeholder_Obligations => Strings.Get("Shell.Placeholder_Obligations", "ΦΠΑ, ΕΦΚΑ και κάθε φορολογική προθεσμία.");
    public static string Placeholder_Reports => Strings.Get("Shell.Placeholder_Reports", "Συγκεντρωτικές αναφορές για όλο το πελατολόγιο.");
    public static string Placeholder_Connections => Strings.Get("Shell.Placeholder_Connections", "Τράπεζες και AADE myDATA.");
    public static string Placeholder_Settings => Strings.Get("Shell.Placeholder_Settings", "Προφίλ, επιχείρηση, χρήστες και εμφάνιση.");
    public static string Placeholder_Help => Strings.Get("Shell.Placeholder_Help", "Οδηγοί και υποστήριξη.");

    public static string Metric_Available => Strings.Get("Shell.Metric_Available", "Συνολικά διαθέσιμα");
    public static string Metric_ExpectedInflow => Strings.Get("Shell.Metric_ExpectedInflow", "Αναμενόμενες εισροές");
    public static string Metric_PlannedOutflow => Strings.Get("Shell.Metric_PlannedOutflow", "Προγραμματισμένες εκροές");
    public static string Metric_NetPosition90 => Strings.Get("Shell.Metric_NetPosition90", "Καθαρή θέση 90 ημερών");
    public static string Metric_Next30Days => Strings.Get("Shell.Metric_Next30Days", "Επόμενες 30 ημέρες");
    public static string Metric_HealthyForecast => Strings.Get("Shell.Metric_HealthyForecast", "Υγιής πρόβλεψη");
    public static string Metric_TightForecast => Strings.Get("Shell.Metric_TightForecast", "Οριακή πρόβλεψη");
    public static string Metric_RunwayDays => Strings.Get("Shell.Metric_RunwayDays", "Αυτονομία {0} ημερών");
    public static string Metric_RunwayUnlimited => Strings.Get("Shell.Metric_RunwayUnlimited", "Χωρίς προβλεπόμενο έλλειμμα");
    public static string Forecast_Title => Strings.Get("Shell.Forecast_Title", "Πρόβλεψη ταμειακών ροών");
    public static string Forecast_Subtitle => Strings.Get("Shell.Forecast_Subtitle", "Ιστορικά και προβλεπόμενα στοιχεία 90 ημερών");
    public static string Forecast_Confidence => Strings.Get("Shell.Forecast_Confidence", "{0}% βεβαιότητα");
    public static string Forecast_Actual => Strings.Get("Shell.Forecast_Actual", "Πραγματικά");
    public static string Forecast_Projected => Strings.Get("Shell.Forecast_Projected", "Πρόβλεψη");
    public static string Forecast_NotEnoughHistory => Strings.Get("Shell.Forecast_NotEnoughHistory", "Δεν υπάρχουν αρκετά δεδομένα ακόμα");
    public static string Forecast_NotEnoughHistoryBody => Strings.Get("Shell.Forecast_NotEnoughHistoryBody", "Συνδέστε έναν τραπεζικό λογαριασμό και αφήστε να περάσουν μερικές εβδομάδες κινήσεων πριν εμφανιστεί η πρόβλεψη.");
    public static string Obligations_Title => Strings.Get("Shell.Obligations_Title", "Επόμενες υποχρεώσεις");
    public static string Obligations_Total => Strings.Get("Shell.Obligations_Total", "Συνολικά {0}");
    public static string Obligations_All => Strings.Get("Shell.Obligations_All", "Όλες");
    public static string Obligations_DueInDays => Strings.Get("Shell.Obligations_DueInDays", "Σε {0} ημέρες");
    public static string Obligations_DueToday => Strings.Get("Shell.Obligations_DueToday", "Σήμερα");
    public static string Obligations_Overdue => Strings.Get("Shell.Obligations_Overdue", "Εκπρόθεσμη");
    public static string Obligations_None => Strings.Get("Shell.Obligations_None", "Καμία υποχρέωση στις επόμενες 30 ημέρες");
    public static string Accounts_Title => Strings.Get("Shell.Accounts_Title", "Τραπεζικοί λογαριασμοί");
    public static string Accounts_Manage => Strings.Get("Shell.Accounts_Manage", "Διαχείριση");
    public static string Accounts_Connected => Strings.Get("Shell.Accounts_Connected", "Συνδεδεμένο");
    public static string Accounts_NeverSynced => Strings.Get("Shell.Accounts_NeverSynced", "Χωρίς συγχρονισμό");
    public static string Accounts_SyncedAt => Strings.Get("Shell.Accounts_SyncedAt", "Τελευταίος συγχρονισμός {0}");
    public static string Accounts_None => Strings.Get("Shell.Accounts_None", "Δεν έχετε συνδέσει τραπεζικό λογαριασμό");
    public static string Insight_Title => Strings.Get("Shell.Insight_Title", "ROIVO INSIGHT");
    public static string Greeting => Strings.Get("Shell.Greeting", "Καλημέρα, {0}");
    public static string GreetingSubtitle => Strings.Get("Shell.GreetingSubtitle", "Η οικονομική εικόνα της επιχείρησής σας σήμερα.");

    public static string Insight_Positive => Strings.Get("Shell.Insight_Positive", "Η πρόβλεψη δείχνει {0} επιπλέον ρευστότητα στο τρίμηνο, με αυτονομία {1} ημερών. Καλή στιγμή για προγραμματισμό.");
    public static string Insight_Negative => Strings.Get("Shell.Insight_Negative", "Η πρόβλεψη δείχνει μείωση {0} στο τρίμηνο. Ελέγξτε τις επερχόμενες υποχρεώσεις και τα ανοιχτά τιμολόγια.");

    public static string Status_Healthy => Strings.Get("Shell.Status_Healthy", "Υγιής");
    public static string Status_Attention => Strings.Get("Shell.Status_Attention", "Προσοχή");
    public static string Status_Urgent => Strings.Get("Shell.Status_Urgent", "Άμεση ενέργεια");
    public static string Portfolio_Greeting => Strings.Get("Shell.Portfolio_Greeting", "Καλημέρα, {0}");
    public static string Portfolio_Subtitle => Strings.Get("Shell.Portfolio_Subtitle", "Η συνολική εικόνα των επιχειρήσεων που διαχειρίζεστε.");
    public static string Portfolio_NewBusiness => Strings.Get("Shell.Portfolio_NewBusiness", "Νέα επιχείρηση");
    public static string Portfolio_MetricsLabel => Strings.Get("Shell.Portfolio_MetricsLabel", "Βασικά μεγέθη χαρτοφυλακίου");
    public static string Portfolio_Clients => Strings.Get("Shell.Portfolio_Clients", "Επιχειρήσεις");
    public static string Portfolio_Count => Strings.Get("Shell.Portfolio_Count", "{0} από {1} επιχειρήσεις");
    public static string Portfolio_SearchPlaceholder => Strings.Get("Shell.Portfolio_SearchPlaceholder", "Όνομα ή ΑΦΜ");
    public static string Portfolio_SearchLabel => Strings.Get("Shell.Portfolio_SearchLabel", "Αναζήτηση επιχειρήσεων");
    public static string Portfolio_AllStatuses => Strings.Get("Shell.Portfolio_AllStatuses", "Όλες οι καταστάσεις");
    public static string Portfolio_Metric_Clients => Strings.Get("Shell.Portfolio_Metric_Clients", "Επιχειρήσεις");
    public static string Portfolio_Metric_MatchRate => Strings.Get("Shell.Portfolio_Metric_MatchRate", "Μέσο ποσοστό αντιστοίχισης");
    public static string Portfolio_Metric_Unmatched => Strings.Get("Shell.Portfolio_Metric_Unmatched", "Εκκρεμείς συμφωνίες");
    public static string Portfolio_Metric_Deadlines => Strings.Get("Shell.Portfolio_Metric_Deadlines", "Υποχρεώσεις 30 ημερών");
    public static string Portfolio_Metric_ClientsNote => Strings.Get("Shell.Portfolio_Metric_ClientsNote", "{0} χρειάζονται προσοχή");
    public static string Portfolio_Metric_ClientsAllWell => Strings.Get("Shell.Portfolio_Metric_ClientsAllWell", "Όλες σε καλή κατάσταση");
    public static string Portfolio_Metric_UnmatchedNote => Strings.Get("Shell.Portfolio_Metric_UnmatchedNote", "Σε {0} επιχειρήσεις");
    public static string Portfolio_Metric_DeadlinesNote => Strings.Get("Shell.Portfolio_Metric_DeadlinesNote", "{0} προθεσμίες");
    public static string Portfolio_Col_Business => Strings.Get("Shell.Portfolio_Col_Business", "Επιχείρηση");
    public static string Portfolio_Col_MatchRate => Strings.Get("Shell.Portfolio_Col_MatchRate", "Αντιστοίχιση");
    public static string Portfolio_Col_Runway => Strings.Get("Shell.Portfolio_Col_Runway", "Αυτονομία");
    public static string Portfolio_Col_Unmatched => Strings.Get("Shell.Portfolio_Col_Unmatched", "Ασυμφωνίες");
    public static string Portfolio_Col_Deadlines => Strings.Get("Shell.Portfolio_Col_Deadlines", "Προθεσμίες");
    public static string Portfolio_Col_Status => Strings.Get("Shell.Portfolio_Col_Status", "Κατάσταση");
    public static string Portfolio_Col_Actions => Strings.Get("Shell.Portfolio_Col_Actions", "Ενέργειες");
    public static string Portfolio_RowActions => Strings.Get("Shell.Portfolio_RowActions", "Ενέργειες για {0}");
    public static string Portfolio_OpenHint => Strings.Get("Shell.Portfolio_OpenHint", "Κλικ για άνοιγμα");
    public static string Portfolio_NoMatches => Strings.Get("Shell.Portfolio_NoMatches", "Καμία επιχείρηση δεν ταιριάζει με την αναζήτηση");
    public static string Portfolio_Invoices => Strings.Get("Shell.Portfolio_Invoices", "{0} παραστατικά");
    public static string Portfolio_NoRunway => Strings.Get("Shell.Portfolio_NoRunway", "Χωρίς πρόβλεψη");
    public static string Signal_Label => Strings.Get("Shell.Signal_Label", "ROIVO SIGNAL");
    public static string Signal_NeedAttention => Strings.Get("Shell.Signal_NeedAttention", "{0} επιχειρήσεις χρειάζονται την προσοχή σας σήμερα.");
    public static string Signal_OneNeedsAttention => Strings.Get("Shell.Signal_OneNeedsAttention", "Μία επιχείρηση χρειάζεται την προσοχή σας σήμερα.");
    public static string Signal_AllWell => Strings.Get("Shell.Signal_AllWell", "Όλες οι επιχειρήσεις σας είναι σε καλή κατάσταση σήμερα.");
    public static string Signal_Detail => Strings.Get("Shell.Signal_Detail", "{0} ασυμφώνητα παραστατικά και {1} προθεσμίες μέσα στον επόμενο μήνα.");
    public static string Signal_ViewActions => Strings.Get("Shell.Signal_ViewActions", "Προβολή ενεργειών");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Shell() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Shell.Group_Workspace"] = "WORKSPACE",
        ["Shell.Group_Monitoring"] = "MONITORING",
        ["Shell.Group_Tasks"] = "TASKS",
        ["Shell.Group_Management"] = "MANAGEMENT",
        ["Shell.Nav_Overview"] = "Overview",
        ["Shell.Nav_Cashflow"] = "Cashflow",
        ["Shell.Nav_Reconciliation"] = "Reconciliation",
        ["Shell.Nav_Obligations"] = "Obligations",
        ["Shell.Nav_Reports"] = "Reports",
        ["Shell.Nav_Connections"] = "Connections",
        ["Shell.Nav_Help"] = "Help",
        ["Shell.Nav_Settings"] = "Settings",
        ["Shell.Nav_Label"] = "Main navigation",
        ["Shell.AccountantWorkspace"] = "Accountancy practice",
        ["Shell.BrandTagline"] = "Cashflow intelligence",
        ["Shell.SearchPlaceholder"] = "Search...",
        ["Shell.SearchLabel"] = "Global search",
        ["Shell.Notifications"] = "Notifications",
        ["Shell.Theme_ToDark"] = "Switch to dark theme",
        ["Shell.Theme_ToLight"] = "Switch to light theme",
        ["Shell.OpenMenu"] = "Open menu",
        ["Shell.Placeholder_Heading"] = "In progress",
        ["Shell.Placeholder_Body"] = "This section is being redesigned. Your data is safe and still available on the existing pages.",
        ["Shell.Placeholder_Cashflow"] = "Inflows, outflows and the 90-day forecast.",
        ["Shell.Placeholder_Reconciliation"] = "Matching invoices against bank transactions.",
        ["Shell.Placeholder_Obligations"] = "VAT, social security and every tax deadline.",
        ["Shell.Placeholder_Reports"] = "Consolidated reports across your client book.",
        ["Shell.Placeholder_Connections"] = "Banks and AADE myDATA.",
        ["Shell.Placeholder_Settings"] = "Profile, business, users and appearance.",
        ["Shell.Placeholder_Help"] = "Guides and support.",
        ["Shell.Metric_Available"] = "Total available",
        ["Shell.Metric_ExpectedInflow"] = "Expected inflows",
        ["Shell.Metric_PlannedOutflow"] = "Planned outflows",
        ["Shell.Metric_NetPosition90"] = "90-day net position",
        ["Shell.Metric_Next30Days"] = "Next 30 days",
        ["Shell.Metric_HealthyForecast"] = "Healthy forecast",
        ["Shell.Metric_TightForecast"] = "Tight forecast",
        ["Shell.Metric_RunwayDays"] = "{0} days of runway",
        ["Shell.Metric_RunwayUnlimited"] = "No projected shortfall",
        ["Shell.Forecast_Title"] = "Cashflow forecast",
        ["Shell.Forecast_Subtitle"] = "90 days of history and projection",
        ["Shell.Forecast_Confidence"] = "{0}% confidence",
        ["Shell.Forecast_Actual"] = "Actual",
        ["Shell.Forecast_Projected"] = "Forecast",
        ["Shell.Forecast_NotEnoughHistory"] = "Not enough history yet",
        ["Shell.Forecast_NotEnoughHistoryBody"] = "Connect a bank account and let a few weeks of transactions build up before the forecast appears.",
        ["Shell.Obligations_Title"] = "Upcoming obligations",
        ["Shell.Obligations_Total"] = "{0} in total",
        ["Shell.Obligations_All"] = "All",
        ["Shell.Obligations_DueInDays"] = "In {0} days",
        ["Shell.Obligations_DueToday"] = "Today",
        ["Shell.Obligations_Overdue"] = "Overdue",
        ["Shell.Obligations_None"] = "No obligations in the next 30 days",
        ["Shell.Accounts_Title"] = "Bank accounts",
        ["Shell.Accounts_Manage"] = "Manage",
        ["Shell.Accounts_Connected"] = "Connected",
        ["Shell.Accounts_NeverSynced"] = "Never synced",
        ["Shell.Accounts_SyncedAt"] = "Last synced {0}",
        ["Shell.Accounts_None"] = "No bank account connected yet",
        ["Shell.Insight_Title"] = "ROIVO INSIGHT",
        ["Shell.Greeting"] = "Good morning, {0}",
        ["Shell.GreetingSubtitle"] = "Your financial picture today.",
        ["Shell.Insight_Positive"] = "The forecast adds {0} over the quarter, with {1} days of runway. A good moment to plan ahead.",
        ["Shell.Insight_Negative"] = "The forecast is down {0} over the quarter. Check the upcoming obligations and your open invoices.",
        ["Shell.Status_Healthy"] = "Healthy",
        ["Shell.Status_Attention"] = "Attention",
        ["Shell.Status_Urgent"] = "Needs action",
        ["Shell.Portfolio_Greeting"] = "Good morning, {0}",
        ["Shell.Portfolio_Subtitle"] = "The whole picture across the businesses you manage.",
        ["Shell.Portfolio_NewBusiness"] = "New business",
        ["Shell.Portfolio_MetricsLabel"] = "Portfolio headline figures",
        ["Shell.Portfolio_Clients"] = "Businesses",
        ["Shell.Portfolio_Count"] = "{0} of {1} businesses",
        ["Shell.Portfolio_SearchPlaceholder"] = "Name or VAT number",
        ["Shell.Portfolio_SearchLabel"] = "Search businesses",
        ["Shell.Portfolio_AllStatuses"] = "All statuses",
        ["Shell.Portfolio_Metric_Clients"] = "Businesses",
        ["Shell.Portfolio_Metric_MatchRate"] = "Average match rate",
        ["Shell.Portfolio_Metric_Unmatched"] = "Pending reconciliations",
        ["Shell.Portfolio_Metric_Deadlines"] = "Obligations in 30 days",
        ["Shell.Portfolio_Metric_ClientsNote"] = "{0} need attention",
        ["Shell.Portfolio_Metric_ClientsAllWell"] = "All in good shape",
        ["Shell.Portfolio_Metric_UnmatchedNote"] = "Across {0} businesses",
        ["Shell.Portfolio_Metric_DeadlinesNote"] = "{0} deadlines",
        ["Shell.Portfolio_Col_Business"] = "Business",
        ["Shell.Portfolio_Col_MatchRate"] = "Reconciled",
        ["Shell.Portfolio_Col_Runway"] = "Runway",
        ["Shell.Portfolio_Col_Unmatched"] = "Unmatched",
        ["Shell.Portfolio_Col_Deadlines"] = "Deadlines",
        ["Shell.Portfolio_Col_Status"] = "Status",
        ["Shell.Portfolio_Col_Actions"] = "Actions",
        ["Shell.Portfolio_RowActions"] = "Actions for {0}",
        ["Shell.Portfolio_OpenHint"] = "Click to open",
        ["Shell.Portfolio_NoMatches"] = "No business matches that search",
        ["Shell.Portfolio_Invoices"] = "{0} invoices",
        ["Shell.Portfolio_NoRunway"] = "No forecast",
        ["Shell.Signal_Label"] = "ROIVO SIGNAL",
        ["Shell.Signal_NeedAttention"] = "{0} businesses need your attention today.",
        ["Shell.Signal_OneNeedsAttention"] = "One business needs your attention today.",
        ["Shell.Signal_AllWell"] = "Every business you manage is in good shape today.",
        ["Shell.Signal_Detail"] = "{0} unmatched invoices and {1} deadlines within the next month.",
        ["Shell.Signal_ViewActions"] = "View actions",
    });
}
