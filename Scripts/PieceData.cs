using Godot;

namespace Tessellate;

/// <summary>
/// Definition of a glass piece — its shape and color,
/// separate from any scene node.
/// </summary>
public readonly struct PieceData
{
	public readonly Vector2[] Vertices;
	public readonly Color Color;

	public PieceData(Vector2[] vertices, Color color)
	{
		Vertices = vertices;
		Color = color;
	}
}
