using Godot;
using System.Collections.Generic;
using ExtensionMethods;

public partial class RailgunWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return .005f; } }
	public override float horizontalDispersion { get { return .01f; } }

	public string onDeathSpawn = "RailTrail";
	public string explosionFx = "RailExplosion";
	public string decalMark = "RailMark";

	public float maxRange = 400f;
	public Color modulate;
	[Export]
	public MultiAudioStream humStream;
	[Export]
	public string _humSound;
	[Export]
	public float pushForce = 0f;

	public Color white = Colors.White;
	private List<MeshInstance3D> modulateMeshes;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.slugAmmo] <= 0 && fireTime < .1f)
		{
			if ((!putAway) && (Sounds.Length > 1))
			{
				audioStream.Stream = Sounds[1];
				audioStream.Play();
			}
			putAway = true;

		}

		if (putAway)
			return;

		if (fireTime >= 0)
		{
			Color color = modulate.Lerp(white, fireTime);
			for (int i = 0; i < modulateMeshes.Count; i++)
				modulateMeshes[i].SetInstanceShaderParameter("modulate", color);
		}
	}

	protected override void OnInit()
	{
		if (Sounds.Length > 2)
		{
			audioStream.Stream = Sounds[2];
			audioStream.Play();
		}

		humStream.Stream = SoundManager.LoadSound(_humSound, true);
		humStream.Play();

		modulate = playerInfo.playerThing.modulate;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.slugAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.slugAmmo]);

		modulateMeshes = GameManager.GetModulateMeshes(playerInfo.WeaponHand, fxMeshes);
		modulateMeshes.AddRange(playerInfo.playerThing.avatar.GetWeaponModulateMeshes());
		for (int i = 0; i < modulateMeshes.Count; i++)
			modulateMeshes[i].SetInstanceShaderParameter("modulate", modulate);

		List<MeshInstance3D> removeList = GameManager.GetModulateMeshes(muzzleObject, fxMeshes);
		removeList.AddRange(playerInfo.playerThing.avatar.GetWeaponModulateMeshes(true));

		for (int i = 0; i < modulateMeshes.Count; i++)
		{
			MeshInstance3D testMesh = modulateMeshes[i];
			if (removeList.Contains(testMesh))
				modulateMeshes.Remove(testMesh);
		}
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.slugAmmo] <= 0)
			return false;

		playerInfo.Ammo[PlayerInfo.slugAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.slugAmmo]);

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

		for (int i = 0; i < modulateMeshes.Count; i++)
			modulateMeshes[i].SetInstanceShaderParameter("modulate", white);
		//Hitscan attack
		{
			Transform3D global = playerInfo.playerCamera.GlobalTransform;
			Vector3 d = global.ForwardVector();
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | (1 << GameManager.RagdollLayer) | GameManager.TakeDamageMask & ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.RagdollLayer))));
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
					Vector3 Distance = (collider.GlobalPosition - global.Origin);
					float lenght;
					Vector3 impulseDir = Distance.GetLenghtAndNormalize(out lenght);
					int Damage = GD.RandRange(DamageMin, DamageMax);
					Damage += Mathf.CeilToInt(Mathf.Lerp(0, DamageMax, lenght / maxRange));
					if (hasQuad)
						Damage *= GameManager.Instance.QuadMul;

					damageable.Impulse(impulseDir, pushForce);
					damageable.Damage(Damage, DamageType.Rail, playerInfo.playerThing);

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
					SpriteController RailHit = (SpriteController)ThingsManager.thingsPrefabs[explosionFx].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(RailHit);
					RailHit.Position = collision + (normal * .1f);
					RailHit.SetForward(normal);
					RailHit.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				}

				RailTrail railTrail = (RailTrail)ThingsManager.thingsPrefabs[onDeathSpawn].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(railTrail);
				railTrail.Init(muzzleObject.GlobalPosition, collision, modulate);
				railTrail.Position = muzzleObject.GlobalPosition.Lerp(collision, 0.5f);
				railTrail.LookAt(collision);

				if (CheckIfCanMark(SpaceState, collider, collision))
				{
					SpriteController RailMark = (SpriteController)ThingsManager.thingsPrefabs[decalMark].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(RailMark);
					RailMark.GlobalPosition = collision + (normal * .05f);
					RailMark.SetForward(normal);
					RailMark.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
					if (collider is Crusher)
						RailMark.referenceNode = collider;
					if (RailMark.GetChildCount() > 0)
					{
						Node child = RailMark.GetChild(0);
						if (child is Light3D light)
							light.LightColor = modulate;
					}
				}
			}
			else
			{
				RailTrail railTrail = (RailTrail)ThingsManager.thingsPrefabs[onDeathSpawn].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(railTrail);
				railTrail.Init(muzzleObject.GlobalPosition, End, modulate);
				railTrail.Position = muzzleObject.GlobalPosition.Lerp(End, 0.5f);
				railTrail.LookAt(End);
			}

		}
		return true;
	}
}
