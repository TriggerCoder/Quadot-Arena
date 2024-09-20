using Godot;
using System;
using ExtensionMethods;
public partial class ProxLauncherWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return .02f; } }
	public override float horizontalDispersion { get { return .02f; } }
	[Export]
	public string AttackProjectileName;
	[Export]
	public AnimationTree weaponAnimation;
	[Export]
	public float animationSpeed = 1;

	public Vector3 spawnPos;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.minesAmmo] <= 0 && fireTime < .1f)
		{
			if ((!putAway) && (Sounds.Length > 1))
			{
				audioStream.Stream = Sounds[1];
				audioStream.Play();
			}
			putAway = true;
		}
	}
	protected override void OnInit()
	{
		if (Sounds.Length > 2)
		{
			audioStream.Stream = Sounds[2];
			audioStream.Play();
		}
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.minesAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.minesAmmo]);

		weaponAnimation.Active = true;
		weaponAnimation.Set("parameters/fire_shot/active", true);
		weaponAnimation.Set("parameters/TimeScale/scale", animationSpeed);
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.minesAmmo] <= 0)
			return false;

		playerInfo.Ammo[PlayerInfo.minesAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.minesAmmo]);

		if (GameOptions.UseMuzzleLight)
		{
			if (muzzleLight != null)
			{
				muzzleLight.LightEnergy = 1.0f;
				if (muzzleObject != null)
					if (!muzzleObject.Visible)
					{
						muzzleObject.Visible = true;
						playerInfo.playerThing.avatar.MuzzleFlashSetActive(true);
					}
			}
		}
		//maximum fire rate 20/s, unless you use negative number (please don't)
		float currentFireRate = _fireRate;
		if (playerInfo.haste)
			currentFireRate = _hasteFireRate;
		fireTime = currentFireRate + .05f;
		coolTimer = 0f;
		playerInfo.playerPostProcessing.playerHUD.SetAmmoCoolDown(true);

		if (Sounds.Length > 0)
		{
			audioStream.Stream = Sounds[0];
			audioStream.Play();
		}

		if (hasQuad)
			SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(quadSound));

		weaponAnimation.Set("parameters/fire_shot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
		//Projectile attack
		{
			Transform3D global = playerInfo.playerCamera.GlobalTransform;
			Vector3 d = global.ForwardVector();

			PhysicProjectile mine = (PhysicProjectile)ThingsManager.thingsPrefabs[AttackProjectileName].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(mine);
			mine.owner = playerInfo.playerThing;
			if (hasQuad)
				mine.EnableQuad();
			if (muzzleObject != null)
				mine.GlobalPosition = muzzleObject.GlobalPosition + d;
			else
				mine.GlobalPosition = global.Origin;
			mine.Init(playerInfo.playerLayer);
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			mine.SetForward(d);
			Vector3 velocity = -d * 20;
			mine.LinearVelocity = velocity;
			velocity = new Vector3((float)GD.RandRange(-20f, 20f), (float)GD.RandRange(5f, 10f), (float)GD.RandRange(-20f, 20f));
			mine.AngularVelocity = velocity;
		}

		return true;
	}
}
