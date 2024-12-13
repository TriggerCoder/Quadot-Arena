using Godot;
using System;
using ExtensionMethods;

public partial class PhysicProjectile : RigidBody3D
{
	public Node3D owner;
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

	public AudioStream[] Sounds = new AudioStream[0];
	public bool inWater = false;
	public AudioStream waterSound;

	private Rid Sphere;
	private PhysicsShapeQueryParameters3D SphereCast;
	private PhysicsPointQueryParameters3D PointIntersect;

	public override void _Ready()
	{
		Sphere = PhysicsServer3D.SphereShapeCreate();
		SphereCast = new PhysicsShapeQueryParameters3D();
		SphereCast.ShapeRid = Sphere;
		PhysicsServer3D.ShapeSetData(Sphere, projectileRadius);

		PointIntersect = new PhysicsPointQueryParameters3D();
		PointIntersect.CollideWithAreas = true;
		PointIntersect.CollideWithBodies = false;
		PointIntersect.CollisionMask = (1 << GameManager.FogLayer);

		Sounds = new AudioStream[_sounds.Length];
		for (int i = 0; i < _sounds.Length; i++)
			Sounds[i] = SoundManager.LoadSound(_sounds[i]);

		BodyEntered += OnBodyEntered;
		OnInit();
	}
	public void Init(uint ignoreSelfLayer)
	{
		CollisionMask = ((1 << GameManager.ColliderLayer) | (1 << GameManager.RagdollLayer) | (1 << GameManager.WaterLayer) | GameManager.TakeDamageMask & ~(ignoreSelfLayer));
	}
	public void EnableQuad()
	{
		damageMin *= GameManager.Instance.QuadMul;
		damageMax *= GameManager.Instance.QuadMul;
		blastDamage *= GameManager.Instance.QuadMul;
		pushForce *= GameManager.Instance.QuadMul * .5f;
		OnEnableQuad();
	}
	public void ChangeWater(bool inside, AudioStream sound)
	{
		inWater = inside;
		waterSound = sound;
	}

	public void Explode()
	{
		CollisionObject3D Hit = null;
		Vector3 Collision = GlobalPosition;
		Vector3 Normal = Vector3.Zero;
		Vector3 d = GlobalTransform.ForwardVector();

		var SpaceState = GetWorld3D().DirectSpaceState;
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
				OnCollision(Collision, Normal, d, Hit);
			}
		}

		//explosion
		OnExplosion(Collision, d, SpaceState);
		OnExplosionFX(Collision, d);

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
				SpriteController DecalMark = (SpriteController)ThingsManager.thingsPrefabs[decalMark].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(DecalMark);
				DecalMark.GlobalPosition = Collision + (Normal * .03f);
				DecalMark.SetForward(Normal);
				DecalMark.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				if (Hit is Crusher)
					DecalMark.referenceNode = Hit;
				if (!string.IsNullOrEmpty(secondaryMark))
				{
					SpriteController SecondMark = (SpriteController)ThingsManager.thingsPrefabs[secondaryMark].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(SecondMark);
					SecondMark.GlobalPosition = Collision + (Normal * .05f);
					SecondMark.SetForward(Normal);
					SecondMark.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
					if (Hit is Crusher)
						SecondMark.referenceNode = Hit;
				}
			}
		}
		else if (!string.IsNullOrEmpty(OnDeathSpawn))
		{
			Node3D DeathSpawn = (Node3D)ThingsManager.thingsPrefabs[OnDeathSpawn].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(DeathSpawn);
			DeathSpawn.Position = GlobalPosition;
		}

		OnDestroy(Collision);
		return;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		OnCheckWaterChange();
		OnPhysicsUpdate(deltaTime);

		if (destroyTimer > 0)
			destroyTimer -= deltaTime;
		if (destroyTimer < 0)
			Explode();
	}

	protected virtual void OnPhysicsUpdate(float deltaTime) { }
	protected virtual void OnInit() { }
	protected virtual void OnEnableQuad() { }
	protected virtual void OnCollision(Vector3 collision, Vector3 normal, Vector3 direction, CollisionObject3D collider) { }
	protected virtual void OnExplosion(Vector3 collision, Vector3 direction, PhysicsDirectSpaceState3D SpaceState) { }
	protected virtual void OnExplosionFX(Vector3 collision, Vector3 direction) { }
	protected virtual void OnBodyEntered(Node other) { }
	protected virtual void OnCheckWaterChange() { }
	protected virtual void OnDestroy(Vector3 collision)
	{
		if (!string.IsNullOrEmpty(_onDeathSound))
			SoundManager.Create3DSound(collision, SoundManager.LoadSound(_onDeathSound));
		QueueFree();
	}

	public bool CheckIfCanMark(PhysicsDirectSpaceState3D SpaceState, CollisionObject3D collider, Vector3 collision)
	{
		if (collider is Damageable)
			return false;

		//Don't mark moving platforms
		if (collider is Crusher)
			return false;

		//Check if mapcollider are noMarks
		if (MapLoader.noMarks.Contains(collider))
			return false;

		//Check if collision in inside a fog Area
		PointIntersect.Position = collision;

		var hits = SpaceState.IntersectPoint(PointIntersect);
		if (hits.Count == 0)
			return true;

		return false;
	}
}
