# Effector `ISkiaShaderEffectFactory` + `RenderTransform` — Content anchor shifts (all platforms)

## Summary

When using Effector's **shader pipeline** (`ISkiaShaderEffectFactory`) on a
visual that also has a non-identity **`RenderTransform`** (e.g. `ScaleTransform`),
the content's anchor point shifts incorrectly.  The bug reproduces on **all
platforms** (Desktop Linux, Android — likely Windows/macOS too).

**Key finding:** the shader pipeline works perfectly when no `RenderTransform`
is active.  The bug is triggered by the combination of an active Effector shader
effect *and* a non-identity `RenderTransform` on the same visual.  **Affects all
platforms** (confirmed on Linux Desktop and Android).

The **image-filter pipeline** (`ISkiaEffectFactory` returning `SKImageFilter`)
does **not** exhibit this issue because it avoids the content capture/composite
code path.

**Status:** Bug persists in Effector 0.3.0 despite PR#9 (`fix/shader-rendertransform-anchor-shift`).
The other three workarounds from 0.2.0 (env var, Android patching, Linux native assets) are
resolved in 0.3.0 and have been removed from this repro.

## Environment

| Component | Version |
|---|---|
| Effector | **0.3.0** |
| Avalonia | 11.3.12 |
| SkiaSharp | 3.119.2 |
| .NET | 9.0 (Android), 8.0 (Desktop) |
| Android min SDK | 21 |
| Test device | *(fill in your device / emulator)* |
| AOT / Trimming | disabled |

## Steps to reproduce

1. Build and deploy to an **Android device**:
   ```
   dotnet build src/Android/Android.csproj -c Debug -m:1
   dotnet build src/Android/Android.csproj -c Debug -m:1 -t:Install
   ```

2. **Baseline (effect only, no transform):**
   - Tap **"Toggle Shader Effect"** — green overlay appears.
   - Keep the Scale slider at 1.0.
   - ✅ Content stays centered.  No bug.

3. **Trigger the bug (effect + transform):**
   - With the shader effect ON, drag the **Scale slider** to 1.3 (or any
     value ≠ 1.0).
   - ❌ **Actual:** Content shifts to the upper-left with clipping.
   - ✅ **Expected:** Content stays centered, only scaled.

4. **Animated repro:**
   - With the shader effect ON, tap **"▶ Animate Scale Pulse (1→1.3→1)"**.
   - On Android: the content visibly slides/clips during the animation.
   - On Desktop: **same bug** — anchor point shifts when shader + scale are active.

5. **Desktop reproduces the same bug:**
   ```
   dotnet build src/Desktop/Desktop.csproj -c Debug -m:1
   dotnet run --project src/Desktop/Desktop.csproj
   ```
   Repeat steps 2–4 — the anchor shift is visible on Desktop too.

## Analysis

The shader pipeline's `DrawMaskedShaderOverlay` (decompiled from
`EffectorRuntime` in Effector 0.2.0) captures the visual's content to an
intermediate `SKSurface`, then composites the shader overlay via:

```csharp
canvas.Save();
canvas.ResetMatrix();
canvas.Translate(DeviceEffectBounds.Left, DeviceEffectBounds.Top);
canvas.ClipRect(intersectedBounds);
canvas.SaveLayer(intersectedBounds, paint{BlendMode});
  // draw shader rect
  canvas.DrawImage(snapshot, 0, 0, paint{DstIn});  // mask to content shape
canvas.Restore();
canvas.Restore();
```

The `ResetMatrix()` call discards the current render transform.  The
subsequent `Translate(DeviceEffectBounds)` presumably re-positions to where
the content should be — but when a `RenderTransform` is active,
`DeviceEffectBounds` appears to reflect the **pre-transform** position rather
than the **post-transform** position, causing the content snapshot to be drawn
at an incorrect offset.  **This affects all platforms, not just Android.**

Effector 0.3.0 (PR#9) added host-bounds tracking on transform mutations and
visible-area clipping, but the fundamental anchor drift still reproduces.

### Observations

- **Shader alone (Scale=1.0):** works ✅
- **Shader + Scale≠1.0:** content anchor point shifts ❌ **(all platforms)**
- **No shader + Scale≠1.0:** content scales correctly ✅
- The displacement increases with the scale factor deviation from 1.0.

Specifically on Android, with `RenderTransformOrigin="0.5,0.5"`:

| Shader | Scale | Anchor behavior |
|---|---|---|
| OFF | any | Top-left corner of the Border stays fixed. Panel scales from its center within the container. Correct. |
| ON | 1.0 | Overlay renders. Content stays centered. No issue. |
| ON | ≠1.0 | **BUG:** Anchor point shifts to near the content's center instead of the Border's top-left staying fixed. Content displaces visibly. **Affects all platforms.** |

The displacement increases with the scale factor deviation from 1.0.

### Possible root cause

`DeviceEffectBounds` is computed before the `RenderTransform` is factored in,
or the intermediate surface capture includes the transform but the re-composite
step (`ResetMatrix` + `Translate`) doesn't apply the same transform, leading to
a mismatch on Android's GPU pipeline.

## Project structure

```
src/
  App/                 Shared Avalonia library (effect + UI)
  Desktop/             Desktop head (for comparison)
  Android/             Android head (reproduces the bug)
```
