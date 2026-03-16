using Godot;
using System.Collections.Generic;

namespace Tessellate;

/// <summary>
/// A single draggable glass piece — an irregular polygon that the player
/// can pick up, rotate, flip, and place onto the window canvas.
/// </summary>
public partial class GlassPiece : Area2D
{
	[Export] public float CameWidth { get; set; } = 3.0f;

	private static readonly Color CameColor = new(0.15f, 0.15f, 0.15f);

	[Signal] public delegate void PieceDroppedEventHandler();
	[Signal] public delegate void DragStartedEventHandler();

	private Polygon2D _glass;
	private Line2D _came;
	private CollisionPolygon2D _collision;

	private bool _isDragging;
	private Vector2 _dragOffset;
	private bool _isPlaced;
	private List<GlassPiece> _dragGroup;

	public bool IsPlaced => _isPlaced;
	public Vector2 DragStartPosition { get; private set; }
	public float DragStartRotation { get; private set; }

	public override void _Ready()
	{
		_glass = GetNode<Polygon2D>("Glass");
		_came = GetNode<Line2D>("Came");
		_collision = GetNode<CollisionPolygon2D>("Collision");

		InputEvent += OnInputEvent;
		SetProcessInput(false);
		SetProcessUnhandledKeyInput(false);
	}

	// --- Public API ---

	public void SetInteractive(bool interactive) => InputPickable = interactive;

	public void SetShape(Vector2[] vertices)
	{
		_glass.Polygon = vertices;
		_collision.Polygon = vertices;

		var camePoints = new Vector2[vertices.Length + 1];
		vertices.CopyTo(camePoints, 0);
		camePoints[vertices.Length] = vertices[0];

		_came.Points = camePoints;
		_came.Width = CameWidth;
		_came.DefaultColor = CameColor;
	}

	public void SetColor(Color color)
	{
		if (_glass != null)
			_glass.Color = color;
	}

	public Vector2[] GetGlobalVertices()
	{
		var localVerts = _glass.Polygon;
		var globalVerts = new Vector2[localVerts.Length];
		for (int i = 0; i < localVerts.Length; i++)
			globalVerts[i] = GlobalTransform * localVerts[i];
		return globalVerts;
	}

	public void AnimateSnap(SnapResult snap, float duration = 0.12f)
	{
		var tween = CreateTween().SetParallel(true);
		tween.TweenProperty(this, "rotation", Rotation + snap.Rotation, duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(this, "global_position", GlobalPosition + snap.Translation, duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
	}

	public void SetDragGroup(List<GlassPiece> group) => _dragGroup = group;

	public void CancelDrag()
	{
		if (!_isDragging) return;
		_isDragging = false;
		_dragGroup = null;
		SetProcessInput(false);
		SetProcessUnhandledKeyInput(false);
	}

	// --- Input handling ---

	private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		switch (@event)
		{
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mb:
				StartDrag(mb.GlobalPosition);
				break;
			case InputEventScreenTouch { Pressed: true } touch:
				StartDrag(touch.Position);
				break;
			case InputEventScreenTouch { Pressed: false }:
				Drop();
				break;
		}
	}

	public override void _Input(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseMotion motion:
				MoveTo(motion.GlobalPosition + _dragOffset);
				break;
			case InputEventScreenDrag drag:
				MoveTo(drag.Position + _dragOffset);
				break;
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
				Drop();
				break;
			case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
				RotationDegrees += 45;
				break;
		}
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true } key) return;

		switch (key.Keycode)
		{
			case Key.R:
				RotationDegrees += 45;
				break;
			case Key.F:
				Scale = new Vector2(Scale.X * -1, Scale.Y);
				break;
		}
	}

	private void MoveTo(Vector2 newPosition)
	{
		var delta = newPosition - GlobalPosition;
		GlobalPosition = newPosition;
		if (_dragGroup == null) return;
		foreach (var member in _dragGroup)
			member.GlobalPosition += delta;
	}

	private void StartDrag(Vector2 pointerPosition)
	{
		_isDragging = true;
		_isPlaced = false;
		_dragOffset = GlobalPosition - pointerPosition;
		DragStartPosition = GlobalPosition;
		DragStartRotation = Rotation;
		SetProcessInput(true);
		SetProcessUnhandledKeyInput(true);
		EmitSignal(SignalName.DragStarted);
	}

	private void Drop()
	{
		_isDragging = false;
		_isPlaced = true;
		_dragGroup = null;
		SetProcessInput(false);
		SetProcessUnhandledKeyInput(false);
		EmitSignal(SignalName.PieceDropped);
	}
}
