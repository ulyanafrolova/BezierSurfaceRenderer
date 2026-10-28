using System.Numerics;
using BezierSurfaceRenderer.Core.Geometry;
using BezierSurfaceRenderer.Core.Mathematics;

namespace BezierSurfaceRenderer.Core.SceneModel;

/// <summary>
/// Model state: surfaces, their meshes at the current density, and the accumulated orientation.
/// Model units: all control points lie in roughly [−1,1]³; the App scales units → pixels so that
/// the circumscribed sphere (radius √3) always fits the viewport (spec §1, "any rotation").
/// </summary>
public sealed class Scene
{
    public IReadOnlyList<BezierSurface> Surfaces { get; }
    public IReadOnlyList<Mesh> Meshes { get; private set; }
    public Mat3 Orientation { get; private set; } = Mat3.Identity;
    public int Divisions { get; private set; }

    public Scene(IReadOnlyList<BezierSurface> surfaces, int divisions) => throw new NotImplementedException();

    /// <summary>Re-triangulates all surfaces (slider change) and re-applies the current orientation.</summary>
    public void SetDivisions(int divisions) => throw new NotImplementedException();

    /// <summary>Orientation = (increment · Orientation).Orthonormalized(); rotates every vertex.</summary>
    public void Rotate(in Mat3 increment) => throw new NotImplementedException();

    public void ResetOrientation() => throw new NotImplementedException();

    /// <summary>Rotated control points of a surface (for drawing the Bézier control net).</summary>
    public Vector3 RotatedControlPoint(BezierSurface surface, int i, int j) => throw new NotImplementedException();
}
