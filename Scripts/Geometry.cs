using Godot;

namespace Tessellate;

/// <summary>
/// Shared geometry utilities used by snapping and connectivity.
/// </summary>
public static class Geometry
{
	public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
	{
		var ab = b - a;
		float lengthSq = ab.LengthSquared();
		if (lengthSq < 0.0001f) return a;
		float t = Mathf.Clamp((point - a).Dot(ab) / lengthSq, 0f, 1f);
		return a + ab * t;
	}

	public static float NormalizeAngle(float angle)
	{
		while (angle > Mathf.Pi) angle -= Mathf.Tau;
		while (angle < -Mathf.Pi) angle += Mathf.Tau;
		return angle;
	}

	public static Vector2 RotatePoint(Vector2 point, Vector2 pivot, float angle)
	{
		return pivot + (point - pivot).Rotated(angle);
	}
}
