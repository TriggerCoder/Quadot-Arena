using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class WaterSurface : Area3D
{
	public List<Aabb> Boxes = new List<Aabb>();
	private List<PlayerThing> currentPlayers = new List<PlayerThing>();
	private Dictionary<Node3D, int> CurrentColliders = new Dictionary<Node3D, int>();
	public string waterIn = "player/watr_in";
	public string waterOut = "player/watr_out";
	public string waterUnder = "player/watr_un";
	public string zap = "world/button_zap";
	private AudioStream inSound;
	private AudioStream outSound;
	private AudioStream underSound;
	private AudioStream zapSound;
	public DamageableType damageable = DamageableType.None;

	public enum DamageableType
	{
		None,
		Lava,
		Slime
	}
	public override void _Ready()
	{
		Gravity /= 2;
		GravitySpaceOverride = SpaceOverride.Replace;
		AngularDamp = 3;
		AngularDampSpaceOverride = SpaceOverride.Replace;
		LinearDamp = 3;
		LinearDampSpaceOverride = SpaceOverride.Replace;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExit;
		inSound = SoundManager.LoadSound(waterIn);
		outSound = SoundManager.LoadSound(waterOut);
		underSound = SoundManager.LoadSound(waterUnder);
		zapSound = SoundManager.LoadSound(zap);
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < currentPlayers.Count; i++)
		{
			PlayerThing currentPlayer = currentPlayers[i];
			if (currentPlayer.Dead)
			{
//				GameManager.Print(currentPlayer.Name + "died in the Water " + Name);
				currentPlayers.Remove(currentPlayer);
				continue;
			}

			for (int j = 0; j < Boxes.Count; j++) 
			{
				if (Boxes[j].HasPoint(currentPlayer.GlobalPosition))
				{
					float deep = Boxes[j].GetEndpoint(2).Y - currentPlayer.GlobalPosition.Y;
					if (!currentPlayer.underWater)
					{
						if (deep > GameManager.Instance.playerHeight)
						{
							SoundManager.Create3DSound(currentPlayer.GlobalPosition, underSound);
							currentPlayer.playerInfo.playerPostProcessing.SetWaterEffect();
							currentPlayer.waterLever = 2;
							currentPlayer.drownTime = 12;
							currentPlayer.underWater = true;
						}
						else if (deep < 1)
							currentPlayer.drownTime = 12;
					}
					else if (currentPlayer.waterLever > 1)
					{
						if (deep < 1)
						{
							currentPlayer.playerInfo.playerPostProcessing.ResetEffects();
							currentPlayer.PlayModelSound("gasp");
							currentPlayer.underWater = false;
							currentPlayer.drownTime = 12;
						}
					}
					break;
				}
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		if (CurrentColliders.Count == 0)
			return;

		var CurrentBodies = GetOverlappingBodies();
		int CurrentBodiesNum = CurrentBodies.Count;
		if (CurrentBodiesNum == 0)
		{
			CurrentColliders.Clear();
			return;
		}

		for (int i = 0; i < CurrentBodiesNum; i++)
		{
			Node3D CurrentBody = CurrentBodies[i];
			if (CurrentColliders.ContainsKey(CurrentBody))
			{
				PlayerThing playerThing = CurrentBody as PlayerThing;
				if ((!playerThing.ready) || (playerThing.Dead))
				{
					CurrentColliders.Remove(CurrentBody);
					continue;
				}
				//We need antibounce
				int value = CurrentColliders[CurrentBody]++;
				if (value > 1)
				{
					PlayerEnterIntoWater(CurrentBody as PlayerThing);
					CurrentColliders.Remove(CurrentBody);
				}
			}
		}
	}

	public void ElectroShockDischarge(PlayerThing player)
	{
		int damage = player.playerInfo.Ammo[PlayerInfo.lightningAmmo];
		if (player.playerInfo.quadDamage)
			damage *= 4;

		int ammo = damage - player.hitpoints;
		player.playerInfo.Ammo[PlayerInfo.lightningAmmo] = ammo >= 0 ? ammo : 0;
		player.Damage(damage, DamageType.Electric, player);
		SoundManager.Create3DSound(player.GlobalPosition, zapSound);
		if (ammo <= 0)
			return;

		var CurrentBodies = GetOverlappingBodies();
		int totalEnemies = 0;
		for (int i = 0; i < CurrentBodies.Count; i++)
		{
			Node3D CurrentBody = CurrentBodies[i];
			if (CurrentBody is PlayerThing)
			{
				PlayerThing enemy = (PlayerThing)CurrentBody;
				if (enemy == player)
					continue;
				totalEnemies++;
			}
		}
		if (totalEnemies == 0)
			return;

		damage = ammo / totalEnemies;
		for (int i = 0; i < CurrentBodies.Count; i++)
		{
			Node3D CurrentBody = CurrentBodies[i];
			if (CurrentBody is PlayerThing)
			{
				PlayerThing enemy = (PlayerThing)CurrentBody;
				if (enemy == player)
					continue;
				enemy.Damage(damage, DamageType.Electric, player);
			}
		}
	}

	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		//Hide smoke and modify physics of grenade in water
		if (other is PhysicProjectile projectile)
			projectile.ChangeWater(true, underSound);

		if (other is PlayerThing player)
		{
			if (player.currentState == GameManager.FuncState.Ready)
				return;

			//Will check everything back on the main thread
			if (!CurrentColliders.ContainsKey(other))
				CurrentColliders.Add(other, 0);
		}
	}

	void OnBodyExit(Node3D other)
	{
		if (GameManager.Paused)
			return;

		//Restore smoke and physics of grenade outside water
		if (other is Grenade grenade)
			grenade.ChangeWater(false, inSound);

		if (other is PlayerThing playerThing)
		{
			if (currentPlayers.Contains(playerThing))
			{
				if (playerThing.underWater)
				{
					playerThing.playerInfo.playerPostProcessing.ResetEffects();
					playerThing.PlayModelSound("gasp");
				}
				playerThing.underWater = false;
				playerThing.waterLever = 0;
				playerThing.currentWaterSurface = null;
				playerThing.inDamageable = DamageableType.None;
				playerThing.avatar.lowerAnimation = PlayerModel.LowerAnimation.Jump;
				SoundManager.Create3DSound(playerThing.GlobalPosition, outSound);
				currentPlayers.Remove(playerThing);
//				GameManager.Print("Finally " + other.Name + "got out of the Water " + Name);
			}
		}
	}

	void PlayerEnterIntoWater(PlayerThing playerThing)
	{
		if (!currentPlayers.Contains(playerThing))
		{
			playerThing.waterLever = 1;
			playerThing.currentWaterSurface = this;
			playerThing.inDamageable = damageable;
//			playerThing.playerInfo.playerPostProcessing.SetWaterEffect();
			SoundManager.Create3DSound(playerThing.GlobalPosition, inSound);
			currentPlayers.Add(playerThing);
//			GameManager.Print(playerThing.Name + " Jump into the Water " + Name);
		}
	}

}
