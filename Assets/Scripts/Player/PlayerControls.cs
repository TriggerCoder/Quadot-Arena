using Godot;
using System;
using System.Globalization;

public partial class PlayerControls : InterpolatedNode3D
{
	[Export]
	public PlayerInfo playerInfo;
	[Export]
	public PlayerThing playerThing;

	public PlayerWeapon playerWeapon;
	[Export]
	public PlayerCamera playerCamera;

	public SeparationRayShape3D feetRay;

	public CapsuleShape3D	collider;

	private Vector2 centerHeight = new Vector2(0.4f, 0.2f);	// character controller center height, x standing, y crouched
	private Vector2 height = new Vector2(1.5f, 1.1f);			// character controller height, x standing, y crouched
	private float camerasHeight = .05f;
	private float ccHeight = .05f;

	public Vector2 viewDirection = new Vector2(0, 0);

	public Vector3 lastGlobalPosition = new Vector3(0, 0, 0);

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

	//Head/Weapon Bob
	public float vBob = .005f;
	public float hBob = .05f;
	public bool bobActive;
	public Vector2 currentBob = Vector2.Zero;

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

	public PlayerInput playerInput;
	public struct PlayerInput
	{
		private readonly int _Device;
		public int Device { get { return _Device; } }

		private readonly string _Move_Forward;
		public string Move_Forward { get { return _Move_Forward; } }

		private readonly string _Move_Back;
		public string Move_Back { get { return _Move_Back; } }
		private readonly string _Move_Left;
		public string Move_Left { get { return _Move_Left; } }
		private readonly string _Move_Right;
		public string Move_Right { get { return _Move_Right; } }
		private readonly string _Action_Fire;
		public string Action_Fire { get { return _Action_Fire; } }
		private readonly string _Action_Jump;
		public string Action_Jump { get { return _Action_Jump; } }
		private readonly string _Action_Crouch;
		public string Action_Crouch { get { return _Action_Crouch; } }
		private readonly string _Action_Run;
		public string Action_Run { get { return _Action_Run; } }
		private readonly string _Action_CameraSwitch;
		public string Action_CameraSwitch { get { return _Action_CameraSwitch; } }
		private readonly string _Action_WeaponSwitch_Up;
		public string Action_WeaponSwitch_Up { get { return _Action_WeaponSwitch_Up; } }
		private readonly string _Action_WeaponSwitch_Down;
		public string Action_WeaponSwitch_Down { get { return _Action_WeaponSwitch_Down; } }
		public PlayerInput(int num)
		{
			_Device = num;
			_Move_Forward = "Move_Forward_" + num;
			_Move_Back = "Move_Back_" + num;
			_Move_Left = "Move_Left_" + num;
			_Move_Right = "Move_Right_" + num;
			_Action_Fire = "Action_Fire_" + num;
			_Action_Jump = "Action_Jump_" + num;
			_Action_Crouch = "Action_Crouch_" + num;
			_Action_Run = "Action_Run_" + num;
			_Action_CameraSwitch = "Action_CameraSwitch_" + num;
			_Action_WeaponSwitch_Up = "Action_WeaponSwitch_Up_" + num;
			_Action_WeaponSwitch_Down = "Action_WeaponSwitch_Down_" + num;
		}
	}

	private Vector3 rotAngle = Vector3.Zero;

	public override void _Ready()
	{
		moveSpeed = runSpeed;
		currentMoveType = MoveType.Run;
		playerThing.CollisionLayer = playerInfo.playerLayer;
	}

	public void Init(int contollerNum)
	{
		playerInput = new PlayerInput(contollerNum);
	}

	public override void _Input(InputEvent @event)
	{
		if (GameManager.Paused)
			return;

		switch (playerInput.Device)
		{
			default:

			break;
			case GameManager.ControllerType.MouseKeyboard:
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
			break;
		}

	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;
		
		float deltaTime = (float)delta;

		if (playerThing.Dead)
		{
			if (playerCamera != null)
				bobActive = false;

			if (deathTime < respawnDelay)
				deathTime += deltaTime;
			else
			{
				if (Input.IsActionJustPressed(playerInput.Action_Jump) || Input.IsActionJustPressed(playerInput.Action_Fire))
				{
					deathTime = 0;
					viewDirection = Vector2.Zero;

					if (playerWeapon != null)
					{
						playerWeapon.QueueFree();
						playerWeapon = null;
					}

					playerInfo.Reset();
					playerThing.InitPlayer();
				}
			}
			return;
		}

		if (!playerThing.ready)
			return;

		if (Input.IsActionJustPressed(playerInput.Action_CameraSwitch))
			playerCamera.ChangeThirdPersonCamera(!playerCamera.currentThirdPerson);

		playerThing.avatar.ChangeView(viewDirection, deltaTime);
		playerThing.avatar.CheckLegTurn(playerCamera.GlobalTransform.Basis.Z);

		controllerIsGrounded = playerThing.IsOnFloor();
		playerThing.avatar.isGrounded = controllerIsGrounded;
		//Player can only crounch if it is grounded
		if ((Input.IsActionJustPressed(playerInput.Action_Crouch)) && (controllerIsGrounded))
		{
			if (oldSpeed == 0)
				oldSpeed = moveSpeed;
			moveSpeed = crouchSpeed;
			currentMoveType = MoveType.Crouch;
			ChangeHeight(false);
		}
		else if (Input.IsActionJustReleased(playerInput.Action_Crouch))
		{
			if (oldSpeed != 0)
				moveSpeed = oldSpeed;
			if (moveSpeed == walkSpeed)
				currentMoveType = MoveType.Walk;
			else
				currentMoveType = MoveType.Run;
			ChangeHeight(true);
			oldSpeed = 0;
		}
		else //CheckRun
		{
			if (GameOptions.runToggle)
			{
				if (Input.IsActionJustReleased(playerInput.Action_Run))
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
				if (Input.IsActionPressed(playerInput.Action_Run))
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
				playerThing.avatar.TurnLegs((int)currentMoveType, cMove.sidewaysSpeed, cMove.forwardSpeed, deltaTime);
			if (wishJump)
				AnimateLegsOnJump();
		}
		else
			playerThing.avatar.TurnLegsOnJump(cMove.sidewaysSpeed, deltaTime);

		if ((GlobalPosition - lastGlobalPosition).LengthSquared() > .0001f)
			bobActive = true;
		else
			bobActive = false;

		currentBob = GetBob();

		if (Input.IsActionPressed(playerInput.Action_Fire))
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

		if (playerInput.Device == GameManager.ControllerType.MouseKeyboard)
			CheckWeaponChangeByIndex();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		rotAngle.Y = viewDirection.Y;
		playerInfo.RotationDegrees = rotAngle;
		rotAngle.Y -= 30f;
		for (int i = 0; i < playerThing.weaponCollider.Length; i++)
		{
			Vector3 weaponColliderAngles = playerThing.weaponCollider[i].RotationDegrees;
			playerThing.weaponCollider[i].RotationDegrees = new Vector3(weaponColliderAngles.X, rotAngle.Y + (30f * i), weaponColliderAngles.Z);
		}
		float deltaTime = (float)delta;

		if (playerThing.Dead)
		{
//			if (controller.enabled)
			{
				// Reset the gravity velocity
				playerVelocity = Vector3.Down * GameManager.Instance.gravity;
				ApplyMove(deltaTime);
			}
			return;
		}

		if (!playerThing.ready)
			return;

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
		lastGlobalPosition = GlobalPosition;
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
		Vector2 Move = Input.GetVector(playerInput.Move_Left, playerInput.Move_Right, playerInput.Move_Forward, playerInput.Move_Back);

		cMove.forwardSpeed = Move.Y;
		cMove.sidewaysSpeed = Move.X;
	}

	public void ChangeHeight(bool Standing)
	{
		float newCenter = centerHeight.Y;
		float newHeight = height.Y;

		if (Standing)
		{
			newCenter = centerHeight.X;
			newHeight = height.X;
		}
		playerThing.Torso.Position = new Vector3(0, newCenter, 0);
		collider.Height = newHeight;

		//Don't move camera on thirdperson
		if (playerCamera.currentThirdPerson)
			playerCamera.yOffset = .85f;
		else
			playerCamera.yOffset = 2 * newCenter + camerasHeight;
	}


	private void QueueJump()
	{
		if (holdJumpToBhop)
		{
			wishJump = Input.IsActionPressed(playerInput.Action_Jump);
			return;
		}

		if (Input.IsActionJustPressed(playerInput.Action_Jump) && !wishJump)
			wishJump = true;
		if (Input.IsActionJustReleased(playerInput.Action_Jump))
			wishJump = false;
	}

	private Vector2 GetBob()
	{
		Vector2 bob;
		float speed = playerVelocity.Length();
		float moveSpeed = walkSpeed;
		if (moveSpeed != walkSpeed)
			moveSpeed = runSpeed;
		bob.X = Mathf.Cos(GameManager.CurrentTimeMsec * moveSpeed) * hBob * speed;
		if (currentMoveType == MoveType.Crouch)
			bob.X *= 5;

		bob.Y = Mathf.Sin(GameManager.CurrentTimeMsec * moveSpeed) * vBob * speed;
		if (currentMoveType == MoveType.Crouch)
			bob.Y *= 5;

		return bob;
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
		if (Input.IsActionJustPressed(playerInput.Action_WeaponSwitch_Up))
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
		else if (Input.IsActionJustPressed(playerInput.Action_WeaponSwitch_Down))
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
