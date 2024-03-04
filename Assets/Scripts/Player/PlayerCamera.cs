using Godot;
using System;

public partial class PlayerCamera : Node3D
{
	[Export]
	public PlayerControls playerControls;
	[Export]
	public Camera3D ThirdPerson;
	[Export]
	public Camera3D ViewCamera;
	[Export]
	public Camera3D ViewPortCamera;
	[Export]
	public SubViewport ViewPort;
	public Camera3D CurrentCamera;
	private Vector3 rotAngle = Vector3.Zero;
	public bool currentThirdPerson = false;

	public float yOffset = .85f;
	private float learpYOffset = .85f;
	private float interp = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CurrentCamera = ViewCamera;
		SetLocalViewPortToCamera(ViewCamera, false);
		ViewPortCamera.Current = true;
		GameManager.Instance.SetViewPortToCamera(ViewPortCamera);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		if (GameOptions.HeadBob && playerControls.bobActive && !currentThirdPerson)
			interp = Mathf.Lerp(interp, 1, deltaTime * 5);
		else
			interp = Mathf.Lerp(interp, 0, deltaTime * 6);

		Vector2 Bob = playerControls.currentBob * interp;

		if (learpYOffset != yOffset)
			learpYOffset = Mathf.Lerp(learpYOffset, yOffset, 10 * deltaTime);

		Position = new Vector3 (0, learpYOffset + Bob.Y, 0);
		rotAngle.X = playerControls.viewDirection.X;
		rotAngle.Z = Bob.X;
		RotationDegrees = rotAngle;
	}

	public void ChangeThirdPersonCamera(bool enable)
	{
		currentThirdPerson = enable;
		if (enable)
		{
			ThirdPerson.Visible = true;
			CurrentCamera = ThirdPerson;
			SetLocalViewPortToCamera(ThirdPerson,true);
			playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask);
			return;
		}
		CurrentCamera = ViewCamera;
		SetLocalViewPortToCamera(ViewCamera,false);
		ThirdPerson.Visible = false;
		playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask & ~((uint)(playerControls.playerInfo.viewLayer)));
	}
	public void SetLocalViewPortToCamera(Camera3D camera, bool debug)
	{
		var CamRID = camera.GetCameraRid();
		var viewPortRID = ViewPort.GetViewportRid();
		RenderingServer.ViewportAttachCamera(viewPortRID, CamRID);
	}
}
