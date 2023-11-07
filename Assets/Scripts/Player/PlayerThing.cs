using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class PlayerThing : CharacterBody3D, Damageable
{
	[Export]
	public PlayerInfo playerInfo;
	[Export]
	public PlayerControls playerControls;
	[Export]
	public MultiAudioStream audioStream;

	public string modelName = "major";
	public string skinName = "daemia";

	[Export]
	public Node3D player;
	public PlayerModel avatar;

	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }
	public BloodType BloodColor { get { return BloodType.Red; } }

	public int hitpoints = 100;
	public int armor = 0;
	public float painTime = 0f;
	public float lookTime = .5f;
	public bool finished = false;
	public bool radsuit = false;
	public bool invul = false;
	public bool ready = false;
	private enum LookType
	{
		Left = 0,
		Center = 1,
		Right = 2
	}
	private LookType whereToLook = LookType.Center;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (!ready)
			InitPlayer();
	}
	public void InitPlayer()
	{
		avatar = new PlayerModel();
		player.AddChild(avatar);
		avatar.LoadPlayer(modelName, skinName, (GameManager.AllPlayerViewMask & ~((uint)(playerInfo.viewLayer))), playerControls);
/*
		gameObject.layer = playerInfo.playerLayer;
		Vector3 destination = SpawnerManager.FindSpawnLocation();
		TeleporterThing.TelefragEverything(destination, gameObject);
		transform.position = destination;
		playerControls.teleportDest = destination;

		playerControls.capsuleCollider.enabled = true;
		playerControls.controller.enabled = true;
		playerInfo.playerHUD.pickupFlashTime = 0f;
		playerInfo.playerHUD.painFlashTime = 0f;

		int playerLayer = ((1 << GameManager.Player1Layer) |
							(1 << GameManager.Player2Layer) |
							(1 << GameManager.Player3Layer) |
							(1 << GameManager.Player4Layer)) & ~(1 << (playerInfo.playerLayer));

		playerCamera.SkyholeCamera.cullingMask = (((1 << (GameManager.DefaultLayer)) |
													(1 << (GameManager.DebrisLayer)) |
													(1 << (GameManager.ThingsLayer)) |
													(1 << (GameManager.RagdollLayer)) |
													(1 << (GameManager.CombinesMapMeshesLayer)) |
													(1 << (playerInfo.playerLayer - 5)) |
													playerLayer));

		if (playerControls.playerWeapon == null)
			playerControls.SwapToBestWeapon();

		playerInfo.playerHUD.HUDUpdateHealthNum();
		playerInfo.playerHUD.HUDUpdateArmorNum();

		playerCamera.ChangeThirdPersonCamera(false);

		playerControls.enabled = true;
*/		ready = true;
	}
	public void PlayModelSound(string soundName)
	{
		soundName = "player/" + modelName + "/" + soundName;
		audioStream.Stream = SoundManager.LoadSound(soundName);
		audioStream.Play();
	}
	public void Impulse(Vector3 direction, float force)
	{
		float length = force / 80;

		//Gravity will be the only force down
		Vector3 impulse = direction * length;
		float magnitude = impulse.Length();
		impulse.Y = 0;
		impulse = impulse.Normalized() * magnitude;

		playerControls.impulseVector += impulse;
	}
	public void Damage(int amount, DamageType damageType = DamageType.Generic, Node3D attacker = null)
	{
		if (Dead)
			return;
	}
}
