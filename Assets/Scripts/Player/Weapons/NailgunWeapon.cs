using Godot;
using System;
using ExtensionMethods;

public partial class NailgunWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return .031f; } }
	public override float horizontalDispersion { get { return .035f; } }

	[Export]
	public string AttackProjectileName;

	public string onDeathSpawn = "BulletHit";
	public string decalMark = "ShotMark";

	public float maxRange = 400f;
	public float pushForce = 350;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.nailAmmo] <= 0 && fireTime < .1f)
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
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.nailAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.nailAmmo]);
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.nailAmmo] <= 0)
			return false;

		playerInfo.Ammo[PlayerInfo.nailAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.nailAmmo]);

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

		//Hitscan attack
		Transform3D global = playerInfo.playerCamera.GlobalTransform;
		Vector3 d = global.ForwardVector();

		for (int i = 0; i < 10; i++)
		{
			Projectile nail = (Projectile)ThingsManager.thingsPrefabs[AttackProjectileName].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(nail);
			nail.owner = playerInfo.playerThing;
			if (hasQuad)
				nail.EnableQuad();
			if (muzzleObject != null)
				nail.GlobalPosition = muzzleObject.GlobalPosition + d;
			else
				nail.GlobalPosition = global.Origin;
			nail.ignoreSelfLayer = playerInfo.playerLayer;
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			nail.SetForward(-d);
			nail.InvoqueSetTransformReset();
		}

		return true;
	}
}
