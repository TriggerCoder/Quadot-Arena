using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
public partial class GameManager : Node
{
	[Export]
	WorldEnvironment worldEnvironment;
	[Export]
	public Node3D Sun;
	[Export]
	public string autoloadMap = "q3dm1";
	[Export]
	public int tessellations = 5;
	[Export]
	public float colorLightning = 1f;
	[Export]
	public float mixBrightness = 0.25f;             // Range from 0 to 1, .25f Is the nicest
	[Export]
	public float shadowIntensity = 1f;
	[Export]
	public PlayerViewPort playerViewPort;

	public static GameManager Instance;

	public Color ambientLightColor;
	// Quake3 also uses Doom and Wolf3d scaling down
	public const float sizeDividor = 1f / 32f;
	public const float modelScale = 1f / 64f;

	//Physic Layers
	public const short DefaultLayer = 0;
	public const short NoCollisionLayer = 1;

	public const short ColliderLayer = 2;
	public const short InvisibleBlockerLayer = 3;
	public const short WalkTriggerLayer = 4;

	public const short ThingsLayer = 5;
	public const short DamageablesLayer = 6;
	public const short Player1Layer = 7;
	public const short Player2Layer = 8;
	public const short Player3Layer = 9;
	public const short Player4Layer = 10;
	public const short Player5Layer = 11;
	public const short Player6Layer = 12;
	public const short Player7Layer = 13;
	public const short Player8Layer = 14;
	public const short RagdollLayer = 15;
	public const short FogLayer = 16;

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
	public const short PlayerNormalDepthLayer = 16;
	public const short NotVisibleLayer = 20;

	//Physic Masks
	public const uint TakeDamageMask = ((1 << DamageablesLayer) | 
										(1 << Player1Layer) | 
										(1 << Player2Layer) | 
										(1 << Player3Layer) | 
										(1 << Player4Layer) | 
										(1 << Player5Layer) | 
										(1 << Player6Layer) | 
										(1 << Player7Layer) | 
										(1 << Player8Layer));

	public const uint NoHitMask = ((1 << NoCollisionLayer) | 
									(1 << InvisibleBlockerLayer) | 
									(1 << WalkTriggerLayer) | 
									(1 << ThingsLayer));

	//Rendering Masks
	public const int InvisibleMask = 0;
	public const uint AllPlayerViewMask = ((1 << Player1ViewLayer) | (1 << Player2ViewLayer) | (1 << Player3ViewLayer) | (1 << Player4ViewLayer) | (1 << Player5ViewLayer) | (1 << Player6ViewLayer) | (1 << Player7ViewLayer) | (1 << Player8ViewLayer));

	//SplitScreen Players
	public const int MaxLocalPlayers = 8;

	public bool paused = true;
	public static bool Paused { get { return Instance.paused; } }

	private float timeMs = 0.0f;
	public static float CurrentTimeMsec { get { return Instance.timeMs; } }
	public float gravity = 25f;					//default 800 * sizeDividor
	public float friction = 6;
	public float waterFriction = 6;
	public float waterDeadFall = 4.5f;
	public float terminalVelocity = 100f;
	public float barrierVelocity = 1024f;
	public float playerHeight = 1.2f;

	public float PlayerDamageReceive = 1f;
	public int PlayerAmmoReceive = 1;
	private Godot.Environment environment;

	public int skipFrames = 5;
	public Node3D TemporaryObjectsHolder;
	[Export]
	public PackedScene playerPrefab;
	[Export]
	public MusicType musicType = MusicType.None;
	[Export]
	public GameType gameType = GameType.FreeForAll;

	public List<PlayerThing> Players = new List<PlayerThing>();
	public enum FuncState
	{
		None,
		Ready,
		Start,
		End
	}
	public enum PrintType
	{
		Log,
		Info,
		Warning,
		Error
	}
	public enum MusicType
	{
		None,
		Static,
		Dynamic
	}
	public enum GameType
	{
		SinglePlayer,
		FreeForAll,
		Tournament,
		TeamDeathmatch,
		CaptureTheFlag,
		OneFlagCTF,
		Overload,
		Harvester
	}
	public static class ControllerType
	{
		public const int MouseKeyboard = 0;
		public const int Joy_0 = 1;
		public const int Joy_1 = 2;
		public const int Joy_2 = 3;
		public const int Joy_3 = 4;
		public const int Joy_4 = 5;
		public const int Joy_5 = 6;
		public const int Joy_6 = 7;
		public const int Joy_7 = 8;
	}

	private FuncState currentState = FuncState.None;
	
	private static PrintType printType = PrintType.Log;
	private static int printLine = 0;
	public override void _Ready()
	{
		GD.Randomize();
		//Used in order to parse float with "." as decimal separator
		CultureInfo CurrentCultureInfo = new CultureInfo("en", false);
		CurrentCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
		CurrentCultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
		CultureInfo.DefaultThreadCurrentCulture = CurrentCultureInfo;

		Input.MouseMode = Input.MouseModeEnum.Captured;
		environment = worldEnvironment.Environment;
		ambientLightColor = environment.AmbientLightColor;
		Instance = this;

		TemporaryObjectsHolder = new Node3D();
		TemporaryObjectsHolder.Name = "TemporaryObjectsHolder";
		AddChild(TemporaryObjectsHolder);
		PakManager.LoadPK3Files();
		QShaderManager.ProcessShaders();
		MaterialManager.LoadFXShaders();
		MaterialManager.SetAmbient();
		if (MapLoader.Load(autoloadMap))
		{
			ClusterPVSManager.Instance.ResetClusterList();
			MapLoader.GenerateMapCollider();
			MapLoader.GenerateMapFog();
			MapLoader.GenerateSurfaces();
			MapLoader.SetLightVolData();
			ThingsManager.AddThingsToMap();
		}
		currentState = FuncState.Ready;
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
				if (Input.IsActionJustPressed("Action_Fire_0"))
				{
					paused = false;
					Input.MouseMode = Input.MouseModeEnum.Captured;
				}
			}
		}
	}
	public override void _Process(double delta)
	{
		if (!paused)
		{
			timeMs += (float)delta;
			RenderingServer.GlobalShaderParameterSet("MsTime", CurrentTimeMsec);
		}

		switch(currentState)
		{
			default:
			break;
			case FuncState.Ready:
				//skip frames are used to easen up deltaTime after loading
				if (skipFrames > 0)
				{
					skipFrames--;
					if (skipFrames == 0)
					{
						if (Players.Count == 0)
						{
							PlayerThing player = (PlayerThing)playerPrefab.Instantiate();
							player.Name = "Player 0";
							AddChild(player);
							player.playerInfo.SetPlayer(Players.Count);
							player.playerControls.Init(Players.Count);
							player.InitPlayer();
							Players.Add(player);
						}
						paused = false;
						currentState = FuncState.Start;
					}
				}
				break;
		}
	}

	public void SetViewPortToCamera(Camera3D camera)
	{
		var CamRID = camera.GetCameraRid();
		var viewPortRID = playerViewPort.viewPort.GetViewportRid();
		RenderingServer.ViewportAttachCamera(viewPortRID, CamRID);
	}

	public static List<Node> GetAllChildrens(Node parent)
	{
		List<Node> list = new List<Node>();

		var Childrens = parent.GetChildren();
		foreach (var child in Childrens)
		{
			if (child.IsQueuedForDeletion())
				continue;

			list.Add(child);
			list.AddRange(GetAllChildrens(child));
		}
		return list;
	}

	public static List<MeshInstance3D> CreateFXMeshInstance3D(Node parent)
	{
		var Childrens = GetAllChildrens(parent);
		List<MeshInstance3D> fxMeshes = new List<MeshInstance3D>();
		foreach (var child in Childrens)
		{
			if (child is MeshInstance3D mesh)
			{
				MeshInstance3D fxMesh = new MeshInstance3D();
				fxMesh.Mesh = mesh.Mesh;
				fxMesh.Layers = mesh.Layers;
				fxMesh.Visible = false;
				mesh.AddChild(fxMesh);
				fxMeshes.Add(fxMesh);
			}
		}
		return fxMeshes;
	}
	public static void ChangeQuadFx(List<MeshInstance3D> fxMeshes, bool enable, bool viewModel = false)
	{
		for (int i = 0; i < fxMeshes.Count; i++)
		{
			MeshInstance3D mesh = fxMeshes[i];
			if (enable)
			{
				if (viewModel)
					mesh.SetSurfaceOverrideMaterial(0, MaterialManager.quadWeaponFxMaterial);
				else
					mesh.SetSurfaceOverrideMaterial(0, MaterialManager.quadFxMaterial);
				mesh.Visible = true;
			}
			else
				mesh.Visible = false;
		}
	}
	public static void Print(string Message, PrintType type = PrintType.Log)
	{
		if (type >= printType)
		{
			GD.Print(printLine + ": " + Message);
			printLine++;
		}
	}
}
