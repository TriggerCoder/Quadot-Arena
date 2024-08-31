using Godot;
using System;
using ExtensionMethods;

public partial class BFG10KWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return .02f; } }
	public override float horizontalDispersion { get { return .02f; } }
	[Export]
	public string AttackProjectileName;
	public Vector3 spawnPos;
	[Export]
	public MultiAudioStream humStream;
	[Export]
	public string[] _humSounds;
	public AudioStream[] humSounds;

	private float chargeDelay = 0;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.bfgAmmo] < 40 && fireTime < .1f)
		{
			if ((!putAway) && (Sounds.Length > 1))
			{
				audioStream.Stream = Sounds[1];
				audioStream.Play();
			}
			putAway = true;
		}
	}

	protected override void OnPhysicsUpdate(float deltaTime)
	{
		if (chargeDelay > 0)
		{
			muzzleObject.Scale = Vector3.One * Mathf.Max(Mathf.Lerp(1, 0.1f, chargeDelay), 0);
			playerInfo.playerThing.avatar.MuzzleFlashSetScale(muzzleObject.Scale);
			chargeDelay -= deltaTime;
		}

		if (chargeDelay < 0)
		{
			chargeDelay = 0;

			playerInfo.Ammo[PlayerInfo.bfgAmmo] -= 40;
			playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.bfgAmmo]);

			if (GameOptions.UseMuzzleLight)
			{
				if (muzzleLight != null)
				{
					muzzleLight.Show();
					muzzleLight.LightEnergy = 1.0f;
					if (muzzleObject != null)
						if (!muzzleObject.Visible)
						{
							muzzleObject.Scale = Vector3.One;
							muzzleObject.Visible = true;
							playerInfo.playerThing.avatar.MuzzleFlashSetScale(Vector3.One);
							playerInfo.playerThing.avatar.MuzzleFlashSetActive(true);
						}
				}
			}

			humStream.Stream = humSounds[0];
			humStream.Play();

			if (Sounds.Length > 0)
				SoundManager.Create3DSound(GlobalPosition, Sounds[0]);

			if (hasQuad)
				SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(quadSound));

			playerInfo.playerThing.avatar.Attack();
			//Projectile attack
			{
				Transform3D global = playerInfo.playerCamera.GlobalTransform;
				Vector3 d = global.ForwardVector();

				Projectile bfgBall = (Projectile)ThingsManager.thingsPrefabs[AttackProjectileName].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(bfgBall);
				bfgBall.owner = playerInfo.playerThing;
				if (hasQuad)
					bfgBall.EnableQuad();
				if (muzzleObject != null)
					bfgBall.GlobalPosition = muzzleObject.GlobalPosition + d;
				else
					bfgBall.GlobalPosition = global.Origin;
				bfgBall.ignoreSelfLayer = playerInfo.playerLayer;
				Vector2 r = GetDispersion();
				d += global.RightVector() * r.X + global.UpVector() * r.Y;
				d = d.Normalized();
				bfgBall.SetForward(-d);
				bfgBall.InvoqueSetTransformReset();
			}
		}
	}
	protected override void OnInit()
	{
		if (Sounds.Length > 2)
		{
			audioStream.Stream = Sounds[2];
			audioStream.Play();
		}
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.bfgAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.bfgAmmo]);

		humSounds = new AudioStream[_humSounds.Length];
		for (int i = 0; i < _humSounds.Length; i++)
			humSounds[i] = SoundManager.LoadSound(_humSounds[i], true);

		humStream.Stream = humSounds[0];
		humStream.Play();
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.bfgAmmo] < 40)
			return false;

		if (GameOptions.UseMuzzleLight)
		{
			if (muzzleLight != null)
			{
				muzzleLight.Hide();
				muzzleLight.LightEnergy = 1.0f;
				if (muzzleObject != null)
					if (!muzzleObject.Visible)
					{
						muzzleObject.Scale = new Vector3(.1f, .1f, .1f);
						muzzleObject.Visible = true;
						playerInfo.playerThing.avatar.MuzzleFlashSetScale(muzzleObject.Scale);
						playerInfo.playerThing.avatar.MuzzleFlashSetActive(true);
					}
			}
		}
		//maximum fire rate 20/s, unless you use negative number (please don't)
		float currentFireRate = _fireRate;
		if (playerInfo.haste)
			currentFireRate = _hasteFireRate;
		fireTime = currentFireRate + .05f;
		chargeDelay = currentFireRate - .05f;

		coolTimer = 0f;
		playerInfo.playerPostProcessing.playerHUD.SetAmmoCoolDown(true);

		humStream.Stream = humSounds[1];
		humStream.Play();

		return false;
	}
}