using System.Numerics;
using BezierSurfaceRenderer.Core.Geometry;

namespace BezierSurfaceRenderer.Core.Rendering;

public enum ObjectColorSource { SolidColor, Texture }

/// <summary>Everything the filled-triangle pass needs for one frame.</summary>
public sealed record RenderSettings(
    LightingParameters Lighting,
    Vector3 LightPosition,
    ObjectColorSource ColorSource,
    Vector3 SolidColor,
    Texture? Texture,
    bool UseNormalMap,
    Texture? NormalMap);

/// <summary>
/// Filled-triangle pass. For every rotated triangle independently:
///   project (x', y') → screen, scan-convert with <see cref="EdgeTableFiller"/>, and for each pixel:
///   barycentric (λ1, λ2, λ3) in screen space (exact under orthographic projection) →
///   interpolate z, N, Pu, Pv, u, v → depth test → [normal map] → IO (solid / texture(u,v)) → Lighting.Shade.
/// Rendering is split into horizontal bands processed in parallel; each band owns its rows of
/// the colour and depth buffers, so no locking is needed.
/// </summary>
public sealed class SceneRenderer
{
    public void RenderFilled(FrameBuffer target, IReadOnlyList<Mesh> meshes, RenderSettings settings)
        => throw new NotImplementedException();
}
