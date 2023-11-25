using Godot;
using System;
public partial class DoorCollider : Area3D
{
	public DoorController door;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public int Damage { get { return door.damage; } }
	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (door.CurrentState != DoorController.State.Closing)
			return;

		GameManager.Print("Someone " + other.Name + " entered this " + Name);
		if (other is PlayerThing playerThing)
		{
			playerThing.Damage(Damage, DamageType.Crusher);
			if (!door.crusher)
				door.CurrentState = DoorController.State.Opening;
		}
	}
}
