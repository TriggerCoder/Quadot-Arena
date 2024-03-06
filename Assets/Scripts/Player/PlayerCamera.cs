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
	public PlayerPostProcessing playerPostProcessing;

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
		playerPostProcessing.SetLocalViewPortToCamera(ViewCamera);
	}
	/*
	public Vector2 cartesian_to_spherical(Vector3 cartesianVector)
	{
		float theta, phi;
		theta = Mathf.Acos(cartesianVector.Z); // Polar angle
		if ((cartesianVector.X == 0.0) && (cartesianVector.Y == 0.0))
		{
			theta = Mathf.Pi;
			if (cartesianVector.Z > 0.0)
				theta = 0.0f;
			phi = 0.0f;
		}
		else
		{
			float add = Mathf.Pi;
			phi = Mathf.Pi / 2.0f;
			if (cartesianVector.Y < 0.0)
				add = -Mathf.Pi;

			if (cartesianVector.X != 0.0)
			{
				phi = Mathf.Atan(cartesianVector.Y / cartesianVector.X); // Azimuthal angle
				if (cartesianVector.X < 0.0)
					phi += add;
			}
		}
		phi = (phi + Mathf.Pi) / (2.0f * Mathf.Pi);
		theta /= Mathf.Pi;
		GameManager.Print("Theta :" + theta + " Phi: " + phi);
		return new Vector2(theta, phi);
	}
	public Vector3 spherical_to_cartesian(Vector2 sphericalVector)
	{
		sphericalVector.X *= Mathf.Pi;
		sphericalVector.Y = (2.0f * sphericalVector.Y) - 1.0f;
		sphericalVector.Y *= Mathf.Pi;
		float x = Mathf.Sin(sphericalVector.X) * Mathf.Cos(sphericalVector.Y);
		float y = Mathf.Sin(sphericalVector.X) * Mathf.Sin(sphericalVector.Y);
		float z = Mathf.Cos(sphericalVector.X);
		return new Vector3(x, y, z);
	}
	public void PrintForward()
	{
		Vector3 vector = GlobalTransform.Basis.Z;
		GameManager.Print("Forward X:" + vector.X + " Y:" + vector.Y + " Z:" + vector.Z);
		Vector2 sphericalVector = cartesian_to_spherical(vector);
		sphericalVector = sphericalVector.Clamp(Vector2.Zero, Vector2.One);
		Vector3 dirVector = spherical_to_cartesian(sphericalVector);
		GameManager.Print("Dir X:" + dirVector.X + " Y:" + dirVector.Y + " Z:" + dirVector.Z);
	}
	*/
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
	//	PrintForward();
	}

	public void ChangeThirdPersonCamera(bool enable)
	{
		currentThirdPerson = enable;
		if (enable)
		{
			ThirdPerson.Visible = true;
			CurrentCamera = ThirdPerson;
			playerPostProcessing.SetLocalViewPortToCamera(ThirdPerson);
			playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask);
			return;
		}
		CurrentCamera = ViewCamera;
		playerPostProcessing.SetLocalViewPortToCamera(ViewCamera);
		ThirdPerson.Visible = false;
		playerControls.playerThing.avatar.ChangeLayer(GameManager.AllPlayerViewMask & ~((uint)(playerControls.playerInfo.viewLayer)));
	}
}
