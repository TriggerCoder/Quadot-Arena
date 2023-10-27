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
			putAway = true;
	}

	protected override void OnInit()
	{
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

		//Hitscan attack
		{
			Transform3D global = playerInfo.playerCamera.CurrentCamera.GlobalTransform;
			Vector3 d = global.Basis.Z;
			Vector2 r = Vector2.Zero;//GetDispersion();
			d += global.Basis.X * r.X + global.Basis.Y * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.CurrentCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | (~playerInfo.playerLayer)));
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.IntersectRay(RayCast);
			if (hit.ContainsKey("collider"))
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				Vector3 collision = (Vector3)hit["position"];
				Vector3 normal = (Vector3)hit["normal"];
				var BulletHit = (Node3D)ThingsManager.thingsPrefabs["BulletHit"].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletHit);
				BulletHit.Position = collision;
			}
		/*	if (Physics.Raycast(ray, out hit, maxRange, ~(GameManager.NoHit | (1 << playerInfo.playerLayer)), QueryTriggerInteraction.Ignore))
			{
				Damageable target = hit.collider.gameObject.GetComponent<Damageable>();
				if (target != null)
				{
					target.Damage(Random.Range(DamageMin, DamageMax + 1), DamageType.Generic, playerInfo.gameObject);

					if (target.Bleed)
					{
						GameObject blood;
						switch (target.BloodColor)
						{
							default:
							case BloodType.Red:
								blood = PoolManager.GetObjectFromPool("BloodRed");
								break;
							case BloodType.Green:
								blood = PoolManager.GetObjectFromPool("BloodGreen");
								break;
							case BloodType.Blue:
								blood = PoolManager.GetObjectFromPool("BloodBlue");
								break;
						}
						blood.transform.position = hit.point - ray.direction * .2f;
					}
					else
					{
						GameObject puff = PoolManager.GetObjectFromPool("BulletHit");
						puff.transform.position = hit.point - ray.direction * .2f;
					}
				}
				else
				{
					GameObject puff = PoolManager.GetObjectFromPool("BulletHit");
					puff.transform.position = hit.point - ray.direction * .2f;
					puff.transform.right = -hit.normal;
					puff.transform.Rotate(Vector3.right, Random.Range(0, 360));

					if (Sounds.Length > 3)
						AudioManager.Create3DSound(puff.transform.position, Sounds[Random.Range(3, Sounds.Length)], 5f, 1);

					//Check if collider can be marked
					if (!MapLoader.noMarks.Contains(hit.collider))
					{
						GameObject mark = PoolManager.GetObjectFromPool("BulletMark");
						mark.transform.position = hit.point + hit.normal * .05f;
						mark.transform.forward = hit.normal;
						mark.transform.Rotate(Vector3.forward, Random.Range(0, 360));
					}
					Debug.Log(hit.collider.name);
				}
			}
*/
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
