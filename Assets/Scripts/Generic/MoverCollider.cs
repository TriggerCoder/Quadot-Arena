using Godot;
using System;
using System.Collections.Generic;
public partial class MoverCollider : Area3D
{
	public bool checkCollision = false;
	private List<Node3D> CurrentColliders = new List<Node3D>();
	private Action<PlayerThing> OnCollide = new Action<PlayerThing>((p) => { return; });
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public void SetOnCollideAction(Action<PlayerThing> collideAction)
	{
		OnCollide = collideAction;
	}

	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (!checkCollision)
			return;

		if (other is PlayerThing player)
		{
			if (player.currentState == GameManager.FuncState.Ready)
				return;

			if (!CurrentColliders.Contains(other))
				CurrentColliders.Add(other);
		}
	}

	public void CheckCurrentColliders()
	{
		var CurrentBodies = GetOverlappingBodies();
		int CurrentBodiesNum = CurrentBodies.Count;
		if (CurrentBodiesNum == 0)
			return;

		for (int i = 0; i < CurrentBodiesNum; i++)
		{
			Node3D CurrentBody = CurrentBodies[i];
			if (CurrentBody is PlayerThing player)
			{
				if (player.currentState == GameManager.FuncState.Ready)
					return;

				OnCollide.Invoke(player);
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
			if (CurrentColliders.Contains(CurrentBody))
			{
				if (CurrentBody is PlayerThing player)
					OnCollide.Invoke(player);
			}
		}
	}
}
