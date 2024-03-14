using Godot;
using System;
using System.Collections.Generic;

public partial class JumpPadThing : Area3D
{
	public string BoingSound = "world/jumppad";

	private Vector3 destination;
	private Vector3 position;
	private MultiAudioStream audioStream;

	//Anti Bounce
	private Dictionary<Node3D, int> CurrentColliders = new Dictionary<Node3D, int>();
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}
	public void Init(Vector3 dest, Vector3 center)
	{
		destination = dest;
		position = GlobalPosition;
		if (string.IsNullOrEmpty(BoingSound))
			return;

		audioStream = new MultiAudioStream();
		AddChild(audioStream);
		audioStream.GlobalPosition = center;
		audioStream.Bus = "BKGBus";
		audioStream.Stream = SoundManager.LoadSound(BoingSound);
	}

	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (other is PlayerThing playerThing)
		{
			if (!playerThing.ready)
				return;

			//Dead player don't use jumppads
			if (playerThing.Dead)
				return;

			if (!CurrentColliders.ContainsKey(other))
				CurrentColliders.Add(other, 0);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		if (CurrentColliders.Count == 0)
			return;

		var CurrentBodies = GetOverlappingBodies();
		int CurrentBodiesNum = CurrentBodies.Count;
		if (CurrentBodiesNum == 0)
		{
			CurrentColliders.Clear();
			return;
		}

		for (int i = 0; i < CurrentBodiesNum; i++)
		{
			Node3D CurrentBody = CurrentBodies[i];
			if (CurrentColliders.ContainsKey(CurrentBody))
			{
				PlayerThing playerThing = CurrentBody as PlayerThing;
				if ((!playerThing.ready) || (playerThing.Dead))
				{
					CurrentColliders.Remove(CurrentBody);
					continue;
				}

				int value = CurrentColliders[CurrentBody]++;
				if (value > 1)
				{
					if (!string.IsNullOrEmpty(BoingSound))
						audioStream.Play();
					playerThing.JumpPadDest(destination);
					CurrentColliders.Remove(CurrentBody);
				}
			}
		}
	}
}
