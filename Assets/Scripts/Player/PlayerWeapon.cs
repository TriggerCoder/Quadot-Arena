using Godot;
using System.Collections.Generic;
using ExtensionMethods;
public partial class PlayerWeapon : Node3D
{
	[Export]
	public Vector3 Offset = new Vector3(.2f, -.2f, -.14f);
	[Export]
	public Vector3 MuzzleOffset = new Vector3(-0.5f, 0f, 0);
	[Export]
	public MultiAudioStream audioStream;
	[Export]
	public string[] _sounds = new string[0];

	public AudioStreamWav[] Sounds = new AudioStreamWav[0];

	public Node3D muzzleObject = null;

	[Export]
	public string UIModelName;
	[Export]
	public string CompleteModelName;
	[Export]
	public string MuzzleModelName;
	[Export]
	public bool useCrosshair = true;
	[Export]
	public bool fullAuto = true;
	public virtual float avgDispersion { get { return .02f; } } //tan(2.3) / 2
	public virtual float maxDispersion { get { return .03f; } } //tan(3.4) / 2
	[Export]
	public int DamageMin = 5;
	[Export]
	public int DamageMax = 15;
	[Export]
	public float swapSpeed = 12f;
	[Export]
	public float kickSpeed = 6f;
	[Export]
	public float KickOffSet = .1f;

	public Vector2 Sensitivity = new Vector2(.015f, .01f);
	public float rotateSpeed = 4f;
	public float maxTurn = 3f;

	Vector2 MousePosition;
	Vector2 oldMousePosition = Vector2.Zero;

	public PlayerInfo playerInfo = null;

	public float LowerOffset = -.4f;
	public float LowerAmount = 1f;
	public float KickAmount = 0f;
	public int Noise = 0;

	public bool putAway = false;
	public bool weaponReady = false;

	public bool cooldown = false;
	public bool useCooldown = false;
	public float muzzleLightTime = 5f;
	public float cooldownTime = 0f;

	[Export]
	public float _fireRate = .4f;
	public float fireTime = 0f;
	public float faceTime = 1.5f;
	[Export]
	public float _muzzleTime = .1f;
	public float muzzleTimer = 0f;
	[Export]
	protected OmniLight3D muzzleLight;

	protected float coolTimer = 0f;

	private float interp = 0;
	private Vector3 oldPosition = Vector3.Down;
	private PhysicsPointQueryParameters3D PointIntersect;

	public string quadSound = "items/damage3";
	private List<MeshInstance3D> fxMeshes;
	public bool hasQuad = false;
	public override void _Ready()
	{
		Sounds = new AudioStreamWav[_sounds.Length];
		for (int i = 0; i < _sounds.Length; i++)
			Sounds[i] = SoundManager.LoadSound(_sounds[i]);

		if (!GameOptions.UseMuzzleLight)
		{
//			if (muzzleLight != null)
//				muzzleLight.SetProcess(false);
		}

		PointIntersect = new PhysicsPointQueryParameters3D();
		PointIntersect.CollideWithAreas = true;
		PointIntersect.CollideWithBodies = false;
		PointIntersect.CollisionMask = (1 << GameManager.FogLayer);

		Hide();
	}

	public void Init(PlayerInfo p)
	{
		playerInfo = p;
		playerInfo.WeaponHand.AddChild(this);
		playerInfo.WeaponHand.Position = Offset;
		MD3 model = ModelsManager.GetModel(UIModelName);
		if (model != null)
		{
			if (model.readySurfaceArray.Count == 0)
				Mesher.GenerateModelFromMeshes(model, p.uiLayer, false, false, this, false, false, null, true, false, true);
			else
				Mesher.FillModelFromProcessedData(model, p.uiLayer, false, false, this, false, null, false, true, false, true);

			if (playerInfo.playerThing.avatar != null)
				playerInfo.playerThing.avatar.LoadWeapon(model, CompleteModelName, MuzzleModelName, playerInfo.playerLayer);
		}

		if (model.tagsIdbyName.TryGetValue("tag_flash", out int tagId))
			MuzzleOffset = model.tagsbyId[tagId][0].origin;

		fxMeshes = GameManager.CreateFXMeshInstance3D(playerInfo.WeaponHand);

		if (!string.IsNullOrEmpty(MuzzleModelName))
		{
			muzzleObject = new Node3D();
			muzzleObject.Name = "Muzzle";
			AddChild(muzzleObject);
			muzzleObject.Position = MuzzleOffset;
			model = ModelsManager.GetModel(MuzzleModelName, true);
			if (model != null)
			{
				if (model.readySurfaceArray.Count == 0)
					Mesher.GenerateModelFromMeshes(model, p.uiLayer, false, false, muzzleObject, true, false, null, true, false, true);
				else
					Mesher.FillModelFromProcessedData(model, p.uiLayer, false, false, muzzleObject, false, null, false, true, false, true);
			}
			muzzleObject.Visible = false;
			if (muzzleLight != null)
			{
				muzzleLight.Reparent(muzzleObject);
				muzzleLight.Position = new Vector3(0, 0, .05f);
			}
		}

//		playerInfo.playerHUD.HUDUpdateAmmoNum();
		OnInit();
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (!Visible)
			Show();

		if (hasQuad != playerInfo.quadDamage) 
		{
			hasQuad = playerInfo.quadDamage;
			GameManager.ChangeQuadFx(fxMeshes,hasQuad,true);
		}

		float deltaTime = (float)delta;
		if (GameOptions.UseMuzzleLight)
		{
			if (muzzleLight != null)
			{
				if (muzzleLight.Visible)
				{
					muzzleLight.LightEnergy = Mathf.Max(Mathf.Lerp(muzzleLight.LightEnergy, 0, deltaTime * muzzleLightTime), 0);
					if (muzzleLight.LightEnergy <= 0.8f)
					{
						if (muzzleObject != null)
							if (muzzleObject.Visible)
							{
								muzzleObject.Visible = false;
								playerInfo.playerThing.avatar.MuzzleFlashSetActive(false);
							}
					}
				}
			}
		}

		MousePosition.X = playerInfo.playerControls.Look.X + playerInfo.playerControls.playerVelocity.X;
		MousePosition.Y = playerInfo.playerControls.Look.Y + playerInfo.playerControls.playerVelocity.Y;

		ApplyRotation(GetRotation((MousePosition - oldMousePosition) * Sensitivity, deltaTime), deltaTime);
		oldMousePosition = oldMousePosition.Lerp(MousePosition, rotateSpeed * deltaTime);

		if (putAway)
		{
			if (playerInfo.playerThing.avatar != null)
				playerInfo.playerThing.avatar.UnloadWeapon();

			LowerAmount = Mathf.Lerp(LowerAmount, 1, deltaTime * swapSpeed);
			if (LowerAmount > .99f)
			{
				playerInfo.playerPostProcessing.playerHUD.HideAmmo();
				QueueFree();
			}
		}
		else
			LowerAmount = Mathf.Lerp(LowerAmount, 0, deltaTime * swapSpeed);
		LowerAmount = Mathf.Clamp(LowerAmount, 0, 1);
		if (!weaponReady)
		{
			if (LowerAmount < 0.01f)
				weaponReady = true;
		}

		if (fireTime > 0)
		{
			if (fullAuto)
				KickAmount = Mathf.Lerp(KickAmount, 1, deltaTime * kickSpeed);
			else if (fireTime < 0.1f)
				KickAmount = Mathf.Lerp(KickAmount, 0, deltaTime * kickSpeed);
			else
				KickAmount = Mathf.Lerp(KickAmount, 1, deltaTime * kickSpeed);
		}
		else
			KickAmount = Mathf.Lerp(KickAmount, 0, deltaTime * kickSpeed);
		KickAmount = Mathf.Clamp(KickAmount, 0, 1);

		if (GameOptions.HeadBob && playerInfo.playerControls.bobActive)
			interp = Mathf.Lerp(interp, 1, deltaTime * 5);
		else
			interp = Mathf.Lerp(interp, 0, deltaTime * 6);
		interp = Mathf.Clamp(interp, 0, 1);

		Vector2 Bob = playerInfo.playerControls.currentBob * interp;

		Position = oldPosition.Lerp(new Vector3(KickOffSet * KickAmount, LowerOffset * LowerAmount + Bob.Y * .1f, Bob.X * .05f), Mathf.Clamp(10 * deltaTime, 0, 1));
		oldPosition = Position;
		OnUpdate();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		if (fireTime <= 0f)
		{
			faceTime = 1.5f;
			if ((useCooldown) && (cooldown))
			{
				coolTimer += deltaTime;
				if (coolTimer >= cooldownTime)
				{
					coolTimer = 0;
					cooldown = false;
				}
			}
		}
		else
		{
			fireTime -= deltaTime;
			if (fireTime < 0.1f)
				playerInfo.playerPostProcessing.playerHUD.SetAmmoCoolDown(false);

			if (fireTime <= 0)
			{
				faceTime = 1.5f;
				coolTimer = 0;
				if (useCooldown)
					cooldown = true;
				else
				{
				}
			}
			else
			{
				coolTimer += deltaTime;
				faceTime -= deltaTime;
				if (faceTime <= 0)
				{
					faceTime = 0.5f;
					playerInfo.playerPostProcessing.playerHUD.SetAttackFace();
				}
			}
		}
	}
	Quaternion GetRotation(Vector2 mouse, float delta)
	{
		mouse = mouse.LimitLength(maxTurn);

		Quaternion rotX = new Quaternion(Vector3.Forward, mouse.Y);
		Quaternion rotY = new Quaternion(Vector3.Up, mouse.X);

		Quaternion rotZ = GetRotate(delta).FastNormal();

		playerInfo.playerThing.avatar.RotateBarrel(rotZ, rotateSpeed * delta);

		Quaternion targetRot = rotX * rotY * rotZ;

		return targetRot.Normalized();
	}

	void ApplyRotation(Quaternion targetRot, float deltaTime)
	{
		Quaternion = Quaternion.Slerp(targetRot, rotateSpeed * deltaTime);
	}

	protected virtual Quaternion GetRotate(float deltaTime)
	{
		return Quaternion.Identity;
	}
	protected virtual void OnUpdate() { }
	protected virtual void OnInit() { }
	public virtual bool Fire()
	{
		return false;
	}

	public override void _ExitTree()
	{
		playerInfo.playerControls.playerWeapon = null;
	}
	public Vector2 GetDispersion()
	{
		Vector2 dispersion = new Vector2((float)GD.RandRange(-1f, 1f), (float)GD.RandRange(-1f, 1f));
		float dx = Mathf.Abs(dispersion.X);
		float dy = Mathf.Abs(dispersion.Y);

		if (dx == 1)
			return dispersion * maxDispersion;
		if (dy == 1)
			return dispersion * maxDispersion;
		if (dx + dy <= 1)
			return dispersion * avgDispersion;
		if (dx * dx + dy * dy <= 1)
			return dispersion * avgDispersion;
		return dispersion * maxDispersion;
	}

	public bool CheckIfCanMark(PhysicsDirectSpaceState3D SpaceState, CollisionObject3D collider, Vector3 collision)
	{
		//Check if mapcollider are noMarks
		if (MapLoader.noMarks.Contains(collider))
			return false;
		
		//Check if collision in inside a fog Area
		PointIntersect.Position = collision;

		var hits = SpaceState.IntersectPoint(PointIntersect);
		if (hits.Count == 0)
			return true;

		return false;
	}
}
