using Godot;
using System.Collections.Generic;
using ExtensionMethods;

public partial class PlayerThing : CharacterBody3D, Damageable
{
	[Export]
	public CollisionShape3D Torso;
	[Export]
	public CollisionShape3D Feet;
	[Export]
	public PlayerInfo playerInfo;
	[Export]
	public PlayerControls playerControls;
	[Export]
	public MultiAudioStream audioStream;
	
	public PlayerViewPort playerViewPort;

	public InterpolatedTransform interpolatedTransform;
	public string modelName = "crash";
	public string skinName = "default";

	public static string drowningSound = "player/gurp";
	public static string wearOffSound = "items/wearoff";
	public static string regenSound = "items/regen";

	[Export]
	public Node3D player;
	public PlayerModel avatar;

	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }

	public string playerName 
	{ 
		get { return _playerName; }
		set
		{
			_playerName = value;
			playerInfo.playerPostProcessing.playerHUD.playerName.Text = value;
		}
	}
	private string _playerName = "Unnamed Player";

	public BloodType BloodColor { get { return BloodType.Red; } }

	public int hitpoints = 100;
	public int armor = 0;
	public int waterLever = 0;
	public int frags = 0;
	public int deaths = 0;

	public WaterSurface currentWaterSurface = null;

	public float painTime = 0f;
	public float quadTime = 0f;
	public float hasteTime = 0f;
	public float invisTime = 0f;
	public float regenTime = 0f;
	public float enviroSuitTime = 0f;
	public float flightTime = 0f;

	public float lastDamageTime = 0f;

	public float environmentDamageTime = 0f;
	public float drownTime = 0f;

	public int drownDamage = 0;

	public bool underWater = false;
	public bool inLava = false;
	public bool finished = false;
	public bool invul = false;

	public Node3D lastAttacker = null;
	public bool ready { get { return currentState == GameManager.FuncState.Start; } }
	private float handicap = 1;
	public enum HoldableItem
	{
		None = 0,
		Teleporter = 1,
		Medkit = 2
	}
	public HoldableItem holdableItem = HoldableItem.None;

	private int skipFrames = 3;
	public GameManager.FuncState currentState = GameManager.FuncState.None;
	public override void _Ready()
	{
		playerControls.feetRay = new SeparationRayShape3D();
		playerControls.feetRay.Length = .8f;
		Feet.Shape = playerControls.feetRay;

		playerControls.collider = new CapsuleShape3D();
		playerControls.collider.Radius = .5f;
		playerControls.collider.Height = 1.6f;
		Torso.Shape = playerControls.collider;
		currentState = GameManager.FuncState.Ready;
	}
	public void InitPlayer()
	{
		CollisionLayer = playerInfo.playerLayer;
		CollisionMask |= GameManager.TakeDamageMask;
		interpolatedTransform = new InterpolatedTransform();
		interpolatedTransform.Name = "PlayerInterpolatedTransform";
		player.AddChild(interpolatedTransform);
		interpolatedTransform.SetSource(player);
		interpolatedTransform.SetInterpolationReset(playerControls);

		avatar = new PlayerModel();
		interpolatedTransform.AddChild(avatar);
		avatar.LoadPlayer(ref modelName, ref skinName, (GameManager.AllPlayerViewMask & ~((uint)(playerInfo.viewLayer))), playerControls);

		SpawnerManager.SpawnToLocation(this);
		playerControls.ChangeHeight(true);
		playerControls.feetRay.Length = .8f;

		if (playerControls.playerWeapon == null)
			playerControls.SwapToBestWeapon();

		playerInfo.playerPostProcessing.playerHUD.UpdateHealth(hitpoints);
		playerInfo.playerPostProcessing.playerHUD.UpdateArmor(armor);
		playerControls.playerCamera.ChangeThirdPersonCamera(false);
		currentState = GameManager.FuncState.Ready;
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
			amount = Mathf.RoundToInt(amount * handicap * GameManager.Instance.PlayerDamageReceive);

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

		float painFlash = Mathf.Lerp(1, 2.5f, Mathf.Clamp(amount / 60, 0, 1));
		if (attacker == this)
			painFlash /= 2;

		playerInfo.playerPostProcessing.playerHUD.painFlashTime(painFlash);
		if (hitpoints <= 0)
		{
			CollisionLayer = (1 << GameManager.RagdollLayer);
			CollisionMask &= ~GameManager.TakeDamageMask;
			if (playerControls.playerWeapon != null)
				playerControls.playerWeapon.putAway = true;

			playerControls.playerCamera.ChangeThirdPersonCamera(true);

			DropWeaponsAndPowerUps();

			playerInfo.playerPostProcessing.playerHUD.RemoveAllItems();

			if (damageType == DamageType.Drown)
				PlayModelSound("drown");
			else
				PlayModelSound("death" + GD.RandRange(1, 3));
			avatar.Die();
			playerControls.feetRay.Length = 1.6f;
			currentState = GameManager.FuncState.None;
			deaths++;
			playerInfo.playerPostProcessing.playerHUD.deathsText.Text = "-" + deaths;
			if (attacker != this)
			{
				if (attacker == null)
					attacker = lastAttacker;

				if (attacker is PlayerThing agressor)
				{
					handicap = Mathf.Clamp(handicap - .05f, .5f, 2);
					agressor.handicap = Mathf.Clamp(agressor.handicap + .05f, .5f, 2);
					agressor.frags++;
					agressor.playerInfo.playerPostProcessing.playerHUD.fragsText.Text = "+" + agressor.frags;
					GameManager.Instance.CheckDeathCount(agressor.frags);
				}
			}
			ScoreBoard.Instance.RefreshScore();
//			GameManager.Instance.AddDeathCount();
		}
		else if (damageType == DamageType.Drown)
			SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(drowningSound + GD.RandRange(1, 2)));
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
			avatar.Scale = Vector3.One * 1.1f;
		}

		if (attacker != this)
		{
			if (attacker is PlayerThing)
			{
				lastAttacker = attacker;
				lastDamageTime = 3;
			}
			else if (lastAttacker != null)
				lastDamageTime = 3;
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

		if (regenTime > 0)
		{
			itemsToDrop.Add("item_regen");
			itemQuantity.Add("item_regen", Mathf.CeilToInt(regenTime));
		}
		regenTime = 0;

		switch (playerControls.CurrentWeapon)
		{
			default:
				break;
			case PlayerInfo.Shotgun:
				itemsToDrop.Add("weapon_shotgun");
			break;
			case PlayerInfo.GrenadeLauncher:
				itemsToDrop.Add("weapon_grenadelauncher");
			break;
			case PlayerInfo.RocketLauncher:
				itemsToDrop.Add("weapon_rocketlauncher");
			break;
			case PlayerInfo.LightningGun:
				itemsToDrop.Add("weapon_lightning");
			break;
			case PlayerInfo.Railgun:
				itemsToDrop.Add("weapon_railgun");
			break;
			case PlayerInfo.PlasmaGun:
				itemsToDrop.Add("weapon_plasmagun");
			break;
			case PlayerInfo.BFG10K:
				itemsToDrop.Add("weapon_bfg");
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
		bool newTick = GameManager.NewTickSeconds;

		//Pain
		if (painTime > 0f)
		{
			painTime -= deltaTime;
			avatar.Scale = Vector3.One * Mathf.Lerp(1, avatar.Scale.X, painTime);
		}
		else if (painTime < 0f)
		{
			painTime = 0;
			avatar.Scale = Vector3.One;
		}

		//Last Attacker
		if (lastDamageTime > 0f)
			lastDamageTime -= deltaTime;
		if (lastDamageTime < 0f)
		{
			lastDamageTime = 0;
			lastAttacker = null;
		}
			
		//Quad
		if (quadTime > 0f)
		{
			if (quadTime < 4f)
			{
				if (newTick)
					SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(wearOffSound));
			}
			if (newTick)
				playerInfo.playerPostProcessing.playerHUD.UpdatePowerUpTime(PlayerHUD.PowerUpType.Quad,Mathf.FloorToInt(quadTime));
			quadTime -= deltaTime;
		}
		else if (quadTime < 0f) 
		{
			quadTime = 0;
			playerInfo.quadDamage = false;
			playerInfo.playerPostProcessing.playerHUD.RemovePowerUp(PlayerHUD.PowerUpType.Quad);
		}

		//Haste
		if (hasteTime > 0f)
		{
			if (hasteTime < 4f)
			{
				if (newTick)
					SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(wearOffSound));
			}
			if (newTick)
				playerInfo.playerPostProcessing.playerHUD.UpdatePowerUpTime(PlayerHUD.PowerUpType.Haste, Mathf.FloorToInt(hasteTime));
			hasteTime -= deltaTime;
		}
		else if (hasteTime < 0f)
		{
			hasteTime = 0;
			playerInfo.haste = false;
			playerInfo.playerPostProcessing.playerHUD.RemovePowerUp(PlayerHUD.PowerUpType.Haste);
		}

		//Regen
		if (regenTime > 0f)
		{
			if (regenTime < 4f)
			{
				if (newTick)
					SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(wearOffSound));
			}
			if (newTick)
			{
				if (hitpoints < playerInfo.MaxBonusHealth)
				{
					hitpoints += 5;
					if (hitpoints > playerInfo.MaxBonusHealth)
						hitpoints = playerInfo.MaxBonusHealth;
					SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(regenSound));
				}
				playerInfo.playerPostProcessing.playerHUD.UpdateHealth(hitpoints);
				playerInfo.playerPostProcessing.playerHUD.UpdatePowerUpTime(PlayerHUD.PowerUpType.Regen, Mathf.FloorToInt(regenTime));
			}
			regenTime -= deltaTime;
		}
		else if (regenTime < 0f)
		{
			regenTime = 0;
			playerInfo.haste = false;
			playerInfo.playerPostProcessing.playerHUD.RemovePowerUp(PlayerHUD.PowerUpType.Regen);
		}

		if (inLava)
		{
			environmentDamageTime -= deltaTime;
			if (environmentDamageTime < 0f)
			{
				Damage(30, DamageType.Environment);
				environmentDamageTime = 1;
			}
		}
		//Lava CoolDown
		else if (environmentDamageTime > 0)
		{
			environmentDamageTime -= deltaTime;
			if (environmentDamageTime < 0f)
				environmentDamageTime = 0;
		}

		if (waterLever > 1)
		{
			if (drownTime > 0)
				drownTime -= deltaTime;
			if (drownTime < 0f)
			{
				drownDamage += 2;
				Damage(drownDamage, DamageType.Drown);
				drownTime = 1;
			}

		}
		else if (drownTime != 0)
		{
			drownTime = 0;
			drownDamage = 0;
		}

	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		//skip frames are used to easen up collision detection after respawn
		if (currentState == GameManager.FuncState.Ready)
		{
			if (skipFrames > 0)
			{
				skipFrames--;
				if (skipFrames == 0)
				{
					currentState = GameManager.FuncState.Start;
					skipFrames = 3;
				}
			}
		}
	}
}
