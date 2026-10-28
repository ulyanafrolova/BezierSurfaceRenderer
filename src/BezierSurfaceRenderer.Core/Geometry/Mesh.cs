using System.Numerics;
using BezierSurfaceRenderer.Core.Mathematics;

namespace BezierSurfaceRenderer.Core.Geometry;

/// <summary>Mesh vertex as in spec §3: data before rotation, after rotation, and its (u, v).</summary>
public sealed class Vertex
{
    public float U { get; }
    public float V { get; }

    public SurfaceSample Original { get; }
    public SurfaceSample Rotated { get; private set; }

    public Vertex(float u, float v, SurfaceSample original) => throw new NotImplementedException();

    /// <summary>P' = R·P (about scene origin), Pu' = R·Pu, Pv' = R·Pv, N' = normalize(R·N).</summary>
    public void ApplyRotation(in Mat3 r) => throw new NotImplementedException();
}

/// <summary>Spec §3: a triangle is three vertices (shared with neighbouring triangles).</summary>
public sealed record Triangle(Vertex A, Vertex B, Vertex C);

/// <summary>Spec §3: a mesh is a list of triangles. Also keeps the source surface for control-net drawing.</summary>
public sealed class Mesh
{
    public BezierSurface Surface { get; }
    public IReadOnlyList<Vertex> Vertices { get; }
    public IReadOnlyList<Triangle> Triangles { get; }

    public Mesh(BezierSurface surface, IReadOnlyList<Vertex> vertices, IReadOnlyList<Triangle> triangles)
        => throw new NotImplementedException();

    public void ApplyRotation(in Mat3 r) => throw new NotImplementedException();
}

public static class Triangulator
{
    public const int MinDivisions = 1;
    public const int MaxDivisions = 64;

    /// <summary>
    /// Uniform (divisions+1)² grid in [0,1]², two triangles per cell → 2·divisions² triangles.
    /// Vertices are shared, so each sample is evaluated once.
    /// </summary>
    public static Mesh Triangulate(BezierSurface surface, int divisions) => throw new NotImplementedException();
}
