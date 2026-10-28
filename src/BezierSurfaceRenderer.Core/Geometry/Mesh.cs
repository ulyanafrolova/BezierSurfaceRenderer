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

    public Vertex(float u, float v, SurfaceSample original)
    {
        U = u;
        V = v;
        Original = original;
        Rotated = original;
    }

    /// <summary>P' = R·P (about scene origin), Pu' = R·Pu, Pv' = R·Pv, N' = normalize(R·N).</summary>
    public void ApplyRotation(in Mat3 r)
    {
        var o = Original;
        Rotated = new SurfaceSample(r * o.P, r * o.Pu, r * o.Pv, Vector3.Normalize(r * o.N));
    }
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
    {
        Surface = surface;
        Vertices = vertices;
        Triangles = triangles;
    }

    public void ApplyRotation(in Mat3 r)
    {
        foreach (var vertex in Vertices)
            vertex.ApplyRotation(r);
    }
}

public static class Triangulator
{
    public const int MinDivisions = 1;
    public const int MaxDivisions = 64;

    /// <summary>
    /// Uniform (divisions+1)² grid in [0,1]², two triangles per cell → 2·divisions² triangles.
    /// Vertices are shared, so each sample is evaluated once.
    /// </summary>
    public static Mesh Triangulate(BezierSurface surface, int divisions)
    {
        divisions = Math.Clamp(divisions, MinDivisions, MaxDivisions);
        int side = divisions + 1;

        var vertices = new Vertex[side * side];
        for (int a = 0; a < side; a++)
        for (int b = 0; b < side; b++)
        {
            float u = (float)a / divisions, v = (float)b / divisions;
            vertices[a * side + b] = new Vertex(u, v, surface.Evaluate(u, v));
        }

        var triangles = new List<Triangle>(2 * divisions * divisions);
        for (int a = 0; a < divisions; a++)
        for (int b = 0; b < divisions; b++)
        {
            Vertex v00 = vertices[a * side + b], v10 = vertices[(a + 1) * side + b];
            Vertex v01 = vertices[a * side + b + 1], v11 = vertices[(a + 1) * side + b + 1];
            triangles.Add(new Triangle(v00, v10, v11));
            triangles.Add(new Triangle(v00, v11, v01));
        }
        return new Mesh(surface, vertices, triangles);
    }
}
