using Godot;
using System;
using ExtensionMethods;
public partial class ProxMines : PhysicProjectile, Damageable
{
	[Export]
	public int armingTime = 3;
	public bool currentWater = false;
	public GameManager.FuncState CurrentState = GameManager.FuncState.None;

	private float delayArm = 0; 
	private int hitpoints = 1;
	private int surfaceSoundIndex = 2;
	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return false; } }
	public BloodType BloodColor { get { return BloodType.None; } }
	protected override void OnBodyEntered(Node other)
	{
		if (CurrentState != GameManager.FuncState.None)
			return;

		Freeze = true;
		var SpaceState = GetWorld3D().DirectSpaceState;
		PhysicsServer3D.ShapeSetData(Sphere, projectileRadius);
		SphereCast.CollisionMask = (1 << GameManager.ColliderLayer);
		SphereCast.Transform = GlobalTransform;
		var hit = SpaceState.GetRestInfo(SphereCast);
		if (hit.Count > 0)
		{
			CollisionObject3D Hit = (CollisionObject3D)InstanceFromId((ulong)hit["collider_id"]);
			Vector3 Collision = (Vector3)hit["point"];
			Vector3 Normal = (Vector3)hit["normal"];
			GlobalPosition = Collision;
			this.SetForward(-Normal);
			if (MapLoader.mapSurfaceTypes.TryGetValue(Hit, out SurfaceType st))
			{
				if (st.MetalSteps)
					surfaceSoundIndex = 3;
				else if (st.Flesh)
					surfaceSoundIndex = 4;
			}
		}
		else if (other is Damageable damageable)
		{
			if (damageable.Bleed)
				surfaceSoundIndex = 3;
		}
		else if (other is CollisionObject3D collider)
		{
			if (MapLoader.mapSurfaceTypes.TryGetValue(collider, out SurfaceType st))
			{
				if (st.MetalSteps)
					surfaceSoundIndex = 3;
				else if (st.Flesh)
					surfaceSoundIndex = 4;
			}
		}

		CollisionLayer = (1 << GameManager.DamageablesLayer) | (1 << GameManager.ColliderLayer);
		CurrentState = GameManager.FuncState.Ready;
	}

	protected override void OnCheckWaterChange()
	{
		if (currentWater != inWater)
		{
			audioStream.Stream = waterSound;
			audioStream.Play();
			currentWater = inWater;
		}
	}

	protected override void OnPhysicsUpdate(float deltaTime)
	{
		switch (CurrentState)
		{
			default:
			case GameManager.FuncState.None:
				return;
			break;
			case GameManager.FuncState.Ready:
				if (delayArm == 0)
				{
					destroyTimer = 0;
					delayArm = armingTime - deltaTime;
					if (Sounds.Length > surfaceSoundIndex)
						SoundManager.Create3DSound(GlobalPosition, Sounds[surfaceSoundIndex]);
				}
				else if (delayArm > 0)
					delayArm -= deltaTime;
				else if (delayArm < 0)
				{
					delayArm = 0;
					if (Sounds.Length > 0)
						SoundManager.Create3DSound(GlobalPosition, Sounds[0]);
					PhysicsServer3D.ShapeSetData(Sphere, explosionRadius / 3);
					SphereCast.CollisionMask = GameManager.TakeDamageMask;
					SphereCast.Motion = Vector3.Zero;
					SphereCast.Transform = GlobalTransform;
					CurrentState = GameManager.FuncState.Start;
				}
			break;
			case GameManager.FuncState.Start:
				var SpaceState = GetWorld3D().DirectSpaceState;
				var hits = SpaceState.IntersectShape(SphereCast);
				var max = hits.Count;
				bool explode = false;
				for (int i = 0; i < max; i++)
				{
					var hit = hits[i];
					CollisionObject3D collider = (CollisionObject3D)hit["collider"];
					//Never activate owns mines
					if (collider == owner)
						continue;

					if (collider is PlayerThing player)
					{
						explode = true;
						break;
					}
				}
				if (explode)
				{
					if (Sounds.Length > 1)
						SoundManager.Create3DSound(GlobalPosition, Sounds[1]);
					Explode();
				}
				break;
		}
	}
	protected override void OnExplosion(Vector3 Collision, Vector3 direction, PhysicsDirectSpaceState3D SpaceState)
	{
		PhysicsServer3D.ShapeSetData(Sphere, explosionRadius);
		SphereCast.CollisionMask = GameManager.TakeDamageMask | (1 << GameManager.RagdollLayer);
		SphereCast.Motion = Vector3.Zero;
		SphereCast.Transform = new Transform3D(GlobalTransform.Basis, Collision);
		var hits = SpaceState.IntersectShape(SphereCast);
		var max = hits.Count;

		for (int i = 0; i < max; i++)
		{
			var hit = hits[i];

			CollisionObject3D collider = (CollisionObject3D)hit["collider"];
			if (collider is Damageable damageable)
			{
				Vector3 hPosition = collider.Position;
				Vector3 Distance = (hPosition - Collision);
				float lenght;
				Vector3 impulseDir = Distance.GetLenghtAndNormalize(out lenght);
				int damage = blastDamage;
				//mines won't do 0 damage to owner
				if (collider == owner)
					damage = Mathf.CeilToInt(damage / 3);
				damageable.Impulse(impulseDir, Mathf.Lerp(pushForce, 100, lenght / explosionRadius));
				damageable.Damage(Mathf.CeilToInt(Mathf.Lerp(damage, 1, lenght / explosionRadius)), DamageType.Explosion, owner);
			}
		}
	}

	public virtual void Damage(int amount, DamageType damageType = DamageType.Generic, Node3D attacker = null)
	{
		if (Dead)
			return;

		if ((damageType == DamageType.Explosion) || (damageType == DamageType.Plasma))
		{
			hitpoints = 0;
			Explode();
		}
	}
	public void Impulse(Vector3 direction, float force)
	{

	}
}