using Godot;
using System;
using ExtensionMethods;
public partial class Grenade : PhysicProjectile
{
	[Export]
	public ParticlesController fx;
	public bool currentWater = false;
	private Rid ExplosionSphere;
	private PhysicsShapeQueryParameters3D ExplosionSphereCast;
	protected override void OnInit()
	{
		CollisionMask |= (1 << GameManager.InvisibleBlockerLayer);
		ExplosionSphere = PhysicsServer3D.SphereShapeCreate();
		ExplosionSphereCast = new PhysicsShapeQueryParameters3D();
		ExplosionSphereCast.ShapeRid = ExplosionSphere;
		PhysicsServer3D.ShapeSetData(ExplosionSphere, explosionRadius);
	}
	protected override void OnBodyEntered(Node other)
	{
		if (!currentWater)
			if (Sounds.Length > 0)
			{
				audioStream.Stream = Sounds[GD.RandRange(0, Sounds.Length - 1)];
				audioStream.Play();
			}

		if (other is Damageable)
			if (LinearVelocity.LengthSquared() > 10) //force explosion in physics update
				destroyTimer = -1;
	}

	protected override void OnCheckWaterChange()
	{
		if (currentWater != inWater)
		{
			fx.Visible = !inWater;
			audioStream.Stream = waterSound;
			audioStream.Play();
			currentWater = inWater;
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
				{
					Vector3 hPosition = collider.Position;
					Vector3 Distance = (hPosition - Collision);
					float lenght;
					Vector3 impulseDir = Distance.GetLenghtAndNormalize(out lenght);
					int damage = blastDamage;
					//in order to enable grenade jump
					if (collider == owner)
						damage = Mathf.CeilToInt(damage / 3);
					damageable.Impulse(impulseDir, Mathf.Lerp(pushForce, 100, lenght / explosionRadius));
					damageable.Damage(Mathf.CeilToInt(Mathf.Lerp(damage, 1, lenght / explosionRadius)), DamageType.Explosion, owner);
				}
			}
		}
	}
}
