using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Tessellate;

/// <summary>
/// Manages one-at-a-time piece delivery with a bag system
/// that guarantees color variety across the active palette.
/// </summary>
public class PieceQueue
{
	private readonly Queue<PieceData> _queue = new();

	public int Remaining => _queue.Count;

	public PieceData? Dequeue() => _queue.Count > 0 ? _queue.Dequeue() : null;

	public PieceData[] Peek(int count) => _queue.Take(count).ToArray();

	public static PieceQueue Create(Vector2[][] shapes, Color[] palette)
	{
		var queue = new PieceQueue();
		var colorBag = new List<Color>();
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		foreach (var shape in shapes)
		{
			if (colorBag.Count == 0)
			{
				colorBag.AddRange(palette);
				Shuffle(colorBag, rng);
			}

			var color = colorBag[^1];
			colorBag.RemoveAt(colorBag.Count - 1);
			queue._queue.Enqueue(new PieceData(shape, color));
		}

		return queue;
	}

	private static void Shuffle<T>(List<T> list, RandomNumberGenerator rng)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.RandiRange(0, i);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}
