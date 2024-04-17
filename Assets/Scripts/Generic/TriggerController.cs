using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class TriggerController : Node3D
{
	public List<Area3D> Areas = new List<Area3D>();
	public string triggerName = "";
	public bool activated = false;
	private List<Action<PlayerThing>> OnActivate = new List<Action<PlayerThing>>();
	private Dictionary<Node3D,int> CurrentColliders = new Dictionary<Node3D, int> ();
	public bool Repeatable = false;
	public bool AutoReturn = false;
	public float AutoReturnTime = 1f;

	public Func<bool> PreReq = new Func<bool>(() => { return true; });

	public float time = 0f;

	public void SetController(string name, Action<PlayerThing> activeAction)
	{
		triggerName = name;
		OnActivate.Add(activeAction);
	}
	public bool Activate(PlayerThing playerThing)
	{
		if (!PreReq())
		{
			GameManager.Print("TriggerController: Prereq False for: " + triggerName, GameManager.PrintType.Info);
			return false;
		}

		if ((!Repeatable) || (AutoReturn))
			if (activated)
				return false;

		if (AutoReturn)
			time = AutoReturnTime;

		activated = !activated;
		for(int i = 0; i < OnActivate.Count; i++)
			OnActivate[i].Invoke(playerThing);

		return true;
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		if (time <= 0)
			return;
		else
		{
			time -= deltaTime;
			if (time <= 0)
				activated = !activated;
		}
	}
	public void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		GameManager.Print("Someone "+ other.Name + " tried to activate this " + Name);
		if (other is PlayerThing player)
		{
			if (!player.ready)
				return;

			//Dead player don't activate stuff
			if (player.Dead)
				return;

			if (!CurrentColliders.ContainsKey(other))
				CurrentColliders.Add(other, 0);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		if (Areas.Count == 0)
		{
			SetPhysicsProcess(false);
			return;
		}

		if (CurrentColliders.Count == 0)
			return;

		for (int n = 0; n < Areas.Count; n++)
		{
			Area3D Area = Areas[n];
			var CurrentBodies = Area.GetOverlappingBodies();
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
						GameManager.Print("Someone " + CurrentBody.Name + " activated this " + Name);
						Activate(playerThing);
						CurrentColliders.Remove(CurrentBody);
					}
				}
			}
		}
	}
}
