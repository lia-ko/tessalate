using Godot;

namespace Tessellate;

/// <summary>
/// Hardcoded irregular polygon shapes for the prototype.
/// Will be replaced by procedural generation later.
/// </summary>
public static class PieceShapes
{
	public static readonly Vector2[][] All =
	{
		new Vector2[] { new(0, -50), new(45, -15), new(30, 40), new(-30, 40), new(-45, -15) },
		new Vector2[] { new(-20, -55), new(25, -40), new(35, 45), new(-15, 55) },
		new Vector2[] { new(-10, -45), new(35, -30), new(50, 10), new(20, 45), new(-30, 40), new(-45, -5) },
		new Vector2[] { new(0, -40), new(40, 10), new(15, 45), new(-35, 20) },
		new Vector2[] { new(-15, -60), new(20, -45), new(35, 15), new(5, 55), new(-30, 20) },
		new Vector2[] { new(-25, -45), new(15, -50), new(40, -10), new(25, 35), new(-20, 40) },
		new Vector2[] { new(0, -55), new(35, -20), new(30, 30), new(-10, 50), new(-35, 10) },
		new Vector2[] { new(-30, -30), new(10, -45), new(40, 0), new(20, 40), new(-25, 35) },
		new Vector2[] { new(-15, -50), new(30, -35), new(45, 20), new(10, 50), new(-35, 25), new(-40, -15) },
		new Vector2[] { new(0, -45), new(50, -10), new(35, 40), new(-30, 35) },
		new Vector2[] { new(-20, -40), new(20, -45), new(45, 5), new(15, 45), new(-35, 30) },
		new Vector2[] { new(-10, -55), new(25, -35), new(40, 15), new(5, 50), new(-30, 25) },
		new Vector2[] { new(0, -40), new(35, -25), new(45, 20), new(20, 50), new(-25, 40), new(-40, 0) },
		new Vector2[] { new(-25, -35), new(20, -40), new(40, 10), new(10, 45), new(-30, 20) },
		new Vector2[] { new(-15, -50), new(30, -30), new(25, 35), new(-20, 45) },
	};

	public static readonly Color[] CathedralPalette =
	{
		new(0.15f, 0.25f, 0.65f, 0.85f), // deep blue
		new(0.65f, 0.12f, 0.15f, 0.85f), // ruby red
		new(0.18f, 0.55f, 0.30f, 0.85f), // emerald green
		new(0.75f, 0.60f, 0.10f, 0.85f), // amber gold
		new(0.50f, 0.18f, 0.55f, 0.85f), // amethyst purple
	};
}
