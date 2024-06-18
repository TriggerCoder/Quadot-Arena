using ExtensionMethods;
using Godot;

public partial class BloodTrail : RigidBody3D
{
	[Export]
	public float destroyTimer = 1;
	public string[] decalMark = { "BloodMark1", "BloodMark2", "BloodMark3", "BloodMark4", "BloodMark5", "BloodMark6", "BloodMark7", "BloodMark8" };

	private Rid Sphere;
	private PhysicsShapeQueryParameters3D SphereCast;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		Sphere = PhysicsServer3D.SphereShapeCreate();
		PhysicsServer3D.ShapeSetData(Sphere, .5f);
		SphereCast = new PhysicsShapeQueryParameters3D();
		SphereCast.ShapeRid = Sphere;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		destroyTimer -= deltaTime;
		if (destroyTimer < 0)
			QueueFree();
	}

	void OnBodyEntered(Node other)
	{
		var SpaceState = GetWorld3D().DirectSpaceState;
		CollisionObject3D Hit = null;
		Vector3 Collision = Vector3.Zero;
		Vector3 Normal = Vector3.Zero;

		//check for collision on surfaces
		if (destroyTimer > 0)
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

			destroyTimer = -1;
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
