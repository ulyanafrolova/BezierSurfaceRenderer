using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>
/// 3×3 matrix, column-vector convention: P' = R · P,
/// x' = r11·x + r12·y + r13·z, etc. Deliberately not System.Numerics.Matrix4x4,
/// which uses the row-vector convention and would invert multiplication order.
/// </summary>
public readonly struct Mat3
{
    public readonly float M11, M12, M13;
    public readonly float M21, M22, M23;
    public readonly float M31, M32, M33;

    public Mat3(float m11, float m12, float m13,
                float m21, float m22, float m23,
                float m31, float m32, float m33)
    {
        M11 = m11; M12 = m12; M13 = m13;
        M21 = m21; M22 = m22; M23 = m23;
        M31 = m31; M32 = m32; M33 = m33;
    }

    public static Mat3 Identity => new(1, 0, 0, 0, 1, 0, 0, 0, 1);

    public Vector3 Column1 => new(M11, M21, M31);
    public Vector3 Column2 => new(M12, M22, M32);
    public Vector3 Column3 => new(M13, M23, M33);

    /// <summary>Builds M = [c1, c2, c3] (vectors as columns) — used for the normal-map matrix M = [Pu, Pv, N].</summary>
    public static Mat3 FromColumns(Vector3 c1, Vector3 c2, Vector3 c3) =>
        new(c1.X, c2.X, c3.X,
            c1.Y, c2.Y, c3.Y,
            c1.Z, c2.Z, c3.Z);

    /// <summary>Rotation matrices Rx(θ), Ry(θ), Rz(θ).</summary>
    public static Mat3 RotationX(float theta)
    {
        var (s, c) = MathF.SinCos(theta);
        return new(1, 0, 0,
                   0, c, -s,
                   0, s, c);
    }

    public static Mat3 RotationY(float theta)
    {
        var (s, c) = MathF.SinCos(theta);
        return new(c, 0, s,
                   0, 1, 0,
                   -s, 0, c);
    }

    public static Mat3 RotationZ(float theta)
    {
        var (s, c) = MathF.SinCos(theta);
        return new(c, -s, 0,
                   s, c, 0,
                   0, 0, 1);
    }

    /// <summary>Rotation about an arbitrary unit axis (Rodrigues' formula).</summary>
    public static Mat3 FromAxisAngle(Vector3 unitAxis, float theta)
    {
        var (s, c) = MathF.SinCos(theta);
        float t = 1 - c;
        float x = unitAxis.X, y = unitAxis.Y, z = unitAxis.Z;
        return new(t * x * x + c,     t * x * y - s * z, t * x * z + s * y,
                   t * x * y + s * z, t * y * y + c,     t * y * z - s * x,
                   t * x * z - s * y, t * y * z + s * x, t * z * z + c);
    }

    /// <summary>Matrix product A · B (apply B first, then A).</summary>
    public static Mat3 operator *(Mat3 a, Mat3 b) =>
        new(a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33);

    /// <summary>R · v for a column vector v.</summary>
    public static Vector3 operator *(Mat3 m, Vector3 v) =>
        new(m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z,
            m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z,
            m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z);

    /// <summary>Gram–Schmidt re-orthonormalisation; removes float drift after many incremental compositions.</summary>
    public Mat3 Orthonormalized()
    {
        Vector3 c1 = Vector3.Normalize(Column1);
        Vector3 c2 = Vector3.Normalize(Column2 - Vector3.Dot(Column2, c1) * c1);
        Vector3 c3 = Vector3.Cross(c1, c2);
        return FromColumns(c1, c2, c3);
    }

}
