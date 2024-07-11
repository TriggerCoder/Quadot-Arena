using Godot;
using System;
using System.Collections.Generic;

public partial class ConsoleManager : Control
{
	[Export]
	public LineEdit commandLine;
	[Export]
	public RichTextLabel history;
	public static ConsoleManager Instance;
	public int moveSpeed = 1000;

	private GameManager.FuncState currentState = GameManager.FuncState.None;
	private List<string> textHistory = new List<string>(2048);

	public bool visible = false;
	private Vector2 tempPosition;
	public override void _Ready()
	{
		Instance = this;
		tempPosition = Position;
		commandLine.TextSubmitted += CommandSubmited;
	}

	public void ChangeConsole()
	{
		if (currentState != GameManager.FuncState.End)
			return;

		visible = !visible;
		if (visible)
		{
			commandLine.FocusMode = FocusModeEnum.All;
			Show();
		}
		else
			commandLine.FocusMode = FocusModeEnum.None;

		currentState = GameManager.FuncState.Start;
	}

	public void CommandSubmited(string command)
	{
		GameManager.Print("Command: " + command);
		commandLine.Text = "";
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		switch (currentState)
		{
			default:
			break;
			case GameManager.FuncState.None:
				if (MaterialManager.consoleMaterial != null)
				{
					Material = MaterialManager.consoleMaterial;
					currentState = GameManager.FuncState.End;
				}
			break;
			case GameManager.FuncState.Start:
				if (visible)
				{
					tempPosition.Y += moveSpeed * deltaTime;
					if (tempPosition.Y > 0)
					{
						tempPosition.Y = 0;
						currentState = GameManager.FuncState.End;
						commandLine.GrabFocus();
					}
					Position = tempPosition;
				}
				else
				{
					tempPosition.Y -= moveSpeed * deltaTime;
					if (tempPosition.Y  < -360)
					{
						tempPosition.Y = -360;
						Hide();
						currentState = GameManager.FuncState.End;
					}
					Position = tempPosition;
				}
				break;
		}
	}
}
