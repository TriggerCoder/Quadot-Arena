using Godot;
using System.Collections.Generic;
using ExtensionMethods;

public partial class BFGBall : Projectile
{
	[Export]
	public Node3D[] boltOrigin;
	[Export]
	PackedScene Bolt;
	public List<LightningBolt> lightningBolt = new List<LightningBolt>();
	[Export]
	public int electricDamageMin = 6;
	[Export]
	public int electricDamageMax = 9;
	[Export]
	public float lightningRadius = 24f;
	[Export]
	public float electricDamageRate = .05f;
	[Export]
	public string[] _humSounds;
	public AudioStream[] humSounds;

	private float damageTime = 0;
	private Color Green = new Color(0x006158FF);
	private Rid ElectricSphere;
	private PhysicsShapeQueryParameters3D ElectricSphereCast;
	private Rid ExplosionSphere;
	private PhysicsShapeQueryParameters3D ExplosionSphereCast;

	public enum CurrentHum
	{
		None,
		Idle,
		Fire
	}

	private CurrentHum currentHum = CurrentHum.None;

	protected override void OnInit()
	{
		humSounds = new AudioStream[_humSounds.Length];
		for (int i = 0; i < _humSounds.Length; i++)
			humSounds[i] = SoundManager.LoadSound(_humSounds[i], true);

		audioStream.Stream = humSounds[0];
		audioStream.Play();
		currentHum = CurrentHum.Idle;

		LightningBolt lightBolt = (LightningBolt)Bolt.Instantiate();
		lightBolt.SetArcsColors(Green);
		lightBolt.SetArcsLayers(GameManager.AllPlayerViewMask);
		lightningBolt.Add(lightBolt);
		boltOrigin[0].AddChild(lightningBolt[0]);

		ElectricSphere = PhysicsServer3D.SphereShapeCreate();
		ElectricSphereCast = new PhysicsShapeQueryParameters3D();
		ElectricSphereCast.ShapeRid = ElectricSphere;
		PhysicsServer3D.ShapeSetData(ElectricSphere, lightningRadius);

		ExplosionSphere = PhysicsServer3D.SphereShapeCreate();
		ExplosionSphereCast = new PhysicsShapeQueryParameters3D();
		ExplosionSphereCast.ShapeRid = ExplosionSphere;
		PhysicsServer3D.ShapeSetData(ExplosionSphere, explosionRadius);
	}

	protected override void OnEnableQuad()
	{
		electricDamageMin *= GameManager.Instance.QuadMul;
		electricDamageMax *= GameManager.Instance.QuadMul;
	}

	protected override void OnPhysicsUpdate(float deltaTime, PhysicsDirectSpaceState3D SpaceState)
	{
		if (damageTime > 0)
			damageTime -= deltaTime;

		ElectricSphereCast.CollisionMask = GameManager.TakeDamageMask | (1 << GameManager.RagdollLayer);
		ElectricSphereCast.Motion = Vector3.Zero;
		ElectricSphereCast.Transform = GlobalTransform;
		var hits = SpaceState.IntersectShape(ElectricSphereCast);
		var max = hits.Count;

		int damaged = 0;
		for (int i = 0; (i < max) && (damaged < boltOrigin.Length); i++)
		{
			var hit = hits[i];

			CollisionObject3D posibleCollider = (CollisionObject3D)hit["collider"];
			if (posibleCollider == owner)
				continue;

			if (posibleCollider is Damageable)
			{
				Vector3 collision = posibleCollider.GlobalPosition;
				var RayCast = PhysicsRayQueryParameters3D.Create(GlobalPosition, collision, ((1 << GameManager.ColliderLayer) | (1 << GameManager.RagdollLayer) | GameManager.TakeDamageMask));
				var check = SpaceState.IntersectRay(RayCast);
				if (check.Count > 0)
				{
					CollisionObject3D collider = (CollisionObject3D)check["collider"];
					if (collider == owner)
						continue;

					if (collider is PhysicProjectile)
						continue;

					if (collider is Damageable damageable)
					{
						boltOrigin[damaged].Show();
						boltOrigin[damaged].LookAt(collision);
						if (damaged == lightningBolt.Count)
						{
							LightningBolt lightBolt = (LightningBolt)Bolt.Instantiate();
							lightBolt.SetArcsColors(Green);
							lightBolt.SetArcsLayers(GameManager.AllPlayerViewMask);
							lightningBolt.Add(lightBolt);
							boltOrigin[damaged].AddChild(lightningBolt[damaged]);
						}
						lightningBolt[damaged].SetBoltMesh(GlobalPosition, collision);
						if (damageTime <= 0)
							damageable.Damage(GD.RandRange(electricDamageMin, electricDamageMax), DamageType.Lightning, owner);
						damaged++;
					}
				}
			}

		}
		if (damaged > 0)
		{
			if (damageTime <= 0)
				damageTime = electricDamageRate + .05f;
			if (currentHum != CurrentHum.Fire)
			{
				audioStream.Stream = humSounds[1];
				audioStream.Play();
				currentHum = CurrentHum.Fire;
			}
			for (int i = damaged; i < boltOrigin.Length; i++)
				boltOrigin[i].Hide();
		}
		else
		{
			if (currentHum != CurrentHum.Idle)
			{
				audioStream.Stream = humSounds[0];
				audioStream.Play();
				currentHum = CurrentHum.Idle;
			}
			for (int i = 0; i < boltOrigin.Length; i++)
				boltOrigin[i].Hide();
		}
	}
	protected override void OnCollision(Vector3 collision, Vector3 normal, Vector3 direction, CollisionObject3D collider)
	{
		if (collider is Damageable damageable)
		{
			Vector3 impulseDir = direction.Normalized();
			damageable.Impulse(impulseDir, pushForce);
			damageable.Damage(GD.RandRange(damageMin, damageMax) * 100, DamageType.BFGBall, owner);
		}
	}
	protected override void OnExplosion(Vector3 Collision, Vector3 direction, PhysicsDirectSpaceState3D SpaceState)
	{
		ExplosionSphereCast.CollisionMask = GameManager.TakeDamageMask | (1 << GameManager.RagdollLayer);
		ExplosionSphereCast.Motion = Vector3.Zero;
		ExplosionSphereCast.Transform = new Transform3D(GlobalTransform.Basis, Collision);
		var hits = SpaceState.IntersectShape(ExplosionSphereCast);
		var max = hits.Count;

		for (int i = 0; i < max; i++)
		{
			var hit = hits[i];

			CollisionObject3D collider = (CollisionObject3D)hit["collider"];
			if (collider is Damageable damageable)
			{
				//BFG never does self damage
				if (collider == owner)
					continue;

				Vector3 collision = collider.GlobalPosition;
				bool collide = false;
				if ((collider.CollisionLayer & (1 << GameManager.ColliderLayer)) == 0)
				{
					var RayCast = PhysicsRayQueryParameters3D.Create(GlobalPosition, collision, (1 << GameManager.ColliderLayer));
					var check = SpaceState.IntersectRay(RayCast);
					if (check.Count == 0)
						collide = true;
				}
				else
					collide = true;

				if (collide)
					damageable.Damage(GD.RandRange(damageMin, damageMax) * 100, DamageType.BFGBall, owner);
			}
		}

		PlayerInfo playerInfo = ((PlayerThing)owner).playerInfo;
		if (playerInfo != null)
		{
			Camera3D rayCaster = playerInfo.playerCamera.ViewCamera;
			Quaternion Camerarotation;
			Vector3 dir = direction;
			dir.Y = 0;
			int numRay = 0;
			Camerarotation = rayCaster.Quaternion;
			rayCaster.LookAt(playerInfo.playerCamera.GlobalPosition - dir);
			for (int index = 0; (index < BFGTracers.samples) && (numRay < 40); index++)
			{
				Vector3 Origin = playerInfo.playerCamera.GlobalPosition;
				dir = rayCaster.ProjectRayNormal(new Vector2(BFGTracers.hx[index] * 1280, BFGTracers.hy[index] * 720));
				Vector3 End = Origin + dir * 300;
				var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | GameManager.TakeDamageMask & ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.RagdollLayer))));
				var rayhit = SpaceState.IntersectRay(RayCast);
				if (rayhit.Count > 0)
				{
					CollisionObject3D collider = (CollisionObject3D)rayhit["collider"];

					if (collider is Damageable damageable)
					{
						while ((damageable.Dead == false) && (numRay < 40))
						{
							damageable.Impulse(dir, pushForce);
							damageable.Damage(GD.RandRange(49, 88), DamageType.BFGBlast, owner);
							numRay++;
						}
						if (!string.IsNullOrEmpty(SecondaryOnDeathSpawn))
						{
							Node3D DeathSpawn = (Node3D)ThingsManager.thingsPrefabs[SecondaryOnDeathSpawn].Instantiate();
							GameManager.Instance.TemporaryObjectsHolder.AddChild(DeathSpawn);
							DeathSpawn.Position = collider.GlobalPosition;
							DeathSpawn.SetForward(-direction);
							DeathSpawn.Rotate(direction, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
						}
					}
				}
			}
			rayCaster.Quaternion = Camerarotation;
		}
	}

	protected override void OnExplosionFX(Vector3 collision, Vector3 direction)
	{
		if (!string.IsNullOrEmpty(OnDeathSpawn))
		{
			Node3D DeathSpawn = (Node3D)ThingsManager.thingsPrefabs[OnDeathSpawn].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(DeathSpawn);
			DeathSpawn.Position = collision + direction;
			DeathSpawn.SetForward(-direction);
			DeathSpawn.Rotate(direction, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
		}
	}

}