using Godot;
using System;
using System.Drawing;
using System.Xml.Linq;

public partial class PlayerControls : Node3D
{
	[Export]
	public PlayerInfo playerInfo;
	[Export]
	public PlayerThing playerThing;
	//	[Export]
	//	public PlayerWeapon playerWeapon;
	[Export]
	public PlayerCamera playerCamera;

	private Vector2 centerHeight = new Vector2(0.2f, -.05f); // character controller center height, x standing, y crouched
	private Vector2 height = new Vector2(2.0f, 1.5f); // character controller height, x standing, y crouched
	private float camerasHeight = .65f;
	private float ccHeight = .05f;

	public Vector2 viewDirection = new Vector2(0, 0);

	public Vector3 lastPosition = new Vector3(0, 0, 0);

	public Vector3 impulseVector = Vector3.Zero;

	public Vector3 jumpPadVel = Vector3.Zero;

	public float impulseDampening = 4f;
	[Export]
	public CharacterBody3D controller;
//	[Export]
//	public CapsuleCollider capsuleCollider;
//	[Export]
//	public PlayerInput playerInput;

	// Movement stuff
	public float crouchSpeed = 3.0f;                // Crouch speed
	public float walkSpeed = 5.0f;                  // Walk speed
	public float runSpeed = 7.0f;                   // Run speed
	private float oldSpeed = 0;                     // Previous move speed

	public float moveSpeed;                         // Ground move speed
	public float runAcceleration = 14.0f;           // Ground accel
	public float runDeacceleration = 10.0f;         // Deacceleration that occurs when running on the ground
	public float airAcceleration = 2.0f;            // Air accel
	public float airDecceleration = 2.0f;           // Deacceleration experienced when ooposite strafing
	public float airControl = 0.3f;                 // How precise air control is
	public float sideStrafeAcceleration = 50.0f;    // How fast acceleration occurs to get up to sideStrafeSpeed when
	public float sideStrafeSpeed = 1.0f;            // What the max speed to generate when side strafing
	public float jumpSpeed = 8.0f;                  // The speed at which the character's up axis gains when hitting jump
	public bool holdJumpToBhop = false;             // When enabled allows player to just hold jump button to keep on bhopping perfectly. Beware: smells like casual.

	public Vector3 playerVelocity = Vector3.Zero;
	private bool wishJump = false;
	private bool wishFire = false;
	private bool controllerIsGrounded = true;

	private float deathTime = 0;
	private float respawnDelay = 1.7f;
	struct currentMove
	{
		public float forwardSpeed;
		public float sidewaysSpeed;
	}

	private currentMove cMove;

	public int CurrentWeapon = -1;
	public int SwapWeapon = -1;

	public MoveType currentMoveType = MoveType.Run;
	public enum MoveType
	{
		Crouch,
		Walk,
		Run
	}

	//Cached Transform
	public Transform3D cTransform;
	public Vector3 teleportDest = Vector3.Zero;

	private Vector3 rotAngle = Vector3.Zero;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		moveSpeed = runSpeed;
		currentMoveType = MoveType.Run;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion eventMouseMotion)
		{
			Vector2 Look = eventMouseMotion.Relative;
			viewDirection.Y -= Look.X * GameOptions.MouseSensitivity.X;
			viewDirection.X -= Look.Y * GameOptions.MouseSensitivity.Y;

			if (viewDirection.Y < -180)
				viewDirection.Y += 360;
			if (viewDirection.Y > 180)
				viewDirection.Y -= 360;

			//restricted up/down looking angle
			if (viewDirection.X < -85)
				viewDirection.X = -85;
			if (viewDirection.X > 85)
				viewDirection.X = 85;
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		controllerIsGrounded = playerThing.IsOnFloor();
		//Player can only crounch if it is grounded
		if ((Input.IsActionJustPressed("Action_Crouch")) && (controllerIsGrounded))
		{
			if (oldSpeed == 0)
				oldSpeed = moveSpeed;
			moveSpeed = crouchSpeed;
			currentMoveType = MoveType.Crouch;
//			ChangeHeight(false);
		}
		else if (Input.IsActionJustReleased("Action_Crouch"))
		{
			if (oldSpeed != 0)
				moveSpeed = oldSpeed;
			if (moveSpeed == walkSpeed)
				currentMoveType = MoveType.Walk;
			else
				currentMoveType = MoveType.Run;
//			ChangeHeight(true);
			oldSpeed = 0;
		}
		else //CheckRun
		{
			if (GameOptions.runToggle)
			{
				if (Input.IsActionJustReleased("Action_Run"))
				{
					if (moveSpeed == walkSpeed)
					{
						moveSpeed = runSpeed;
						currentMoveType = MoveType.Run;
					}
					else
					{
						moveSpeed = walkSpeed;
						currentMoveType = MoveType.Walk;
					}
				}
			}
			else
			{
				if (Input.IsActionPressed("Action_Run"))
				{
					moveSpeed = runSpeed;
					currentMoveType = MoveType.Run;
				}
				else
				{
					moveSpeed = walkSpeed;
					currentMoveType = MoveType.Walk;
				}
			}
		}

		//Movement Checks
		if (currentMoveType != MoveType.Crouch)
			QueueJump();
/*
		if (controllerIsGrounded)
		{
			if (playerThing.avatar.enableOffset)
				playerThing.avatar.TurnLegs((int)currentMoveType, cMove.sidewaysSpeed, cMove.forwardSpeed);
			if (wishJump)
				AnimateLegsOnJump();
		}
		else
			playerThing.avatar.TurnLegsOnJump(cMove.sidewaysSpeed);
*/
	}

	public override void _PhysicsProcess(double delta)
	{
		rotAngle.Y = viewDirection.Y;
		playerThing.RotationDegrees = rotAngle;

		float deltaTime = (float)delta;
		//Movement Checks
		if (controllerIsGrounded)
			GroundMove(deltaTime);
		else
			AirMove(deltaTime);

		//apply move
		ApplyMove(deltaTime);
	}

	void ApplyMove(float deltaTime)
	{
		//		lastPosition = cTransform.position;
		playerThing.Velocity = (playerVelocity + impulseVector + jumpPadVel);
		playerThing.MoveAndSlide();

		//dampen impulse
		if (impulseVector.LengthSquared() > 0)
		{
			impulseVector.Lerp(Vector3.Zero, impulseDampening * deltaTime);
			if (impulseVector.LengthSquared() < 1f)
				impulseVector = Vector3.Zero;
		}
	}
	private void SetMovementDir()
	{
		Vector2 Move = Input.GetVector("Move_Left", "Move_Right", "Move_Forward", "Move_Back");

		cMove.forwardSpeed = Move.Y;
		cMove.sidewaysSpeed = Move.X;
	}
	private void QueueJump()
	{
		if (holdJumpToBhop)
		{
			wishJump = Input.IsActionPressed("Action_Jump");
			return;
		}

		if (Input.IsActionJustPressed("Action_Jump") && !wishJump)
			wishJump = true;
		if (Input.IsActionJustReleased("Action_Jump"))
			wishJump = false;
	}
	private void GroundMove(float deltaTime)
	{
		Vector3 wishdir;

		// Do not apply friction if the player is queueing up the next jump
		if (!wishJump)
			ApplyFriction(1.0f, deltaTime);
		else
			ApplyFriction(0, deltaTime);

		SetMovementDir();

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		wishdir = playerThing.Transform.Basis * wishdir;
		wishdir = wishdir.Normalized();

		float wishspeed = wishdir.Length();
		wishspeed *= moveSpeed;

		Accelerate(wishdir, wishspeed, runAcceleration, deltaTime, runSpeed);

		// Reset the gravity velocity
		playerVelocity.Y = -GameManager.Instance.gravity * deltaTime;

		if (wishJump)
		{
			playerVelocity.Y = jumpSpeed;
			wishJump = false;
		}
	}

	private void ApplyFriction(float t, float deltaTime)
	{
		Vector3 vec = playerVelocity;
		float speed;
		float newspeed;
		float control;
		float drop;

		vec.Y = 0.0f;
		speed = vec.Length();
		drop = 0.0f;

		//Player is always grounded when we are here, no need to re-check
		//if (controller.isGrounded)
		{
			control = speed < runDeacceleration ? runDeacceleration : speed;
			drop = control * GameManager.Instance.friction * deltaTime * t;
		}

		newspeed = speed - drop;

		if (newspeed < 0)
			newspeed = 0;
		if (speed > 0)
			newspeed /= speed;

		playerVelocity.X *= newspeed;
		playerVelocity.Z *= newspeed;
	}
	private void Accelerate(Vector3 wishdir, float wishspeed, float accel, float deltaTime, float wishaccel = 0)
	{
		float addspeed;
		float accelspeed;
		float currentspeed;

		currentspeed = playerVelocity.Dot(wishdir);
		addspeed = wishspeed - currentspeed;
		if (addspeed <= 0)
			return;
		if (wishaccel == 0)
			wishaccel = wishspeed;
		accelspeed = accel * deltaTime * wishaccel;
		if (accelspeed > addspeed)
			accelspeed = addspeed;

		playerVelocity.X += accelspeed * wishdir.X;
		playerVelocity.Z += accelspeed * wishdir.Z;
	}

	private void AirMove(float deltaTime)
	{
		Vector3 wishdir;
		float accel;

		SetMovementDir();

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		wishdir = playerThing.Transform.Basis * wishdir;
		float wishspeed = wishdir.Length();
		wishspeed *= moveSpeed;

		wishdir = wishdir.Normalized();

		//Aircontrol
		float wishspeed2 = wishspeed;
		if (playerVelocity.Dot(wishdir) < 0)
			accel = airDecceleration;
		else
			accel = airAcceleration;
		// If the player is ONLY strafing left or right
		if ((cMove.forwardSpeed == 0) && (cMove.sidewaysSpeed != 0))
		{
			if (wishspeed > sideStrafeSpeed)
				wishspeed = sideStrafeSpeed;
			accel = sideStrafeAcceleration;
		}

		Accelerate(wishdir, wishspeed, accel, deltaTime);
		if (airControl > 0)
			AirControl(wishdir, wishspeed2, deltaTime);

		// Apply gravity
		if (jumpPadVel.LengthSquared() > 0)
			playerVelocity.Y = 0;
		else
			playerVelocity.Y -= GameManager.Instance.gravity * deltaTime;
	}

	private void AirControl(Vector3 wishdir, float wishspeed, float deltaTime)
	{
		float zspeed;
		float speed;
		float dot;
		float k;

		// Can't control movement if not moving forward or backward
		if ((cMove.forwardSpeed == 0) || (Mathf.Abs(wishspeed) < 0.001))
			return;
		zspeed = playerVelocity.Y;
		playerVelocity.Y = 0;
		/* Next two lines are equivalent to idTech's VectorNormalize() */
		speed = playerVelocity.Length();
		playerVelocity = playerVelocity.Normalized();

		dot = playerVelocity.Dot(wishdir);
		k = 32;
		k *= airControl * dot * dot * deltaTime;

		// Change direction while slowing down
		if (dot > 0)
		{
			playerVelocity.X = playerVelocity.X * speed + wishdir.X * k;
			playerVelocity.Y = playerVelocity.Y * speed + wishdir.Y * k;
			playerVelocity.Z = playerVelocity.Z * speed + wishdir.Z * k;

			playerVelocity = playerVelocity.Normalized();
		}

		playerVelocity.X *= speed;
		playerVelocity.Y = zspeed; // Note this line
		playerVelocity.Z *= speed;
	}
}
