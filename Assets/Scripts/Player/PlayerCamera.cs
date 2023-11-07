using Godot;
using System;

public partial class PlayerCamera : Node3D
{
	[Export]
	public PlayerControls playerControls;
	[Export]
	public Camera3D ThirdPerson;
	[Export]
	public Camera3D SkyholeCamera;
	[Export]
	public Camera3D UICamera;

	public Camera3D CurrentCamera;
	private Vector3 rotAngle = Vector3.Zero;
	public bool currentThirdPerson = false;
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

	public void ChangeThirdPersonCamera(bool enable)
	{
		currentThirdPerson = enable;
		if (enable)
		{
			CurrentCamera = ThirdPerson;
			GameManager.Instance.SetViewPortToCamera(ThirdPerson);
			UICamera.CullMask = 0;
			playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask);
			return;
		}
		CurrentCamera = SkyholeCamera;
		GameManager.Instance.SetViewPortToCamera(SkyholeCamera);
		UICamera.CullMask = playerControls.playerInfo.uiLayer;
		playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask & ~((uint)(playerControls.playerInfo.viewLayer)));
	}
}
