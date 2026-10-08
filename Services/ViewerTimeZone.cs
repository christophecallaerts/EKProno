namespace EKProno.Services;

/// <summary>
/// FR-009 / FR-017: kickoffs are entered and displayed in the viewer's own time zone.
/// The browser reports it once into a cookie (see <c>_TimeZonePartial</c>); without one we
/// fall back to the server's zone rather than guessing UTC.
/// </summary>
public static class ViewerTimeZone
{
    public const string CookieName = "ekprono-tz";

    public static TimeZoneInfo Of(HttpContext httpContext) =>
        ScheduleService.ResolveTimeZone(httpContext.Request.Cookies[CookieName]);
}
