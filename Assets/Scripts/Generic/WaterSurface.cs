using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class WaterSurface : Area3D
{
	public List<Aabb> Boxes = new List<Aabb>();
	private List<PlayerThing> currentPlayers = new List<PlayerThing>();
	//Anti Bounce
	private Dictionary<Node3D, int> CurrentColliders = new Dictionary<Node3D, int>();
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
			if (currentPlayer.Dead)
			{
//				GameManager.Print(currentPlayer.Name + "died in the Water " + Name);
				currentPlayers.Remove(currentPlayer);
				continue;
			}

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
				int value = CurrentColliders[CurrentBody]++;
				if (value > 1)
				{
					PlayerEnterIntoWater(CurrentBody as PlayerThing);
					CurrentColliders.Remove(CurrentBody);
				}
			}
		}
	}
	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (other is PlayerThing)
		{
			if (!CurrentColliders.ContainsKey(other))
				CurrentColliders.Add(other, 0);
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
				playerThing.playerInfo.playerPostProcessing.ResetEffects();
				playerThing.avatar.lowerAnimation = PlayerModel.LowerAnimation.Jump;
				SoundManager.Create3DSound(playerThing.GlobalPosition, outSound);
				currentPlayers.Remove(playerThing);
//				GameManager.Print("Finally " + other.Name + "got out of the Water " + Name);
			}
		}
	}

	void PlayerEnterIntoWater(PlayerThing playerThing)
	{
		if (!currentPlayers.Contains(playerThing))
		{
			playerThing.waterLever = 1;
			playerThing.playerInfo.playerPostProcessing.SetWaterEffect();
			SoundManager.Create3DSound(playerThing.GlobalPosition, inSound);
			currentPlayers.Add(playerThing);
//			GameManager.Print(playerThing.Name + " Jump into the Water " + Name);
		}
	}

}
