using Godot;
using System;

public partial class Projectile : Node3D
{
	public Node3D owner;
//	public GameObject fx;
	[Export]
	public string projectileName;
	[Export]
	public bool destroyAfterUse = true;
	[Export]
	public float _lifeTime = 1;
	[Export]
	public float speed = 4f;
	[Export]
	public int rotateSpeed = 0;
	[Export] 
	public int damageMin = 3;
	[Export]
	public int damageMax = 24;
	[Export]
	public int blastDamage = 0;
	[Export]
	public float projectileRadius = .2f;
	[Export]
	public float explosionRadius = 2f;
	[Export]
	public DamageType damageType = DamageType.Generic;
	[Export] 
	public float pushForce = 0f;
	[Export] 
	public Node3D OnDeathSpawn;
	[Export] 
	public string decalMark;
	[Export]
	public Node3D SecondaryOnDeathSpawn;
	[Export]
	public string _onFlySound;
	[Export]
	public string _onDeathSound;
	[Export]
	public MultiAudioStream audioStream;

	//Needed for homing projectires
	public Node3D target = null;
	const float capAngle = 16.875f;

	public bool goingUp = false;

	float time = 0f;
	public override void _Ready()
	{
		if (!string.IsNullOrEmpty(_onFlySound))
		{
			audioStream.Stream = SoundManager.LoadSound(_onFlySound, true);
			audioStream.Play();
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		time += deltaTime;

		if (time >= _lifeTime)
		{
			if (OnDeathSpawn != null)
			{
//				GameObject go = PoolManager.GetObjectFromPool(OnDeathSpawn.name);
//				go.transform.position = cTransform.position - cTransform.forward * .2f;
			}
			QueueFree();
			return;
		}
		//check for collision
		CollisionObject3D Hit = null;
		Vector3 Collision = Vector3.Zero;
		Vector3 Normal = Vector3.Zero;
		Vector3 d = GlobalTransform.Basis.Z;
		float nearest = float.MaxValue;
		{
			var Sphere = PhysicsServer3D.SphereShapeCreate();
			PhysicsServer3D.ShapeSetData(Sphere, projectileRadius);
			var SphereCast = new PhysicsShapeQueryParameters3D();
			SphereCast.ShapeRid = Sphere;
			SphereCast.CollisionMask = ~GameManager.NoHitMask;
			SphereCast.Motion = d * speed * deltaTime;
			SphereCast.Transform = GlobalTransform;
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.GetRestInfo(SphereCast);
			if (hit.ContainsKey("collider"))
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				GD.Print("We HIT SOMETHING! " + collider.Name);
				Vector3 collision = (Vector3)hit["point"];
				Vector3 normal = (Vector3)hit["normal"];

				GD.Print(collider.Name);
				float distance = GlobalPosition.DistanceSquaredTo(collision);
				if (distance < nearest)
				{
					nearest = distance;
					Hit = collider;
					Collision = collision;
					Normal = normal;
				}
/*
				if ((damageType == DamageType.Rocket) || (damageType == DamageType.Grenade) || (damageType == DamageType.Plasma) || (damageType == DamageType.BFGBall))
				{
					Vector3 impulseDir = dir.normalized;

					Damageable d = hit.collider.GetComponent<Damageable>();
					if (d != null)
					{
						switch (damageType)
						{
							case DamageType.BFGBall:
								d.Damage(Random.Range(damageMin, damageMax + 1) * 100, damageType, owner);
								d.Impulse(impulseDir, pushForce);
								break;
							default:
								d.Damage(Random.Range(damageMin, damageMax + 1), damageType, owner);
								d.Impulse(impulseDir, pushForce);
								break;
						}
					}
				}
				*/
			}
		}

		//explosion
		if (nearest < float.MaxValue)
		{
			var Sphere = PhysicsServer3D.SphereShapeCreate();
			PhysicsServer3D.ShapeSetData(Sphere, explosionRadius);
			var SphereCast = new PhysicsShapeQueryParameters3D();
			SphereCast.ShapeRid = Sphere;
			SphereCast.CollisionMask = GameManager.TakeDamageMask;
			SphereCast.Motion = Vector3.Zero;
			SphereCast.Transform = new Transform3D(GlobalTransform.Basis, GlobalTransform.Origin - d * nearest);
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.GetRestInfo(SphereCast);
			if (hit.ContainsKey("collider"))
			{
				float distance;
/*				Damageable d = hit.GetComponent<Damageable>();

				if (d != null)
				{
					Vector3 hPosition = hit.transform.position;
					Vector3 impulseDir = (hPosition - cPosition).normalized;

					switch (damageType)
					{
						case DamageType.Explosion:
						case DamageType.Rocket:
							distance = (hPosition - cPosition).magnitude;
							d.Damage(Mathf.CeilToInt(Mathf.Lerp(blastDamage, 1, distance / explosionRadius)), DamageType.Explosion, owner);
							d.Impulse(impulseDir, Mathf.Lerp(pushForce, 100, distance / explosionRadius));
							break;
						case DamageType.Plasma:
							if (hit.gameObject == owner) //Plasma never does self damage
								continue;
							else
								d.Damage(Random.Range(damageMin, damageMax + 1), damageType, owner);
							break;
						case DamageType.BFGBall:
							if (hit.gameObject == owner) //BFG never does self damage
								continue;
							else
								d.Damage(Random.Range(damageMin, damageMax + 1) * 100, damageType, owner);
							break;
						case DamageType.Telefrag:
							distance = (hPosition - cPosition).magnitude;
							d.Damage(blastDamage, DamageType.Telefrag, owner);
							d.Impulse(impulseDir, Mathf.Lerp(pushForce, 100, distance / explosionRadius));
							break;
						default:
							d.Damage(Random.Range(damageMin, damageMax + 1), damageType, owner);
							break;
					}
				}
				*/
			}

			if (OnDeathSpawn != null)
			{
//				GameObject go = PoolManager.GetObjectFromPool(OnDeathSpawn.name);
//				go.transform.position = cTransform.position - cTransform.forward * .2f;
			}

			//Check if collider can be marked
			if (Hit != null)
			{
				if (!MapLoader.noMarks.Contains(Hit))
				{
					Node3D BulletMark = (Node3D)ThingsManager.thingsPrefabs["BulletMark"].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletMark);
					BulletMark.Position = Collision + (d * .05f);
					BulletMark.LookAt(Collision - Normal, Vector3.Up);
					BulletMark.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				}
			}
			/*
			if (damageType == DamageType.BFGBall)
			{
				PlayerInfo playerInfo = owner.GetComponent<PlayerInfo>();
				if (playerInfo != null)
				{
					Camera rayCaster = playerInfo.playerCamera.SkyholeCamera;
					Quaternion Camerarotation;
					RaycastHit[] hitRays = new RaycastHit[3];
					Ray r;
					int numRay = 0;
					int index = 0;
					Vector3 dir = cTransform.forward;
					dir.y = 0;
					Camerarotation = rayCaster.transform.rotation;
					rayCaster.transform.rotation = Quaternion.LookRotation(dir);
					for (int k = 0; (k <= BFGTracers.samples) && (numRay <= 40); k++)
					{
						r = rayCaster.ViewportPointToRay(new Vector3(BFGTracers.hx[index], BFGTracers.hy[index], 0f));
						index++;
						if (index >= BFGTracers.pixels)
							index = 0;
						int max = Physics.RaycastNonAlloc(r, hitRays, 300, GameManager.TakeDamageMask, QueryTriggerInteraction.Ignore);
						if (max > hitRays.Length)
							max = hitRays.Length;
						for (int i = 0; i < max; i++)
						{
							GameObject hit = hitRays[i].collider.gameObject;
							Damageable d = hit.GetComponent<Damageable>();
							if (d != null)
							{
								if (hit == owner)
									continue;

								while ((d.Dead == false) && (numRay <= 40))
								{
									d.Damage(Random.Range(49, 88), DamageType.BFGBlast, owner);
									d.Impulse(r.direction, pushForce);
									numRay++;
								}
								GameObject go = PoolManager.GetObjectFromPool(SecondaryOnDeathSpawn.name);
								go.transform.position = hit.transform.position + hit.transform.up * 0.5f;
							}
						}
					}
					rayCaster.transform.rotation = Camerarotation;
				}
			}
			*/
			if (!string.IsNullOrEmpty(_onDeathSound))
				SoundManager.Create3DSound(Collision, SoundManager.LoadSound(_onDeathSound));
			QueueFree();
			return;
		}
		/*
		if (target != null)
		{
			Vector3 aimAt = (target.transform.position - cTransform.position).normalized;
			float angle = Vector3.SignedAngle(aimAt, cTransform.forward, cTransform.up);
			if (Mathf.Abs(angle) > capAngle)
			{
				Quaternion newRot;
				if (angle > 0)
					newRot = Quaternion.AngleAxis(capAngle, cTransform.up);
				else
					newRot = Quaternion.AngleAxis(-capAngle, cTransform.up);
				aimAt = (newRot * cTransform.forward).normalized;
			}
			cTransform.forward = aimAt;
		}

		if (rotateSpeed != 0)
			cTransform.RotateAround(cTransform.position, cTransform.forward, rotateSpeed * deltaTime);

		if (goingUp)
			cTransform.position = cTransform.position + cTransform.up * speed * deltaTime;
		else
		*/
			Position -= d * speed * deltaTime;

	}
}
