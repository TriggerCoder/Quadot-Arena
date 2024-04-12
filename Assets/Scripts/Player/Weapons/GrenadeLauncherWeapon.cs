using Godot;
using System;
using ExtensionMethods;

public partial class GrenadeLauncherWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .01f; } }
	public override float maxDispersion { get { return .02f; } }
	[Export]
	public string AttackProjectileName;
	[Export]
	public AnimationTree weaponAnimation;
	[Export]
	public AnimationPlayer animation;
	public Vector3 spawnPos;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.grenadesAmmo] <= 0 && fireTime < .1f)
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
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.grenadesAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.grenadesAmmo]);

		animation.SpeedScale = kickSpeed;
		weaponAnimation.Active = true;
		weaponAnimation.Set("parameters/fire_shot/active", true);
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.grenadesAmmo] <= 0)
			return false;

		playerInfo.Ammo[PlayerInfo.grenadesAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.grenadesAmmo]);

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

			Grenade grenade = (Grenade)ThingsManager.thingsPrefabs[AttackProjectileName].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(grenade);
			grenade.owner = playerInfo.playerThing;
			if (muzzleObject != null)
				grenade.GlobalPosition = muzzleObject.GlobalPosition + d;
			else
				grenade.GlobalPosition = global.Origin;
			grenade.Init(playerInfo.playerLayer);
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			grenade.SetForward(d);
			Vector3 velocity = -d * 25;
			grenade.LinearVelocity = velocity;
			velocity = new Vector3((float)GD.RandRange(-20f, 20f), (float)GD.RandRange(5f, 10f), (float)GD.RandRange(-20f, 20f));
			grenade.AngularVelocity = velocity;
		}

		return true;
	}
}
