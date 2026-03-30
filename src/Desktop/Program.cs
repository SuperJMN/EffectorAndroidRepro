using System;
using Avalonia;

namespace EffectorAndroidRepro;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Enable Effector runtime shaders (required for SkiaSharp 3.x).
        Environment.SetEnvironmentVariable("EFFECTOR_ENABLE_DIRECT_RUNTIME_SHADERS", "true");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
