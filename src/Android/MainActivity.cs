using System;
using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace EffectorAndroidRepro;

[Activity(
    Label = "Effector Repro",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        // Must be set before EffectorRuntime static init (which reads the env var once).
        Environment.SetEnvironmentVariable("EFFECTOR_ENABLE_DIRECT_RUNTIME_SHADERS", "true");

        base.OnCreate(savedInstanceState);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder)
            .LogToTrace();
}
