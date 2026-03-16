using Godot;

namespace Tessellate;

/// <summary>
/// Records a piece's position and rotation before a move,
/// so it can be restored on undo.
/// </summary>
public readonly struct UndoEntry
{
	public readonly GlassPiece Piece;
	public readonly Vector2 Position;
	public readonly float Rotation;

	public UndoEntry(GlassPiece piece, Vector2 position, float rotation)
	{
		Piece = piece;
		Position = position;
		Rotation = rotation;
	}
}
