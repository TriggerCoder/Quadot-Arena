using Godot;
using System;
using ExtensionMethods;

public partial class MachineGunWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .017f; } } // tan(2) / 2
	public override float maxDispersion { get { return .049f; } } // tan(5.6) / 2

	public string caseName = "MachineGunAmmoCase";
	public string onDeathSpawn = "BulletHit";
	public string decalMark = "BulletMark";
	public float maxRange = 400f;

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
		
		if (hasQuad)
			SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(quadSound));

		//Hitscan attack
		{
			Transform3D global = playerInfo.playerCamera.GlobalTransform;
			Vector3 d = global.ForwardVector();
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.RagdollLayer))));
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.IntersectRay(RayCast);
			if (hit.Count > 0)
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				Vector3 collision = (Vector3)hit["position"];
				Vector3 normal = (Vector3)hit["normal"];
				Node3D BulletHit = (Node3D)ThingsManager.thingsPrefabs[onDeathSpawn].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletHit);
				BulletHit.Position = collision + (normal * .2f);
				BulletHit.SetForward(normal);
				BulletHit.Rotate(BulletHit.UpVector(), -Mathf.Pi * .5f);
				BulletHit.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				if (Sounds.Length > 3)
					SoundManager.Create3DSound(collision, Sounds[GD.RandRange(3, Sounds.Length - 1)]);

				if (CheckIfCanMark(SpaceState, collider, collision))
				{
					Node3D BulletMark = (Node3D)ThingsManager.thingsPrefabs[decalMark].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletMark);
					BulletMark.Position = collision + (normal * .05f);
					BulletMark.SetForward(-normal);
					BulletMark.Rotate((BulletMark.UpVector()).Normalized(), -Mathf.Pi * .5f);
					BulletMark.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
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
