using Godot;
using System.Collections.Generic;

public partial class PlatformController : AnimatableBody3D
{
	private float lenght;
	private float speed;

	private Vector3 startPosition;
	private Vector3 dirVector;

	public MultiAudioStream audioStream;
	private float deltaTime;
	private MoverCollider moverCollider;
	public void Init(Vector3 dir, float sp, float phase, int height, List<Aabb> Boxes, Vector3 origin, string noise)
	{
		dirVector = dir;

		lenght = height * GameManager.sizeDividor;
		speed = sp;

		deltaTime = 1000 * sp * phase;

		startPosition = Position;
		
		audioStream = new MultiAudioStream();
		AddChild(audioStream);
		audioStream.Bus = "BKGBus";
		audioStream.Position = origin;
		if (!string.IsNullOrEmpty(noise))
		{
			audioStream.Stream = SoundManager.LoadSound(noise, true);
			audioStream.Play();
		}

		if (Boxes.Count == 0)
			return;

		moverCollider = new MoverCollider();
		AddChild(moverCollider);
		moverCollider.CollisionLayer = (1 << GameManager.WalkTriggerLayer);
		moverCollider.CollisionMask = GameManager.TakeDamageMask;

		moverCollider.SetOnCollideAction((p) =>
		{
			p.Damage(1000, DamageType.Crusher);
		});

		for (int i = 0; i < Boxes.Count; i++) 
		{
			BoxShape3D Box = new BoxShape3D();

			Vector3 size = Boxes[i].Abs().Size;
			Vector3 center = Boxes[i].GetCenter();
			center.Y -= .3f;

			if (dir.X > 0)
			{
				size.Y = Mathf.Max(size.Y - .3f, .1f);
				size.Z = Mathf.Max(size.Z - .3f, .1f);
			}
			else if (dir.Y > 0)
			{
				size.X = Mathf.Max(size.X - .3f, .1f);
				size.Y = Mathf.Max(size.Y - .3f, .1f);
				size.Z = Mathf.Max(size.Z - .3f, .1f);
			}
			else
			{
				size.X = Mathf.Max(size.X - .3f, .1f);
				size.Y = Mathf.Max(size.Y - .3f, .1f);
			}

			Box.Size = size;

			CollisionShape3D mc = new CollisionShape3D();
			mc.Shape = Box;
			mc.Position = center;
			moverCollider.AddChild(mc);
		}

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
