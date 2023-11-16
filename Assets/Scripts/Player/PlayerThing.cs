using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class PlayerThing : CharacterBody3D, Damageable
{
	[Export]
	public CollisionShape3D Feets;
	[Export]
	public PlayerInfo playerInfo;
	[Export]
	public PlayerControls playerControls;
	[Export]
	public Node3D weaponCollider;
	[Export]
	public MultiAudioStream audioStream;

	public string modelName = "crash";
	public string skinName = "cc-crash_blue";

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
		playerControls.feetRay = new SeparationRayShape3D();
		playerControls.feetRay.Length = .992f;
		Feets.Shape = playerControls.feetRay;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

	}
	public void InitPlayer()
	{
		CollisionLayer = playerInfo.playerLayer;
		avatar = new PlayerModel();
		player.AddChild(avatar);
		avatar.LoadPlayer(modelName, skinName, (GameManager.AllPlayerViewMask & ~((uint)(playerInfo.viewLayer))), playerControls);

		Vector3 destination = SpawnerManager.FindSpawnLocation();
		TeleporterThing.TelefragEverything(destination, this);
		GlobalPosition = destination;
		playerControls.teleportDest = destination;

		if (playerControls.playerWeapon == null)
			playerControls.SwapToBestWeapon();

//		playerInfo.playerHUD.HUDUpdateHealthNum();
//		playerInfo.playerHUD.HUDUpdateArmorNum();

		playerControls.playerCamera.ChangeThirdPersonCamera(false);
		ready = true;
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

		if ((damageType != DamageType.Environment) || (damageType != DamageType.Crusher))
			amount = Mathf.RoundToInt(amount * GameManager.Instance.PlayerDamageReceive);

		if (invul)
			if ((damageType != DamageType.Crusher) && (damageType != DamageType.Telefrag))
				amount = 0;

		if (amount <= 0)
		{
			if ((damageType == DamageType.Crusher) || (damageType == DamageType.Telefrag))
				amount = 1000;
			else
				return;
		}

		if (armor > 0)
		{
			int subjectiveToMega = Mathf.Min(Mathf.Max(armor - 100, 0), amount);
			int subjectiveToNormal = Mathf.Min(armor, amount - subjectiveToMega);
			int absorbed = Mathf.Max(subjectiveToMega / 2 + subjectiveToNormal / 3, 1);

			armor -= absorbed;
			amount -= absorbed;
		}

		hitpoints -= amount;

		//Cap Negative Damage
		if (hitpoints < -99)
			hitpoints = -99;

//		playerInfo.playerHUD.HUDUpdateHealthNum();
//		playerInfo.playerHUD.HUDUpdateArmorNum();

/*
		if (amount > 60)
			playerInfo.playerHUD.painFlashTime = 2.5f;
		else if (amount > 40)
			playerInfo.playerHUD.painFlashTime = 2f;
		else if (amount > 20)
			playerInfo.playerHUD.painFlashTime = 1.5f;
		else
			playerInfo.playerHUD.painFlashTime = 1f;
*/
		if (hitpoints <= 0)
		{
//			playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Dead);

			if (playerControls.playerWeapon != null)
				playerControls.playerWeapon.putAway = true;

			playerControls.playerCamera.ChangeThirdPersonCamera(true);
			PlayModelSound("death" + GD.RandRange(1, 3));
			avatar.Die();
			playerControls.feetRay.Length = 1.2f;
			ready = false;
//			GameManager.Instance.AddDeathCount();
		}
		else if (painTime <= 0f)
		{
			if (hitpoints > 75)
				PlayModelSound("pain100_1");
			else if (hitpoints > 50)
				PlayModelSound("pain75_1");
			else if (hitpoints > 25)
				PlayModelSound("pain50_1");
			else
				PlayModelSound("pain25_1");

			painTime = 1f;
		}
	}
	public void JumpPadDest(Vector3 destination)
	{
		Vector3 position = GlobalPosition;
		Vector3 horizontalVelocity = destination - position;
		float height = destination.Y - position.Y;

		if (height <= 0)
		{
			playerControls.jumpPadVel = Vector3.Zero;
			return;
		}

		float time = Mathf.Sqrt((2 * height) / GameManager.Instance.gravity);
		float verticalVelocity = time * GameManager.Instance.gravity;

		horizontalVelocity.Y = 0;
		float forward = horizontalVelocity.Length() / time;
		horizontalVelocity = horizontalVelocity.Normalized() * forward;
		playerControls.jumpPadVel = horizontalVelocity;
		playerControls.jumpPadVel.Y = verticalVelocity;
		playerControls.playerVelocity = Vector3.Zero;
		playerControls.AnimateLegsOnJump();
	}
}
