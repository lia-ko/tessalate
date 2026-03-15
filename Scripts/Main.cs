using Godot;

namespace Tessellate;

/// <summary>
/// Main scene controller. Sets up the window canvas and spawns
/// a few hardcoded glass pieces for the Phase 1 prototype.
/// </summary>
public partial class Main : Node2D
{
    private PackedScene _pieceScene;
    private WindowCanvas _canvas;

    // Hardcoded piece shapes — irregular polygons (4-6 sides)
    private static readonly Vector2[][] PieceShapes = new[]
    {
        // Piece 1: irregular pentagon
        new Vector2[]
        {
            new(0, -50), new(45, -15), new(30, 40),
            new(-30, 40), new(-45, -15)
        },
        // Piece 2: elongated quad
        new Vector2[]
        {
            new(-20, -55), new(25, -40), new(35, 45),
            new(-15, 55)
        },
        // Piece 3: wide hexagon
        new Vector2[]
        {
            new(-10, -45), new(35, -30), new(50, 10),
            new(20, 45), new(-30, 40), new(-45, -5)
        },
        // Piece 4: small triangle-ish quad
        new Vector2[]
        {
            new(0, -40), new(40, 10), new(15, 45),
            new(-35, 20)
        },
        // Piece 5: irregular pentagon (tall)
        new Vector2[]
        {
            new(-15, -60), new(20, -45), new(35, 15),
            new(5, 55), new(-30, 20)
        },
    };

    // Stained glass color palette (Cathedral)
    private static readonly Color[] Palette = new[]
    {
        new Color(0.15f, 0.25f, 0.65f, 0.85f),  // deep blue
        new Color(0.65f, 0.12f, 0.15f, 0.85f),   // ruby red
        new Color(0.18f, 0.55f, 0.30f, 0.85f),    // emerald green
        new Color(0.75f, 0.60f, 0.10f, 0.85f),    // amber gold
        new Color(0.50f, 0.18f, 0.55f, 0.85f),    // amethyst purple
    };

    public override void _Ready()
    {
        _pieceScene = GD.Load<PackedScene>("res://Scenes/GlassPiece.tscn");
        _canvas = GetNode<WindowCanvas>("WindowCanvas");

        SpawnPieces();
    }

    private void SpawnPieces()
    {
        var viewport = GetViewportRect().Size;

        for (int i = 0; i < PieceShapes.Length; i++)
        {
            var piece = _pieceScene.Instantiate<GlassPiece>();
            AddChild(piece);

            piece.SetShape(PieceShapes[i]);
            piece.SetColor(Palette[i % Palette.Length]);

            // Spread pieces along the bottom of the screen
            float xPos = 120 + i * 180;
            float yPos = viewport.Y - 150;
            piece.GlobalPosition = new Vector2(xPos, yPos);
        }
    }
}
