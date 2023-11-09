using Godot;
using System;
using System.Globalization;

public partial class PlayerControls : Node3D
{
	[Export]
	public PlayerInfo playerInfo;
	[Export]
	public PlayerThing playerThing;

	public PlayerWeapon playerWeapon;
	[Export]
	public PlayerCamera playerCamera;

	private Vector2 centerHeight = new Vector2(0.2f, -.05f);	// character controller center height, x standing, y crouched
	private Vector2 height = new Vector2(2.0f, 1.5f);			// character controller height, x standing, y crouched
	private float camerasHeight = .65f;
	private float ccHeight = .05f;

	public Vector2 viewDirection = new Vector2(0, 0);

	public Vector3 lastPosition = new Vector3(0, 0, 0);

	public Vector3 impulseVector = Vector3.Zero;

	public Vector3 jumpPadVel = Vector3.Zero;

	public float impulseDampening = 4f;

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

	public Vector2 Look = Vector2.Zero;
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
		playerThing.CollisionLayer = playerInfo.playerLayer;
	}

	public override void _Input(InputEvent @event)
	{
		if (GameManager.Paused)
			return;

		if (@event is InputEventMouseMotion eventMouseMotion)
		{
			Look = eventMouseMotion.Relative;
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
		if (GameManager.Paused)
			return;
		
		float deltaTime = (float)delta;

		if (Input.IsActionJustPressed("Action_CameraSwitch"))
			playerCamera.ChangeThirdPersonCamera(!playerCamera.currentThirdPerson);

		playerThing.avatar.ChangeView(viewDirection, deltaTime);
		playerThing.avatar.CheckLegTurn(playerCamera.CurrentCamera.GlobalTransform.Basis.Z);

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
		if (controllerIsGrounded)
		{
			if (playerThing.avatar.enableOffset)
				playerThing.avatar.TurnLegs((int)currentMoveType, cMove.sidewaysSpeed, cMove.forwardSpeed);
			if (wishJump)
				AnimateLegsOnJump();
		}
		else
			playerThing.avatar.TurnLegsOnJump(cMove.sidewaysSpeed);


		if (Input.IsActionPressed("Action_Fire"))
			wishFire = true;

		//swap weapon
		if (playerWeapon == null)
		{
			if (SwapWeapon == -1)
				SwapToBestWeapon();

			if (SwapWeapon > -1)
			{
				CurrentWeapon = SwapWeapon;
				playerWeapon = (PlayerWeapon)playerInfo.WeaponPrefabs[CurrentWeapon].Instantiate();
				playerWeapon.Init(playerInfo);
				SwapWeapon = -1;
			}
		}

		CheckMouseWheelWeaponChange();

		CheckWeaponChangeByIndex();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		rotAngle.Y = viewDirection.Y;
		playerInfo.RotationDegrees = rotAngle;

		float deltaTime = (float)delta;

		//Movement Checks
		if (controllerIsGrounded)
			GroundMove(deltaTime);
		else
			AirMove(deltaTime);

		//apply move
		ApplyMove(deltaTime);

		//dampen jump pad impulse
		if (jumpPadVel.LengthSquared() > 0)
		{
			jumpPadVel.Y -= (GameManager.Instance.gravity * deltaTime);
			if ((jumpPadVel.Y < 0) && (controllerIsGrounded))
				jumpPadVel = Vector3.Zero;
		}

		if (wishFire)
		{
			wishFire = false;
			if (playerWeapon == null)
				return;

			if (playerWeapon.Fire())
			{
//				playerInfo.playerHUD.HUDUpdateAmmoNum();
				playerThing.avatar.Attack();
			}
		}
	}

	void ApplyMove(float deltaTime)
	{
		playerThing.Velocity = (playerVelocity + impulseVector + jumpPadVel);
		playerThing.MoveAndSlide();

		//dampen impulse
		if (impulseVector.LengthSquared() > 0)
		{
			impulseVector = impulseVector.Lerp(Vector3.Zero, impulseDampening * deltaTime);
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
		wishdir = playerInfo.Transform.Basis * wishdir;
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
		wishdir = playerInfo.Transform.Basis * wishdir;
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

	public void AnimateLegsOnJump()
	{
		if (cMove.forwardSpeed <= 0)
			playerThing.avatar.lowerAnimation = PlayerModel.LowerAnimation.Jump;
		else if (cMove.forwardSpeed > 0)
			playerThing.avatar.lowerAnimation = PlayerModel.LowerAnimation.JumpBack;
		playerThing.avatar.enableOffset = false;
		playerThing.PlayModelSound("jump1");
	}
	public bool TrySwapWeapon(int weapon)
	{
		if (CurrentWeapon == weapon || SwapWeapon != -1)
			return false;

		if (weapon < 0 || weapon >= playerInfo.Weapon.Length)
			return false;

		if (!playerInfo.Weapon[weapon])
			return false;

		switch (weapon)
		{
			default:
				return false;

			case 0:
				break;

			case 1:
				if (playerInfo.Ammo[0] <= 0)
					return false;
				break;
			case 2:
				if (playerInfo.Ammo[1] <= 0)
					return false;
				break;

			case 3:
				if (playerInfo.Ammo[2] <= 0)
					return false;
				break;

			case 4:
				if (playerInfo.Ammo[3] <= 0)
					return false;
				break;

			case 5:
				if (playerInfo.Ammo[4] <= 0)
					return false;
				break;
			case 6:
				if (playerInfo.Ammo[5] <= 0)
					return false;
				break;
			case 7:
				if (playerInfo.Ammo[6] <= 0)
					return false;
				break;
			case 8:
				if (playerInfo.Ammo[7] <= 0)
					return false;
				break;
		}

		if (playerWeapon != null)
			playerWeapon.putAway = true;

		SwapWeapon = weapon;
		return true;
	}
	public void SwapToBestWeapon()
	{
		if (TrySwapWeapon(8)) return; //bfg10k
		if (TrySwapWeapon(5)) return; //lightning gun
		if (TrySwapWeapon(7)) return; //plasma gun
		if (TrySwapWeapon(6)) return; //railgun
		if (TrySwapWeapon(2)) return; //shotgun
		if (TrySwapWeapon(1)) return; //machinegun
		if (TrySwapWeapon(4)) return; //rocketlauncher
		if (TrySwapWeapon(3)) return; //grenade launcher
		if (TrySwapWeapon(0)) return; //gauntlet
	}

	public void CheckMouseWheelWeaponChange()
	{
		if (Input.IsActionJustPressed("Action_WeaponSwitch_Up"))
		{
			bool gotWeapon = false;
			for (int NextWeapon = CurrentWeapon + 1; NextWeapon < 9; NextWeapon++)
			{
				gotWeapon = TrySwapWeapon(NextWeapon);
				if (gotWeapon)
					break;
			}
			if (!gotWeapon)
				TrySwapWeapon(0);
		}
		else if (Input.IsActionJustPressed("Action_WeaponSwitch_Down"))
		{
			bool gotWeapon = false;
			for (int NextWeapon = CurrentWeapon - 1; NextWeapon >= 0; NextWeapon--)
			{
				gotWeapon = TrySwapWeapon(NextWeapon);
				if (gotWeapon)
					break;
			}
			if (!gotWeapon)
				SwapToBestWeapon();
		}
	}

	public void CheckWeaponChangeByIndex()
	{
		for (int i = 0; i < 10; i++)
		{
			if (Input.IsActionJustPressed("Action_WeaponSwitch_"+i))
			{
				TrySwapWeapon(i);
				break;
			}
		}
	}
}
