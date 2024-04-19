using Godot;
using System;
public partial class JumpPadThing : Area3D
{
	public string BoingSound = "world/jumppad";

	private Vector3 destination;
	private Vector3 position;
	private MultiAudioStream audioStream;
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

			if (!string.IsNullOrEmpty(BoingSound))
				audioStream.Play();

			playerThing.JumpPadDest(destination);
		}
	}
}