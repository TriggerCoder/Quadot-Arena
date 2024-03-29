using Godot;
using System;
using System.Threading;

public partial class NodeAnimation : Node3D
{
	[Export]
	public bool rotEnable = false;
	[Export]
	public float rotFPS = 0;
	[Export]
	public bool rotClockwise = true;

	[Export]
	public bool posEnable = false;
	[Export]
	public float posAmplitude = 0;
	[Export]
	public float posFPS = 0;

	private Node3D parent;
	private Vector3 rotateAxis;
	private float height = 0;
	private float timer = 0;

	private GameManager.FuncState currentState = GameManager.FuncState.None;
	public override void _Ready()
	{
		Init();
	}

	public void Init()
	{
		if ((!rotEnable) && (!posEnable))
			SetProcess(false);
		else
			SetProcess(true);

		parent = GetParentNode3D();

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
		parent.Position = new Vector3(parent.Position.X, offSet, parent.Position.Z);
	}

	public void Start()
	{
		height = parent.Position.Y;
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
