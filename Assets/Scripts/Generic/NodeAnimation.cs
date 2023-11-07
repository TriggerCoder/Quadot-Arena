using Godot;
using System;
using System.Threading;

public partial class NodeAnimation : Node3D
{
	[Export]
	public bool rotEnable;
	[Export]
	public float rotFPS;
	[Export]
	public bool rotClockwise;

	[Export]
	public bool posEnable;
	[Export]
	public float posAmplitude;
	[Export]
	public float posFPS;

	private Node3D parent;
	private Vector3 rotateAxis;
	private float height = 0;
	private float timer = 0;

	private GameManager.FuncState currentState = GameManager.FuncState.None;
	public override void _Ready()
	{
		if ((!rotEnable) && (!posEnable))
			SetProcess(false);
		parent = (Node3D)GetParent();

		if (rotEnable)
		{
			if (rotClockwise)
				rotateAxis = Vector3.Up;
			else
				rotateAxis = Vector3.Down;
		}
		currentState = GameManager.FuncState.Ready;
	}

	void RotateNode(float deltaTime)
	{
		if (!rotEnable)
			return;

		parent.RotateObjectLocal(rotateAxis, Mathf.DegToRad(deltaTime * rotFPS));
	}

	void MoveNode(float deltaTime)
	{
		if (!posEnable)
			return;

		timer += deltaTime * posFPS;
		float offSet = posAmplitude * Mathf.Sin(timer) + height;
		parent.GlobalPosition = new Vector3(parent.GlobalPosition.X, offSet, parent.GlobalPosition.Z);
	}

	public void Start()
	{
		height = parent.GlobalPosition.Y;
		currentState = GameManager.FuncState.Start;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		switch(currentState)
		{
			default:

			break;
			case GameManager.FuncState.Ready:
				Start();
			break;
		}

		float deltaTime = (float)delta;
		RotateNode(deltaTime);
		MoveNode(deltaTime);
	}
}
