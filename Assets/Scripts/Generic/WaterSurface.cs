using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class WaterSurface : Area3D
{
	public List<Aabb> Boxes = new List<Aabb>();
	private List<PlayerThing> currentPlayers = new List<PlayerThing>();
	public string waterIn = "player/watr_in";
	public string waterOut = "player/watr_out";
	private AudioStreamWav inSound;
	private AudioStreamWav outSound;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExit;
		inSound = SoundManager.LoadSound(waterIn);
		outSound = SoundManager.LoadSound(waterOut);
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < currentPlayers.Count; i++)
		{
			PlayerThing currentPlayer = currentPlayers[i];
			for (int j = 0; j < Boxes.Count; j++) 
			{
				if (Boxes[j].HasPoint(currentPlayer.GlobalPosition))
				{
					float deep = Boxes[j].GetEndpoint(2).Y - currentPlayer.GlobalPosition.Y;
					if (deep > GameManager.Instance.playerHeight)
						currentPlayer.waterLever = 2;
					break;
				}
			}
		}
	}

	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (other is PlayerThing playerThing)
		{
			if (!currentPlayers.Contains(playerThing))
			{
				playerThing.waterLever = 1;
				SoundManager.Create3DSound(playerThing.GlobalPosition, inSound);
				currentPlayers.Add(playerThing);
				GameManager.Print(other.Name + " Jump into the Water " + Name);
			}
		}
	}

	void OnBodyExit(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (other is PlayerThing playerThing)
		{
			if (currentPlayers.Contains(playerThing))
			{
				playerThing.waterLever = 0;
				SoundManager.Create3DSound(playerThing.GlobalPosition, outSound);
				currentPlayers.Remove(playerThing);
				GameManager.Print("Finally " + other.Name + "got out of the Water " + Name);
			}
		}
	}
}
