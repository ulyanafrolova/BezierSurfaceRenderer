using System.Numerics;
using BezierSurfaceRenderer.Core.Mathematics;
using static BezierSurfaceRenderer.Core.Tests.TestUtil;

namespace BezierSurfaceRenderer.Core.Tests;

public class Mat3Tests
{
    private const float Quarter = MathF.PI / 2;

    [Fact]
    public void RotationsMatchAssignmentMatrices_ColumnVectorConvention()
    {
        // Rx(90°): y → z ; Ry(90°): z → x ; Rz(90°): x → y
        Near(Vector3.UnitZ, Mat3.RotationX(Quarter) * Vector3.UnitY);
        Near(Vector3.UnitX, Mat3.RotationY(Quarter) * Vector3.UnitZ);
        Near(Vector3.UnitY, Mat3.RotationZ(Quarter) * Vector3.UnitX);
    }

    [Fact]
    public void ProductAppliesRightOperandFirst()
    {
        Mat3 r = Mat3.RotationZ(Quarter) * Mat3.RotationX(Quarter);
        // X first: y → z; then Z leaves z unchanged.
        Near(Vector3.UnitZ, r * Vector3.UnitY);
        // X leaves x; Z: x → y.
        Near(Vector3.UnitY, r * Vector3.UnitX);
    }

    [Theory]
    [InlineData(0.3f)]
    [InlineData(-2.1f)]
    public void AxisAngleMatchesElementaryRotations(float theta)
    {
        AssertSame(Mat3.RotationX(theta), Mat3.FromAxisAngle(Vector3.UnitX, theta));
        AssertSame(Mat3.RotationY(theta), Mat3.FromAxisAngle(Vector3.UnitY, theta));
        AssertSame(Mat3.RotationZ(theta), Mat3.FromAxisAngle(Vector3.UnitZ, theta));
    }

    [Fact]
    public void FromColumnsPlacesVectorsAsColumns()
    {
        var m = Mat3.FromColumns(new(1, 2, 3), new(4, 5, 6), new(7, 8, 9));
        Near(new Vector3(1, 2, 3), m * Vector3.UnitX);
        Near(new Vector3(4, 5, 6), m * Vector3.UnitY);
        Near(new Vector3(7, 8, 9), m * Vector3.UnitZ);
    }

    [Fact]
    public void OrthonormalizedRemovesDriftAfterManyIncrements()
    {
        var inc = Mat3.FromAxisAngle(Vector3.Normalize(new Vector3(1, 2, 3)), 0.0137f);
        Mat3 fixedUp = Mat3.Identity;
        for (int k = 0; k < 20000; k++)
        {
            fixedUp = (inc * fixedUp).Orthonormalized();
        }
        AssertOrthonormal(fixedUp, 1e-5f);
    }

    internal static void AssertOrthonormal(Mat3 m, float eps)
    {
        Near(1, m.Column1.Length(), eps);
        Near(1, m.Column2.Length(), eps);
        Near(1, m.Column3.Length(), eps);
        Near(0, Vector3.Dot(m.Column1, m.Column2), eps);
        Near(0, Vector3.Dot(m.Column1, m.Column3), eps);
        Near(0, Vector3.Dot(m.Column2, m.Column3), eps);
        Near(m.Column3, Vector3.Cross(m.Column1, m.Column2), eps); // right-handed: det = +1
    }

    private static void AssertSame(Mat3 a, Mat3 b)
    {
        Near(a.Column1, b.Column1);
        Near(a.Column2, b.Column2);
        Near(a.Column3, b.Column3);
    }
}
