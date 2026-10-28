using System.Numerics;
using BezierSurfaceRenderer.Core.Mathematics;

namespace BezierSurfaceRenderer.Core.Interaction;

/// <summary>
/// Maps mouse movement to an INCREMENTAL rotation (spec §2, R_new = R_inc · R_cur).
///
/// Left drag — virtual trackball (Shoemake arcball): consecutive cursor positions p0, p1
/// (scene coords, origin centre, y up) are projected onto a sphere of radius r covering the viewport
/// (points outside fall onto the sphere's silhouette, giving roll about the view axis).
///   axis = normalize(s0 × s1), angle = acos(clamp(s0 · s1)), R_inc = Rot(axis, angle).
/// Shift/right drag — pure roll about the view axis z: angle = atan2 difference around the centre.
///
/// The controller only yields R_inc; the scene composes and re-orthonormalises. Angles are never
/// clamped and nothing is computed from the drag's start position.
/// </summary>
public sealed class ArcballController
{
    private const float Epsilon = 1e-7f;

    public float Radius { get; set; } = 1f;

    public Mat3 TrackballIncrement(Vector2 previous, Vector2 current)
    {
        Vector3 s0 = ProjectToSphere(previous), s1 = ProjectToSphere(current);
        Vector3 axis = Vector3.Cross(s0, s1);
        float length = axis.Length();
        if (length < Epsilon) return Mat3.Identity;

        float angle = MathF.Acos(Math.Clamp(Vector3.Dot(s0, s1), -1f, 1f));
        return Mat3.FromAxisAngle(axis / length, angle);
    }

    public Mat3 RollIncrement(Vector2 previous, Vector2 current)
    {
        if (previous.LengthSquared() < Epsilon || current.LengthSquared() < Epsilon) return Mat3.Identity;

        // Signed angle from previous to current around the centre (counter-clockwise positive, y up).
        float cross = previous.X * current.Y - previous.Y * current.X;
        float angle = MathF.Atan2(cross, Vector2.Dot(previous, current));
        return Mat3.RotationZ(angle);
    }

    /// <summary>Exposed for tests and for the documentation's worked example.</summary>
    public Vector3 ProjectToSphere(Vector2 point)
    {
        Vector2 q = point / Radius;
        float d2 = q.LengthSquared();
        if (d2 <= 1f) return new Vector3(q.X, q.Y, MathF.Sqrt(1f - d2));

        // Outside the ball: nearest point on its silhouette (z = 0) → rotation about the view axis.
        q /= MathF.Sqrt(d2);
        return new Vector3(q.X, q.Y, 0f);
    }
}
