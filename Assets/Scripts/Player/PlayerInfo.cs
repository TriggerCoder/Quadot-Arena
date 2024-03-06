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
//	public Canvas UICanvas;
	[Export]
	public Node3D WeaponHand;

	//Weapons and Ammo
	public int[] Ammo = new int[8] { 1000, 0, 0, 50, 0, 0, 0, 0 }; //bullets, shells, grenades, rockets, lightning, slugs, cells, bfgammo
	public bool[] Weapon = new bool[9] { false, true, false, false, true, false, false, false, false }; //gauntlet, machinegun, shotgun, grenade launcher, rocket launcher, lightning gun, railgun, plasma gun, bfg10k
	public int[] MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

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
		playerCamera.playerPostProcessing.InitPost();
	}

	public void Reset()
	{
		Ammo = new int[8] { 100, 0, 0, 50, 0, 0, 0, 0 };
		Weapon = new bool[9] { false, true, false, false, true, false, false, false, false };
		MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

		godMode = false;

//		playerHUD.pickupFlashTime = 0f;
//		playerHUD.painFlashTime = 0f;
		playerThing.waterLever = 0;
		playerThing.hitpoints = 100;
		playerThing.armor = 0;

		playerControls.impulseVector = Vector3.Zero;
		playerControls.CurrentWeapon = -1;
		playerControls.SwapWeapon = -1;
//		playerControls.SwapToBestWeapon();

//		playerHUD.HUDUpdateAmmoNum();
//		playerHUD.HUDUpdateHealthNum();
//		playerHUD.HUDUpdateArmorNum();
	}
	public override void _Process(double delta)
	{
		ClusterPVSManager.CheckPVS(viewLayer, GlobalPosition);
		//if camera is thirdperson, need to make sure at all that it can see 
		if (playerCamera.currentThirdPerson)
			ClusterPVSManager.CheckPVS(viewLayer, playerCamera.CurrentCamera.GlobalPosition);
	}
}
