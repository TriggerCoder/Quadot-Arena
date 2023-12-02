using Godot;
using System;

public partial class ShotgunWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .049f; } } // tan(4) / 2
	public override float maxDispersion { get { return .062f; } } //tan(7.1) / 2

	public string caseName = "ShotgunShell";
	public string onDeathSpawn = "BulletHit";
	public string decalMark = "ShotMark";

	public float vDispersion = .7f;
	public float maxRange = 400f;
	public float pushForce = 350;
	protected override void OnUpdate()
	{
		if (playerInfo.Ammo[1] <= 0 && fireTime < .1f)
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
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[1] <= 0)
			return false;

		playerInfo.Ammo[1]--;

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
		fireTime = _fireRate + .05f;
		coolTimer = 0f;

		if (Sounds.Length > 0)
		{
			audioStream.Stream = Sounds[0];
			audioStream.Play();
		}

		//Hitscan attack
		Transform3D global = playerInfo.playerCamera.CurrentCamera.GlobalTransform;
		Vector3 d = global.Basis.Z;

		for (int i = 0; i < 11; i++)
		{
			Vector2 r = GetDispersion();
			d += global.Basis.X * r.X + global.Basis.Y * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.CurrentCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.RagdollLayer))));
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.IntersectRay(RayCast);
			if (hit.ContainsKey("collider"))
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				Vector3 collision = (Vector3)hit["position"];
				Vector3 normal = (Vector3)hit["normal"];
				Node3D BulletHit = (Node3D)ThingsManager.thingsPrefabs[onDeathSpawn].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletHit);
				BulletHit.Position = collision + (normal * .2f);
				if (Mathf.IsZeroApprox(normal.Dot(Vector3.Forward)))
					BulletHit.Rotation = Transform3D.Identity.LookingAt(normal, Vector3.Forward).Basis.GetEuler();
				else
					BulletHit.Rotation = Transform3D.Identity.LookingAt(normal, Vector3.Up).Basis.GetEuler();
				BulletHit.Rotate(BulletHit.Basis.Y, -Mathf.Pi * .5f);
				BulletHit.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				if (Sounds.Length > 3)
					SoundManager.Create3DSound(collision, Sounds[GD.RandRange(3, Sounds.Length - 1)]);

				if (!MapLoader.noMarks.Contains(collider))
				{
					Node3D BulletMark = (Node3D)ThingsManager.thingsPrefabs[decalMark].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletMark);
					BulletMark.Position = collision + (normal * .05f);
					if (Mathf.IsZeroApprox(normal.Dot(Vector3.Forward)))
						BulletMark.Rotation = Transform3D.Identity.LookingAt(-normal, Vector3.Forward).Basis.GetEuler();
					else
						BulletMark.Rotation = Transform3D.Identity.LookingAt(-normal, Vector3.Up).Basis.GetEuler();
					BulletMark.Rotate((BulletMark.Basis.Y).Normalized(), -Mathf.Pi * .5f);
					BulletMark.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
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
