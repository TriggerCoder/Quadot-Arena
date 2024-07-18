using Godot;
using System;
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
	public int[] Ammo = new int[8] { 100, 0, 0, 0, 0, 0, 0, 0 }; //bullets, shells, grenades, rockets, lightning, slugs, cells, bfgammo
	public bool[] Weapon = new bool[9] { true, true, false, false, false, false, false, false, false }; //gauntlet, machinegun, shotgun, grenade launcher, rocket launcher, lightning gun, railgun, plasma gun, bfg10k
	public int[] MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 300 };

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


	//Const Ammo Nums
	public const int bulletsAmmo = 0;
	public const int shellsAmmo = 1;
	public const int grenadesAmmo = 2;
	public const int rocketsAmmo = 3;
	public const int lightningAmmo = 4;
	public const int slugAmmo = 5;
	public const int cellsAmmo = 6;
	public const int bfgAmmo = 7;

	//PowerUps
	public bool godMode = false;
	public bool quadDamage = false;
	public bool haste = false;

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
		var Childrens = GameManager.GetAllChildrens(WeaponHand);
		foreach (var child in Childrens)
		{
			if (child is MeshInstance3D mesh)
				mesh.Layers = uiLayer;
		}

		if (playerThing.avatar == null)
			return;

		if (playerCamera.currentThirdPerson)
			playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask);
		else
			playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask & ~((uint)(playerControls.playerInfo.viewLayer)));
	}

	public void Reset()
	{
		Ammo = new int[8] { 100, 0, 0, 0, 0, 0, 0, 0 };
		Weapon = new bool[9] { true, true, false, false, false, false, false, false, false };
		MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 300 };

		godMode = false;
		quadDamage = false;
		haste = false;

		playerPostProcessing.ResetEffects();

		playerThing.currentWaterSurface = null;
		playerThing.underWater = false;
		playerThing.waterLever = 0;
		playerThing.inLava = false;
		playerThing.hitpoints = 100;
		playerThing.armor = 0;

		playerThing.painTime = 0;
		playerThing.quadTime = 0;
		playerThing.hasteTime = 0;
		playerThing.invisTime = 0;
		playerThing.regenTime = 0;
		playerThing.enviroSuitTime = 0;
		playerThing.flightTime = 0;
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
}
