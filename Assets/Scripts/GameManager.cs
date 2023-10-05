using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	[Export]
	WorldEnvironment worldEnvironment;
	[Export]
	public string autoloadMap = "q3dm1";
	[Export]
	public int tessellations = 5;
	[Export]
	public float colorLightning = 1f;
	[Export]
	public CharacterBody3D Player;

	public static GameManager Instance;
	// Quake3 also uses Doom and Wolf3d scaling down
	public const float sizeDividor = 1f / 32f;
	public const float modelScale = 1f / 64f;

	//Physic Layers
	public const short DefaultLayer = 0;                //Same on 3DRender Layer
	public const short FXLayer = 1;                     //Same on 3DRender Layer

	public const short ColliderLayer = 2;
	public const short InvisibleBlockerLayer = 3;
	public const short WalkTriggerLayer = 4;

	public const short ThingsLayer = 5;
	public const short DamageablesLayer = 6;
	public const short RagdollLayer = 7;

	//3DRender Layer
	public const short MapMeshesLayer = 2;
	public const short Player1ViewLayer = 3;
	public const short Player2ViewLayer = 4;
	public const short Player3ViewLayer = 5;
	public const short Player4ViewLayer = 6;
	public const short Player5ViewLayer = 7;
	public const short Player6ViewLayer = 8;
	public const short Player7ViewLayer = 9;
	public const short Player8ViewLayer = 10;

	public const short Player1Layer = 11;
	public const short Player2Layer = 12;
	public const short Player3Layer = 13;
	public const short Player4Layer = 14;
	public const short Player5Layer = 15;
	public const short Player6Layer = 16;
	public const short Player7Layer = 17;
	public const short Player8Layer = 18;

	public const int TakeDamageMask = (1 << DamageablesLayer);

	public const int NoHit = ((1 << FXLayer) | (1 << InvisibleBlockerLayer) | (1 << WalkTriggerLayer) | (1 << ThingsLayer));

	public bool paused = true;
	public static bool Paused { get { return Instance.paused; } }

	public static Color ambientLight { get { return Instance.ambientLightColor; } }

	public float gravity = 25f;
	public float friction = 6;
	public float terminalVelocity = 100f;
	public float barrierVelocity = 1024f;

	private Godot.Environment environment;
	private Color ambientLightColor;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		environment = worldEnvironment.Environment;
		ambientLightColor = environment.AmbientLightColor;
		Instance = this;
		PakManager.LoadPK3Files();
		if (MapLoader.Load(autoloadMap))
		{
			MapLoader.GenerateMapCollider();
			MapLoader.GenerateSurfaces();
		}
		return;
	}

	public override void _Input(InputEvent @event)
	{

	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
