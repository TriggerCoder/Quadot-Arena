using Godot;
using System.Collections;
using System.Collections.Generic;
public partial class DestroyAfterTime : Node
{
	[Export]
	public float destroyTimer = 10;

	private Node parent;
	private GameManager.FuncState currentState = GameManager.FuncState.None;
	public override void _Ready()
	{
		currentState = GameManager.FuncState.Ready;
	}

	public void Start()
	{
		parent = GetParent();
		currentState = GameManager.FuncState.Start;
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		switch (currentState)
		{
			default:

				break;
			case GameManager.FuncState.Ready:
				Start();
				break;
		}
		float deltaTime = (float)delta;
		destroyTimer -= deltaTime;
		if (destroyTimer < 0)
			parent.QueueFree();

	}
}
