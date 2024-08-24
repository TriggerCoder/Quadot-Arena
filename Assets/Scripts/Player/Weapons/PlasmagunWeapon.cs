using Godot;
using System;
using ExtensionMethods;

public partial class PlasmagunWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return .02f; } }
	public override float horizontalDispersion { get { return .02f; } }
	[Export]
	public string AttackProjectileName;
	public Vector3 spawnPos;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.cellsAmmo] <= 0 && fireTime < .1f)
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
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.cellsAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.cellsAmmo]);
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.cellsAmmo] <= 0)
			return false;

		playerInfo.Ammo[PlayerInfo.cellsAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.cellsAmmo]);

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

		if (Sounds.Length > 0)
		{
			audioStream.Stream = Sounds[0];
			audioStream.Play();
		}

		if (hasQuad)
			SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(quadSound));

		//Projectile attack
		{
			Transform3D global = playerInfo.playerCamera.GlobalTransform;
			Vector3 d = global.ForwardVector();

			Projectile plasma = (Projectile)ThingsManager.thingsPrefabs[AttackProjectileName].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(plasma);
			plasma.owner = playerInfo.playerThing;
			if (hasQuad)
				plasma.EnableQuad();
			if (muzzleObject != null)
				plasma.GlobalPosition = muzzleObject.GlobalPosition + d;
			else
				plasma.GlobalPosition = global.Origin;
			plasma.ignoreSelfLayer = playerInfo.playerLayer;
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			plasma.SetForward(-d);
			plasma.InvoqueSetTransformReset();
		}

		return true;
	}
}