using Sentry.Unity;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
#if DIAG_SENTRY
        options.Enabled = true; // exclude in Android builds
#else
        options.Enabled = false;  // include otherwise
#endif
    }
}