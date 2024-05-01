using Godot;
using System;
using System.Collections.Generic;
public partial class DoorCollider : Area3D
{
	public DoorController door;

	private Dictionary<Node3D, float> CurrentColliders = new Dictionary<Node3D, float>();
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public int Damage { get { return door.damage; } }
	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if ((door.CurrentState != DoorController.State.Closing) && (door.CurrentState != DoorController.State.Opening))
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
					PlayerThing playerThing = CurrentBody as PlayerThing;
					CurrentColliders.Remove(CurrentBody);
					playerThing.Damage(Damage, DamageType.Crusher);
					if (!door.crusher)
						door.CurrentState = DoorController.State.Opening;
				}
			}
		}
	}

}
