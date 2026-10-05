using System.Globalization;
using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Pins the thread's culture to the recipient's language for the duration of a
/// single template render, then restores whatever was there before.
/// </summary>
/// <remarks>
/// <para>
/// The resource layer resolves every string through
/// <see cref="Strings.Get"/>, which reads
/// <see cref="CultureInfo.CurrentUICulture"/>. That is right for a web request
/// — the request culture provider has already set it from the user's cookie —
/// but wrong for the notification jobs: Hangfire runs them on a worker thread
/// whose culture is the machine default, so an ambient read would send a Greek
/// email to an English user, silently and in production.
/// </para>
/// <para>
/// So language is an explicit argument on every <c>Render</c> entry point,
/// defaulting to Greek (the product's primary language), and the renderer
/// brackets its own work with this scope. The alternative — threading an
/// <c>english</c> flag down to each of the ~60 resource reads — would mean
/// duplicating the whole <see cref="Strings"/> API with an explicit-culture
/// overload for no behavioural gain. Setting and restoring the culture keeps
/// the existing resource call sites untouched while making the language a
/// typed parameter at the boundary where it matters.
/// </para>
/// <para>
/// Culture assignment flows with the execution context, not the raw thread, so
/// a render that awaited nothing (these are all synchronous) cannot leak the
/// culture into a sibling job.
/// </para>
/// </remarks>
internal static class EmailCulture
{
    private static readonly CultureInfo GreekCulture = CultureInfo.GetCultureInfo("el-GR");
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-IE");

    /// <summary>Enters a render scope for the recipient's language.</summary>
    /// <param name="english">True for English; false for Greek, the default.</param>
    internal static Scope For(bool english) => new(english ? EnglishCulture : GreekCulture);

    /// <summary>Restores the previous culture when disposed.</summary>
    internal readonly struct Scope : IDisposable
    {
        private readonly CultureInfo _previousCulture;
        private readonly CultureInfo _previousUiCulture;

        internal Scope(CultureInfo culture)
        {
            _previousCulture = CultureInfo.CurrentCulture;
            _previousUiCulture = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
        }
    }
}
