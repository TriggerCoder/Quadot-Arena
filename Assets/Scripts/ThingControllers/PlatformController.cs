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
	private MoverCollider moverCollider;
	public void Init(Vector3 dir, float sp, int height, Aabb box, string noise)
	{
		dirVector = dir;

		lenght = height * GameManager.sizeDividor;
		speed = sp;

		platform = box;
		startPosition = Position;
		
		Vector3 center = platform.GetCenter();
		audioStream = new MultiAudioStream();
		AddChild(audioStream);
		audioStream.Bus = "BKGBus";
		audioStream.Position = center;
		if (!string.IsNullOrEmpty(noise))
		{
			audioStream.Stream = SoundManager.LoadSound(noise, true);
			audioStream.Play();
		}

		moverCollider = new MoverCollider();
		AddChild(moverCollider);
		moverCollider.CollisionLayer = (1 << GameManager.WalkTriggerLayer);
		moverCollider.CollisionMask = GameManager.TakeDamageMask;

		Vector3 size = platform.Abs().Size;
		if (dir.X > 0)
		{
			center.Y -= .25f;
			size.Y = Mathf.Max(size.Y - .25f, .1f);
			size.Z = Mathf.Max(size.Z - .25f, .1f);
		}
		else if (dir.Y > 0)
		{
			size.X = Mathf.Max(size.X - .25f, .1f);
			size.Z = Mathf.Max(size.Z - .25f, .1f);
		}
		else
		{
			center.Y -= .25f;
			size.X = Mathf.Max(size.X - .25f, .1f);
			size.Y = Mathf.Max(size.Y - .25f, .1f);
		}

		moverCollider.GlobalPosition = center;
		moverCollider.GlobalBasis = GlobalBasis;

		moverCollider.SetOnCollideAction((p) =>
		{
			p.Damage(1000, DamageType.Crusher);
		});

		BoxShape3D Box = new BoxShape3D();
		Box.Size = size;

		CollisionShape3D mc = new CollisionShape3D();
		mc.Shape = Box;
		moverCollider.AddChild(mc);
		moverCollider.checkCollision = true;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		deltaTime += (float)delta;
		float newDistance = Mathf.Sin(2 * Mathf.Pi * deltaTime / speed) * lenght;
		Vector3 newPosition = startPosition + dirVector * newDistance;
		Position = newPosition;
	}
}
