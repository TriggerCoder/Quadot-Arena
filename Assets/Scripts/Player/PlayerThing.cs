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

	public static string wearOffSound = "items/wearoff";

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
	public float quadTime = 0f;
	public float hasteTime = 0f;
	public float invisTime = 0f;
	public float regenTime = 0f;
	public float enviroSuitTime = 0f;
	public float flightTime = 0f;

	public bool finished = false;
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

		playerInfo.playerPostProcessing.playerHUD.UpdateHealth(hitpoints);
		playerInfo.playerPostProcessing.playerHUD.UpdateArmor(armor);
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
			CollisionLayer = (1 << GameManager.RagdollLayer);
			if (playerControls.playerWeapon != null)
				playerControls.playerWeapon.putAway = true;

			playerControls.playerCamera.ChangeThirdPersonCamera(true);

			DropWeaponsAndPowerUps();

			playerInfo.playerPostProcessing.playerHUD.RemoveAllItems();

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
		playerInfo.playerPostProcessing.playerHUD.UpdateHealth(hitpoints);
		playerInfo.playerPostProcessing.playerHUD.UpdateArmor(armor);
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

	public void DropWeaponsAndPowerUps()
	{
		List<string> itemsToDrop = new List<string>();
		Dictionary<string, int> itemQuantity = new Dictionary<string, int>();

		if (quadTime > 0)
		{
			itemsToDrop.Add("item_quad");
			itemQuantity.Add("item_quad", Mathf.CeilToInt(quadTime));
		}
		quadTime = 0;

		if (hasteTime > 0)
		{
			itemsToDrop.Add("item_haste");
			itemQuantity.Add("item_haste", Mathf.CeilToInt(hasteTime));
		}
		hasteTime = 0;

		switch (playerControls.CurrentWeapon)
		{
			default:
				break;
			case 2:
				itemsToDrop.Add("weapon_shotgun");
				break;
			case 4:
				itemsToDrop.Add("weapon_rocketlauncher");
				break;
			case 7:
				itemsToDrop.Add("weapon_plasmagun");
				break;
		}

		for (int i = 0; i < itemsToDrop.Count; i++)
		{
			string currentItem = itemsToDrop[i];
			RigidBody3D dropItem = (RigidBody3D)ThingsManager.thingsPrefabs[ThingsManager.ItemDrop].Instantiate();
			if (dropItem != null)
			{
				GameManager.Instance.TemporaryObjectsHolder.AddChild(dropItem);
				dropItem.GlobalPosition = ThingsManager.ItemLocationDropToFloor(GlobalPosition + Vector3.Up);
				Vector3 velocity = new Vector3((float)GD.RandRange(-20f, 20f), (float)GD.RandRange(5f, 10f), (float)GD.RandRange(-20f, 20f));
				dropItem.LinearVelocity = velocity;
			}

			ThingController thingObject = (ThingController)ThingsManager.thingsPrefabs[currentItem].Instantiate();
			if (thingObject != null)
			{
				if (dropItem != null)
				{
					thingObject.parent = dropItem;
					dropItem.AddChild(thingObject);
				}
				else
				{
					GameManager.Instance.TemporaryObjectsHolder.AddChild(thingObject);
					thingObject.GlobalPosition = ThingsManager.ItemLocationDropToFloor(GlobalPosition + Vector3.Up);
				}
				thingObject.SetRespawnTime(-1);
				int ammount;
				if (itemQuantity.TryGetValue(currentItem, out ammount))
					thingObject.itemPickup.amount = ammount;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		if (painTime > 0f)
			painTime -= deltaTime;

		if (quadTime > 0f)
		{
			if (PlayWearOffSound(ref quadTime, deltaTime))
				SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(wearOffSound));
			playerInfo.playerPostProcessing.playerHUD.UpdatePowerUpTime(PlayerHUD.PowerUpType.Quad,Mathf.CeilToInt(quadTime));
		}
		else if (quadTime < 0f) 
		{
			quadTime = 0;
			playerInfo.quadDamage = false;
			playerInfo.playerPostProcessing.playerHUD.RemovePowerUp(PlayerHUD.PowerUpType.Quad);
		}

		if (hasteTime > 0f)
		{
			if (PlayWearOffSound(ref hasteTime, deltaTime))
				SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(wearOffSound));
			playerInfo.playerPostProcessing.playerHUD.UpdatePowerUpTime(PlayerHUD.PowerUpType.Haste, Mathf.CeilToInt(hasteTime));
		}
		else if (hasteTime < 0f)
		{
			hasteTime = 0;
			playerInfo.haste = false;
			playerInfo.playerPostProcessing.playerHUD.RemovePowerUp(PlayerHUD.PowerUpType.Haste);
		}
	}

	public bool PlayWearOffSound(ref float Time, float deltaTime)
	{
		float check = 0;
		if (Time > 1f)
			check = 1;
		if (Time > 2f)
			check = 2;
		if (Time > 3f)
			check = 3;

		Time -= deltaTime;
		if (Time < check)
			return true;
		return false;
	}
}
