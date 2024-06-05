using ExtensionMethods;
using Godot;
using System;

public partial class Grenade : RigidBody3D
{
	public Node3D owner;
	[Export]
	public ParticlesController fx;
	[Export]
	public MultiAudioStream audioStream;
	[Export]
	public string[] _sounds = new string[0];
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
	public string _onDeathSound;
	[Export]
	public float destroyTimer = 2.5f;

	public AudioStreamWav[] Sounds = new AudioStreamWav[0];
	public bool inWater = false;

	public AudioStreamWav waterSound;
	private GameManager.FuncState currentState = GameManager.FuncState.None;
	public override void _Ready()
	{
		Sounds = new AudioStreamWav[_sounds.Length];
		for (int i = 0; i < _sounds.Length; i++)
			Sounds[i] = SoundManager.LoadSound(_sounds[i]);
		BodyEntered += OnBodyEntered;
	}

	public void Init(uint ignoreSelfLayer)
	{
		CollisionMask = ((1 << GameManager.ColliderLayer) | (1 << GameManager.RagdollLayer) | (1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.WaterLayer) | GameManager.TakeDamageMask & ~(ignoreSelfLayer));
	}
	public void EnableQuad()
	{
		damageMin *= GameManager.Instance.QuadMul;
		damageMax *= GameManager.Instance.QuadMul;
		blastDamage *= GameManager.Instance.QuadMul;
		pushForce *= GameManager.Instance.QuadMul;
	}
	void OnBodyEntered(Node other)
	{
		if (!inWater)
			if (Sounds.Length > 0)
			{
				audioStream.Stream = Sounds[GD.RandRange(0, Sounds.Length - 1)];
				audioStream.Play();
			}

		if (other is Damageable)
			if (LinearVelocity.LengthSquared() > 10)
				Explode();
	}

	public void ChangeWater(bool inside, AudioStreamWav sound)
	{
		inWater = inside;
		waterSound = sound;
		currentState = GameManager.FuncState.Ready;
	}

	void Explode()
	{
		var Sphere = PhysicsServer3D.SphereShapeCreate();
		var SphereCast = new PhysicsShapeQueryParameters3D();
		var SpaceState = GetWorld3D().DirectSpaceState;
		SphereCast.ShapeRid = Sphere;

		CollisionObject3D Hit = null;
		Vector3 Collision = Vector3.Zero;
		Vector3 Normal = Vector3.Zero;
		Vector3 d = GlobalTransform.ForwardVector();

		//check for collision on surfaces
		{
			PhysicsServer3D.ShapeSetData(Sphere, projectileRadius);
			SphereCast.CollisionMask = (1 << GameManager.ColliderLayer);
			SphereCast.Transform = GlobalTransform;
			var hit = SpaceState.GetRestInfo(SphereCast);
			if (hit.Count > 0)
			{
				Hit = (CollisionObject3D)InstanceFromId((ulong)hit["collider_id"]);
				Collision = (Vector3)hit["point"];
				Normal = (Vector3)hit["normal"];
			}
		}

		//explosion
		PhysicsServer3D.ShapeSetData(Sphere, explosionRadius);
		SphereCast.CollisionMask = GameManager.TakeDamageMask | (1 << GameManager.RagdollLayer);
		SphereCast.Motion = Vector3.Zero;
		SphereCast.Transform = GlobalTransform;
		var hits = SpaceState.IntersectShape(SphereCast);
		var max = hits.Count;

		for (int i = 0; i < max; i++)
		{
			var hit = hits[i];
			CollisionObject3D collider = (CollisionObject3D)hit["collider"];

			if (collider is Damageable damageable)
			{
				Vector3 hPosition = collider.Position;
				Vector3 Distance = (hPosition - GlobalPosition);
				float lenght;
				Vector3 impulseDir = Distance.GetLenghtAndNormalize(out lenght);

				damageable.Damage(Mathf.CeilToInt(Mathf.Lerp(blastDamage, 1, lenght / explosionRadius)), DamageType.Explosion, owner);
				damageable.Impulse(impulseDir, Mathf.Lerp(pushForce, 100, lenght / explosionRadius));
			}
		}

		if (Hit != null)
		{
			if (!string.IsNullOrEmpty(OnDeathSpawn))
			{
				Node3D DeathSpawn = (Node3D)ThingsManager.thingsPrefabs[OnDeathSpawn].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(DeathSpawn);
				DeathSpawn.Position = Collision;
				DeathSpawn.SetForward(-d);
				DeathSpawn.Rotate(d, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
			}

			//Check if collider can be marked
			if (CheckIfCanMark(SpaceState, Hit, Collision))
			{
				Node3D DecalMark = (Node3D)ThingsManager.thingsPrefabs[decalMark].Instantiate();
				Hit.AddChild(DecalMark);
				DecalMark.Position = Collision + (Normal * .03f);
				DecalMark.SetForward(-Normal);
				DecalMark.Rotate((DecalMark.UpVector()).Normalized(), -Mathf.Pi * .5f);
				DecalMark.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				if (!string.IsNullOrEmpty(secondaryMark))
				{
					Node3D SecondMark = (Node3D)ThingsManager.thingsPrefabs[secondaryMark].Instantiate();
					Hit.AddChild(SecondMark);
					SecondMark.Position = Collision + (Normal * .05f);
					SecondMark.SetForward(-Normal);
					SecondMark.Rotate((SecondMark.UpVector()).Normalized(), -Mathf.Pi * .5f);
					SecondMark.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				}
			}
		}
		else if (!string.IsNullOrEmpty(OnDeathSpawn))
		{
			Node3D DeathSpawn = (Node3D)ThingsManager.thingsPrefabs[OnDeathSpawn].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(DeathSpawn);
			DeathSpawn.Position = GlobalPosition;
		}

		if (!string.IsNullOrEmpty(_onDeathSound))
			SoundManager.Create3DSound(Collision, SoundManager.LoadSound(_onDeathSound));
		QueueFree();
		return;
	}
	public bool CheckIfCanMark(PhysicsDirectSpaceState3D SpaceState, CollisionObject3D collider, Vector3 collision)
	{
		if (collider is Damageable)
			return false;

		//Check if mapcollider are noMarks
		if (MapLoader.noMarks.Contains(collider))
			return false;

		//Check if collision in inside a fog Area
		var PointIntersect = new PhysicsPointQueryParameters3D();
		PointIntersect.CollideWithAreas = true;
		PointIntersect.CollideWithBodies = false;
		PointIntersect.CollisionMask = (1 << GameManager.FogLayer);
		PointIntersect.Position = collision;

		var hits = SpaceState.IntersectPoint(PointIntersect);
		if (hits.Count == 0)
			return true;

		return false;
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (currentState == GameManager.FuncState.Ready)
		{
			fx.Visible = !inWater;
			audioStream.Stream = waterSound;
			audioStream.Play();
			currentState = GameManager.FuncState.None;
		}

		float deltaTime = (float)delta;
		destroyTimer -= deltaTime;

		if (destroyTimer < 0)
			Explode();
	}
}
