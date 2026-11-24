using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>
/// Maps mouse movement to an incremental rotation (R_new = R_inc · R_cur).
/// Positions are in pixels relative to the viewport centre, y up.
///
/// Trackball (Shoemake arcball): consecutive cursor positions p0, p1 are projected onto a sphere of
/// radius r covering the viewport (points outside fall onto its silhouette, giving roll about z).
///   axis = normalize(s0 × s1), angle = acos(clamp(s0 · s1)), R_inc = Rot(axis, angle).
/// </summary>
public static class Arcball
{
    private const float Epsilon = 1e-7f;

    public static Mat3 TrackballIncrement(Vector2 previous, Vector2 current, float radius)
    {
        Vector3 s0 = ProjectToSphere(previous, radius), s1 = ProjectToSphere(current, radius);
        Vector3 axis = Vector3.Cross(s0, s1);
        float length = axis.Length();
        if (length < Epsilon) return Mat3.Identity;

        float angle = MathF.Acos(Math.Clamp(Vector3.Dot(s0, s1), -1f, 1f));
        return Mat3.FromAxisAngle(axis / length, angle);
    }

    public static Vector3 ProjectToSphere(Vector2 point, float radius)
    {
        Vector2 q = point / radius;
        float d2 = q.LengthSquared();
        if (d2 <= 1f) return new Vector3(q.X, q.Y, MathF.Sqrt(1f - d2));

        // Outside the ball: nearest point on its silhouette (z = 0) → rotation about the view axis.
        q /= MathF.Sqrt(d2);
        return new Vector3(q.X, q.Y, 0f);
    }
}
