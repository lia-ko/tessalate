using Godot;

namespace Tessellate;

/// <summary>
/// The stained glass window frame — draws a dark arched window silhouette.
/// </summary>
public partial class WindowCanvas : Node2D
{
    [Export] public float WindowWidth { get; set; } = 600f;
    [Export] public float WindowHeight { get; set; } = 900f;

    private static readonly Color FrameColor = new(0.1f, 0.1f, 0.12f);
    private static readonly Color BorderColor = new(0.2f, 0.2f, 0.22f);
    private const float BorderWidth = 6f;

    public override void _Draw()
    {
        float x = -WindowWidth / 2;
        float y = -WindowHeight / 2;

        // Dark background rectangle
        DrawRect(new Rect2(x, y, WindowWidth, WindowHeight), FrameColor);

        // Arched top — semicircle at top of rectangle
        var archCenter = new Vector2(0, y);
        float archRadius = WindowWidth / 2;
        DrawArc(archCenter, archRadius, 0, Mathf.Pi, 64, FrameColor, archRadius);

        // Border (lead came frame)
        DrawRect(new Rect2(x, y, WindowWidth, WindowHeight), BorderColor, false, BorderWidth);
        DrawArc(archCenter, archRadius, Mathf.Pi, Mathf.Tau, 64, BorderColor, BorderWidth);
    }
}
