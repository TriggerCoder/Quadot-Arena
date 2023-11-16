using Godot;
using System;

public partial class Projectile : Node3D
{
	public Node3D owner;
	[Export]
	public ParticlesController fx;
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
	public string OnDeathSpawn;
	[Export] 
	public string decalMark;
	[Export]
	public string secondaryMark;
	[Export]
	public string SecondaryOnDeathSpawn;
	[Export]
	public string _onFlySound;
	[Export]
	public string _onDeathSound;
	[Export]
	public MultiAudioStream audioStream;

	public uint ignoreSelfLayer = 0;

	//Needed for homing projectires
	public Node3D target = null;
	const float capAngle = 16.875f;

	public bool goingUp = false;

	float time = 0f;
	private Rid Sphere;
	private PhysicsShapeQueryParameters3D SphereCast;
	public override void _Ready()
	{
		if (!string.IsNullOrEmpty(_onFlySound))
		{
			audioStream.Stream = SoundManager.LoadSound(_onFlySound, true);
			audioStream.Play();
		}
		Sphere = PhysicsServer3D.SphereShapeCreate();
		SphereCast = new PhysicsShapeQueryParameters3D();
		SphereCast.ShapeRid = Sphere;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		time += deltaTime;

		if (time >= _lifeTime)
		{
			if (!string.IsNullOrEmpty(OnDeathSpawn))
			{
//				GameObject go = PoolManager.GetObjectFromPool(OnDeathSpawn.name);
//				go.transform.position = cTransform.position - cTransform.forward * .2f;
			}
			if (!string.IsNullOrEmpty(_onDeathSound))
				SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(_onDeathSound));
			QueueFree();
			return;
		}

		CollisionObject3D Hit = null;
		Vector3 Collision = Vector3.Zero;
		Vector3 Normal = Vector3.Zero;
		Vector3 d = GlobalTransform.Basis.Z;

		var SpaceState = GetWorld3D().DirectSpaceState;
		//check for collision
		{			
			PhysicsServer3D.ShapeSetData(Sphere, projectileRadius);
			SphereCast.CollisionMask = ~(GameManager.NoHitMask | ignoreSelfLayer);
			SphereCast.Motion = -d * speed * deltaTime;
			SphereCast.Transform = GlobalTransform;
			var result = SpaceState.CastMotion(SphereCast);

			if (result[1] < 1)
			{
				SphereCast.Transform = new Transform3D(GlobalTransform.Basis, GlobalTransform.Origin + (SphereCast.Motion * result[1]));
				var hit = SpaceState.GetRestInfo(SphereCast);
				if (hit.ContainsKey("collider_id"))
				{
					CollisionObject3D collider = (CollisionObject3D)InstanceFromId((ulong)hit["collider_id"]);
					if (collider != owner)
					{
						Collision = (Vector3)hit["point"];
						Normal = (Vector3)hit["normal"];
						Hit = collider;

						if ((damageType == DamageType.Rocket) || (damageType == DamageType.Grenade) || (damageType == DamageType.Plasma) || (damageType == DamageType.BFGBall))
						{
							Vector3 impulseDir = d.Normalized();

							if (Hit is Damageable damageable)
							{
								switch (damageType)
								{
									case DamageType.BFGBall:
										damageable.Damage(GD.RandRange(damageMin, damageMax) * 100, damageType, owner);
										damageable.Impulse(impulseDir, pushForce);
										break;
									default:
										damageable.Damage(GD.RandRange(damageMin, damageMax), damageType, owner);
										damageable.Impulse(impulseDir, pushForce);
										break;
								}
							}
						}
					}
				}
			}
		}

		//explosion
		if (Hit != null)
		{
			PhysicsServer3D.ShapeSetData(Sphere, explosionRadius);
			SphereCast.CollisionMask = GameManager.TakeDamageMask;
			SphereCast.Motion = Vector3.Zero;
			SphereCast.Transform = new Transform3D(GlobalTransform.Basis, Collision);
			var hits = SpaceState.IntersectShape(SphereCast);
			var max = hits.Count;

			for (int i = 0; i < max; i++)
			{
				var hit = hits[i];
				if (!hit.ContainsKey("collider"))
					continue;
				
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];

				if (collider is Damageable damageable)
				{
					Vector3 hPosition = collider.Position;
					Vector3 Distance = (hPosition - Collision);
					Vector3 impulseDir = Distance.Normalized();
					float lenght;
					switch (damageType)
					{
						case DamageType.Explosion:
						case DamageType.Rocket:
							lenght = Distance.Length();
							damageable.Damage(Mathf.CeilToInt(Mathf.Lerp(blastDamage, 1, lenght / explosionRadius)), DamageType.Explosion, owner);
							damageable.Impulse(impulseDir, Mathf.Lerp(pushForce, 100, lenght / explosionRadius));
						break;
						case DamageType.Plasma:
							if (collider == owner) //Plasma never does self damage
								continue;
							else
								damageable.Damage(GD.RandRange(damageMin, damageMax), damageType, owner);
						break;
						case DamageType.BFGBall:
							if (collider == owner) //BFG never does self damage
								continue;
							else
								damageable.Damage(GD.RandRange(damageMin, damageMax) * 100, damageType, owner);
						break;
						case DamageType.Telefrag:
							lenght = Distance.Length();
							damageable.Damage(blastDamage, DamageType.Telefrag, owner);
							damageable.Impulse(impulseDir, Mathf.Lerp(pushForce, 100, lenght / explosionRadius));
						break;
						default:
							damageable.Damage(GD.RandRange(damageMin, damageMax), damageType, owner);
						break;
					}
				}
			}

			if (!string.IsNullOrEmpty(OnDeathSpawn))
			{
				Node3D DeathSpawn = (Node3D)ThingsManager.thingsPrefabs[OnDeathSpawn].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(DeathSpawn);
				DeathSpawn.Position = Collision + d;
				DeathSpawn.LookAt(Collision - Normal, Vector3.Up);
				DeathSpawn.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				if (fx != null)
				{
					fx.Reparent(DeathSpawn);
					fx.enableLifeTime = true;
				}
			}

				//Check if collider can be marked
			if (!MapLoader.noMarks.Contains(Hit))
			{
				Node3D DecalMark = (Node3D)ThingsManager.thingsPrefabs[decalMark].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(DecalMark);
				DecalMark.Position = Collision + (d * .05f);
				DecalMark.LookAt(Collision - Normal, Vector3.Up);
				DecalMark.Rotate((DecalMark.Basis.Y).Normalized(), -Mathf.Pi * .5f);
				DecalMark.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				if (!string.IsNullOrEmpty(secondaryMark))
				{
					Node3D SecondMark = (Node3D)ThingsManager.thingsPrefabs[secondaryMark].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(SecondMark);
					SecondMark.Position = Collision + (d * .03f);
					SecondMark.LookAt(Collision - Normal, Vector3.Up);
					SecondMark.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				}
			}
			/*
			if (damageType == DamageType.BFGBall)
			{
				PlayerInfo playerInfo = owner.GetComponent<PlayerInfo>();
				if (playerInfo != null)
				{
					Camera rayCaster = playerInfo.playerCamera.ViewCamera;
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
