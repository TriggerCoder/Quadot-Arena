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
	public bool[] Weapon = new bool[9] { false, true, false, false, false, false, false, false, false }; //gauntlet, machinegun, shotgun, grenade launcher, rocket launcher, lightning gun, railgun, plasma gun, bfg10k
	public int[] MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

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

	public int MaxHealth = 100;
	public int MaxBonusHealth = 200;
	public int MaxArmor = 100;
	public int MaxBonusArmor = 200;
	[Export]
	public PackedScene[] WeaponPrefabs = new PackedScene[9];

	public int viewLayer;
	public uint playerLayer;
	public uint uiLayer;
	public int localPlayerNum;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
	public void SetPlayer(int playerNum)
	{
		localPlayerNum = playerNum;
		viewLayer = (1 << (GameManager.Player1ViewLayer + localPlayerNum));
		playerLayer = (uint)(1 << (GameManager.Player1Layer + localPlayerNum));
		uiLayer = (uint)(1 << (GameManager.Player1UIViewLayer + localPlayerNum));
		playerCamera.ViewCamera.CullMask = (uint)viewLayer | uiLayer;
		playerCamera.ThirdPerson.CullMask = (uint)viewLayer;
		playerCamera.playerPostProcessing.ViewMask = (uint)viewLayer;
		playerCamera.playerPostProcessing.UIMask = uiLayer;
		playerCamera.playerPostProcessing.InitPost(this);
	}

	public void Reset()
	{
		Ammo = new int[8] { 100, 0, 0, 0, 0, 0, 0, 0 };
		Weapon = new bool[9] { false, true, false, false, false, false, false, false, false };
		MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

		godMode = false;
		quadDamage = false;

		playerPostProcessing.ResetEffects();
//		playerHUD.pickupFlashTime = 0f;
//		playerHUD.painFlashTime = 0f;
		playerThing.waterLever = 0;
		playerThing.hitpoints = 100;
		playerThing.armor = 0;

		playerControls.impulseVector = Vector3.Zero;
		playerControls.CurrentWeapon = -1;
		playerControls.SwapWeapon = -1;
//		playerControls.SwapToBestWeapon();

//		playerHUD.HUDUpdateHealthNum();
//		playerHUD.HUDUpdateArmorNum();
	}
	public override void _Process(double delta)
	{
		ClusterPVSManager.CheckPVS(viewLayer, playerCamera.CurrentCamera.GlobalPosition);
		//if camera is thirdperson, need to make sure at all that it can see 
		if (playerCamera.currentThirdPerson)
			ClusterPVSManager.CheckPVS(viewLayer, GlobalPosition);
	}
}
