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
    public float Radius { get; set; }

    public Mat3 TrackballIncrement(Vector2 previous, Vector2 current) => throw new NotImplementedException();

    public Mat3 RollIncrement(Vector2 previous, Vector2 current) => throw new NotImplementedException();

    /// <summary>Exposed for tests and for the documentation's worked example.</summary>
    public Vector3 ProjectToSphere(Vector2 point) => throw new NotImplementedException();
}
