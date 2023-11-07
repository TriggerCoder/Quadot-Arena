using Godot;
using System;

public partial class DestroyAfterTime : Node
{
	[Export]
	public float destroyTimer = 0;
	public override void _Ready()
	{
		if (destroyTimer == 0)
			SetProcess(false);
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		destroyTimer -= deltaTime;
		if (destroyTimer < 0)
			GetParent().QueueFree();
	}
}
