using System.Numerics;

namespace BezierSurfaceRenderer.Core.Mathematics;

/// <summary>
/// 3×3 matrix using the COLUMN-VECTOR convention from the assignment: P' = R · P,
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

    /// <summary>Builds M = [c1, c2, c3] (vectors as columns) — used for the normal-map matrix M = [Pu, Pv, N].</summary>
    public static Mat3 FromColumns(Vector3 c1, Vector3 c2, Vector3 c3) => throw new NotImplementedException();

    /// <summary>Rx(θ), Ry(θ), Rz(θ) exactly as given in the assignment.</summary>
    public static Mat3 RotationX(float theta) => throw new NotImplementedException();
    public static Mat3 RotationY(float theta) => throw new NotImplementedException();
    public static Mat3 RotationZ(float theta) => throw new NotImplementedException();

    /// <summary>Rotation about an arbitrary unit axis (Rodrigues' formula).</summary>
    public static Mat3 FromAxisAngle(Vector3 unitAxis, float theta) => throw new NotImplementedException();

    /// <summary>Matrix product A · B (apply B first, then A).</summary>
    public static Mat3 operator *(Mat3 a, Mat3 b) => throw new NotImplementedException();

    /// <summary>R · v for a column vector v.</summary>
    public static Vector3 operator *(Mat3 m, Vector3 v) => throw new NotImplementedException();

    /// <summary>Gram–Schmidt re-orthonormalisation; removes float drift after many incremental compositions.</summary>
    public Mat3 Orthonormalized() => throw new NotImplementedException();
}
