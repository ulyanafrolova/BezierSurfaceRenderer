using System.Numerics;

namespace BezierSurfaceRenderer.Core;

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

    /// <summary>Radius of the smallest origin-centred sphere containing every control point.</summary>
    public float BoundingRadius { get; }

    public Scene(IReadOnlyList<BezierSurface> surfaces, int divisions)
    {
        Surfaces = surfaces;
        BoundingRadius = surfaces.SelectMany(s => s.ControlPoints.Cast<Vector3>()).Max(p => p.Length());
        Meshes = [];
        SetDivisions(divisions);
    }

    /// <summary>Re-triangulates all surfaces (slider change) and re-applies the current orientation.</summary>
    public void SetDivisions(int divisions)
    {
        Divisions = Math.Clamp(divisions, Triangulator.MinDivisions, Triangulator.MaxDivisions);
        Meshes = Surfaces.Select(s => Triangulator.Triangulate(s, Divisions)).ToList();
        ApplyOrientation();
    }

    /// <summary>Orientation = (increment · Orientation).Orthonormalized(); rotates every vertex.</summary>
    public void Rotate(in Mat3 increment)
    {
        Orientation = (increment * Orientation).Orthonormalized();
        ApplyOrientation();
    }

    public void ResetOrientation()
    {
        Orientation = Mat3.Identity;
        ApplyOrientation();
    }

    /// <summary>Rotated control points of a surface (for drawing the Bézier control net).</summary>
    public Vector3 RotatedControlPoint(BezierSurface surface, int i, int j) => Orientation * surface.ControlPoints[i, j];

    // Vertices are always rotated from their ORIGINAL data by the accumulated matrix, so mesh data
    // never accumulates error; only the orientation is composed incrementally.
    private void ApplyOrientation()
    {
        foreach (var mesh in Meshes)
            mesh.ApplyRotation(Orientation);
    }
}
