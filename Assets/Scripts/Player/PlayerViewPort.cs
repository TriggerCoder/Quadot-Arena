using Godot;
using System;

public partial class PlayerViewPort : Node
{
	[Export]
	public SubViewport Skyhole;
	[Export]
	public SubViewport UI;
	public override void _Ready()
	{
	}
	public override void _Process(double delta)
	{
	}
}
