using Godot;
using ExtensionMethods;

public partial class PlasmaBall : Projectile
{
	[Export]
	public string _onFlySound;
	private Rid ExplosionSphere;
	private PhysicsShapeQueryParameters3D ExplosionSphereCast;
	protected override void OnInit()
	{
		if (!string.IsNullOrEmpty(_onFlySound))
		{
			audioStream.Stream = SoundManager.LoadSound(_onFlySound, true);
			audioStream.Play();
		}
		ExplosionSphere = PhysicsServer3D.SphereShapeCreate();
		ExplosionSphereCast = new PhysicsShapeQueryParameters3D();
		ExplosionSphereCast.ShapeRid = ExplosionSphere;
		PhysicsServer3D.ShapeSetData(ExplosionSphere, explosionRadius);
	}
	protected override void OnCollision(Vector3 collision, Vector3 normal, Vector3 direction, CollisionObject3D collider)
	{
		if (collider is Damageable damageable)
		{
			Vector3 impulseDir = direction.Normalized();
			damageable.Impulse(impulseDir, pushForce);
			damageable.Damage(GD.RandRange(damageMin, damageMax), DamageType.Plasma, owner);
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
				//Plasma never does self damage
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
					damageable.Damage(GD.RandRange(damageMin, damageMax), DamageType.Plasma, owner);
			}
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
	protected override void OnDestroy(Vector3 collision)
	{
		if (!string.IsNullOrEmpty(_onDeathSound))
			SoundManager.Create3DSound(collision, SoundManager.LoadSound(_onDeathSound));
		QueueFree();
	}
}
