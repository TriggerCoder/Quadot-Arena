using Godot;
using System;

public partial class DoorController : AnimatableBody3D, Damageable
{
	public bool doorOn = false;
	public bool playSoundClose = true;
	public string startSound = "movers/doors/dr1_strt";
	public string endSound = "movers/doors/dr1_end";
	public TriggerController tc;
	public int damage = 4;
	public bool crusher = false;
	private int hitpoints = 0;
	private float lip;
	private Aabb door;
	private float speed;

	private float wait = 2;
	private float openWaitTime = 0;
	private Vector3 openPosition, closedPosition;
	private Vector3 dirVector = Vector3.Right;

	public virtual float waitTime { get { return wait; } set { wait = value; } }
	public virtual bool Activated { get { return doorOn; } set { doorOn = value; } }
	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return false; } }
	public BloodType BloodColor { get { return BloodType.None; } }

	public MultiAudioStream audioStream;
	private float openSqrMagnitude;

	[System.Serializable]
	public enum State
	{
		None,
		Closed,
		Closing,
		Open,
		Opening
	}

	public State currentState = State.Closed;
	public void SetInitialState(State initial)
	{
		if (currentState == State.None)
		{
			switch (initial)
			{
				default:
					GameManager.Print("Initial DoorState must be only Open/Closed", GameManager.PrintType.Info);
					break;
				case State.Open:
					currentState = initial;
					break;
				case State.Closed:
					currentState = initial;
					break;
			}
		}
	}

	public State CurrentState
	{
		get { return currentState; }
		set
		{
			if (value == State.Open)
			{
				if (!string.IsNullOrEmpty(endSound))
				{
					audioStream.Stream = SoundManager.LoadSound(endSound);
					audioStream.Play();
				}
				openWaitTime = waitTime;
			}
			else if (value == State.Opening)
			{
				if (!string.IsNullOrEmpty(startSound))
				{
					audioStream.Stream = SoundManager.LoadSound(startSound);
					audioStream.Play();
				}
				Activated = true;
				SetPhysicsProcess(true);
			}
			else if (value == State.Closing)
			{
				if (playSoundClose)
					if (!string.IsNullOrEmpty(startSound))
					{
						audioStream.Stream = SoundManager.LoadSound(startSound);
						audioStream.Play();
					}
				SetPhysicsProcess(true);
			}
			else if (value == State.Closed)
			{
				if (playSoundClose)
					if (!string.IsNullOrEmpty(endSound))
					{
						audioStream.Stream = SoundManager.LoadSound(endSound);
						audioStream.Play();
					}
				Activated = false;
				SetPhysicsProcess(false);
			}
			currentState = value;
		}
	}
	public void Init(int angle, int hp, int sp, float wait, int openlip, Aabb box, int dmg = 0)
	{
		SetAngle(angle);

		hitpoints = hp;
		speed = sp * GameManager.sizeDividor;
		//Speed Need to be limited for crusher
		speed = Mathf.Min(speed, 15.625f);
		waitTime = wait;
		lip = openlip * GameManager.sizeDividor;
		damage = dmg;
		if (dmg > 100)
			crusher = true;
		SetBounds(box);

		audioStream = new MultiAudioStream();
		AddChild(audioStream);
		audioStream.Bus = "BKGBus";
		audioStream.Position = door.GetCenter();
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		switch (CurrentState)
		{
			default:
				break;

			case State.Open:
				if (openWaitTime > 0)
				{
					openWaitTime -= deltaTime;
					if (openWaitTime <= 0)
						CurrentState = State.Closing;
				}
				break;

			case State.Closing:
				{
					float newDistance = deltaTime * speed;
					Vector3 newPosition = Position - dirVector * newDistance;
					float sqrMagnitude = (openPosition - newPosition).LengthSquared();
					if (sqrMagnitude > openSqrMagnitude)
					{
						newPosition = closedPosition;
						CurrentState = State.Closed;
					}
					Position = newPosition;
				}
				break;
			case State.Closed:
				break;

			case State.Opening:
				{
					float newDistance = deltaTime * speed;
					Vector3 newPosition = Position + dirVector * newDistance;
					float sqrMagnitude = (newPosition - closedPosition).LengthSquared();
					if (sqrMagnitude > openSqrMagnitude)
					{
						newPosition = openPosition;
						CurrentState = State.Open;
					}
					Position = newPosition;
				}
				break;
		}
	}
	public void SetAngle(int angle)
	{
		if (angle < 0)
		{
			if (angle == -1)
				dirVector = Vector3.Up;
			else
				dirVector = Vector3.Down;
			return;
		}
		//Remember angles are rotated 90
		Quaternion rotation = new Quaternion(Vector3.Up, Mathf.DegToRad(angle + 90));
		dirVector = rotation * Vector3.Forward;
	}

	public void SetBounds(Aabb box)
	{
		door = box;
		closedPosition = Position;
		Vector3 extension = new Vector3(dirVector.X * (door.Size.X - lip), dirVector.Y * (door.Size.Y - lip), dirVector.Z * (door.Size.Z - lip));
		openPosition = closedPosition + extension;
		openSqrMagnitude = (openPosition - closedPosition).LengthSquared();
	}
	public virtual void Damage(int amount, DamageType damageType = DamageType.Generic, Node3D attacker = null)
	{
		if (Dead)
			return;

		if (!Activated)
			CurrentState = State.Opening;
	}
	public void Impulse(Vector3 direction, float force)
	{

	}
}
