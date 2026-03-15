using Godot;
using System.Collections.Generic;

namespace Tessellate;

/// <summary>
/// The stained glass window frame — the canvas where pieces are placed.
/// Draws a dark background with an arched window silhouette.
/// </summary>
public partial class WindowCanvas : Node2D
{
    private readonly List<GlassPiece> _placedPieces = new();

    public override void _Draw()
    {
        // Draw a simple arched window frame
        var frameColor = new Color(0.1f, 0.1f, 0.12f);
        var windowWidth = 600f;
        var windowHeight = 900f;
        var x = -windowWidth / 2;
        var y = -windowHeight / 2;

        // Dark background rectangle
        DrawRect(new Rect2(x, y, windowWidth, windowHeight), frameColor);

        // Arched top — draw a semicircle at the top of the rectangle
        var archCenter = new Vector2(0, y);
        var archRadius = windowWidth / 2;
        DrawArc(archCenter, archRadius, 0, Mathf.Pi, 64, frameColor, archRadius);

        // Border (lead came frame)
        var borderColor = new Color(0.2f, 0.2f, 0.22f);
        var borderWidth = 6f;
        DrawRect(new Rect2(x, y, windowWidth, windowHeight), borderColor, false, borderWidth);
        DrawArc(archCenter, archRadius, Mathf.Pi, Mathf.Tau, 64, borderColor, borderWidth);
    }

    public void RegisterPiece(GlassPiece piece)
    {
        _placedPieces.Add(piece);
    }
}
