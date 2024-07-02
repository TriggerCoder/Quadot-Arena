using Godot;
using System.Collections.Generic;
using ExtensionMethods;
public partial class GibsController : RigidBody3D
{
	[Export]
	public MultiAudioStream audioStream;
	[Export]
	public string[] _sounds = new string[0];
	[Export]
	public ModelController modelController;

	public string[] decalMark = { "BloodMark1", "BloodMark2", "BloodMark3", "BloodMark4", "BloodMark5", "BloodMark6", "BloodMark7", "BloodMark8" };
	public AudioStream[] Sounds = new AudioStream[0];
	public float spawnTime = .1f;

	private Rid Sphere;
	private PhysicsShapeQueryParameters3D SphereCast;
	private PhysicsPointQueryParameters3D PointIntersect;

	private ConvexPolygonShape3D shape;
	private bool leaveMark = true;
	private float dropTime;
	public override void _Ready()
	{
		Sounds = new AudioStream[_sounds.Length];
		for (int i = 0; i < _sounds.Length; i++)
			Sounds[i] = SoundManager.LoadSound(_sounds[i]);
		BodyEntered += OnBodyEntered;
		if (ThingsManager.gibsShapes.TryGetValue(Name, out shape))
			GenerateCollider(true);

		Sphere = PhysicsServer3D.SphereShapeCreate();
		PhysicsServer3D.ShapeSetData(Sphere, .5f);
		SphereCast = new PhysicsShapeQueryParameters3D();
		SphereCast.ShapeRid = Sphere;

		PointIntersect = new PhysicsPointQueryParameters3D();
		PointIntersect.CollideWithAreas = true;
		PointIntersect.CollideWithBodies = false;
		PointIntersect.CollisionMask = (1 << GameManager.FogLayer);
		dropTime = spawnTime;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		if (dropTime > 0)
			dropTime -= deltaTime;
		else if (dropTime < 0)
		{
			Node3D Blood = (Node3D)ThingsManager.thingsPrefabs[ThingsManager.BloodTrail].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(Blood);
			Blood.GlobalTransform = GlobalTransform;
			dropTime += spawnTime;
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		if (modelController.Model == null)
			return;

		GenerateCollider(false);
	}
	void OnBodyEntered(Node other)
	{
		int soundIndex = 0;
		float speed = LinearVelocity.LengthSquared();

		if (leaveMark)
			CheckCollision();

		if (speed > 30)
			soundIndex = GD.RandRange(0, 2);
		else if (speed > 10)
			soundIndex = GD.RandRange(0, 1);
		else if (speed < 5)
		{
			leaveMark = false;
			return;
		}

		if (Sounds.Length > soundIndex)
		{
			audioStream.Stream = Sounds[soundIndex];
			audioStream.Play();
		}
	}

	void CheckCollision()
	{
		var SpaceState = GetWorld3D().DirectSpaceState;
		CollisionObject3D Hit = null;
		Vector3 Collision = Vector3.Zero;
		Vector3 Normal = Vector3.Zero;

		//Don't spawn more blood
		SetProcess(false);

		//check for collision on surfaces
		{
			SphereCast.CollisionMask = (1 << GameManager.ColliderLayer);
			SphereCast.Transform = GlobalTransform;
			var hit = SpaceState.GetRestInfo(SphereCast);
			if (hit.Count > 0)
			{
				Hit = (CollisionObject3D)InstanceFromId((ulong)hit["collider_id"]);
				Collision = (Vector3)hit["point"];
				Normal = (Vector3)hit["normal"];
			}

			if (Hit == null)
				return;

			if (CheckIfCanMark(SpaceState, Hit, Collision) == false)
				return;

			SpriteController DecalMark = (SpriteController)ThingsManager.thingsPrefabs[decalMark[GD.RandRange(0, decalMark.Length - 1)]].Instantiate();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(DecalMark);
			DecalMark.GlobalPosition = Collision + (Normal * .03f);
			DecalMark.SetForward(Normal);
			DecalMark.Rotate(Normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
			if (Hit is Crusher)
				DecalMark.referenceNode = Hit;
		}
	}

	void GenerateCollider(bool colliderReady)
	{
		CollisionShape3D collisionShape = new CollisionShape3D();
		collisionShape.Name = "GibShape";
		AddChild(collisionShape);

		//Remember models are rotated 90
		Quaternion rotation = new Quaternion(Vector3.Right, Mathf.DegToRad(90));

		if (!colliderReady)
		{
			List<Vector3> modelPoints = new List<Vector3>();
			for (int i = 0; i < modelController.Model.meshes.Count; i++)
			{
				MD3Mesh currentMesh = modelController.Model.meshes[i];
				List<Vector3> verts = new List<Vector3>();
				for (int j = 0; j < currentMesh.numVertices; j++)
				{
					Vector3 newVertex = rotation * currentMesh.verts[0][j];
					verts.Add(newVertex);
				}
				modelPoints.AddRange(verts);
			}
			shape = new ConvexPolygonShape3D();
			shape.Points = modelPoints.ToArray();
			ThingsManager.AddGibsShapes(Name, shape);
		}
		collisionShape.Shape = shape;
		SetPhysicsProcess(false);
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
}
