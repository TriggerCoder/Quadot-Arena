using Godot;
using System;
using ExtensionMethods;

public partial class PlayerControls : InterpolatedNode3D
{
	[Export]
	public PlayerInfo playerInfo;
	[Export]
	public PlayerThing playerThing;

	public PlayerWeapon playerWeapon;
	[Export]
	public PlayerCamera playerCamera;
	[Export]
	public AnimationTree weaponPositionAnimation;
	public SeparationRayShape3D feetRay;

	public CapsuleShape3D	collider;
	public CapsuleShape3D damageCollider;

	private Vector2 centerHeight = new Vector2(0.5f, 0.2f);	// character controller center height, x standing, y crouched
	private Vector2 height = new Vector2(1.6f, 1.1f);		// character controller height, x standing, y crouched
	private float camerasHeight = .05f;
	private float ccHeight = .05f;

	public Vector2 viewDirection = new Vector2(0, 0);

	public Vector3 lastGlobalPosition = new Vector3(0, 0, 0);

	public Vector3 impulseVector = Vector3.Zero;

	public Vector3 jumpPadVel = Vector3.Zero;

	// Movement stuff
	public const float crouchSpeed = 3.0f;					// Crouch speed
	public const float walkSpeed = 5.0f;					// Walk speed
	public const float runSpeed = 10.0f;					// Run speed
	public const float swimSpeed = 7.0f;					// Swim speed
	private float oldSpeed = 0;								// Previous move speed
	public float fallSpeed = 0;								// Acumulated fallSpeed

	public float moveSpeed;									// Ground move speed
	public const float runAcceleration = 14.0f;				// Ground accel
	public const float runDeacceleration = 10.0f;			// Deacceleration that occurs when running on the ground
	public const float waterAcceleration = 8.0f;			// Water accel
	public const float airAcceleration = 2.0f;				// Air accel
	public const float airDecceleration = 2.0f;				// Deacceleration experienced when ooposite strafing
	public const float airControl = 0.3f;					// How precise air control is
	public const float sideStrafeAcceleration = 50.0f;		// How fast acceleration occurs to get up to sideStrafeSpeed when
	public const float sideStrafeSpeed = 1.0f;				// What the max speed to generate when side strafing
	public const float jumpSpeed = 8.0f;					// The speed at which the character's up axis gains when hitting jump
	public const float fallSpeedLimit = -22f;				// The max fallSpeed without taking damage, modified to Quake Live value

	public Vector3 playerVelocity = Vector3.Zero;
	private Vector2 deadZone = Vector2.Zero;

	public bool WhishJump { get { return wishJump; } }

	private bool wishRestart = false;
	private bool wishJump = false;
	private bool wishSink = false;
	private bool wishFire = false;
	private bool wishActivate = false;
	private bool controllerIsGrounded = true;
	private bool controllerWasGrounded = true;
	private bool onLadder = false;
	private float deathTime = 0;
	private const float respawnDelay = 1.7f;

	private int lastJumpIndex = PlayerModel.LowerAnimation.Jump;
	private bool applyFallDamage = true;

	//Head/Weapon Bob
	public const float vBob = .005f;
	public const float hBob = .05f;
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

	public PlayerThing.FootStepType footStep = PlayerThing.FootStepType.Normal;

	public PlayerInput playerInput;
	public struct PlayerInput
	{
		private readonly int _Device;
		public int Device { get { return _Device; } }

		private readonly string _Start;
		public string Start { get { return _Start; } }
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
		private readonly string _Action_Item;
		public string Action_Item { get { return _Action_Item; } }
		private readonly string _Action_CameraSwitch;
		public string Action_CameraSwitch { get { return _Action_CameraSwitch; } }
		private readonly string _Action_WeaponSwitch_Up;
		public string Action_WeaponSwitch_Up { get { return _Action_WeaponSwitch_Up; } }
		private readonly string _Action_WeaponSwitch_Down;
		public string Action_WeaponSwitch_Down { get { return _Action_WeaponSwitch_Down; } }
		public PlayerInput(int num)
		{
			_Device = num;
			_Start = "Start_" + num;
			_Move_Forward = "Move_Forward_" + num;
			_Move_Back = "Move_Back_" + num;
			_Move_Left = "Move_Left_" + num;
			_Move_Right = "Move_Right_" + num;
			_Action_Fire = "Action_Fire_" + num;
			_Action_Jump = "Action_Jump_" + num;
			_Action_Crouch = "Action_Crouch_" + num;
			_Action_Run = "Action_Run_" + num;
			_Action_Item = "Action_Item_" + num;
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
		weaponPositionAnimation.Active = true;
		weaponPositionAnimation.Set("parameters/fall_shot/active", true);
	}

	public void Init(int contollerNum)
	{
		playerInput = new PlayerInput(contollerNum);
	}

	public override void _Input(InputEvent @event)
	{
		if (GameManager.Paused)
			return;

		if (@event is InputEventJoypadButton)
		{
			if (Input.IsActionJustPressed(playerInput.Start))
			{
				int Joy = playerInput.Device - 1;
				deadZone = new Vector2(Input.GetJoyAxis(Joy, JoyAxis.RightY), Input.GetJoyAxis(Joy, JoyAxis.RightX));
			}
		}

		if (playerInput.Device != GameManager.ControllerType.MouseKeyboard)
			return;

		if (GameManager.Console.visible)
			return;

		if (@event is InputEventMouseMotion eventMouseMotion)
		{
			Look = eventMouseMotion.Relative;
			viewDirection.Y -= Look.X * playerInfo.configData.MouseSensitivity[0];
			viewDirection.X -= Look.Y * playerInfo.configData.MouseSensitivity[1] * (playerInfo.configData.InvertView ? -1: 1);
		}
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		bool consoleOpen = false;

		if (playerThing.Dead)
		{
			if (playerCamera != null)
				bobActive = false;

			if (deathTime < respawnDelay)
				deathTime += deltaTime;
			else if (playerThing.interpolatedTransform == null)
			{
				if (wishRestart)
					return;
				if (Input.IsActionJustPressed(playerInput.Action_Jump) || Input.IsActionJustPressed(playerInput.Action_Fire))
					wishRestart = true;
			}
			return;
		}

		if (playerInput.Device != GameManager.ControllerType.MouseKeyboard)
		{
			int Joy = playerInput.Device - 1;
			float Y = Input.GetJoyAxis(Joy, JoyAxis.RightX);
			float X = Input.GetJoyAxis(Joy, JoyAxis.RightY);
			if (Mathf.Abs(Y) > deadZone.Y)
				Y = Y - Mathf.Sign(Y) * deadZone.Y;
			else
				Y = 0;
			if (Mathf.Abs(X) > deadZone.X)
				X = X - Mathf.Sign(X) * deadZone.X;
			else
				X = 0;
			viewDirection.Y -= (Y) * playerInfo.configData.StickSensitivity[0];
			viewDirection.X -= (X) * playerInfo.configData.StickSensitivity[1] * (playerInfo.configData.InvertView ? -1: 1);
		}
		else if (GameManager.Console.visible)
			consoleOpen = true;

		if (viewDirection.Y < -180)
			viewDirection.Y += 360;
		if (viewDirection.Y > 180)
			viewDirection.Y -= 360;

		//restricted up/down looking angle
		if (viewDirection.X < -85)
			viewDirection.X = -85;
		if (viewDirection.X > 85)
			viewDirection.X = 85;

		rotAngle.Y = viewDirection.Y;
		playerInfo.RotationDegrees = rotAngle;

		if (!playerThing.ready)
			return;

		controllerIsGrounded = playerThing.IsOnFloor();
		playerThing.avatar.isGrounded = controllerIsGrounded;

		if (consoleOpen)
			return;

		if (Input.IsActionJustPressed(playerInput.Action_CameraSwitch))
			playerCamera.ChangeThirdPersonCamera(!playerCamera.currentThirdPerson);

		playerThing.avatar.ChangeView(viewDirection, deltaTime);
		playerThing.avatar.CheckLegTurn(playerCamera.GlobalTransform.ForwardVector());

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

		bool doGoundChecks = false;
		PlayerThing.FootStepType currentFootStep = PlayerThing.FootStepType.None;
		//Movement Checks
		if (playerThing.waterLever > 0)
		{
			if (playerThing.waterLever < 2)
				doGoundChecks = true;
			else
			{
				if (controllerIsGrounded)
					playerThing.avatar.TurnLegs((int)currentMoveType, cMove.sidewaysSpeed, cMove.forwardSpeed, deltaTime);
				else
					playerThing.avatar.Swim();
				wishJump = Input.IsActionPressed(playerInput.Action_Jump);

				if (wishJump)
					wishSink = false;
				else
					wishSink = Input.IsActionPressed(playerInput.Action_Crouch);
			}
		}
		else
			doGoundChecks = true;

		if (doGoundChecks)
		{
			if (currentMoveType != MoveType.Crouch)
				QueueJump();
			if (controllerIsGrounded)
			{
				if (!controllerWasGrounded)
					AnimateLegsOnLand();
				else if (playerThing.avatar.enableOffset)
					playerThing.avatar.TurnLegs((int)currentMoveType, cMove.sidewaysSpeed, cMove.forwardSpeed, deltaTime);
				if (wishJump)
					AnimateLegsOnJump();
				else
				{
					KinematicCollision3D lastCollision = playerThing.GetLastSlideCollision();
					if (lastCollision != null)
					{
						CollisionObject3D collisionObject = (CollisionObject3D)lastCollision.GetCollider();
						if (MapLoader.mapSurfaceTypes.TryGetValue(collisionObject, out SurfaceType st))
						{
							if (playerThing.waterLever == 1)
								currentFootStep = PlayerThing.FootStepType.Splash;
							else if (st.NoSteps)
								currentFootStep = PlayerThing.FootStepType.None;
							else if (st.MetalSteps)
								currentFootStep = PlayerThing.FootStepType.Clank;
							else if (st.Flesh)
								currentFootStep = PlayerThing.FootStepType.Flesh;
							else
								currentFootStep = footStep;

							if (st.NoFallDamage)
								applyFallDamage = false;
							if (st.Ladder)
								onLadder = true;
						}

					}
				}

			}
			else if (!onLadder)
			{
				KinematicCollision3D lastCollision = playerThing.GetLastSlideCollision();
				if (lastCollision != null)
				{
					CollisionObject3D collisionObject = (CollisionObject3D)lastCollision.GetCollider();
					if (MapLoader.mapSurfaceTypes.TryGetValue(collisionObject, out SurfaceType st))
					{
						if (st.Ladder)
							onLadder = true;
					}
				}
				if (onLadder)
					playerThing.avatar.TurnLegs((int)currentMoveType, cMove.sidewaysSpeed, cMove.forwardSpeed, deltaTime);
				else
					playerThing.avatar.TurnLegsOnJump(cMove.sidewaysSpeed, deltaTime);
			}
		}

		if ((GlobalPosition - lastGlobalPosition).LengthSquared() > .0001f)
		{
			bobActive = true;
			if ((controllerIsGrounded) && (currentMoveType == MoveType.Run))
				playerThing.PlayStepSound(currentFootStep);
		}
		else
			bobActive = false;

		currentBob = GetBob();

		if (Input.IsActionPressed(playerInput.Action_Fire))
			wishFire = true;

		if (Input.IsActionPressed(playerInput.Action_Item))
			wishActivate = true;

		//swap weapon
		if (playerWeapon == null)
		{
			if (SwapWeapon == -1)
			{
				if (playerInfo.configData.SafeSwap)
					SwapToBestSafeWeapon();
				else
					SwapToBestWeapon();
			}

			if (SwapWeapon > -1)
			{
				CurrentWeapon = SwapWeapon;
				playerWeapon = (PlayerWeapon)playerInfo.WeaponPrefabs[CurrentWeapon].Instantiate();
				playerWeapon.Init(playerInfo);
				playerInfo.playerPostProcessing.playerHUD.ChangeWeapon(CurrentWeapon);
				SwapWeapon = -1;
			}
		}

		CheckMouseWheelWeaponChange();

		if (playerInput.Device == GameManager.ControllerType.MouseKeyboard)
			CheckWeaponChangeByIndex();
	}

	public void CheckCrash()
	{
		if (fallSpeed > fallSpeedLimit)
		{
			playerThing.PlayModelSound("land1", false, false);
			fallSpeed = 0;
			applyFallDamage = true;
			return;
		}
		if (fallSpeed > -30)
		{
			if (applyFallDamage)
				playerThing.Damage(5, DamageType.Land);
			weaponPositionAnimation.Set("parameters/TimeScale/scale", 1.4f);
			weaponPositionAnimation.Set("parameters/depth/add_amount", .75f);
		}
		else
		{
			if (applyFallDamage)
				playerThing.Damage(10, DamageType.Fall);
			weaponPositionAnimation.Set("parameters/TimeScale/scale", 1f);
			weaponPositionAnimation.Set("parameters/depth/add_amount", 1f);
		}
		applyFallDamage = true;
		weaponPositionAnimation.Set("parameters/fall_shot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
		fallSpeed = 0;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		if (playerThing.Dead)
		{
			if (wishRestart)
			{
				wishRestart = false;
				deathTime = 0;
				viewDirection = Vector2.Zero;

				if (playerWeapon != null)
				{
					playerWeapon.QueueFree();
					playerWeapon = null;
				}

				playerInfo.Reset();
				playerThing.InitPlayer();
				return;
			}

			// Reset the gravity velocity
			float gravityAccumulator = GameManager.Instance.gravity;
			if (playerThing.waterLever > 0)
				gravityAccumulator = GameManager.Instance.waterDeadFall;
			playerVelocity = Vector3.Down * gravityAccumulator;
			ApplyMove(deltaTime);
			return;
		}

		if (!playerThing.ready)
			return;

		bool doGoundChecks = false;
		//Movement Checks
		if (playerThing.waterLever > 0)
		{
			if (playerThing.waterLever > 1)
				WaterMove(deltaTime);
			else
				doGoundChecks = true;
		}
		else
			doGoundChecks = true;

        if (doGoundChecks)
        {
			if (onLadder)
				LadderMove(deltaTime);
			else if (controllerIsGrounded)
			{
				GroundMove(deltaTime);
				if (!controllerWasGrounded)
					CheckCrash();
			}
			else
				AirMove(deltaTime);
		}

		controllerWasGrounded = controllerIsGrounded;
		//apply move
		ApplyMove(deltaTime);

		//dampen jump pad impulse
		if (jumpPadVel.LengthSquared() > 0)
		{
			if (playerInfo.flight && wishJump)
			{
				playerVelocity.Y = jumpPadVel.Y;
				if (playerVelocity.Y > GameManager.Instance.flightAccel)
					playerVelocity.Y = GameManager.Instance.flightAccel;
				jumpPadVel = Vector3.Zero;
			}
			else
			{
				jumpPadVel.Y -= (GameManager.Instance.gravity * deltaTime);
				if (jumpPadVel.Y < 0)
				{
					fallSpeed = jumpPadVel.Y;
					if (controllerIsGrounded)
						jumpPadVel = Vector3.Zero;
				}
			}
		}

		if (wishFire)
		{
			wishFire = false;
			if (playerWeapon == null)
				return;

			if (!playerWeapon.weaponReady)
				return;

			if (playerWeapon.Fire())
			{
				playerThing.avatar.Attack();
			}
		}

		if (wishActivate)
		{
			wishActivate = false;
			switch (playerThing.holdableItem)
			{
				default:
				case PlayerThing.HoldableItem.None:
				break;
				case PlayerThing.HoldableItem.Teleporter:
					playerThing.holdableItem = PlayerThing.HoldableItem.None;
					playerInfo.playerPostProcessing.playerHUD.RemoveHoldableItem();
					SpawnerManager.SpawnToLocation(playerThing);
				break;
			}
		}
	}

	void ApplyMove(float deltaTime)
	{
		if ((playerThing.Dead == false) && (impulseVector.LengthSquared() > 0))
		{
			if ((controllerIsGrounded) && (impulseVector.Y > 0))
			{
				impulseVector.Y -= -GameManager.Instance.gravity * deltaTime;
				if (impulseVector.Y < 0)
					impulseVector.Y = 0;
			}
			playerVelocity += impulseVector;
			impulseVector = Vector3.Zero;
		}

		playerThing.Velocity = (playerVelocity + jumpPadVel);
		lastGlobalPosition = GlobalPosition;
		playerThing.MoveAndSlide();
		if (onLadder)
		{
			//Need to check if player is still on a ladder
			int count = playerThing.GetSlideCollisionCount();
			bool foundLadder = false;
			for (int i = 0; i < count; i++)
			{
				KinematicCollision3D slideCollision = playerThing.GetSlideCollision(i);
				if (slideCollision == null)
					continue;

				CollisionObject3D collisionObject = (CollisionObject3D)slideCollision.GetCollider();
				if (MapLoader.mapSurfaceTypes.TryGetValue(collisionObject, out SurfaceType st))
				{
					if (st.Ladder)
					{
						foundLadder = true;
						break;
					}
				}
			}
			onLadder = foundLadder;
		}
	}
	private void SetMovementDir()
	{
		Vector2 Move = Input.GetVector(playerInput.Move_Left, playerInput.Move_Right, playerInput.Move_Forward, playerInput.Move_Back);
		if ((GameManager.Console.visible) && (playerInput.Device == GameManager.ControllerType.MouseKeyboard))
			Move = Vector2.Zero;

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
		playerThing.damageShape.Position = new Vector3(0, newCenter, 0);

		collider.Height = newHeight;
		damageCollider.Height = newHeight - .1f; ;
		//Don't move camera on thirdperson
		if (playerCamera.currentThirdPerson)
			playerCamera.yOffset = .85f;
		else
			playerCamera.yOffset = 2 * newCenter + camerasHeight;
	}


	private void QueueJump()
	{
		if ((playerInfo.configData.AutoHop) || (playerInfo.flight))
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

	private void WaterMove(float deltaTime)
	{
		Vector3 wishdir;
		float curreAcel;

		playerVelocity.Y *= ApplyFriction(0.6f, deltaTime);

		SetMovementDir();

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		if (wishdir.LengthSquared() == 0)
		{
			if (!wishJump)
				wishdir.Y = -1;
		}
		else
			wishdir.Y = -cMove.forwardSpeed * viewDirection.X / 90;
		wishdir = playerInfo.Transform.Basis * wishdir;
		float wishspeed;
		wishdir = wishdir.GetLenghtAndNormalize(out wishspeed);
		wishspeed *= swimSpeed;

		//Check if Haste
		if (playerInfo.haste)
			wishspeed *= 1.3f;

		curreAcel = Accelerate(wishdir, wishspeed, waterAcceleration, deltaTime);
		playerVelocity.Y += curreAcel * wishdir.Y;
		if (wishJump)
			playerVelocity.Y += 1f;
		else if (wishSink)
			playerVelocity.Y -= 1f;
	}

	private void LadderMove(float deltaTime)
	{
		Vector3 wishdir;
		float friction = 1.0f;

		ApplyFriction(friction, deltaTime);

		SetMovementDir();

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		if (wishdir.LengthSquared() == 0)
			wishdir.Y = 0;
		else
			wishdir.Y = -cMove.forwardSpeed * viewDirection.X / 90;
		wishdir = playerInfo.Transform.Basis * wishdir;
		float wishspeed;
		wishdir = wishdir.GetLenghtAndNormalize(out wishspeed);
		wishspeed *= moveSpeed;

		//Check if Haste
		if (playerInfo.haste)
			wishspeed *= 1.3f;

		Accelerate(wishdir, wishspeed, runAcceleration, deltaTime, runSpeed);
		playerVelocity.Y = wishdir.Y * runSpeed;
	}
	private void GroundMove(float deltaTime)
	{
		Vector3 wishdir;
		float friction = 0;

		// Do not apply friction if the player is queueing up the next jump
		if (!wishJump)
			friction = 1.0f;
		if (playerThing.waterLever > 0)
			friction = 0.75f;

		ApplyFriction(friction, deltaTime);

		SetMovementDir();

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		wishdir = playerInfo.Transform.Basis * wishdir;
		float wishspeed;
		wishdir = wishdir.GetLenghtAndNormalize(out wishspeed);
		wishspeed *= moveSpeed;

		//Check if Haste
		if (playerInfo.haste)
			wishspeed *= 1.3f;

		Accelerate(wishdir, wishspeed, runAcceleration, deltaTime, runSpeed);

		// Reset the gravity velocity
		playerVelocity.Y = -GameManager.Instance.gravity * deltaTime;
		if (playerInfo.flight && wishJump)
			playerVelocity.Y = GameManager.Instance.gravity * deltaTime;
		else if ((controllerIsGrounded) && (wishJump))
		{
			float currentJumpSpeed = jumpSpeed;
			if (playerThing.waterLever > 0)
				currentJumpSpeed *= friction;
			playerVelocity.Y = currentJumpSpeed;
			wishJump = false;
		}
	}

	private float ApplyFriction(float t, float deltaTime)
	{
		Vector3 vec = playerVelocity;
		float speed;
		float newspeed;
		float control;
		float drop;

		vec.Y = 0.0f;
		speed = vec.Length();
		drop = 0.0f;

		if (playerThing.waterLever < 2)
		{
			control = speed < runDeacceleration ? runDeacceleration : speed;
			drop = control * GameManager.Instance.friction * deltaTime * t;
		}
		if (playerThing.waterLever > 0)
			drop += speed * GameManager.Instance.waterFriction * playerThing.waterLever * deltaTime * t;

		newspeed = speed - drop;

		if (newspeed < 0)
			newspeed = 0;
		if (speed > 0)
			newspeed /= speed;

		playerVelocity.X *= newspeed;
		playerVelocity.Z *= newspeed;
		return newspeed;
	}
	private float Accelerate(Vector3 wishdir, float wishspeed, float accel, float deltaTime, float wishaccel = 0)
	{
		float addspeed;
		float accelspeed;
		float currentspeed;
		bool autohop = false;
		currentspeed = playerVelocity.Dot(wishdir);
		addspeed = wishspeed - currentspeed;
		if (addspeed <= 0)
			return 0;
		if (wishaccel == 0)
			wishaccel = wishspeed;
		else if (playerInfo.configData.AutoHop && wishJump)
			autohop = true;

		accelspeed = accel * deltaTime * wishaccel;
		if (accelspeed > addspeed)
			accelspeed = addspeed;

		if (autohop)
			accelspeed *= .9f;

		playerVelocity.X += accelspeed * wishdir.X;
		playerVelocity.Z += accelspeed * wishdir.Z;
		return accelspeed;
	}

	private void AirMove(float deltaTime)
	{
		Vector3 wishdir;
		float accel;

		SetMovementDir();

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		wishdir = playerInfo.Transform.Basis * wishdir;
		float wishspeed;
		wishdir = wishdir.GetLenghtAndNormalize(out wishspeed);
		wishspeed *= moveSpeed;

		//Check if Haste
		if (playerInfo.haste)
			wishspeed *= 1.3f;

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
		{
			if (playerInfo.flight && wishJump)
			{
				playerVelocity.Y += GameManager.Instance.flightAccel * deltaTime;
				if (playerVelocity.Y > GameManager.Instance.flightAccel)
					playerVelocity.Y = GameManager.Instance.flightAccel;
			}
			else
			{
				if (playerInfo.flight && (playerVelocity.Y > 0))
					playerVelocity.Y = 0;
				playerVelocity.Y -= GameManager.Instance.gravity * deltaTime;
				fallSpeed = playerVelocity.Y;
			}
		}
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
		playerVelocity = playerVelocity.GetLenghtAndNormalize(out speed);

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

	public void AnimateLegsOnLand()
	{
		if (lastJumpIndex == PlayerModel.LowerAnimation.Jump)
			playerThing.avatar.lowerAnimation = PlayerModel.LowerAnimation.Land;
		else
			playerThing.avatar.lowerAnimation = PlayerModel.LowerAnimation.LandBack;
	}

	public void AnimateLegsOnJump()
	{
		if (cMove.forwardSpeed <= 0)
			lastJumpIndex = PlayerModel.LowerAnimation.Jump;
		else
			lastJumpIndex = PlayerModel.LowerAnimation.JumpBack;

		playerThing.avatar.lowerAnimation = lastJumpIndex;
		playerThing.avatar.enableOffset = false;
		playerThing.PlayModelSound("jump1", true, false);
	}

	public bool HasAmmo(int weapon)
	{
		switch (weapon)
		{
			default:
				return false;

			case PlayerInfo.Gauntlet:
				break;
			case PlayerInfo.HeavyMachineGun:
			case PlayerInfo.MachineGun:
				if (playerInfo.Ammo[PlayerInfo.bulletsAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.Shotgun:
				if (playerInfo.Ammo[PlayerInfo.shellsAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.GrenadeLauncher:
				if (playerInfo.Ammo[PlayerInfo.grenadesAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.RocketLauncher:
				if (playerInfo.Ammo[PlayerInfo.rocketsAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.LightningGun:
				if (playerInfo.Ammo[PlayerInfo.lightningAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.Railgun:
				if (playerInfo.Ammo[PlayerInfo.slugAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.PlasmaGun:
				if (playerInfo.Ammo[PlayerInfo.cellsAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.BFG10K:
				if (playerInfo.Ammo[PlayerInfo.bfgAmmo] < 40)
					return false;
				break;

			case PlayerInfo.NailGun:
				if (playerInfo.Ammo[PlayerInfo.nailAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.ChainGun:
				if (playerInfo.Ammo[PlayerInfo.chainAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.ProxLauncher:
				if (playerInfo.Ammo[PlayerInfo.minesAmmo] <= 0)
					return false;
				break;

		}
		return true;
	}
	public bool TrySwapWeapon(int weapon)
	{
		if (CurrentWeapon == weapon || SwapWeapon != -1)
			return false;

		if (weapon < 0 || weapon >= playerInfo.Weapon.Length)
			return false;

		if (!playerInfo.Weapon[weapon])
			return false;

		if (!HasAmmo(weapon))
			return false;

		if (playerWeapon != null)
		{
			if (!playerWeapon.weaponReady)
				return false;

			playerWeapon.putAway = true;
		}

		SwapWeapon = weapon;
		return true;
	}
	public void SwapToBestSafeWeapon()
	{
		if (TrySwapWeapon(PlayerInfo.BFG10K)) return;
		if (TrySwapWeapon(PlayerInfo.ChainGun)) return;
		if (TrySwapWeapon(PlayerInfo.PlasmaGun)) return;
		if (TrySwapWeapon(PlayerInfo.HeavyMachineGun)) return;
		if (TrySwapWeapon(PlayerInfo.LightningGun)) return;
		if (TrySwapWeapon(PlayerInfo.NailGun)) return;
		if (TrySwapWeapon(PlayerInfo.Shotgun)) return;
		if (TrySwapWeapon(PlayerInfo.MachineGun)) return;
		if (TrySwapWeapon(PlayerInfo.Railgun)) return;
		if (TrySwapWeapon(PlayerInfo.ProxLauncher)) return;
		if (TrySwapWeapon(PlayerInfo.RocketLauncher)) return;
		if (TrySwapWeapon(PlayerInfo.GrenadeLauncher)) return;
		if (TrySwapWeapon(PlayerInfo.Gauntlet)) return;
	}
	public void SwapToBestWeapon()
	{
		if (TrySwapWeapon(PlayerInfo.BFG10K)) return;
		if (TrySwapWeapon(PlayerInfo.ChainGun)) return;
		if (TrySwapWeapon(PlayerInfo.PlasmaGun)) return;
		if (TrySwapWeapon(PlayerInfo.HeavyMachineGun)) return;
		if (TrySwapWeapon(PlayerInfo.Railgun)) return;
		if (TrySwapWeapon(PlayerInfo.LightningGun)) return;
		if (TrySwapWeapon(PlayerInfo.RocketLauncher)) return;
		if (TrySwapWeapon(PlayerInfo.ProxLauncher)) return;
		if (TrySwapWeapon(PlayerInfo.GrenadeLauncher)) return;
		if (TrySwapWeapon(PlayerInfo.NailGun)) return;
		if (TrySwapWeapon(PlayerInfo.Shotgun)) return;
		if (TrySwapWeapon(PlayerInfo.MachineGun)) return;
		if (TrySwapWeapon(PlayerInfo.Gauntlet)) return;
	}
	public void CheckMouseWheelWeaponChange()
	{
		if (Input.IsActionJustPressed(playerInput.Action_WeaponSwitch_Up))
		{
			bool gotWeapon = false;
			for (int NextWeapon = CurrentWeapon + 1; NextWeapon < 14; NextWeapon++)
			{
				gotWeapon = TrySwapWeapon(NextWeapon);
				if (gotWeapon)
					break;
			}
			if (!gotWeapon)
			{
				if (TrySwapWeapon(PlayerInfo.MachineGun))
					return;
				TrySwapWeapon(PlayerInfo.Gauntlet);
			}
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
			{
				for (int NextWeapon = 13; NextWeapon >= 0; NextWeapon--)
				{
					gotWeapon = TrySwapWeapon(NextWeapon);
					if (gotWeapon)
						break;
				}
			}
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
