using Godot;
using System;
using ExtensionMethods;

public partial class ChainGunWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return vertDispersion[warmed]; } }
	public override float horizontalDispersion { get { return horDispersion[warmed]; } }

	public readonly float[] vertDispersion  = new float[2] { .066f, .11f };
	public readonly float[] horDispersion = new float[2] { .066f, .11f };

	public string caseName = "MachineGunAmmoCase";
	public string onDeathSpawn = "BulletHit";
	public string decalMark = "BulletMark";
	public float maxRange = 400f;

	public float warmTime = 1;
	public float[] barrelSpeed = new float[2] { 10, 12 };
	public float[] hastebarrelSpeed = new float[2] { 11, 12 };
	private float currentRotSpeed = 0;
	private int warmed = 0;
	private float[] fireRate;
	private float[] hasteFireRate;

	[Export]
	public MultiAudioStream humStream;
	[Export]
	public string _humSound;

	private bool timeToMuzzleLight = true;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.chainAmmo] <= 0 && fireTime < .1f)
		{
			if ((!putAway) && (Sounds.Length > 4))
			{
				audioStream.Stream = Sounds[4];
				audioStream.Play();
			}
			putAway = true;
		}

		if (warmTime > 0)
		{
			warmTime -= deltaTime;
			if (warmTime < 0)
				warmed = 1;
		}

	}

	protected override void OnInit()
	{
		if (Sounds.Length > 5)
		{
			audioStream.Stream = Sounds[5];
			audioStream.Play();
		}

		humStream.Stream = SoundManager.LoadSound(_humSound, true);

		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.chainAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.chainAmmo]);

		fireRate = new float[2] { _fireRate, _hasteFireRate };
		hasteFireRate = new float[2] { _hasteFireRate, _hasteFireRate * .7f };
	}

	protected override void OnCoolDown()
	{
		warmed = 0;
		timeToMuzzleLight = true;
		humStream.Stop();
		if (Sounds.Length > 9)
		{
			audioStream.Stream = Sounds[9];
			audioStream.Play();
		}
	}

	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.chainAmmo] <= 0)
			return false;

		if (cooldown)
			return false;

		if (fireTime <= 0)
		{
			warmTime = 1;
			humStream.Play();
		}

		playerInfo.Ammo[PlayerInfo.chainAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.chainAmmo]);

		if (GameOptions.UseMuzzleLight)
		{
			if (muzzleLight != null)
			{
				if (muzzleObject != null)
					if (!muzzleObject.Visible)
					{
						if (timeToMuzzleLight)
						{
							if (hasQuad)
								SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(quadSound));
							muzzleLight.LightEnergy = 1.0f;
							muzzleObject.Visible = true;
							playerInfo.playerThing.avatar.MuzzleFlashSetActive(true);
						}
						timeToMuzzleLight = !timeToMuzzleLight;
					}
			}
		}
		//maximum fire rate 20/s, unless you use negative number (please don't)
		float currentFireRate = fireRate[warmed];
		if (playerInfo.haste)
			currentFireRate = hasteFireRate[warmed];
		fireTime = currentFireRate + .05f;
		coolTimer = 0f;

		if (Sounds.Length > 3)
		{
			audioStream.Stream = Sounds[GD.RandRange(0,3)];
			audioStream.Play();
		}

		//Hitscan attack
		{
			Transform3D global = playerInfo.playerCamera.GlobalTransform;
			Vector3 d = global.ForwardVector();
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
				int soundIndex = 6;
				bool hitFx = true;
				if (collider is Damageable damageable)
				{
					if (hasQuad)
						damageable.Damage(GD.RandRange(DamageMin * GameManager.Instance.QuadMul, DamageMax * GameManager.Instance.QuadMul), DamageType.Bullet, playerInfo.playerThing);
					else
						damageable.Damage(GD.RandRange(DamageMin, DamageMax), DamageType.Bullet, playerInfo.playerThing);

					if (damageable.Bleed)
					{
						hitFx = false;
						soundIndex = 7;
						Node3D Blood = (Node3D)ThingsManager.thingsPrefabs[ThingsManager.Blood].Instantiate();
						GameManager.Instance.TemporaryObjectsHolder.AddChild(Blood);
						Blood.GlobalPosition = collision + (normal * .05f);
					}
				}
				if (hitFx)
				{
					if (MapLoader.mapSurfaceTypes.TryGetValue(collider, out SurfaceType st))
					{
						if (st.MetalSteps)
							soundIndex = 8;
						else if (st.Flesh)
							soundIndex = 7;
					}

					Node3D BulletHit = (Node3D)ThingsManager.thingsPrefabs[onDeathSpawn].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletHit);
					BulletHit.Position = collision + (normal * .2f);
					BulletHit.SetForward(normal);
					BulletHit.Rotate(BulletHit.UpVector(), -Mathf.Pi * .5f);
					BulletHit.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				}

				if (Sounds.Length > soundIndex)
					SoundManager.Create3DSound(collision, Sounds[soundIndex]);

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
			RigidBody3D ammocase = (RigidBody3D)ThingsManager.thingsPrefabs[caseName].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(ammocase);
			ammocase.Position = GlobalPosition;
			ammocase.Quaternion = new Quaternion(Vector3.Right, currentRotSpeed);
			ammocase.ApplyImpulse(new Vector3((float)GD.RandRange(-.2f, .02f), (float)GD.RandRange(.2f, .4f), (float)GD.RandRange(-.2f, .2f)));
		}
		return true;
	}
	protected override Quaternion RotateBarrel(float deltaTime)
	{
		if (fireTime > 0f)
		{
			float currenBarrelSpeed = barrelSpeed[warmed];
			if (playerInfo.haste)
				currenBarrelSpeed = hastebarrelSpeed[warmed];
			currentRotSpeed += currenBarrelSpeed * deltaTime;
			if (currentRotSpeed >= 360)
				currentRotSpeed -= 360;
		}
		return new Quaternion(Vector3.Left, currentRotSpeed);
	}
}
