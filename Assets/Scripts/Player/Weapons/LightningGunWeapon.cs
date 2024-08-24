using Godot;
using System;
using ExtensionMethods;

public partial class LightningGunWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return .02f; } }
	public override float horizontalDispersion { get { return .02f; } }

	public string onDeathSpawn = "LightningExplosion";
	public string decalMark = "LightningMark";
	public float maxRange = 24f;
	public Vector3 spawnPos;
	[Export]
	public Node3D boltOrigin;
	[Export]
	public LightningBolt lightningBolt;
	[Export]
	public MultiAudioStream humStream;
	[Export]
	public string[] _humSounds;
	public AudioStream[] humSounds;
	private Quaternion baseRotation;

	private Node3D avatarboltOrigin;
	private LightningBolt avatarLightningBolt;
	private Vector3 destination = Vector3.Zero;
	public enum CurrentHum
	{
		None,
		Idle,
		Fire
	}

	private CurrentHum currentHum = CurrentHum.None;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.lightningAmmo] <= 0 && fireTime < .1f)
		{
			if ((!putAway) && (Sounds.Length > 1))
			{
				audioStream.Stream = Sounds[1];
				audioStream.Play();
			}
			putAway = true;
		}

		if (fireTime <= 0)
		{
			if (currentHum != CurrentHum.Idle)
			{
				humStream.Stream = humSounds[0];
				humStream.Play();
				currentHum = CurrentHum.Idle;
			}

			if (boltOrigin.Visible)
				boltOrigin.Hide();

			if (!putAway)
				if (avatarboltOrigin.Visible)
				{
					avatarboltOrigin.Hide();
					boltOrigin.Quaternion = baseRotation;
				}
		}
		else if (!putAway)
			avatarboltOrigin.LookAt(destination);

	}
	protected override void OnInit()
	{
		if (Sounds.Length > 2)
		{
			audioStream.Stream = Sounds[2];
			audioStream.Play();
		}

		humSounds = new AudioStream[_humSounds.Length];
		for (int i = 0; i < _humSounds.Length; i++)
			humSounds[i] = SoundManager.LoadSound(_humSounds[i], true);

		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.lightningAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.lightningAmmo]);
		lightningBolt.SetArcsLayers(playerInfo.uiLayer);

		humStream.Stream = humSounds[0];
		humStream.Play();
		currentHum = CurrentHum.Idle;

		baseRotation = boltOrigin.Quaternion;

		avatarboltOrigin = new Node3D();
		avatarboltOrigin.Name = "BoltOrigin";
		avatarLightningBolt = (LightningBolt)ThingsManager.thingsPrefabs["LightningBolt"].Instantiate();
		avatarboltOrigin.AddChild(avatarLightningBolt);
		playerInfo.playerThing.avatar.AddLightningBolt(avatarboltOrigin);
	}
	public override bool Fire()
	{
		if (putAway)
			return false;

		if (playerInfo.playerThing.waterLever > 1)
		{
			if (playerInfo.playerThing.currentWaterSurface != null)
			{
				playerInfo.playerThing.currentWaterSurface.ElectroShockDischarge(playerInfo.playerThing);
				boltOrigin.Hide();
				avatarboltOrigin.Hide();
				return false;
			}
		}

		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.lightningAmmo] <= 0)
			return false;

		playerInfo.Ammo[PlayerInfo.lightningAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.lightningAmmo]);

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

		if (fireTime <= 0)
		{
			if (Sounds.Length > 0)
			{
				audioStream.Stream = Sounds[0];
				audioStream.Play();
			}

			if (hasQuad)
				SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(quadSound));

			if (currentHum != CurrentHum.Fire)
			{
				humStream.Stream = humSounds[1];
				humStream.Play();
				currentHum = CurrentHum.Fire;
			}
			boltOrigin.Show();
			avatarboltOrigin.Show();
		}
		//maximum fire rate 20/s, unless you use negative number (please don't)
		fireTime = _fireRate + .05f;
		coolTimer = 0f;

		//Hitscan attack
		{
			Transform3D global = playerInfo.playerCamera.GlobalTransform;
			Vector3 d = global.ForwardVector();
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | (1 << GameManager.RagdollLayer) | (1 << GameManager.WaterLayer) | GameManager.TakeDamageMask & ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.RagdollLayer))));
			if (MapLoader.waterSurfaces.Count > 0)
				RayCast.CollideWithAreas = true;
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.IntersectRay(RayCast);
			if (hit.Count > 0)
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				Vector3 collision = (Vector3)hit["position"];
				Vector3 normal = (Vector3)hit["normal"];

				if (collider is Damageable damageable)
				{
					if (hasQuad)
						damageable.Damage(GD.RandRange(DamageMin * GameManager.Instance.QuadMul, DamageMax * GameManager.Instance.QuadMul), DamageType.Lightning, playerInfo.playerThing);
					else
						damageable.Damage(GD.RandRange(DamageMin, DamageMax), DamageType.Lightning, playerInfo.playerThing);

					if (damageable.Bleed)
					{
						Node3D Blood = (Node3D)ThingsManager.thingsPrefabs[ThingsManager.Blood].Instantiate();
						GameManager.Instance.TemporaryObjectsHolder.AddChild(Blood);
						Blood.GlobalPosition = collision + (normal * .05f);
					}
				}

				boltOrigin.LookAt(collision);
				lightningBolt.SetBoltMesh(boltOrigin.GlobalPosition, collision);
				avatarboltOrigin.LookAt(collision);
				avatarLightningBolt.SetBoltMesh(avatarboltOrigin.GlobalPosition, collision);
				destination = collision;

				Node3D LightningExplosion = (Node3D)ThingsManager.thingsPrefabs[onDeathSpawn].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(LightningExplosion);
				LightningExplosion.Position = collision + (normal * .2f);
				LightningExplosion.SetForward(normal);
				LightningExplosion.Rotate(LightningExplosion.UpVector(), -Mathf.Pi * .5f);
				LightningExplosion.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));

				if (Sounds.Length > 3)
					SoundManager.Create3DSound(collision, Sounds[GD.RandRange(3, Sounds.Length - 1)]);

				if (CheckIfCanMark(SpaceState, collider, collision))
				{
					SpriteController ElectricMark = (SpriteController)ThingsManager.thingsPrefabs[decalMark].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(ElectricMark);
					ElectricMark.GlobalPosition = collision + (normal * .05f);
					ElectricMark.SetForward(normal);
					ElectricMark.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
					if (collider is Crusher)
						ElectricMark.referenceNode = collider;
				}
			}
			else
			{
				boltOrigin.Quaternion = baseRotation;
				lightningBolt.SetBoltLenght(maxRange);
				avatarboltOrigin.LookAt(End);
				avatarLightningBolt.SetBoltLenght(maxRange);
				destination = End;
			}
		}
		return true;
	}
}