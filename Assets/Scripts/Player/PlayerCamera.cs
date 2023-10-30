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

	public Camera3D CurrentCamera;
	private Vector3 rotAngle = Vector3.Zero;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CurrentCamera = SkyholeCamera;
		GameManager.Instance.SetViewPortToCamera(SkyholeCamera);
		GameManager.Instance.SetViewPortToCamera(UICamera,true);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		rotAngle.X = playerControls.viewDirection.X;

		RotationDegrees = rotAngle;
	}
}
