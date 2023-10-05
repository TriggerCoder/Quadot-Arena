using Godot;
using System;

public partial class PlayerCamera : Node3D
{
	[Export]
	public PlayerControls playerControls;
	[Export]
	public Camera3D SkyboxCamera;
	[Export]
	public Camera3D SkyholeCamera;
	[Export]
	public Camera3D UICamera;

	private Vector3 rotAngle = Vector3.Zero;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		rotAngle.X = playerControls.viewDirection.X;
		rotAngle.Y = playerControls.viewDirection.Y;

		SkyholeCamera.Position = playerControls.playerThing.Position + Vector3.Up * 0.85f;
		SkyholeCamera.RotationDegrees = rotAngle;
		UICamera.Position = playerControls.playerThing.Position + Vector3.Up * 0.85f;
		UICamera.RotationDegrees = rotAngle;

	}
}
