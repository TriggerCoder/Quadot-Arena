using Godot;
using ExtensionMethods;

public partial class Nail : Projectile
{
	[Export]
	string AttackProjectileName = "";
	[Export]
	public string[] _hitSounds;
	public AudioStream[] hitSounds;
	public bool hasBounced = false;
	protected override void OnInit()
	{
		hitSounds = new AudioStream[_hitSounds.Length];
		for (int i = 0; i < _hitSounds.Length; i++)
			hitSounds[i] = SoundManager.LoadSound(_hitSounds[i]);
	}
	protected override void OnCollision(Vector3 collision, Vector3 normal, Vector3 direction, CollisionObject3D collider)
	{
		int soundIndex = 0;
		bool hitFx = true;
		if (collider is Damageable damageable)
		{
			Vector3 impulseDir = direction.Normalized();
			damageable.Impulse(impulseDir, pushForce);
			damageable.Damage(GD.RandRange(damageMin, damageMax), DamageType.Generic, owner);

			if (damageable.Bleed)
			{
				hitFx = false;
				soundIndex = 1; 
				Node3D Blood = (Node3D)ThingsManager.thingsPrefabs[ThingsManager.Blood].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(Blood);
				Blood.GlobalPosition = collision + (normal * .05f);
			}
		}
		if (hitFx)
		{
			if (MapLoader.mapSurfaceTypes.TryGetValue(collider, out SurfaceType st))
			{
				if (st.MetalSteps)
					soundIndex = 2;
				else if (st.Flesh)
					soundIndex = 1;
			}
			//65% chance of bounce
			if ((!hasBounced) && (GD.Randf() < 0.65f))
			{
				Vector3 Reflection = normal * 2 * direction.Dot(normal) - direction;
				Nail nail = (Nail)ThingsManager.thingsPrefabs[AttackProjectileName].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(nail);
				nail.owner = owner;
				nail.damageMin = damageMin;
				nail.damageMax = damageMax;
				nail.GlobalPosition = collision + Reflection * .1f;
				nail.ignoreSelfLayer = ignoreSelfLayer;
				nail.hasBounced = true;
				nail.SetForward(Reflection);
				nail.InvoqueSetTransformReset();
			}
		}
		if (hitSounds.Length > soundIndex)
			SoundManager.Create3DSound(collision, hitSounds[soundIndex]);
	}
}
