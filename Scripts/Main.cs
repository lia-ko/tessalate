using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Tessellate;

/// <summary>
/// Main scene controller. Orchestrates the piece queue, preview display,
/// edge-snapping, lock mode for cluster movement, and undo.
/// </summary>
public partial class Main : Node2D
{
	[Export] public float SnapDistance { get; set; } = 120.0f;
	[Export] public float PreviewScale { get; set; } = 0.55f;
	[Export] public float ConnectionThreshold { get; set; } = 8.0f;
	[Export] public int MaxUndoSteps { get; set; } = 5;

	private PackedScene _pieceScene;
	private PieceQueue _queue;
	private Button _lockButton;
	private Button _undoButton;
	private bool _isLocked;

	private readonly List<GlassPiece> _allPieces = new();
	private readonly List<UndoEntry> _undoHistory = new();
	private GlassPiece _activePiece;
	private GlassPiece _preview1;
	private GlassPiece _preview2;

	public override void _Ready()
	{
		_pieceScene = GD.Load<PackedScene>("res://Scenes/GlassPiece.tscn");

		_lockButton = GetNode<Button>("UI/LockButton");
		_lockButton.Pressed += ToggleLock;

		_undoButton = GetNode<Button>("UI/UndoButton");
		_undoButton.Pressed += Undo;

		_queue = PieceQueue.Create(PieceShapes.All, PieceShapes.CathedralPalette);
		AdvanceQueue();
	}

	// --- Queue ---

	private void AdvanceQueue()
	{
		var next = _queue.Dequeue();
		if (!next.HasValue) return;

		_activePiece = CreatePiece(next.Value, GetActivePosition());
		RefreshPreviews();
	}

	private void RefreshPreviews()
	{
		_preview1?.QueueFree();
		_preview2?.QueueFree();
		_preview1 = null;
		_preview2 = null;

		var upcoming = _queue.Peek(2);
		var viewport = GetViewportRect().Size;
		float y = viewport.Y - 150;

		if (upcoming.Length > 0)
			_preview1 = CreatePreview(upcoming[0], new Vector2(viewport.X - 160, y));

		if (upcoming.Length > 1)
		{
			_preview2 = CreatePreview(upcoming[1], new Vector2(viewport.X - 60, y));
			_preview2.Modulate = new Color(1, 1, 1, 0.5f);
		}
	}

	// --- Piece creation ---

	private GlassPiece CreatePiece(PieceData data, Vector2 position)
	{
		var piece = _pieceScene.Instantiate<GlassPiece>();
		AddChild(piece);
		piece.SetShape(data.Vertices);
		piece.SetColor(data.Color);
		piece.GlobalPosition = position;
		piece.SetInteractive(true);

		piece.DragStarted += () => OnDragStarted(piece);
		piece.PieceDropped += () => OnPieceDropped(piece);

		_allPieces.Add(piece);
		return piece;
	}

	private GlassPiece CreatePreview(PieceData data, Vector2 position)
	{
		var piece = _pieceScene.Instantiate<GlassPiece>();
		AddChild(piece);
		piece.SetShape(data.Vertices);
		piece.SetColor(data.Color);
		piece.GlobalPosition = position;
		piece.Scale = new Vector2(PreviewScale, PreviewScale);
		piece.SetInteractive(false);
		return piece;
	}

	// --- Piece events ---

	private void OnDragStarted(GlassPiece dragging)
	{
		foreach (var piece in _allPieces)
		{
			if (piece != dragging)
				piece.CancelDrag();
		}

		if (_isLocked && dragging != _activePiece)
		{
			var group = FindConnectedGroup(dragging);
			if (group.Count > 0)
				dragging.SetDragGroup(group);
		}
	}

	private void OnPieceDropped(GlassPiece dropped)
	{
		// Record undo for repositioned pieces only (not first-time queue placements)
		if (dropped != _activePiece)
			RecordUndo(dropped);

		TrySnap(dropped);

		if (dropped == _activePiece)
		{
			_activePiece = null;
			AdvanceQueue();
		}
	}

	// --- Undo ---

	private void RecordUndo(GlassPiece piece)
	{
		_undoHistory.Add(new UndoEntry(piece, piece.DragStartPosition, piece.DragStartRotation));
		if (_undoHistory.Count > MaxUndoSteps)
			_undoHistory.RemoveAt(0);
	}

	private void Undo()
	{
		if (_undoHistory.Count == 0) return;

		int last = _undoHistory.Count - 1;
		var entry = _undoHistory[last];
		_undoHistory.RemoveAt(last);

		entry.Piece.GlobalPosition = entry.Position;
		entry.Piece.Rotation = entry.Rotation;
	}

	// --- Snapping ---

	private void TrySnap(GlassPiece piece)
	{
		var group = FindConnectedGroup(piece);
		var groupSet = new HashSet<GlassPiece>(group) { piece };

		var neighborVerts = _allPieces
			.Where(p => !groupSet.Contains(p) && p.IsPlaced)
			.Select(p => p.GetGlobalVertices())
			.ToArray();

		if (neighborVerts.Length == 0) return;

		var snap = EdgeSnap.FindBestSnap(
			piece.GetGlobalVertices(), piece.GlobalPosition,
			neighborVerts, SnapDistance);

		if (snap.HasValue)
		{
			piece.AnimateSnap(snap.Value);
			foreach (var member in group)
				member.AnimateSnap(new SnapResult(0f, snap.Value.Translation));
		}
	}

	// --- Lock mode ---

	private void ToggleLock()
	{
		_isLocked = !_isLocked;
		_lockButton.Text = _isLocked ? "Unlock" : "Lock";
	}

	// --- Connectivity ---

	private List<GlassPiece> FindConnectedGroup(GlassPiece start)
	{
		var group = new List<GlassPiece>();
		var visited = new HashSet<GlassPiece> { start };
		var frontier = new Queue<GlassPiece>();
		frontier.Enqueue(start);

		while (frontier.Count > 0)
		{
			var current = frontier.Dequeue();
			var currentVerts = current.GetGlobalVertices();

			foreach (var candidate in _allPieces)
			{
				if (visited.Contains(candidate) || !candidate.IsPlaced)
					continue;

				if (ArePiecesConnected(currentVerts, candidate.GetGlobalVertices()))
				{
					visited.Add(candidate);
					frontier.Enqueue(candidate);
					group.Add(candidate);
				}
			}
		}

		return group;
	}

	private bool ArePiecesConnected(Vector2[] vertsA, Vector2[] vertsB)
	{
		float threshold = ConnectionThreshold;
		float thresholdSq = threshold * threshold;

		foreach (var a in vertsA)
		{
			foreach (var b in vertsB)
			{
				if (a.DistanceSquaredTo(b) < thresholdSq)
					return true;
			}
		}

		foreach (var a in vertsA)
		{
			for (int i = 0; i < vertsB.Length; i++)
			{
				if (a.DistanceTo(Geometry.ClosestPointOnSegment(a, vertsB[i], vertsB[(i + 1) % vertsB.Length])) < threshold)
					return true;
			}
		}

		foreach (var b in vertsB)
		{
			for (int i = 0; i < vertsA.Length; i++)
			{
				if (b.DistanceTo(Geometry.ClosestPointOnSegment(b, vertsA[i], vertsA[(i + 1) % vertsA.Length])) < threshold)
					return true;
			}
		}

		return false;
	}

	// --- Helpers ---

	private Vector2 GetActivePosition()
	{
		var viewport = GetViewportRect().Size;
		return new Vector2(viewport.X / 2, viewport.Y - 150);
	}
}
