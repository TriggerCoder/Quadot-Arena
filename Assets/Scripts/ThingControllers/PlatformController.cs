using Godot;
using System;

public partial class PlatformController : AnimatableBody3D
{
	private float lenght;
	private Aabb platform;
	private float speed;

	private Vector3 startPosition;
	private Vector3 dirVector;

	public MultiAudioStream audioStream;
	private float deltaTime = 0;

	private Rid Box;
	private PhysicsShapeQueryParameters3D BoxCast;
	private Node3D collisionArea;
	public void Init(Vector3 dir, float sp, int height, Aabb box, string noise)
	{
		dirVector = dir;

		lenght = height * GameManager.sizeDividor;
		speed = sp;

		platform = box;
		startPosition = Position;

		audioStream = new MultiAudioStream();
		AddChild(audioStream);
		audioStream.Bus = "BKGBus";
		audioStream.Position = platform.GetCenter();
		if (!string.IsNullOrEmpty(noise))
		{
			audioStream.Stream = SoundManager.LoadSound(noise, true);
			audioStream.Play();
		}

		Box = PhysicsServer3D.BoxShapeCreate();
		BoxCast = new PhysicsShapeQueryParameters3D();
		BoxCast.ShapeRid = Box;
		PhysicsServer3D.ShapeSetData(Box, platform.Abs().Size);
		BoxCast.CollisionMask = GameManager.TakeDamageMask;
		collisionArea = new Node3D();
		AddChild(collisionArea);
		collisionArea.GlobalPosition = platform.GetCenter();
		collisionArea.GlobalBasis = GlobalBasis;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		deltaTime += (float)delta;
		float newDistance = Mathf.Sin(2 * Mathf.Pi * deltaTime / speed) * lenght;
		Vector3 newPosition = startPosition + dirVector * newDistance;
//		CheckCollision(newPosition);
		Position = newPosition;
	}
	private void CheckCollision(Vector3 newPosition)
	{
		BoxCast.Motion = newPosition - Position;
		BoxCast.Transform = collisionArea.GlobalTransform;
		var SpaceState = GetWorld3D().DirectSpaceState;
		var result = SpaceState.CastMotion(BoxCast);
		if (result[1] < 1)
		{
			BoxCast.Transform = new Transform3D(collisionArea.GlobalBasis, collisionArea.GlobalPosition + (BoxCast.Motion * result[1]));
			var hit = SpaceState.GetRestInfo(BoxCast);
			if (hit.Count > 0)
			{
				CollisionObject3D collider = (CollisionObject3D)InstanceFromId((ulong)hit["collider_id"]);
				if (collider is PlayerThing player)
					player.Damage(10000, DamageType.Crusher);
			}
		}
	}

}
