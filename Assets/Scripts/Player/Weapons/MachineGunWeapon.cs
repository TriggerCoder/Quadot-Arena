using Godot;
using System;
using System.Diagnostics;

public partial class MachineGunWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .017f; } } // tan(2) / 2
	public override float maxDispersion { get { return .049f; } } // tan(5.6) / 2

	public string caseName;

	public float maxRange = 400f;
	[Export]
	public float barrelSpeed = 10;

	private float currentRotSpeed = 0;

	protected override void OnUpdate()
	{
		if (playerInfo.Ammo[0] <= 0 && fireTime < .1f)
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

		if (playerInfo.Ammo[0] <= 0)
			return false;

		playerInfo.Ammo[0]--;

		if (GameOptions.UseMuzzleLight)
		{
			if (muzzleLight != null)
			{
				muzzleLight.LightEnergy = 1.0f;
				if (muzzleObject != null)
					if (!muzzleObject.Visible)
					{
						muzzleObject.Visible = true;
//						playerInfo.playerThing.avatar.MuzzleFlashSetActive(true);
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
		{
			Transform3D global = playerInfo.playerCamera.CurrentCamera.GlobalTransform;
			Vector3 d = global.Basis.Z;
			Vector2 r = GetDispersion();
			d += global.Basis.X * r.X + global.Basis.Y * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.CurrentCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer))));
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.IntersectRay(RayCast);
			if (hit.ContainsKey("collider"))
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				Vector3 collision = (Vector3)hit["position"];
				Vector3 normal = (Vector3)hit["normal"];
				Node3D BulletHit = (Node3D)ThingsManager.thingsPrefabs["BulletHit"].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletHit);
				BulletHit.Position = collision + (d * .2f);
				BulletHit.LookAt(collision + normal, Vector3.Up);
				BulletHit.Rotate(BulletHit.Basis.Y, -Mathf.Pi * .5f);
				BulletHit.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				if (Sounds.Length > 3)
					SoundManager.Create3DSound(collision, Sounds[GD.RandRange(3, Sounds.Length - 1)]);

				if (!MapLoader.noMarks.Contains(collider))
				{
					Node3D BulletMark = (Node3D)ThingsManager.thingsPrefabs["BulletMark"].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletMark);
					BulletMark.Position = collision + (d * .05f);
					BulletMark.LookAt(collision - normal, Vector3.Up);
					BulletMark.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				}
			}
		}
		return true;
	}
	protected override Quaternion GetRotate(float deltaTime)
	{
		if (fireTime > 0f)
		{
			currentRotSpeed += barrelSpeed * deltaTime;
			if (currentRotSpeed >= 360)
				currentRotSpeed -= 360;
		}
		return new Quaternion(Vector3.Left, currentRotSpeed);
	}
}
