using Godot;
using System;
using System.Collections.Generic;

public partial class ElevatorController : AnimatableBody3D, Crusher
{
	public bool playSoundDown = true;
	public string startSound = "movers/plats/pt1_strt";
	public string endSound = "movers/plats/pt1_end";
	public TriggerController tc;
	public int damage = 4;
	public bool crusher = false;
	private float lip;
	private Aabb elevator;
	private float speed;

	private float UpWaitTime = 0;
	private Vector3 UpPosition, DownPosition;
	private Vector3 dirVector = Vector3.Up;

	private float waitTime = 2;

	public MultiAudioStream audioStream;
	private float UpSqrMagnitude;
	private List<PlayerThing> playersToCrush = new List<PlayerThing>();
	public enum State
	{
		None,
		Down,
		Lowering,
		Up,
		Rising
	}

	public State currentState = State.Down;

	public State CurrentState
	{
		get { return currentState; }
		set
		{
			if (value == State.Up)
			{
				if ((currentState != State.Up) && (!string.IsNullOrEmpty(endSound)))
				{
					audioStream.Stream = SoundManager.LoadSound(endSound);
					audioStream.Play();
				}
				UpWaitTime = waitTime;
			}
			else if (value == State.Rising)
			{
				if (!string.IsNullOrEmpty(startSound))
				{
					audioStream.Stream = SoundManager.LoadSound(startSound);
					audioStream.Play();
				}
				SetPhysicsProcess(true);
			}
			else if (value == State.Lowering)
			{
				if (playSoundDown)
					if (!string.IsNullOrEmpty(startSound))
					{
						audioStream.Stream = SoundManager.LoadSound(startSound);
						audioStream.Play();
					}
				SetPhysicsProcess(true);
			}
			else if (value == State.Down)
			{
				if (playSoundDown)
					if (!string.IsNullOrEmpty(endSound))
					{
						audioStream.Stream = SoundManager.LoadSound(endSound);
						audioStream.Play();
					}
				SetPhysicsProcess(false);
			}
			currentState = value;
		}
	}
	public void Init(int sp, int Height, int Uplip, Aabb box, int model, int dmg = 4)
	{		
		speed = sp * GameManager.sizeDividor;
		lip = Uplip * GameManager.sizeDividor;
		damage = dmg;
		if (dmg > 100)
			crusher = true;

		if (model >= 0)
		{
			elevator = box;
			if (Height == 0)
				SetBounds();
			else
				SetBounds(Height);
		}
		else
			SetBounds(Height);

		audioStream = new MultiAudioStream();
		AddChild(audioStream);
		audioStream.Bus = "BKGBus";
		audioStream.Position = elevator.GetCenter();
		CurrentState = State.Up;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		if (playersToCrush.Count > 0)
		{
			for (int i = 0; i < playersToCrush.Count; i++)
			{
				playersToCrush[i].Damage(damage, DamageType.Crusher);
				if (!crusher)
					CurrentState = State.Rising;

			}
			playersToCrush.Clear();
		}

		float deltaTime = (float)delta;
		switch (CurrentState)
		{
			default:
			break;
			case State.Up:
			if (UpWaitTime > 0)
			{
				UpWaitTime -= deltaTime;
				if (UpWaitTime <= 0)
					CurrentState = State.Lowering;
			}
			break;
			case State.Lowering:
			{
				float newDistance = deltaTime * speed;
				Vector3 newPosition = Position - dirVector * newDistance;
				float sqrMagnitude = (UpPosition - newPosition).LengthSquared();
				if (sqrMagnitude > UpSqrMagnitude)
				{
					newPosition = DownPosition;
					CurrentState = State.Down;
				}
				Position = newPosition;
			}
			break;
			case State.Down:
			break;
			case State.Rising:
			{
				float newDistance = deltaTime * speed;
				Vector3 newPosition = Position + dirVector * newDistance;
				float sqrMagnitude = (newPosition - DownPosition).LengthSquared();
				if (sqrMagnitude > UpSqrMagnitude)
				{
					newPosition = UpPosition;
					CurrentState = State.Up;
				}
				Position = newPosition;
			}
			break;
		}
	}

	public void SetBounds()
	{
		UpPosition = Position;
		Vector3 extension = dirVector * (elevator.Size.Y - lip);
		DownPosition = UpPosition - extension;
		UpSqrMagnitude = (UpPosition - DownPosition).LengthSquared();
	}

	public void SetBounds(float height)
	{
		height *= GameManager.sizeDividor;
		UpPosition = Position;
		DownPosition = UpPosition - dirVector * height;
		UpSqrMagnitude = (UpPosition - DownPosition).LengthSquared();
	}

	public void Crush(PlayerThing player)
	{
		//Crush on main thread
		if (!playersToCrush.Contains(player))
			playersToCrush.Add(player);
	}
}
