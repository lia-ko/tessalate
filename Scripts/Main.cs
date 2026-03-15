using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Tessellate;

/// <summary>
/// Main scene controller. Spawns glass pieces and coordinates
/// edge-snapping when pieces are dropped near each other.
/// </summary>
public partial class Main : Node2D
{
    [Export] public float SnapDistance { get; set; } = 40.0f;
    [Export] public float SnapAngle { get; set; } = 0.5f; // ~30 degrees

    private PackedScene _pieceScene;
    private readonly List<GlassPiece> _pieces = new();

    private static readonly Vector2[][] PieceShapes =
    {
        new Vector2[] { new(0, -50), new(45, -15), new(30, 40), new(-30, 40), new(-45, -15) },
        new Vector2[] { new(-20, -55), new(25, -40), new(35, 45), new(-15, 55) },
        new Vector2[] { new(-10, -45), new(35, -30), new(50, 10), new(20, 45), new(-30, 40), new(-45, -5) },
        new Vector2[] { new(0, -40), new(40, 10), new(15, 45), new(-35, 20) },
        new Vector2[] { new(-15, -60), new(20, -45), new(35, 15), new(5, 55), new(-30, 20) },
    };

    private static readonly Color[] CathedralPalette =
    {
        new(0.15f, 0.25f, 0.65f, 0.85f), // deep blue
        new(0.65f, 0.12f, 0.15f, 0.85f),  // ruby red
        new(0.18f, 0.55f, 0.30f, 0.85f),  // emerald green
        new(0.75f, 0.60f, 0.10f, 0.85f),  // amber gold
        new(0.50f, 0.18f, 0.55f, 0.85f),  // amethyst purple
    };

    public override void _Ready()
    {
        _pieceScene = GD.Load<PackedScene>("res://Scenes/GlassPiece.tscn");
        SpawnPieces();
    }

    private void SpawnPieces()
    {
        var viewportHeight = GetViewportRect().Size.Y;

        for (int i = 0; i < PieceShapes.Length; i++)
        {
            var piece = _pieceScene.Instantiate<GlassPiece>();
            AddChild(piece);

            piece.SetShape(PieceShapes[i]);
            piece.SetColor(CathedralPalette[i % CathedralPalette.Length]);
            piece.GlobalPosition = new Vector2(120 + i * 180, viewportHeight - 150);

            piece.DragStarted += () => OnDragStarted(piece);
            piece.PieceDropped += () => OnPieceDropped(piece);

            _pieces.Add(piece);
        }
    }

    private void OnDragStarted(GlassPiece dragging)
    {
        // Cancel any other in-progress drag — only one piece at a time
        foreach (var piece in _pieces)
        {
            if (piece != dragging)
                piece.CancelDrag();
        }
    }

    private void OnPieceDropped(GlassPiece dropped)
    {
        var neighborVerts = _pieces
            .Where(p => p != dropped && p.IsPlaced)
            .Select(p => p.GetGlobalVertices())
            .ToArray();

        if (neighborVerts.Length == 0) return;

        var snap = EdgeSnap.FindBestSnap(
            dropped.GetGlobalVertices(), dropped.GlobalPosition,
            neighborVerts, SnapDistance, SnapAngle);

        if (snap.HasValue)
            dropped.AnimateSnap(snap.Value);
    }
}
