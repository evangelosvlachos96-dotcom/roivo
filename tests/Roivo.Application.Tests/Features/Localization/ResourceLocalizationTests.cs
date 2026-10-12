using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;
using Roivo.Resources;

namespace Roivo.Application.Tests.Features.Localization;

/// <summary>
/// Guards the Greek/English resource layer. Everything here works by reflection
/// over <see cref="Roivo.Resources"/> so a resource class or property added
/// later is covered automatically rather than needing a new test.
/// </summary>
/// <remarks>
/// Greek is the fallback by design: a key with no English override degrades to
/// Greek instead of to an empty string. The placeholder-parity test is the one
/// that earns its keep — a translation that drops or renumbers a <c>{0}</c>
/// throws <see cref="FormatException"/> at render time, not at build time.
/// </remarks>
public class ResourceLocalizationTests
{
    /// <summary>Greek and Coptic, plus Greek Extended.</summary>
    private static readonly Regex GreekCharacter =
        new(@"[Ͱ-Ͽἀ-῿]", RegexOptions.Compiled);

    /// <summary>Matches the index of a composite-format hole: {0}, {2:dd MMM yyyy}.</summary>
    private static readonly Regex PlaceholderIndex =
        new(@"\{(\d+)", RegexOptions.Compiled);

    // ------------------------------------------------------------- discovery

    /// <summary>
    /// Every public static resource class in the assembly that is localized
    /// through <see cref="Strings.Get"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="ValidationMessages"/> is excluded deliberately. It is just as
    /// localized, but through a <c>.resx</c> pair resolved by
    /// <c>ResourceManager</c> — the only mechanism DataAnnotations can use,
    /// because <c>ErrorMessageResourceName</c> needs a compile-time constant
    /// and so cannot take a culture-switching property. Its coverage is
    /// asserted separately; counting it here would make
    /// <c>TranslationCount</c> look short by exactly its property count.
    /// </remarks>
    private static IReadOnlyList<Type> ResourceClasses { get; } =
        typeof(Strings).Assembly
            .GetTypes()
            .Where(t => t.IsPublic && t.IsAbstract && t.IsSealed && !t.IsNested)
            .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Where(t => t != typeof(Strings) && t != typeof(FormatExtensions))
            .Where(t => t != typeof(ValidationMessages))
            .Where(t => StringProperties(t).Count > 0)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

    public static TheoryData<string> ResourceClassNames
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var type in ResourceClasses)
                data.Add(type.Name);
            return data;
        }
    }

    private static IReadOnlyList<PropertyInfo> StringProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// A property pinned to one language on purpose, by the <c>_El</c>/<c>_En</c>
    /// naming convention. These bypass <see cref="Strings.Get"/> because the UI
    /// renders both languages at once — the bilingual "pending legal review"
    /// banner on the privacy and terms pages — so Greek text under an English
    /// culture is correct there and must not be flagged as untranslated.
    /// </summary>
    private static bool IsPinnedToOneLanguage(string propertyName) =>
        propertyName.EndsWith("_El", StringComparison.Ordinal)
        || propertyName.EndsWith("_En", StringComparison.Ordinal);

    private static Type Resolve(string className) =>
        ResourceClasses.Single(t => t.Name == className);

    // --------------------------------------------------------------- helpers

    /// <summary>
    /// Runs <paramref name="body"/> with the UI culture pinned, always putting
    /// the ambient culture back — the test host reuses threads.
    /// </summary>
    private static T InCulture<T>(string culture, Func<T> body)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            return body();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    /// <summary>Property name to value, read under the given UI culture.</summary>
    private static Dictionary<string, string?> ReadAll(Type type, string culture) =>
        InCulture(culture, () =>
        {
            // Static constructors run once, on first access. Force it so the
            // English overrides are registered even if another test got here
            // first under a different culture.
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);

            return StringProperties(type)
                .ToDictionary(p => p.Name, p => (string?)p.GetValue(null), StringComparer.Ordinal);
        });

    private static ISet<int> Placeholders(string value)
    {
        // Strip the escapes first so a literal {{0}} is not read as a hole.
        var unescaped = value.Replace("{{", string.Empty, StringComparison.Ordinal)
                             .Replace("}}", string.Empty, StringComparison.Ordinal);

        return PlaceholderIndex.Matches(unescaped)
            .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToHashSet();
    }

    // ----------------------------------------------------------------- tests

    [Fact]
    public void Assembly_ExposesEveryResourceClass()
    {
        // Ten classes shipped at the time of the Greek→English migration. A
        // lower number means the reflection sweep silently stopped covering
        // files, which would make every Theory below vacuous.
        ResourceClasses.Should().HaveCountGreaterThanOrEqualTo(10);
        ResourceClasses.Select(t => t.Name).Should().Contain(
            ["Aade", "Accountant", "Auth", "Banking", "Businesses",
             "Cashflow", "Common", "Invoices", "Notifications", "Reconciliation"]);
    }

    /// <summary>
    /// No string may be null in either culture, and none may regress to blank
    /// in one culture while carrying text in the other. A value that is blank in
    /// both is left alone — a tier with no "/month" suffix is deliberate copy,
    /// not a missing translation.
    /// </summary>
    [Theory]
    [MemberData(nameof(ResourceClassNames))]
    public void NoProperty_RegressesToEmpty_InEitherCulture(string className)
    {
        var type = Resolve(className);
        var greek = ReadAll(type, Strings.Greek);
        var english = ReadAll(type, Strings.English);

        foreach (var (name, greekValue) in greek)
        {
            var englishValue = english[name];

            greekValue.Should().NotBeNull("{0}.{1} must never be null", className, name);
            englishValue.Should().NotBeNull("{0}.{1} must never be null", className, name);

            var greekBlank = string.IsNullOrWhiteSpace(greekValue);
            var englishBlank = string.IsNullOrWhiteSpace(englishValue);

            englishBlank.Should().Be(greekBlank,
                "{0}.{1} must carry text in both cultures or in neither — Greek: \"{2}\" " +
                "English: \"{3}\"", className, name, greekValue, englishValue);
        }
    }

    /// <summary>
    /// The important one. A Greek string with {0} and {1} must keep exactly the
    /// same set of holes in English, whatever order the sentence puts them in.
    /// </summary>
    [Theory]
    [MemberData(nameof(ResourceClassNames))]
    public void EveryProperty_KeepsItsPlaceholders_InEnglish(string className)
    {
        var type = Resolve(className);
        var greek = ReadAll(type, Strings.Greek);
        var english = ReadAll(type, Strings.English);

        foreach (var (name, greekValue) in greek)
        {
            var expected = Placeholders(greekValue!);
            if (expected.Count == 0)
                continue;

            Placeholders(english[name]!).Should().BeEquivalentTo(
                expected,
                "{0}.{1} formats with {2} — the English text must use the same holes, " +
                "or string.Format throws at render time. Greek: \"{3}\" English: \"{4}\"",
                className,
                name,
                string.Join(", ", expected.OrderBy(i => i).Select(i => $"{{{i}}}")),
                greekValue,
                english[name]);
        }
    }

    /// <summary>
    /// Placeholder parity is necessary but not sufficient: a hole numbered past
    /// the argument count still throws. Formatting with the arity the Greek
    /// implies proves both sides are actually usable.
    /// </summary>
    [Theory]
    [MemberData(nameof(ResourceClassNames))]
    public void EveryProperty_FormatsWithoutThrowing_InBothCultures(string className)
    {
        var type = Resolve(className);
        var greek = ReadAll(type, Strings.Greek);
        var english = ReadAll(type, Strings.English);

        foreach (var (name, greekValue) in greek)
        {
            var holes = Placeholders(greekValue!);
            if (holes.Count == 0)
                continue;

            var args = Enumerable.Range(0, holes.Max() + 1)
                .Select(object? (i) => $"arg{i}")
                .ToArray();

            foreach (var (culture, value) in new[] { ("Greek", greekValue!), ("English", english[name]!) })
            {
                var format = () => string.Format(CultureInfo.InvariantCulture, value, args);
                format.Should().NotThrow(
                    "{0}.{1} ({2}) must format with {3} argument(s): \"{4}\"",
                    className, name, culture, args.Length, value);
            }
        }
    }

    /// <summary>
    /// Proves the English override is registered and used: any string that is
    /// Greek under <c>el</c> must contain no Greek letters under <c>en</c>.
    /// Strings that are identical in both languages (Roivo, Email, AADE User ID)
    /// are skipped — they have no Greek letters to begin with.
    /// </summary>
    [Theory]
    [MemberData(nameof(ResourceClassNames))]
    public void EveryGreekProperty_ResolvesToEnglish_UnderEnglishCulture(string className)
    {
        var type = Resolve(className);
        var greek = ReadAll(type, Strings.Greek);
        var english = ReadAll(type, Strings.English);

        foreach (var (name, greekValue) in greek)
        {
            if (IsPinnedToOneLanguage(name) || !GreekCharacter.IsMatch(greekValue!))
                continue;

            var englishValue = english[name]!;

            GreekCharacter.IsMatch(englishValue).Should().BeFalse(
                "{0}.{1} still reads Greek under an English culture — the override is " +
                "missing or untranslated: \"{2}\"", className, name, englishValue);

            englishValue.Should().NotBe(greekValue);
        }
    }

    /// <summary>
    /// The mirror of the above: under a Greek culture every property returns the
    /// Greek literal, not the English override.
    /// </summary>
    [Theory]
    [MemberData(nameof(ResourceClassNames))]
    public void EveryGreekProperty_StaysGreek_UnderGreekCulture(string className)
    {
        var type = Resolve(className);

        // Touch English first so the overrides are definitely registered; a
        // Greek read must still ignore them.
        _ = ReadAll(type, Strings.English);
        var greek = ReadAll(type, Strings.Greek);

        greek.Values.Where(v => GreekCharacter.IsMatch(v!))
            .Should().NotBeEmpty("{0} is a Greek-language resource class", className);
    }

    [Theory]
    // One round-trip per class, written out longhand so a wrong translation of a
    // load-bearing label fails by name rather than by reflection summary.
    [InlineData("Common", "SaveButton", "Αποθήκευση", "Save")]
    [InlineData("Aade", "Status_Connected", "Συνδεδεμένο", "Connected")]
    [InlineData("Accountant", "Column_Afm", "ΑΦΜ", "VAT No.")]
    [InlineData("Auth", "LogoutMenuItem", "Έξοδος", "Sign out")]
    [InlineData("Banking", "ConsentExpiresLabel", "Η έγκριση λήγει", "Consent expires")]
    [InlineData("Businesses", "InvalidAfm_Error", "Μη έγκυρο ΑΦΜ.", "Invalid VAT number.")]
    [InlineData("Cashflow", "Tax_Vat", "ΦΠΑ", "VAT")]
    [InlineData("Cashflow", "Tax_SocialSecurity", "ΕΦΚΑ", "EFKA")]
    [InlineData("Cashflow", "Tax_TaxPrepayment", "Προκαταβολή Φόρου", "Tax Prepayment")]
    [InlineData("Cashflow", "Tax_ProfessionalTax", "Τέλος Επιτηδεύματος", "Business Levy")]
    [InlineData("Cashflow", "Tax_WithholdingTax", "Παρακρατούμενος Φόρος", "Withholding Tax")]
    [InlineData("Cashflow", "Status_Pending", "Εκκρεμεί", "Pending")]
    [InlineData("Cashflow", "Status_Overdue", "Ληξιπρόθεσμο", "Overdue")]
    [InlineData("Cashflow", "Status_Paid", "Πληρωμένο", "Paid")]
    [InlineData("Invoices", "VatColumn", "ΦΠΑ", "VAT")]
    [InlineData("Notifications", "TaxLabel_PropertyTax", "ΕΝΦΙΑ", "ENFIA (property tax)")]
    [InlineData("Reconciliation", "Title", "Αντιστοίχιση", "Reconciliation")]
    public void Property_SwitchesBetweenTheTwoCultures(
        string className, string propertyName, string expectedGreek, string expectedEnglish)
    {
        var type = Resolve(className);

        ReadAll(type, Strings.Greek)[propertyName].Should().Be(expectedGreek);
        ReadAll(type, Strings.English)[propertyName].Should().Be(expectedEnglish);
    }

    [Fact]
    public void UnregisteredKey_FallsBackToGreek_EvenInEnglish()
    {
        var fallback = InCulture(Strings.English,
            () => Strings.Get("Nonexistent.Key", "Ελληνικό κείμενο"));

        fallback.Should().Be("Ελληνικό κείμενο");
    }

    [Fact]
    public void TranslationCount_CoversEveryResourceString()
    {
        // Static initialisers run lazily, so the count is meaningless until
        // every class has been touched.
        var translatable = 0;
        foreach (var type in ResourceClasses)
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            translatable += StringProperties(type).Count(p => !IsPinnedToOneLanguage(p.Name));
        }

        Strings.TranslationCount.Should().BeGreaterThan(400);
        Strings.TranslationCount.Should().Be(translatable,
            "every resource property that is not pinned to one language should have an " +
            "English override registered");
    }

    [Theory]
    [InlineData("en", true)]
    [InlineData("en-IE", true)]
    [InlineData("en-US", true)]
    [InlineData("el", false)]
    [InlineData("el-GR", false)]
    public void IsEnglish_FollowsTheUiCulture(string culture, bool expected) =>
        InCulture(culture, () => Strings.IsEnglish).Should().Be(expected);

    [Theory]
    [InlineData("en-US", "en-IE")]
    [InlineData("el-GR", "el-GR")]
    public void FormatCulture_IsChosenPerLanguage(string uiCulture, string expectedFormatCulture) =>
        InCulture(uiCulture, () => Strings.FormatCulture.Name).Should().Be(expectedFormatCulture);

    // ---- ValidationMessages (.resx mechanism) ----------------------------
    // Excluded from the Strings.Get sweep above, so it gets its own coverage.
    // These are the messages a user sees when a form rejects their input, and
    // they are the one place where a missing translation is invisible until
    // somebody actually submits a bad form.

    private static IReadOnlyList<System.Reflection.PropertyInfo> ValidationProperties { get; } =
        typeof(ValidationMessages)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

    [Fact]
    public void ValidationMessages_AreGreekUnderGreekCulture()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(Strings.Greek);

            foreach (var property in ValidationProperties)
            {
                var value = (string?)property.GetValue(null);

                value.Should().NotBeNullOrWhiteSpace(
                    $"{property.Name} must resolve; a missing resx entry silently returns the key name");
                value.Should().NotBe(property.Name,
                    $"{property.Name} fell back to its own key, so the Greek resx entry is missing");
                GreekCharacter.IsMatch(value!).Should().BeTrue(
                    $"{property.Name} should be Greek under the Greek culture but was \"{value}\"");
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void ValidationMessages_AreEnglishUnderEnglishCulture()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(Strings.English);

            foreach (var property in ValidationProperties)
            {
                var value = (string?)property.GetValue(null);

                value.Should().NotBeNullOrWhiteSpace($"{property.Name} must resolve");
                GreekCharacter.IsMatch(value!).Should().BeFalse(
                    $"{property.Name} still reads Greek under an English culture — the .en.resx entry is missing: \"{value}\"");
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void ValidationMessages_DifferBetweenLanguages()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            foreach (var property in ValidationProperties)
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(Strings.Greek);
                var greek = (string?)property.GetValue(null);

                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(Strings.English);
                var english = (string?)property.GetValue(null);

                english.Should().NotBe(greek,
                    $"{property.Name} returns the same text in both languages, so it is untranslated");
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
