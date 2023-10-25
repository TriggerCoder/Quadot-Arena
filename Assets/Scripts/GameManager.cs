using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

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
	public float mixBrightness = 0.25f;				// Range from 0 to 1, .25f Is the nicest
	[Export]
	public CharacterBody3D Player;

	public static GameManager Instance;
	// Quake3 also uses Doom and Wolf3d scaling down
	public const float sizeDividor = 1f / 32f;
	public const float modelScale = 1f / 64f;

	//Physic Layers
	public const short DefaultLayer = 0;
	public const short FXLayer = 1;

	public const short ColliderLayer = 2;
	public const short InvisibleBlockerLayer = 3;
	public const short WalkTriggerLayer = 4;

	public const short ThingsLayer = 5;
	public const short DamageablesLayer = 6;
	public const short RagdollLayer = 7;

	//3DRender Layer
	public const short Player1ViewLayer = 0;
	public const short Player2ViewLayer = 1;
	public const short Player3ViewLayer = 2;
	public const short Player4ViewLayer = 3;
	public const short Player5ViewLayer = 4;
	public const short Player6ViewLayer = 5;
	public const short Player7ViewLayer = 6;
	public const short Player8ViewLayer = 7;

	public const short Player1UIViewLayer = 8;
	public const short Player2UIViewLayer = 9;
	public const short Player3UIViewLayer = 10;
	public const short Player4UIViewLayer = 11;
	public const short Player5UIViewLayer = 12;
	public const short Player6UIViewLayer = 13;
	public const short Player7UIViewLayer = 14;
	public const short Player8UIViewLayer = 15;

	//Physic Masks
	public const int TakeDamageMask = (1 << DamageablesLayer);
	public const int NoHitMask = ((1 << FXLayer) | (1 << InvisibleBlockerLayer) | (1 << WalkTriggerLayer) | (1 << ThingsLayer));

	//Rendering Masks
	public const int InvisibleMask = 0;
	public const int AllPlayerViewMask = ((1 << Player1ViewLayer) | (1 << Player2ViewLayer) | (1 << Player3ViewLayer) | (1 << Player4ViewLayer) | (1 << Player5ViewLayer) | (1 << Player6ViewLayer) | (1 << Player7ViewLayer) | (1 << Player8ViewLayer));

	public bool paused = true;
	public static bool Paused { get { return Instance.paused; } }

	public static Color ambientLight { get { return Instance.ambientLightColor; } }

	public float gravity = 25f;
	public float friction = 6;
	public float terminalVelocity = 100f;
	public float barrierVelocity = 1024f;

	private Godot.Environment environment;
	private Color ambientLightColor;

	public bool ready = false;
	public int skipFrames = 5;
	public override void _Ready()
	{
		//Used in order to parse float with "." as decimal separator
		CultureInfo CurrentCultureInfo = new CultureInfo("en", false);
		CurrentCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
		CurrentCultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
		CultureInfo.DefaultThreadCurrentCulture = CurrentCultureInfo;

		Input.MouseMode = Input.MouseModeEnum.Captured;
		environment = worldEnvironment.Environment;
		ambientLightColor = environment.AmbientLightColor;
		Instance = this;
		PakManager.LoadPK3Files();
		if (MapLoader.Load(autoloadMap))
		{
			ClusterPVSManager.Instance.ResetClusterList();
			MapLoader.GenerateMapCollider();
			MapLoader.GenerateSurfaces();
		}
		ready = true;
		return;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey)
		{
			if (Input.IsActionJustPressed("Escape"))
			{
				if (!paused)
				{
					paused = true;
					Input.MouseMode = Input.MouseModeEnum.Visible;
				}
				else
				{
					paused = false;
					Input.MouseMode = Input.MouseModeEnum.Captured;
				}
			}
		}
		else if (paused)
		{
			if (@event is InputEventMouseButton)
			{
				if (Input.IsActionJustPressed("Action_Fire"))
				{
					paused = false;
					Input.MouseMode = Input.MouseModeEnum.Captured;
				}
			}
		}
	}
	public override void _Process(double delta)
	{
		//skip frames are used to easen up deltaTime after loading
		if (ready)
		{
			if (skipFrames > 0)
			{
				skipFrames--;
				if (skipFrames == 0)
					paused = false;
			}
		}
	}
}
