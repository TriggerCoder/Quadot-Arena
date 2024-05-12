using Godot;
using System;
using System.Collections.Generic;
public partial class MoverCollider : Area3D
{
	public bool checkCollision = false;
	private Dictionary<Node3D, float> CurrentColliders = new Dictionary<Node3D, float>();
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

			if (!CurrentColliders.ContainsKey(other))
				CurrentColliders.Add(other, (player.GlobalPosition - GlobalPosition).LengthSquared());
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
				float distance = (CurrentBody.GlobalPosition - GlobalPosition).LengthSquared();
				if (distance < CurrentColliders[CurrentBody])
				{
					if (CurrentBody is PlayerThing player)
						OnCollide.Invoke(player);
				}
			}
		}
	}

}
