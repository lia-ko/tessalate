using Godot;

namespace Tessellate;

/// <summary>
/// Result of an edge-snap calculation: the rotation and translation
/// needed to align a piece flush against a neighbor.
/// </summary>
public readonly struct SnapResult
{
    public readonly float Rotation;
    public readonly Vector2 Translation;

    public SnapResult(float rotation, Vector2 translation)
    {
        Rotation = rotation;
        Translation = translation;
    }
}

/// <summary>
/// Geometry utilities for snapping glass piece edges together.
/// Finds the best rotation + translation to align two pieces flush.
/// </summary>
public static class EdgeSnap
{
    public static SnapResult? FindBestSnap(
        Vector2[] pieceVerts, Vector2 pieceOrigin,
        Vector2[][] neighborVertSets,
        float maxDistance, float maxAngle)
    {
        float bestScore = float.MaxValue;
        float bestRotation = 0f;
        Vector2 bestTranslation = Vector2.Zero;
        bool found = false;

        for (int i = 0; i < pieceVerts.Length; i++)
        {
            var myA = pieceVerts[i];
            var myB = pieceVerts[(i + 1) % pieceVerts.Length];
            var myMid = (myA + myB) * 0.5f;
            float myAngle = (myB - myA).Angle();

            foreach (var otherVerts in neighborVertSets)
            {
                for (int j = 0; j < otherVerts.Length; j++)
                {
                    var otherA = otherVerts[j];
                    var otherB = otherVerts[(j + 1) % otherVerts.Length];

                    // Early out if edges are far apart
                    var otherMid = (otherA + otherB) * 0.5f;
                    if (myMid.DistanceTo(otherMid) > maxDistance * 3f) continue;

                    // Rotation to make edges anti-parallel (flush)
                    float otherAngle = (otherB - otherA).Angle();
                    float rotDelta = NormalizeAngle(otherAngle + Mathf.Pi - myAngle);
                    if (Mathf.Abs(rotDelta) > maxAngle) continue;

                    // Where do my endpoints land after rotation?
                    var rotMyA = RotatePoint(myA, pieceOrigin, rotDelta);
                    var rotMyB = RotatePoint(myB, pieceOrigin, rotDelta);

                    // Try snapping each rotated endpoint to each target endpoint or edge
                    TryCandidate(rotMyA, otherA, rotDelta, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
                    TryCandidate(rotMyA, otherB, rotDelta, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
                    TryCandidate(rotMyB, otherA, rotDelta, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
                    TryCandidate(rotMyB, otherB, rotDelta, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);

                    // Also try snapping to closest point along the target edge
                    var cpA = ClosestPointOnSegment(rotMyA, otherA, otherB);
                    var cpB = ClosestPointOnSegment(rotMyB, otherA, otherB);
                    TryCandidate(rotMyA, cpA, rotDelta, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
                    TryCandidate(rotMyB, cpB, rotDelta, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
                }
            }
        }

        return found ? new SnapResult(bestRotation, bestTranslation) : null;
    }

    private static void TryCandidate(
        Vector2 from, Vector2 to, float rotation, float maxDistance,
        ref float bestScore, ref float bestRotation, ref Vector2 bestTranslation, ref bool found)
    {
        float dist = from.DistanceTo(to);
        if (dist < maxDistance && dist < bestScore)
        {
            bestScore = dist;
            bestRotation = rotation;
            bestTranslation = to - from;
            found = true;
        }
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > Mathf.Pi) angle -= Mathf.Tau;
        while (angle < -Mathf.Pi) angle += Mathf.Tau;
        return angle;
    }

    private static Vector2 RotatePoint(Vector2 point, Vector2 pivot, float angle)
    {
        return pivot + (point - pivot).Rotated(angle);
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float lengthSq = ab.LengthSquared();
        if (lengthSq < 0.0001f) return a;
        float t = Mathf.Clamp((point - a).Dot(ab) / lengthSq, 0f, 1f);
        return a + ab * t;
    }
}
