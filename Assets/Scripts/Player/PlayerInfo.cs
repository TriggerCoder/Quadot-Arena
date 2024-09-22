using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
public partial class PlayerInfo : Node3D
{
	[Export]
	public PlayerControls playerControls;
	[Export]
	public PlayerCamera playerCamera;
	[Export]
	public MultiAudioStream audioStream;
	[Export]
	public PlayerThing playerThing;
	[Export]
	public PlayerPostProcessing playerPostProcessing;
	[Export]
	public Node3D WeaponHand;

	//Weapons and Ammo
	public int[] Ammo = new int[11] { 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //bullets, shells, grenades, rockets, lightning, slugs, cells, bfgammo, nailammo, chainammo, proxammo
	public bool[] Weapon = new bool[14] { true, true, false, false, false, false, false, false, false, false, false, false, false, false }; //gauntlet, machinegun, shotgun, grenade launcher, rocket launcher, lightning gun, railgun, plasma gun, bfg10k, grapple, nailgun, chaingun, proxmine, heavymachinegun
	public int[] MaxAmmo = new int[11] { 200, 200, 200, 200, 200, 200, 200, 300, 25, 300, 200 };
	public int[] DefaultAmmo = new int[11] { 50, 10, 5, 5, 60, 10, 30, 100, 10, 100, 10 };
	//Const Weapon Nums
	public const int Gauntlet = 0;
	public const int MachineGun = 1;
	public const int Shotgun = 2;
	public const int GrenadeLauncher = 3;
	public const int RocketLauncher = 4;
	public const int LightningGun = 5;
	public const int Railgun = 6;
	public const int PlasmaGun = 7;
	public const int BFG10K = 8;
	public const int Grapple = 9;
	public const int NailGun = 10;
	public const int ChainGun = 11;
	public const int ProxLauncher = 12;
	public const int HeavyMachineGun = 13;


	//Const Ammo Nums
	public const int bulletsAmmo = 0;
	public const int shellsAmmo = 1;
	public const int grenadesAmmo = 2;
	public const int rocketsAmmo = 3;
	public const int lightningAmmo = 4;
	public const int slugAmmo = 5;
	public const int cellsAmmo = 6;
	public const int bfgAmmo = 7;
	public const int nailAmmo = 8;
	public const int chainAmmo = 9;
	public const int minesAmmo = 10;


	//PowerUps
	public bool godMode = false;
	public bool quadDamage = false;
	public bool battleSuit = false;
	public bool invis = false;
	public bool regenerating = false;
	public bool haste = false;
	public bool flight = false;

	public int MaxHealth = 100;
	public int MaxBonusHealth = 200;
	public int MaxArmor = 100;
	public int MaxBonusArmor = 200;
	[Export]
	public PackedScene[] WeaponPrefabs = new PackedScene[9];

	public uint viewLayer;
	public uint playerLayer;
	public uint uiLayer;
	public int localPlayerNum;

	public PlayerConfigData configData = new PlayerConfigData();
	public class PlayerConfigData
	{
		public string ModelName { get; set; } = "crash";										// Model Name.
		public string SkinName { get; set; } = "default";										// Skin Name.
		public int FOV { get; set; } = 90;														// View Camera FOV.
		public float[] MouseSensitivity { get; set; } = new float[2] { .5f, .5f };				// Mouse Sensitivity.
		public float[] StickSensitivity { get; set; } = new float[2] { 4f, 3f };				// Controller Stick Sensitivity.
		public bool InvertView { get; set; } = false;											// Y Axis View Invert Controls.
		public bool AutoHop { get; set; } = false;												// Allows player to just hold jump button to keep on bhopping perfectly.
		public bool BloodScreen { get; set; } = true;											// Show Visible Pain Feedback.
		public int[] CroosHair { get; set; } = new int[14] { 5, 5, 5, 5, 5, 5, 107, 5, 5, 5, 5, 5, 5, 5 };     //gauntlet, machinegun, shotgun, grenade launcher, rocket launcher, lightning gun, railgun, plasma gun, bfg10k, grapple, nailgun, chaingun, proxmine, heavymachinegun
		public int CroosHairAlpha { get; set; } = 25;											// CrossHair Alpha Value.
		public int CroosHairScale { get; set; } = 100;											// CrossHair Scale Value.
		public string ModulateColor { get; set; } = "#50a1cd";                                  // Modulate Color.
		public bool AutoSwap { get; set; } = true;												// Auto Swap if new weapon is picked
		public bool SafeSwap { get; set; } = true;												// When out of ammo always swap to safe weapon
		public int HUD2DScale { get; set; } = 100;												// HUD's Sprites Scale.
		public int HUD3DScale  { get; set; } = 100;												// HUD's Models Scale.
		public bool HUDShow { get; set; } = true;												// Show Hud.
	}

	public void SetPlayer(int playerNum)
	{
		localPlayerNum = playerNum;
		viewLayer = (uint)(1 << (GameManager.Player1ViewLayer + localPlayerNum));
		playerLayer = (uint)(1 << (GameManager.Player1Layer + localPlayerNum));
		playerThing.CollisionLayer = playerLayer;
		uiLayer = (uint)(1 << (GameManager.Player1UIViewLayer + localPlayerNum));
		playerCamera.ViewCamera.CullMask = viewLayer | uiLayer;
		playerCamera.ThirdPerson.CullMask = viewLayer;
		playerCamera.playerPostProcessing.ViewMask = viewLayer;
		playerCamera.playerPostProcessing.UIMask = uiLayer;
		playerCamera.playerPostProcessing.InitPost(this);
		for (int i = 0; i < Weapon.Length; i++)
		{
			if (Weapon[i])
				playerPostProcessing.playerHUD.AddWeapon(i);
		}
		LoadSavedConfigData();
	}

	public void UpdatePlayer(int playerNum)
	{
		localPlayerNum = playerNum;
		viewLayer = (uint)(1 << (GameManager.Player1ViewLayer + localPlayerNum));
		playerLayer = (uint)(1 << (GameManager.Player1Layer + localPlayerNum));
		playerThing.CollisionLayer = playerLayer;
		uiLayer = (uint)(1 << (GameManager.Player1UIViewLayer + localPlayerNum));
		playerCamera.ViewCamera.CullMask = viewLayer | uiLayer;
		playerCamera.ThirdPerson.CullMask = viewLayer;
		playerCamera.playerPostProcessing.ViewMask = viewLayer;
		playerCamera.playerPostProcessing.UIMask = uiLayer;
		playerCamera.playerPostProcessing.UpdateLayersPost();
		//Change UI Weapon Layer
		List<MeshInstance3D> Childrens = GameManager.GetAllChildrensByType<MeshInstance3D>(WeaponHand);
		foreach (MeshInstance3D mesh in Childrens)
			mesh.Layers = uiLayer;

		if (playerThing.avatar == null)
			return;

		if (playerCamera.currentThirdPerson)
			playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask);
		else
			playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask & ~((uint)(playerControls.playerInfo.viewLayer)));
	}

	public void Reset()
	{
		Ammo = new int[11] { 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		Weapon = new bool[14] { true, true, false, false, false, false, false, false, false, false, false, false, false, false };
		MaxAmmo = new int[11] { 200, 200, 200, 200, 200, 200, 200, 300, 200, 300, 200 };

		godMode = false;
		quadDamage = false;
		battleSuit = false;
		invis = false;
		regenerating = false;
		haste = false;
		flight = false;

		playerPostProcessing.ResetEffects();

		playerThing.currentWaterSurface = null;
		playerThing.underWater = false;
		playerThing.waterLever = 0;
		playerThing.inDamageable = WaterSurface.DamageableType.None;
		playerThing.hitpoints = 100;
		playerThing.armor = 0;

		playerThing.dropPowerUps = true;

		playerThing.painTime = 0;
		playerThing.quadTime = 0;
		playerThing.hasteTime = 0;
		playerThing.invisTime = 0;
		playerThing.regenTime = 0;
		playerThing.regenFXTime = 0;
		playerThing.enviroSuitTime = 0;
		playerThing.flightTime = 0;
		playerThing.flightSoundTime = 0;
		playerThing.lastDamageTime = 0;

		playerThing.lastAttacker = null;
		playerThing.holdableItem = PlayerThing.HoldableItem.None;

		playerControls.impulseVector = Vector3.Zero;
		playerControls.CurrentWeapon = -1;
		playerControls.SwapWeapon = -1;

		for (int i = 0; i < Weapon.Length; i++)
		{
			if (Weapon[i])
				playerPostProcessing.playerHUD.AddWeapon(i);
		}
	}

	public bool LoadSavedConfigData()
	{
		bool loaded = false;
		string configFile = Directory.GetCurrentDirectory() + "/PlayersConfigs/" + playerThing.playerName + ".cfg";
		if (File.Exists(configFile))
		{
			string jsonString = File.ReadAllText(configFile);
			try
			{
				configData = JsonSerializer.Deserialize(jsonString, SourceGenerationContext.Default.PlayerConfigData);
				loaded = true;
			}
			catch (JsonException)
			{
				configData = new PlayerConfigData();
			}
			if (loaded)
			{
				playerThing.modelName = configData.ModelName;
				playerThing.skinName = configData.SkinName;

				if ((configData.FOV < 30) || (configData.FOV > 130))
					configData.FOV = 90;
				playerCamera.ViewCamera.Fov = configData.FOV;

				if (configData.CroosHair.Length < 14)
					configData.CroosHair = new int[14] { 5, 5, 5, 5, 5, 5, 107, 5, 5, 5, 5, 5, 5, 5 };

				for (int i = 0; i < configData.CroosHair.Length; i++)
				{
					int CrossHair = configData.CroosHair[i];
					if (CrossHair > 50)
						CrossHair -= 100;

					if ((CrossHair < 0) || (CrossHair > 50))
						CrossHair = 5;
				}

				if ((configData.CroosHairAlpha < 0) || (configData.CroosHairAlpha > 100))
					configData.CroosHairAlpha = 25;
				playerPostProcessing.playerHUD.ChangeCrossHairAlpha(configData.CroosHairAlpha);

				if ((configData.CroosHairScale < 10) || (configData.CroosHairScale > 100))
					configData.CroosHairAlpha = 100;
				playerPostProcessing.playerHUD.ChangeCrossHairScale(configData.CroosHairScale);

				Color modulate;
				try
				{
					modulate = new Color(configData.ModulateColor);
				}
				catch (Exception e)
				{
					modulate = new Color("#50a1cd");
				}
				modulate.R = Mathf.Max(0.1f, modulate.R);
				modulate.G = Mathf.Max(0.1f, modulate.G);
				modulate.B = Mathf.Max(0.1f, modulate.B);
				playerThing.modulate = modulate;

				if ((configData.HUD2DScale < 10) || (configData.HUD2DScale > 100))
					configData.HUD2DScale = 100;
				playerPostProcessing.playerHUD.ChangeSpriteScale(configData.HUD2DScale);

				if ((configData.HUD3DScale < 10) || (configData.HUD3DScale > 100))
					configData.HUD3DScale = 100;
				playerPostProcessing.playerHUD.ChangeModelScale(configData.HUD3DScale);

				if (!configData.HUDShow)
					playerPostProcessing.playerHUD.UpdateLayersHud(1 << GameManager.UINotVisibleLayer);
			}
		}
		return loaded;
	}
	public void SaveConfigData()
	{
		string configFile = Directory.GetCurrentDirectory() + "/PlayersConfigs/" + playerThing.playerName + ".cfg";
		FileStream errorFile = File.Open(configFile, FileMode.Create, System.IO.FileAccess.ReadWrite);
		if (File.Exists(configFile))
		{
			errorFile.Seek(0, SeekOrigin.Begin);
			byte[] writeData = JsonSerializer.SerializeToUtf8Bytes(configData, SourceGenerationContext.Default.PlayerConfigData);
			errorFile.Write(writeData);
			errorFile.Close();
		}
	}
}
