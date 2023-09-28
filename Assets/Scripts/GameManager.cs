using Godot;
using System;

public partial class GameManager : Node
{
	[Export]
	public string autoloadMap = "q3dm1";
	[Export]
	public int tessellations = 5;
	[Export]
	public float colorLightning = 1f;

	public static GameManager Instance;
	// Quake3 also uses Doom and Wolf3d scaling down
	public const float sizeDividor = 1f / 32f;
	public const float modelScale = 1f / 64f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		PakManager.LoadPK3Files();
		if (MapLoader.Load(autoloadMap))
		{
			MapLoader.GenerateMapCollider();
			MapLoader.GenerateSurfaces();
		}
		return;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
