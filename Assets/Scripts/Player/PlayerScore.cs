using Godot;
using System;

public partial class PlayerScore : Node3D
{
	[Export]
	public Label3D PlayerName;
	[Export]
	public Label3D Kills;
	[Export]
	public Label3D Deaths;
	[Export]
	public Label3D Impressive;
	[Export]
	public Label3D Gauntlet;
	[Export]
	public Label3D Excellent;
	[Export]
	public Label3D LifeTime;
	public override void _Ready()
	{

	}

}
