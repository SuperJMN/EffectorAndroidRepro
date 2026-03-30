using System;
using Avalonia;
using Avalonia.Media;
using Effector;
using SkiaSharp;

namespace EffectorAndroidRepro;

/// <summary>
/// Minimal shader effect that draws a semi-transparent colored overlay via the
/// Effector <b>shader pipeline</b> (<see cref="ISkiaShaderEffectFactory{T}"/>).
/// On Android the shader pipeline's content capture/composite causes the original
/// content to shift diagonally to the upper-left with progressive clipping.
/// </summary>
[SkiaEffect(typeof(OverlayShaderEffectFactory))]
public sealed class OverlayShaderEffect : SkiaEffectBase
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<OverlayShaderEffect, double>(nameof(Progress), 0.5d);

    static OverlayShaderEffect() => AffectsRender<OverlayShaderEffect>(ProgressProperty);

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }
}

/// <summary>
/// Factory that uses <see cref="ISkiaShaderEffectFactory{T}"/> (shader pipeline).
/// The shader itself is trivial — a uniform-colored semi-transparent overlay.
/// The bug manifests in the framework's content capture/composite, not the shader code.
/// </summary>
public sealed class OverlayShaderEffectFactory :
    ISkiaEffectFactory<OverlayShaderEffect>,
    ISkiaShaderEffectFactory<OverlayShaderEffect>,
    ISkiaEffectValueFactory,
    ISkiaShaderEffectValueFactory
{
    private const string ShaderSource =
        """
        uniform float progress;
        uniform float width;
        uniform float height;

        half4 main(float2 coord) {
            // Simple green tint overlay — intensity driven by progress.
            float alpha = progress * 0.4;
            return half4(0.2 * alpha, 0.8 * alpha, 0.3 * alpha, alpha);
        }
        """;

    // Filter pipeline — returns null so Effector falls through to the shader pipeline.
    public Thickness GetPadding(OverlayShaderEffect effect) => default;
    public Thickness GetPadding(object[] values) => default;
    public SKImageFilter? CreateFilter(OverlayShaderEffect effect, SkiaEffectContext ctx) => null;
    public SKImageFilter? CreateFilter(object[] values, SkiaEffectContext ctx) => null;

    // Shader pipeline — this triggers the content capture/composite code path.
    public SkiaShaderEffect CreateShaderEffect(OverlayShaderEffect effect, SkiaShaderEffectContext ctx) =>
        CreateShaderEffect(new object[] { effect.Progress }, ctx);

    public SkiaShaderEffect CreateShaderEffect(object[] values, SkiaShaderEffectContext ctx)
    {
        var progress = (float)Math.Clamp((double)values[0], 0d, 1d);

        return SkiaRuntimeShaderBuilder.Create(
            ShaderSource,
            ctx,
            uniforms =>
            {
                uniforms.Add("progress", progress);
                uniforms.Add("width", ctx.EffectBounds.Width);
                uniforms.Add("height", ctx.EffectBounds.Height);
            },
            blendMode: SKBlendMode.SrcOver);
    }
}
