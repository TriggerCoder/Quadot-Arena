using ExtensionMethods;
using Godot;
using System;
public partial class PlayerWeapon : Node3D
{
	[Export]
	public Vector3 Offset = new Vector3(.2f, -.2f, -.14f);
	[Export]
	public Vector3 MuzzleOffset = new Vector3(-0.5f, 0f, 0);

	public Node3D muzzleObject = null;

	[Export]
	public string UIModelName;
	[Export]
	public string CompleteModelName;
	[Export]
	public string MuzzleModelName;
	[Export]
	public bool useCrosshair = true;
	public virtual float avgDispersion { get { return .02f; } } //tan(2.3) / 2
	public virtual float maxDispersion { get { return .03f; } } //tan(3.4) / 2
	[Export]
	public int DamageMin = 5;
	[Export]
	public int DamageMax = 15;

	public float swapSpeed = 6f;

	public Vector2 Sensitivity = new Vector2(.015f, .01f);
	public float rotateSpeed = 4f;
	public float maxTurn = 3f;

	Vector2 MousePosition;
	Vector2 oldMousePosition = Vector2.Zero;
	[Export]
	public string[] _sounds = new string[0];
//	public AudioClip[] Sounds = new AudioClip[0];


	public PlayerWeapon Instance;
	public PlayerInfo playerInfo = null;

	public float LowerOffset = -.3f;
	public float LowerAmount = 1f;

	public int Noise = 0;

	public bool putAway = false;
	public void PutAway() { if (Instance != null) Instance.putAway = true; }

	public bool cooldown = false;
	public bool useCooldown = false;
	public float muzzleLightTime = 5f;
	public float cooldownTime = 0f;


	public float _fireRate = .4f;
	public float fireTime = 0f;

	public float _muzzleTime = .1f;
	public float muzzleTimer = 0f;

	protected SpotLight3D muzzleLight;

	protected float coolTimer = 0f;

//	Transform cTransform;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
//		if (Instance != null)
//			Destroy(Instance.gameObject);

		Instance = this;

/*		Sounds = new AudioClip[_sounds.Length];
		for (int i = 0; i < _sounds.Length; i++)
			Sounds[i] = SoundLoader.LoadSound(_sounds[i]);
*/
		if (!GameOptions.UseMuzzleLight)
		{
			if (muzzleLight != null)
				muzzleLight.SetProcess(false);
		}
	}

	public void Init(PlayerInfo p)
	{
		playerInfo = p;
		playerInfo.WeaponHand.AddChild(this);
		playerInfo.WeaponHand.Position = Offset;
		MD3 model = ModelsManager.GetModel(UIModelName);
		if (model != null)
		{
//			if (model.readyMeshes.Count == 0)
				Mesher.GenerateModelFromMeshes(model, p.uiLayer, this);
//			else
//				Mesher.FillModelFromProcessedData(model, gameObject);

//			if (playerInfo.playerThing.avatar != null)
//				playerInfo.playerThing.avatar.LoadWeapon(model, CompleteModelName, MuzzleModelName, playerInfo.playerLayer);
		}

		if (model.tagsIdbyName.TryGetValue("tag_flash", out int tagId))
			MuzzleOffset = model.tagsbyId[tagId][0].origin;
/*
		cTransform = gameObject.transform;
		for (int d = 0; d < cTransform.childCount; d++)
		{
			cTransform.GetChild(d).gameObject.transform.localPosition = Vector3.zero;
			cTransform.GetChild(d).gameObject.transform.localScale = Vector3.one;
		}
*/
		if (!string.IsNullOrEmpty(MuzzleModelName))
		{
			muzzleObject = new Node3D();
			muzzleObject.Name = "Muzzle";
			AddChild(muzzleObject);
			muzzleObject.Position = MuzzleOffset;
			model = ModelsManager.GetModel(MuzzleModelName, true);
			if (model != null)
			{
//				if (model.readyMeshes.Count == 0)
					Mesher.GenerateModelFromMeshes(model, p.uiLayer, muzzleObject, true);
//				else
//					Mesher.FillModelFromProcessedData(model, muzzleObject);
			}
//			muzzleObject.Hide(); ;
/*			if (muzzleLight != null)
			{
				muzzleLight.transform.SetParent(muzzleObject.transform);
				muzzleLight.transform.localPosition = new Vector3(0, 0, .05f);
			}
*/		}

//		GameManager.SetLayerAllChildren(transform, playerInfo.playerLayer - 14);

//		oldMousePosition.X = playerInfo.playerControls.Look.X;
//		oldMousePosition.Y = playerInfo.playerControls.Look.Y;

//		playerInfo.playerHUD.HUDUpdateAmmoNum();
		OnInit();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
/*		if (GameOptions.UseMuzzleLight)
			if (muzzleLight != null)
			{
				if (muzzleLight.enabled)
				{
					muzzleLight.intensity = Mathf.Max(Mathf.Lerp(muzzleLight.intensity, 0, Time.deltaTime * muzzleLightTime), 0);
					if (muzzleLight.intensity <= 0.1f)
					{
						muzzleLight.intensity = 0;
						muzzleLight.enabled = false;
						if (muzzleObject != null)
							if (muzzleObject.activeSelf)
							{
								muzzleObject.SetActive(false);
								playerInfo.playerThing.avatar.MuzzleFlashSetActive(false);
							}
					}
					else if (muzzleLight.intensity <= 0.8f)
						if (muzzleObject != null)
							if (muzzleObject.activeSelf)
							{
								muzzleObject.SetActive(false);
								playerInfo.playerThing.avatar.MuzzleFlashSetActive(false);
							}
				}
			}
*/
		MousePosition.X = playerInfo.playerControls.Look.X + playerInfo.playerControls.playerVelocity.X;
		MousePosition.Y = playerInfo.playerControls.Look.Y + playerInfo.playerControls.playerVelocity.Y;

		ApplyRotation(GetRotation((MousePosition - oldMousePosition) * Sensitivity, deltaTime), deltaTime);
		oldMousePosition = oldMousePosition.Lerp(MousePosition, rotateSpeed * deltaTime);

		if (putAway)
		{
//			if (playerInfo.playerThing.avatar != null)
//				playerInfo.playerThing.avatar.UnloadWeapon();

			LowerAmount = Mathf.Lerp(LowerAmount, 1, deltaTime * swapSpeed);
//			if (LowerAmount > .99f)
//				DestroyAfterTime.DestroyObject(gameObject);
		}
		else
			LowerAmount = Mathf.Lerp(LowerAmount, 0, deltaTime * swapSpeed);

		Position = new Vector3(0, LowerOffset * LowerAmount, 0);

		OnUpdate();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		if (fireTime <= 0f)
		{
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

			if (fireTime <= 0)
			{
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
			}
		}
	}
	Quaternion GetRotation(Vector2 mouse, float delta)
	{
		mouse = mouse.LimitLength(maxTurn);

		Quaternion rotX = new Quaternion(Vector3.Forward, mouse.Y);
		Quaternion rotY = new Quaternion(Vector3.Up, mouse.X);

		Quaternion rotZ = GetRotate(delta);

//		playerInfo.playerThing.avatar.RotateBarrel(rotZ, rotateSpeed * delta);

		Quaternion targetRot = rotX * rotY * rotZ;

		return targetRot;
	}

	void ApplyRotation(Quaternion targetRot, float deltaTime)
	{
		Transform = new Transform3D(new Basis(Transform.Basis.GetRotationQuaternion().Slerp(targetRot, rotateSpeed * deltaTime)), Transform.Origin);
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
	void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}
	public Vector2 GetDispersion()
	{
		var random = new RandomNumberGenerator();
		Vector2 dispersion = new Vector2(random.RandfRange(-1f, 1f), random.RandfRange(-1f, 1f));
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
}
