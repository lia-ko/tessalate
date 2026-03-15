using Godot;
using System;

namespace Tessellate;

/// <summary>
/// A single draggable glass piece — an irregular polygon that the player
/// can pick up, rotate, flip, and place onto the window canvas.
/// </summary>
public partial class GlassPiece : Area2D
{
    [Export] public Color GlassColor { get; set; } = new Color(0.2f, 0.4f, 0.8f, 0.85f);
    [Export] public float CameWidth { get; set; } = 3.0f;
    [Export] public float SnapDistance { get; set; } = 40.0f;
    [Export] public float SnapAngle { get; set; } = 0.5f; // ~30 degrees in radians

    private Polygon2D _glass;
    private Line2D _came;
    private CollisionPolygon2D _collision;

    private bool _isDragging;
    private Vector2 _dragOffset;
    private bool _isPlaced;

    public bool IsPlaced => _isPlaced;

    public override void _Ready()
    {
        _glass = GetNode<Polygon2D>("Glass");
        _came = GetNode<Line2D>("Came");
        _collision = GetNode<CollisionPolygon2D>("Collision");

        _glass.Color = GlassColor;

        InputEvent += OnInputEvent;
    }

    public void SetShape(Vector2[] vertices)
    {
        _glass.Polygon = vertices;
        _collision.Polygon = vertices;

        var camePoints = new Vector2[vertices.Length + 1];
        for (int i = 0; i < vertices.Length; i++)
            camePoints[i] = vertices[i];
        camePoints[vertices.Length] = vertices[0];

        _came.Points = camePoints;
        _came.Width = CameWidth;
        _came.DefaultColor = new Color(0.15f, 0.15f, 0.15f);
    }

    public Vector2[] GetGlobalVertices()
    {
        var localVerts = _glass.Polygon;
        var globalVerts = new Vector2[localVerts.Length];
        for (int i = 0; i < localVerts.Length; i++)
            globalVerts[i] = GlobalTransform * localVerts[i];
        return globalVerts;
    }

    public void SetColor(Color color)
    {
        GlassColor = color;
        if (_glass != null)
            _glass.Color = color;
    }

    private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
                _isDragging = true;
                _dragOffset = GlobalPosition - mb.GlobalPosition;
                _isPlaced = false;
            }
        }

        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                _isDragging = true;
                _dragOffset = GlobalPosition - touch.Position;
                _isPlaced = false;
            }
            else
            {
                Drop();
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isDragging) return;

        if (@event is InputEventMouseMotion motion)
        {
            GlobalPosition = motion.GlobalPosition + _dragOffset;
        }
        else if (@event is InputEventScreenDrag drag)
        {
            GlobalPosition = drag.Position + _dragOffset;
        }
        else if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
        {
            Drop();
        }

        if (@event is InputEventMouseButton rmb && rmb.ButtonIndex == MouseButton.Right && rmb.Pressed)
        {
            RotationDegrees += 45;
        }
    }

    private void Drop()
    {
        _isDragging = false;
        _isPlaced = true;
        TrySnap();
    }

    private void TrySnap()
    {
        float bestScore = float.MaxValue;
        float bestRotation = 0f;
        Vector2 bestTranslation = Vector2.Zero;
        bool found = false;

        var myVerts = GetGlobalVertices();

        foreach (var child in GetParent().GetChildren())
        {
            if (child is not GlassPiece other || other == this || !other.IsPlaced)
                continue;

            var otherVerts = other.GetGlobalVertices();

            // Check every edge pair between the two pieces
            for (int i = 0; i < myVerts.Length; i++)
            {
                var myA = myVerts[i];
                var myB = myVerts[(i + 1) % myVerts.Length];
                var myMid = (myA + myB) * 0.5f;
                float myAngle = (myB - myA).Angle();

                for (int j = 0; j < otherVerts.Length; j++)
                {
                    var otherA = otherVerts[j];
                    var otherB = otherVerts[(j + 1) % otherVerts.Length];
                    var otherMid = (otherA + otherB) * 0.5f;
                    float otherAngle = (otherB - otherA).Angle();

                    // Quick distance check — skip if edges are far apart
                    float midDist = myMid.DistanceTo(otherMid);
                    if (midDist > SnapDistance * 3) continue;

                    // Rotation to make edges anti-parallel (flush facing each other)
                    float rotDelta = NormalizeAngle(otherAngle + Mathf.Pi - myAngle);

                    // Only snap if edges are already roughly aligned
                    if (Mathf.Abs(rotDelta) > SnapAngle) continue;

                    // Simulate where my edge endpoints land after rotation
                    var rotMyA = RotatePoint(myA, GlobalPosition, rotDelta);
                    var rotMyB = RotatePoint(myB, GlobalPosition, rotDelta);

                    // Find the best vertex-to-vertex alignment for positioning
                    // Try all four pairings of the two edge endpoints
                    Vector2[] rotPts = { rotMyA, rotMyB };
                    Vector2[] otherPts = { otherA, otherB };

                    foreach (var rp in rotPts)
                    {
                        foreach (var op in otherPts)
                        {
                            float dist = rp.DistanceTo(op);
                            if (dist < SnapDistance && dist < bestScore)
                            {
                                bestScore = dist;
                                bestRotation = rotDelta;
                                bestTranslation = op - rp;
                                found = true;
                            }
                        }

                        // Also try snapping to closest point along the other edge
                        var cp = ClosestPointOnSegment(rp, otherA, otherB);
                        float cpDist = rp.DistanceTo(cp);
                        if (cpDist < SnapDistance && cpDist < bestScore)
                        {
                            bestScore = cpDist;
                            bestRotation = rotDelta;
                            bestTranslation = cp - rp;
                            found = true;
                        }
                    }
                }
            }
        }

        if (found)
        {
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(this, "rotation",
                Rotation + bestRotation, 0.12f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(this, "global_position",
                GlobalPosition + bestTranslation, 0.12f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
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

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.R && _isDragging)
            {
                RotationDegrees += 45;
            }
            else if (key.Keycode == Key.F && _isDragging)
            {
                Scale = new Vector2(Scale.X * -1, Scale.Y);
            }
        }
    }
}
