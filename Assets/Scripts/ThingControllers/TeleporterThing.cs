using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class TeleporterThing : Area3D
{
	public string TeleportInSound = "world/telein";
	public string TeleportOutSound = "world/teleout";

	public List<Target> destinations = new List<Target>();
	private List<PlayerThing> toTeleport = new List<PlayerThing>();
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}
	public void Init(List<Target> dest, Dictionary<string, string> entityData)
	{
		foreach(var target in dest) 
		{
			Vector3 destination = target.destination;
			int angle = target.angle + 90;
			if (angle < -180)
				angle += 360;
			if (angle > 180)
				angle -= 360;

			destinations.Add(new Target(destination, angle, entityData));
		}
	}
	public static void TelefragEverything(Vector3 position, Node3D node)
	{
		Rid Sphere = PhysicsServer3D.SphereShapeCreate();
		PhysicsShapeQueryParameters3D SphereCast = new PhysicsShapeQueryParameters3D();
		SphereCast.ShapeRid = Sphere;
		PhysicsServer3D.ShapeSetData(Sphere, 2f);
		SphereCast.CollisionMask = GameManager.TakeDamageMask | (1 << GameManager.RagdollLayer);
		SphereCast.Motion = Vector3.Zero;
		SphereCast.Transform = new Transform3D(Basis.Identity, position);
		var SpaceState = node.GetWorld3D().DirectSpaceState;
		var hits = SpaceState.IntersectShape(SphereCast);
		var max = hits.Count;

		for (int i = 0; i < max; i++)
		{
			var hit = hits[i];

			CollisionObject3D collider = (CollisionObject3D)hit["collider"];
			if (collider is Damageable damageable)
			{
				if (collider != node)
					damageable.Damage(10000, DamageType.Telefrag, node);
			}
		}
		return;
	}
	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (other is PlayerThing playerThing)
		{
			if (!playerThing.ready)
				return;

			if (!toTeleport.Contains(playerThing))
				toTeleport.Add(playerThing);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		if (toTeleport.Count == 0)
			return;

		for (int i = 0; i < toTeleport.Count; i++)
		{
			PlayerThing playerThing = toTeleport[i];

			if (!string.IsNullOrEmpty(TeleportOutSound))
				SoundManager.Create3DSound(playerThing.Position, SoundManager.LoadSound(TeleportOutSound));

			Target target = destinations[GD.RandRange(0, destinations.Count - 1)];
			ClusterPVSManager.CheckPVS(playerThing.playerInfo.viewLayer, target.destination);
			TelefragEverything(target.destination, playerThing);
			playerThing.Position = target.destination;
			playerThing.playerControls.InvoqueSetTransformReset();

			if (!string.IsNullOrEmpty(TeleportInSound))
				SoundManager.Create3DSound(target.destination, SoundManager.LoadSound(TeleportInSound));
			playerThing.playerControls.viewDirection.Y = target.angle;
			playerThing.playerControls.impulseVector = Vector3.Zero;
			playerThing.playerControls.playerVelocity = Vector3.Zero;
			playerThing.playerControls.jumpPadVel = Vector3.Zero;
			playerThing.Impulse(Quaternion.FromEuler(new Vector3(0, Mathf.DegToRad(target.angle), 0)) * Vector3.Forward, 1500);
		}
		toTeleport = new List<PlayerThing>();
	}
}
