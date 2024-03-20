using Godot;
using System.Collections.Generic;
using ExtensionMethods;
public partial class PlayerThing : CharacterBody3D, Damageable
{
	[Export]
	public CollisionShape3D Torso;
	[Export]
	public CollisionShape3D[] Feets;
	[Export]
	public PlayerInfo playerInfo;
	[Export]
	public PlayerControls playerControls;
	[Export]
	public MultiAudioStream audioStream;

	public InterpolatedTransform interpolatedTransform;
	public string modelName = "crash";
	public string skinName = "default";

	[Export]
	public Node3D player;
	public PlayerModel avatar;

	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }
	public BloodType BloodColor { get { return BloodType.Red; } }

	public int hitpoints = 100;
	public int armor = 0;
	public int waterLever = 0;
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
		playerControls.feetRay.Length = .8f;
		for (int i = 0; i < Feets.Length; i++)
			Feets[i].Shape = playerControls.feetRay;

		playerControls.collider = new CapsuleShape3D();
		playerControls.collider.Radius = .5f;
		playerControls.collider.Height = 1.6f;
		Torso.Shape = playerControls.collider;
	}
	public void InitPlayer()
	{
		CollisionLayer = playerInfo.playerLayer;
		
		interpolatedTransform = new InterpolatedTransform();
		interpolatedTransform.Name = "PlayerInterpolatedTransform";
		player.AddChild(interpolatedTransform);
		interpolatedTransform.SetSource(player);
		interpolatedTransform.SetInterpolationReset(playerControls);

		avatar = new PlayerModel();
		interpolatedTransform.AddChild(avatar);
		avatar.LoadPlayer(ref modelName, ref skinName, (GameManager.AllPlayerViewMask & ~((uint)(playerInfo.viewLayer))), playerControls);

		Vector3 destination = SpawnerManager.FindSpawnLocation();
		TeleporterThing.TelefragEverything(destination, this);
		Position = destination;
		playerControls.InvoqueSetTransformReset();
		playerControls.ChangeHeight(true);
		playerControls.feetRay.Length = .8f;

		if (playerControls.playerWeapon == null)
			playerControls.SwapToBestWeapon();

//		playerInfo.playerHUD.HUDUpdateHealthNum();
//		playerInfo.playerHUD.HUDUpdateArmorNum();

		playerInfo.playerPostProcessing.playerHUD.UpdatePainMug(hitpoints);
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
		if (!ready)
			return;

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
		if (!ready)
			return;

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


		if (amount > 60)
			playerInfo.playerPostProcessing.playerHUD.painFlashTime(2.5f);
		else if (amount > 40)
			playerInfo.playerPostProcessing.playerHUD.painFlashTime(2f);
		else if (amount > 20)
			playerInfo.playerPostProcessing.playerHUD.painFlashTime(1.5f);
		else
			playerInfo.playerPostProcessing.playerHUD.painFlashTime(1f);

		if (hitpoints <= 0)
		{
//			playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Dead);
			CollisionLayer = (1 << GameManager.RagdollLayer);
			if (playerControls.playerWeapon != null)
				playerControls.playerWeapon.putAway = true;

			playerControls.playerCamera.ChangeThirdPersonCamera(true);
			PlayModelSound("death" + GD.RandRange(1, 3));
			avatar.Die();
			playerControls.feetRay.Length = 1.6f;
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
		playerInfo.playerPostProcessing.playerHUD.UpdatePainMug(hitpoints);
	}
	public void JumpPadDest(Vector3 destination)
	{
		if (!ready)
			return;

		Vector3 position = GlobalPosition;
		Vector3 horizontalVelocity = destination - position;
		float height = destination.Y - (position.Y - playerControls.feetRay.Length);

		if (height <= 0)
		{
			playerControls.jumpPadVel = Vector3.Zero;
			return;
		}

		float time = Mathf.Sqrt((2 * height) / GameManager.Instance.gravity);
		float verticalVelocity = time * GameManager.Instance.gravity;

		horizontalVelocity.Y = 0;
		float dist;
		horizontalVelocity = horizontalVelocity.GetLenghtAndNormalize(out dist);
		float forward = dist / time;
		horizontalVelocity = horizontalVelocity * forward;
		playerControls.jumpPadVel = horizontalVelocity;
		playerControls.jumpPadVel.Y = verticalVelocity;
		playerControls.playerVelocity = Vector3.Zero;
		playerControls.AnimateLegsOnJump();
	}

}
