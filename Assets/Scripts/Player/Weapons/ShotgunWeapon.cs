using Godot;
using System;
using ExtensionMethods;

public partial class ShotgunWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return .049f; } } // tan(4) / 2
	public override float horizontalDispersion { get { return .062f; } } //tan(7.1) / 2

	public string caseName = "ShotgunShell";
	public string onDeathSpawn = "BulletHit";
	public string decalMark = "ShotMark";

	public float maxRange = 400f;
	public float pushForce = 350;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.shellsAmmo] <= 0 && fireTime < .1f)
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
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.shellsAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.shellsAmmo]);
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.shellsAmmo] <= 0)
			return false;

		playerInfo.Ammo[PlayerInfo.shellsAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.shellsAmmo]);

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

		for (int i = 0; i < 11; i++)
		{
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | (1 << GameManager.RagdollLayer) | GameManager.TakeDamageMask & ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer))));
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.IntersectRay(RayCast);
			if (hit.Count > 0)
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				Vector3 collision = (Vector3)hit["position"];
				Vector3 normal = (Vector3)hit["normal"];
				bool hitFx = true;
				if (collider is Damageable damageable)
				{
					if (hasQuad)
						damageable.Damage(GD.RandRange(DamageMin * GameManager.Instance.QuadMul, DamageMax * GameManager.Instance.QuadMul), DamageType.Pellet, playerInfo.playerThing);
					else
						damageable.Damage(GD.RandRange(DamageMin, DamageMax), DamageType.Pellet, playerInfo.playerThing);

					if (damageable.Bleed)
					{
						hitFx = false;
						Node3D Blood = (Node3D)ThingsManager.thingsPrefabs[ThingsManager.Blood].Instantiate();
						GameManager.Instance.TemporaryObjectsHolder.AddChild(Blood);
						Blood.GlobalPosition = collision + (normal * .05f);
					}
				}
				if (hitFx)
				{
					Node3D BulletHit = (Node3D)ThingsManager.thingsPrefabs[onDeathSpawn].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletHit);
					BulletHit.Position = collision + (normal * .2f);
					BulletHit.SetForward(normal);
					BulletHit.Rotate(BulletHit.UpVector(), -Mathf.Pi * .5f);
					BulletHit.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				}

				if (Sounds.Length > 3)
					SoundManager.Create3DSound(collision, Sounds[GD.RandRange(3, Sounds.Length - 1)]);

				if (CheckIfCanMark(SpaceState, collider, collision))
				{
					SpriteController BulletMark = (SpriteController)ThingsManager.thingsPrefabs[decalMark].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletMark);
					BulletMark.GlobalPosition = collision + (normal * .05f);
					BulletMark.SetForward(normal);
					BulletMark.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
					if (collider is Crusher)
						BulletMark.referenceNode = collider;
				}
			}
		}

		//Case Drop
		if (!string.IsNullOrEmpty(caseName))
		{
			for (int i = 0; i < 2; i++)
			{
				RigidBody3D ammocase = (RigidBody3D)ThingsManager.thingsPrefabs[caseName].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(ammocase);
				ammocase.Position = GlobalPosition;
				ammocase.ApplyImpulse(new Vector3((float)GD.RandRange(-.2f, .02f), (float)GD.RandRange(.2f, .4f), (float)GD.RandRange(-.2f, .2f)));
			}
		}
		return true;
	}
}
