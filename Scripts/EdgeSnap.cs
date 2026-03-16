using Godot;

namespace Tessellate;

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
/// Snaps glass pieces together. Tries edge-aligned snap (rotate + translate)
/// first, then falls back to proximity snap (translate only).
/// </summary>
public static class EdgeSnap
{
	private const float RotationPenalty = 20f;

	public static SnapResult? FindBestSnap(
		Vector2[] pieceVerts, Vector2 pieceOrigin,
		Vector2[][] neighborVertSets, float maxDistance)
	{
		var aligned = FindAlignedSnap(pieceVerts, pieceOrigin, neighborVertSets, maxDistance);
		var proximity = FindProximitySnap(pieceVerts, neighborVertSets, maxDistance);

		if (aligned.HasValue && proximity.HasValue)
		{
			float alignedDist = aligned.Value.Translation.Length();
			float proximityDist = proximity.Value.Translation.Length();
			return alignedDist < proximityDist + 20f ? aligned : proximity;
		}

		return aligned ?? proximity;
	}

	private static SnapResult? FindAlignedSnap(
		Vector2[] pieceVerts, Vector2 pieceOrigin,
		Vector2[][] neighborVertSets, float maxDistance)
	{
		float bestScore = maxDistance;
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

					var otherMid = (otherA + otherB) * 0.5f;
					if (myMid.DistanceTo(otherMid) > maxDistance * 3f) continue;

					float otherAngle = (otherB - otherA).Angle();
					float rotDelta = Geometry.NormalizeAngle(otherAngle + Mathf.Pi - myAngle);
					float rotCost = Mathf.Abs(rotDelta) * RotationPenalty;

					var rotMyA = Geometry.RotatePoint(myA, pieceOrigin, rotDelta);
					var rotMyB = Geometry.RotatePoint(myB, pieceOrigin, rotDelta);

					// Vertex-to-vertex
					TryCandidate(rotMyA, otherA, rotDelta, rotCost, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
					TryCandidate(rotMyA, otherB, rotDelta, rotCost, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
					TryCandidate(rotMyB, otherA, rotDelta, rotCost, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
					TryCandidate(rotMyB, otherB, rotDelta, rotCost, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);

					// Vertex-to-edge
					TryCandidate(rotMyA, Geometry.ClosestPointOnSegment(rotMyA, otherA, otherB), rotDelta, rotCost, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
					TryCandidate(rotMyB, Geometry.ClosestPointOnSegment(rotMyB, otherA, otherB), rotDelta, rotCost, maxDistance, ref bestScore, ref bestRotation, ref bestTranslation, ref found);
				}
			}
		}

		return found ? new SnapResult(bestRotation, bestTranslation) : null;
	}

	private static SnapResult? FindProximitySnap(
		Vector2[] pieceVerts, Vector2[][] neighborVertSets, float maxDistance)
	{
		float bestDist = maxDistance;
		Vector2 bestTranslation = Vector2.Zero;
		bool found = false;

		foreach (var otherVerts in neighborVertSets)
		{
			// Piece vertex → neighbor vertex
			foreach (var mv in pieceVerts)
			{
				foreach (var ov in otherVerts)
					TryProximity(mv, ov, ref bestDist, ref bestTranslation, ref found);
			}

			// Piece vertex → neighbor edge
			foreach (var mv in pieceVerts)
			{
				for (int i = 0; i < otherVerts.Length; i++)
				{
					var closest = Geometry.ClosestPointOnSegment(mv, otherVerts[i], otherVerts[(i + 1) % otherVerts.Length]);
					TryProximity(mv, closest, ref bestDist, ref bestTranslation, ref found);
				}
			}

			// Neighbor vertex → piece edge
			foreach (var ov in otherVerts)
			{
				for (int i = 0; i < pieceVerts.Length; i++)
				{
					var closest = Geometry.ClosestPointOnSegment(ov, pieceVerts[i], pieceVerts[(i + 1) % pieceVerts.Length]);
					float dist = ov.DistanceTo(closest);
					if (dist < bestDist)
					{
						bestDist = dist;
						bestTranslation = ov - closest;
						found = true;
					}
				}
			}
		}

		return found ? new SnapResult(0f, bestTranslation) : null;
	}

	private static void TryProximity(Vector2 from, Vector2 to,
		ref float bestDist, ref Vector2 bestTranslation, ref bool found)
	{
		float dist = from.DistanceTo(to);
		if (dist < bestDist)
		{
			bestDist = dist;
			bestTranslation = to - from;
			found = true;
		}
	}

	private static void TryCandidate(
		Vector2 from, Vector2 to, float rotation, float rotCost, float maxDistance,
		ref float bestScore, ref float bestRotation, ref Vector2 bestTranslation, ref bool found)
	{
		float dist = from.DistanceTo(to);
		float score = dist + rotCost;
		if (dist < maxDistance && score < bestScore)
		{
			bestScore = score;
			bestRotation = rotation;
			bestTranslation = to - from;
			found = true;
		}
	}
}
