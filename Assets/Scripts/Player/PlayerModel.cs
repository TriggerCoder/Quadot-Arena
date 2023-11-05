using Godot;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public partial class PlayerModel : Node3D
{
	public int rotationFPS = 15;

	private MD3 head;
	private MD3 upper;
	private MD3 lower;
	private MD3 weapon;

	public int upperAnimation = UpperAnimation.Stand;
	public int lowerAnimation = LowerAnimation.Idle;

	private int airFrames = 0;
	private const int readyToLand = 25;
	public bool enableOffset { get { return _enableOffset; } set { _enableOffset = value; } }
	public bool isGrounded { get { return _isGrounded; } set { if ((!_isGrounded) && (!value)) { airFrames++; if (airFrames > readyToLand) airFrames = readyToLand; } else airFrames = 0; _isGrounded = value; } }

	private bool _enableOffset = true;
	private bool _isGrounded = true;
	private List<ModelAnimation> upperAnim = new List<ModelAnimation>();
	private List<ModelAnimation> lowerAnim = new List<ModelAnimation>();

	private Dictionary<string, string> meshToSkin = new Dictionary<string, string>();
	private int upper_tag_torso = 0;
	private int lower_tag_torso = 0;
	private int upper_tag_head = 0;
	private int upper_tag_weapon = 0;

	private MD3GodotConverted lowerModel;
	private MD3GodotConverted upperModel;
	private MD3GodotConverted headModel;
	private MD3GodotConverted weaponModel;

	private ModelAnimation nextUpper;
	private ModelAnimation nextLower;

	private ModelAnimation currentUpper;
	private ModelAnimation currentLower;

	private int nextFrameUpper;
	private int nextFrameLower;

	private int currentFrameUpper;
	private int currentFrameLower;

	private bool loaded = false;
	private bool ragDoll = false;
	private bool ownerDead = false;
	public class ModelAnimation
	{
		public int index;
		public int startFrame;
		public int endFrame;
		public int loopingFrames;
		public int fps;
		public string strName;
		public int nextFrame = 1;
		public ModelAnimation(int index)
		{
			this.index = index;
		}
	}

	public static class UpperAnimation
	{
		public const int Death1 = 0;
		public const int Dead1 = 1;
		public const int Death2 = 2;
		public const int Dead2 = 3;
		public const int Death3 = 4;
		public const int Dead3 = 5;
		public const int Gesture = 6;
		public const int Attack = 7;
		public const int Melee = 8;
		public const int Drop = 9;
		public const int Raise = 10;
		public const int Stand = 11;
		public const int Stand2 = 12;
	}
	public static class LowerAnimation
	{
		public const int Death1 = 0;
		public const int Dead1 = 1;
		public const int Death2 = 2;
		public const int Dead2 = 3;
		public const int Death3 = 4;
		public const int Dead3 = 5;
		public const int WalkCR = 6;
		public const int Walk = 7;
		public const int Run = 8;
		public const int RunBack = 9;
		public const int Swim = 10;
		public const int Jump = 11;
		public const int Land = 12;
		public const int JumpBack = 13;
		public const int LandBack = 14;
		public const int Idle = 15;
		public const int IdleCR = 16;
		public const int Turn = 17;
		public const int WalkCRBack = 18;
		public const int Fall = 19;
		public const int WalkBack = 20;
		public const int FallBack = 21;
	}

	public int currentMoveType = MoveType.Run;
	private int nextMoveType = MoveType.Run;

	public static class MoveType
	{
		public const int Crouch = 0;
		public const int Walk = 1;
		public const int Run = 2;
	}

	private const int TotalAnimation = 29;

	private Node3D upperBody;
	private Node3D headBody;
	private Node3D barrel;
	private Node3D muzzleFlash;

	private Node3D lowerNode;

	private Node3D playerModel;
	private Node3D tagHeadNode;
	private Node3D weaponNode;

	private float upperLerpTime = 0;
	private float upperCurrentLerpTime = 0;
	private float lowerLerpTime = 0;
	private float lowerCurrentLerpTime = 0;

	private Vector3 turnTo = Vector3.Zero;

	private int hitpoints = 50;

	//Needed to keep impulse once it turn into ragdoll
	private PlayerControls playerControls;
	private float impulseDampening = 4f;
	private Vector3 impulseVector = Vector3.Zero;

	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }
	public BloodType BloodColor { get { return BloodType.Red; } }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
	/*
		void ApplySimpleMove()
		{
			float gravityAccumulator;
			Vector3 currentPosition = playerTransform.position;
			isGrounded = Physics.CheckSphere(currentPosition, .5f, (1 << GameManager.ColliderLayer), QueryTriggerInteraction.Ignore);
			if (isGrounded)
				gravityAccumulator = 0f;
			else
				gravityAccumulator = GameManager.Instance.gravity;
			Vector3 gravity = Vector3.down * gravityAccumulator;
			currentPosition += (gravity + impulseVector) * Time.deltaTime;

			//dampen impulse
			if (impulseVector.sqrMagnitude > 0)
			{
				impulseVector = Vector3.Lerp(impulseVector, Vector3.zero, impulseDampening * Time.deltaTime);
				if (impulseVector.sqrMagnitude < 1f)
					impulseVector = Vector3.zero;
			}
			rb.MovePosition(currentPosition);
		}
	*/
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (!loaded)
			return;

		float deltaTime = (float)delta;

		if (ragDoll)
		{
//			ApplySimpleMove();
			return;
		}

		if (turnTo.LengthSquared() > 0)
		{
//			playerTransform.forward = Vector3.Slerp(playerTransform.forward, turnTo, rotationFPS * deltaTime);
		}

		{
			nextUpper = upperAnim[upperAnimation];
			nextLower = lowerAnim[lowerAnimation];

			if (nextUpper.index == currentUpper.index)
			{
				nextFrameUpper = currentFrameUpper + 1;
				if (nextFrameUpper >= currentUpper.endFrame)
				{
					switch (nextUpper.index)
					{
						default:
							nextUpper = upperAnim[upperAnimation];
							nextFrameUpper = nextUpper.startFrame;
							break;
						case UpperAnimation.Death1:
						case UpperAnimation.Death2:
						case UpperAnimation.Death3:
							upperAnimation++;
							nextUpper = upperAnim[upperAnimation];
							nextFrameUpper = nextUpper.startFrame;
//							ChangeToRagDoll();
							return;
						case UpperAnimation.Attack:
						case UpperAnimation.Raise:
							upperAnimation = UpperAnimation.Stand;
							nextUpper = upperAnim[upperAnimation];
							nextFrameUpper = nextUpper.startFrame;
							break;
						case UpperAnimation.Drop:
							nextFrameUpper = currentUpper.endFrame;
							break;
					}
				}
			}
			else
				nextFrameUpper = nextUpper.startFrame;

			if (nextLower.index == currentLower.index)
			{
				nextFrameLower = currentFrameLower + currentLower.nextFrame;
				//Need to check if correct end frame depending on start frame
				if (((currentLower.nextFrame > 0)
				 && (nextFrameLower >= currentLower.endFrame)) ||
				 ((currentLower.nextFrame < 0)
				 && (nextFrameLower <= currentLower.endFrame)))
				{
					switch (nextLower.index)
					{
						default:

							break;
						case LowerAnimation.Death1:
						case LowerAnimation.Death2:
						case LowerAnimation.Death3:
							lowerAnimation++;
							break;
						case LowerAnimation.Jump:
							lowerAnimation = LowerAnimation.Land;
							break;
						case LowerAnimation.JumpBack:
							lowerAnimation = LowerAnimation.LandBack;
							break;
						case LowerAnimation.Land:
						case LowerAnimation.LandBack:
							lowerAnimation += 7;
							break;
						case LowerAnimation.Turn:
						case LowerAnimation.Fall:
						case LowerAnimation.FallBack:
							if (_isGrounded)
							{
								if (turnTo.LengthSquared() > 0)
								{
//									playerTransform.forward = turnTo;
									turnTo = Vector3.Zero;
								}
								lowerAnimation = LowerAnimation.Idle;
								_enableOffset = true;
							}
							break;
					}
					nextLower = lowerAnim[lowerAnimation];
					nextFrameLower = currentLower.startFrame;
				}
			}
			else
				nextFrameLower = nextLower.startFrame;

			Quaternion upperTorsoRotation = upper.tagsbyId[upper_tag_torso][currentFrameUpper].rotation.Slerp(upper.tagsbyId[upper_tag_torso][nextFrameUpper].rotation, upperCurrentLerpTime);
			Quaternion upperHeadRotation = upper.tagsbyId[upper_tag_head][currentFrameUpper].rotation.Slerp(upper.tagsbyId[upper_tag_head][nextFrameUpper].rotation, upperCurrentLerpTime);
			Quaternion lowerTorsoRotation = lower.tagsbyId[lower_tag_torso][currentFrameLower].rotation.Slerp(lower.tagsbyId[lower_tag_torso][nextFrameLower].rotation, lowerCurrentLerpTime);
			Quaternion weaponRotation = upper.tagsbyId[upper_tag_weapon][currentFrameUpper].rotation.Slerp(upper.tagsbyId[upper_tag_weapon][nextFrameUpper].rotation, upperCurrentLerpTime);

			Vector3 upperTorsoOrigin = upper.tagsbyId[upper_tag_torso][currentFrameUpper].origin.Lerp(upper.tagsbyId[upper_tag_torso][nextFrameUpper].origin, upperCurrentLerpTime);
			Vector3 upperHeadOrigin = upper.tagsbyId[upper_tag_head][currentFrameUpper].origin.Lerp(upper.tagsbyId[upper_tag_head][nextFrameUpper].origin, upperCurrentLerpTime);
			Vector3 lowerTorsoOrigin = lower.tagsbyId[lower_tag_torso][currentFrameLower].origin.Lerp(lower.tagsbyId[lower_tag_torso][nextFrameLower].origin, lowerCurrentLerpTime);
			Vector3 weaponOrigin = upper.tagsbyId[upper_tag_weapon][currentFrameUpper].origin.Lerp(upper.tagsbyId[upper_tag_weapon][nextFrameUpper].origin, upperCurrentLerpTime);

			{
				Vector3 currentOffset = lowerTorsoRotation * upperTorsoOrigin;
				Quaternion currentRotation = lowerTorsoRotation * upperTorsoRotation;

				for (int i = 0; i < upper.meshes.Count; i++)
				{
					MD3Mesh currentMesh = upper.meshes[i];

					for (int j = 0; j < currentMesh.numVertices; j++)
					{
						Vector3 newVertex = currentRotation * currentMesh.verts[currentFrameUpper][j].Lerp(currentMesh.verts[nextFrameUpper][j], upperCurrentLerpTime);
						newVertex += currentOffset;
						Vector3 newNormal = currentMesh.normals[currentFrameUpper][j].Lerp(currentMesh.normals[nextFrameUpper][j], upperCurrentLerpTime);
						upperModel.data[i].meshDataTool.SetVertex(j, newVertex);
						upperModel.data[i].meshDataTool.SetVertexNormal(j, newNormal);
					}
					upperModel.data[i].arrMesh.ClearSurfaces();
					upperModel.data[i].meshDataTool.CommitToSurface(upperModel.data[i].arrMesh);
				}

				Quaternion baseRotation = lowerTorsoRotation;
				currentOffset = baseRotation * upperHeadOrigin;
				currentRotation = baseRotation * upperHeadRotation;

				tagHeadNode.Position = currentOffset;
				tagHeadNode.Basis = new Basis(currentRotation);
//				tagHeadTransform.SetLocalPositionAndRotation(currentOffset, currentRotation);

				currentOffset = baseRotation * weaponOrigin;
				currentRotation = baseRotation * weaponRotation;

				weaponNode.Position = currentOffset;
				weaponNode.Basis = new Basis(currentRotation);
//				weaponTransform.SetLocalPositionAndRotation(currentOffset, currentRotation);


//				if ((_enableOffset) || (ownerDead))
				Position = lowerTorsoOrigin;
//				else
//					playerTransform.localPosition = Vector3.zero;

				currentOffset = upperTorsoRotation * upperTorsoOrigin;
				currentOffset -= lowerTorsoOrigin;
				currentRotation = upperTorsoRotation;

				for (int i = 0; i < lower.meshes.Count; i++)
				{
					MD3Mesh currentMesh = lower.meshes[i];

					for (int j = 0; j < currentMesh.numVertices; j++)
					{
						Vector3 newVertex = currentRotation * currentMesh.verts[currentFrameLower][j].Lerp(currentMesh.verts[nextFrameLower][j], lowerCurrentLerpTime);
						newVertex += currentOffset;
						Vector3 newNormal = currentMesh.normals[currentFrameLower][j].Lerp(currentMesh.normals[nextFrameLower][j], lowerCurrentLerpTime);
						lowerModel.data[i].meshDataTool.SetVertex(j, newVertex);
						lowerModel.data[i].meshDataTool.SetVertexNormal(j, newNormal);

					}
					lowerModel.data[i].arrMesh.ClearSurfaces();
					lowerModel.data[i].meshDataTool.CommitToSurface(lowerModel.data[i].arrMesh);
				}
			}

			upperLerpTime = nextUpper.fps * deltaTime;
			lowerLerpTime = nextLower.fps * deltaTime;

			upperCurrentLerpTime += upperLerpTime;
			lowerCurrentLerpTime += lowerLerpTime;

			if (upperCurrentLerpTime >= 1.0f)
			{
				upperCurrentLerpTime -= 1.0f;
				currentUpper = nextUpper;
				currentFrameUpper = nextFrameUpper;
			}

			if (lowerCurrentLerpTime >= 1.0f)
			{
				lowerCurrentLerpTime -= 1.0f;
				currentLower = nextLower;
				currentFrameLower = nextFrameLower;
			}
		}
	}

	public void ChangeView(Vector2 viewDirection, float deltaTime)
	{
		if (ownerDead)
			return;

		//In order to keep proper animation and not offset it by looking at target, otherwise head could go Exorcist-like
		if (!_enableOffset)
			return;

		float vView = viewDirection.X;
		float hView = viewDirection.Y;

		headBody.Basis = new Basis(headBody.Basis.GetRotationQuaternion().Slerp(Quaternion.FromEuler(new Vector3(0, hView + 90, vView)), rotationFPS * deltaTime));

		int vAngle = (int)Mathf.Round((vView) / (360) * 32) % 32;
		int hAngle = (int)Mathf.Round((hView + 90) / (360) * 32) % 32;

		upperBody.Basis = new Basis(upperBody.Basis.GetRotationQuaternion().Slerp(Quaternion.FromEuler(new Vector3(0, 11.25f * hAngle, 7.5f * vAngle)), rotationFPS * deltaTime));

	}

	public void CheckLegTurn(Vector3 direction)
	{
		if (ownerDead)
			return;

		Vector3 forward = Basis.Z;
		int angle = (int)Mathf.Round((Mathf.Atan2(direction.X, direction.Z)) / (Mathf.Pi * 2) * 8) % 8;

		//Player Models are rotated 90deg
		angle += 2;
		direction = Quaternion.FromEuler(new Vector3(0f, angle * 45f, 0f)) * Vector3.Forward;

		angle = (int)Mathf.Round(((Mathf.Atan2((forward.Z * direction.X) - (direction.Z * forward.X), (forward.X * direction.X) + (forward.Z * direction.Z)))) / (Mathf.Pi * 2) * 8) % 8;

		if (angle != 0)
		{
			turnTo = direction;
			if (lowerAnimation == LowerAnimation.Idle)
				lowerAnimation = LowerAnimation.Turn;
		}
	}
	public void Attack()
	{
		if (ownerDead)
			return;

		upperAnimation = UpperAnimation.Attack;
	}
	/*
	private void ChangeToRagDoll()
	{
		Vector3 currentPosition = playerTransform.position;
		Quaternion currentRotation = playerTransform.rotation;

		//Need to change head mesh from transform position and rotation offsets to vertex to get a correct collider
		Vector3 headOffset = tagHeadTransform.localPosition;
		Quaternion headRotation = tagHeadTransform.localRotation;

		for (int i = 0; i < head.meshes.Count; i++)
		{
			MD3Mesh currentMesh = head.meshes[i];
			currentVect.Clear();
			nextVect.Clear();
			//Head has only 1 frame
			currentVect.AddRange(currentMesh.verts[0]);
			for (int j = 0; j < currentVect.Count; j++)
			{
				currentVect[j] = headRotation * currentVect[j];
				currentVect[j] += headOffset;
			}

			headModel.data[i].meshFilter.mesh.SetVertices(currentVect);
			headModel.data[i].meshFilter.mesh.RecalculateNormals();
		}

		playerTransform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
		playerTransform.SetPositionAndRotation(currentPosition, currentRotation);
		tagHeadTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

		var meshFilterChildren = playerTransform.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		CombineInstance[] combine = new CombineInstance[meshFilterChildren.Length];
		for (var i = 0; i < combine.Length; i++)
			combine[i].mesh = meshFilterChildren[i].mesh;

		var mesh = new Mesh();
		mesh.CombineMeshes(combine, true, false, false);
		mesh.RecalculateNormals();

		MeshCollider mc = playerTransform.gameObject.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;
		mc.convex = true;
		rb = playerTransform.gameObject.AddComponent<Rigidbody>();
		rb.useGravity = false;
		rb.isKinematic = true;


		impulseVector = playerControls.impulseVector;
		playerControls.EnableColliders(false);
		ragDoll = true;

		DestroyAfterTime destroyAfterTime = playerTransform.gameObject.AddComponent<DestroyAfterTime>();
		destroyAfterTime._lifeTime = 10;
	}

	public void Die()
	{
		//Need to reset the torso and head view
		headTransform.localRotation = Quaternion.identity;
		upperTransform.localRotation = Quaternion.identity;

		int deathNum = 2 * UnityEngine.Random.Range(0, 3);
		upperAnimation = deathNum;
		lowerAnimation = deathNum;

		gameObject.layer = GameManager.RagdollLayer;
		GameManager.SetLayerAllChildren(playerTransform, GameManager.RagdollLayer);

		ownerDead = true;
	}
	*/
	public void TurnLegsOnJump(float sideMove)
	{
		Quaternion rotate = Quaternion.Identity;

		if (airFrames < readyToLand)
			return;

		switch (lowerAnimation)
		{
			default:
				return;
				break;
			case LowerAnimation.Idle:
			case LowerAnimation.IdleCR:
			case LowerAnimation.Run:
			case LowerAnimation.Walk:
			case LowerAnimation.WalkCR:
				lowerAnimation = LowerAnimation.Land;
				return;
				break;
			case LowerAnimation.RunBack:
			case LowerAnimation.WalkBack:
			case LowerAnimation.WalkCRBack:
				lowerAnimation = LowerAnimation.LandBack;
				return;
				break;
			case LowerAnimation.Land:
			case LowerAnimation.LandBack:
			case LowerAnimation.Fall:
			case LowerAnimation.FallBack:
				break;
		}

		if (sideMove > 0)
			rotate = new Quaternion(Basis.Y, 30f);
		else if (sideMove < 0)
			rotate = new Quaternion(Basis.Y, -30f);

		lowerNode.Basis = new Basis(rotate);
	}
	public void TurnLegs(int moveType, float sideMove, float forwardMove)
	{
		if (ownerDead)
			return;

		nextMoveType = moveType;

		Quaternion rotate = Quaternion.Identity;
		if (forwardMove < 0)
		{
			switch (nextMoveType)
			{
				default:
				case MoveType.Run:
					lowerAnimation = LowerAnimation.Run;
					break;
				case MoveType.Walk:
					lowerAnimation = LowerAnimation.Walk;
					break;
				case MoveType.Crouch:
					lowerAnimation = LowerAnimation.WalkCR;
					break;
			}
			if (sideMove > 0)
				rotate = new Quaternion(Basis.Y, 30f);
			else if (sideMove < 0)
				rotate = new Quaternion(Basis.Y, -30f);
		}
		else if (forwardMove > 0)
		{
			switch (nextMoveType)
			{
				default:
				case MoveType.Run:
					lowerAnimation = LowerAnimation.RunBack;
					break;
				case MoveType.Walk:
					lowerAnimation = LowerAnimation.WalkBack;
					break;
				case MoveType.Crouch:
					lowerAnimation = LowerAnimation.WalkCRBack;
					break;
			}
			if (sideMove > 0)
				rotate = new Quaternion(Basis.Y, -30f);
			else if (sideMove < 0)
				rotate = new Quaternion(Basis.Y, 30f);
		}
		else if (sideMove != 0)
		{
			switch (nextMoveType)
			{
				default:
				case MoveType.Run:
					lowerAnimation = LowerAnimation.Run;
					break;
				case MoveType.Walk:
					lowerAnimation = LowerAnimation.Walk;
					break;
				case MoveType.Crouch:
					lowerAnimation = LowerAnimation.WalkCR;
					break;
			}
			if (sideMove > 0)
				rotate = new Quaternion(Basis.Y, 50f);
			else if (sideMove < 0)
				rotate = new Quaternion(Basis.Y, -50f);
		}
		else if (lowerAnimation != LowerAnimation.Turn)
		{
			if (nextMoveType == MoveType.Crouch)
				lowerAnimation = LowerAnimation.IdleCR;
			else
				lowerAnimation = LowerAnimation.Idle;
		}
		lowerNode.Basis = new Basis(rotate);
	}

	public void MuzzleFlashSetActive(bool active)
	{
		if (muzzleFlash == null)
			return;

		muzzleFlash.Visible = active;
	}

	public void RotateBarrel(Quaternion rotation, float speed)
	{
		if (barrel == null)
			return;
		barrel.Basis = new Basis(barrel.Basis.GetRotationQuaternion().Slerp(rotation,speed));
	}

	public void LoadWeapon(MD3 newWeapon, string completeModelName, string muzzleModelName, uint layer)
	{
		if (ownerDead)
			return;

		if (weaponModel != null)
			if (weaponModel.node != null)
			{
				weaponModel.node.QueueFree();
				weaponModel.node = null;
			}

		if (!string.IsNullOrEmpty(completeModelName))
		{
			weapon = ModelsManager.GetModel(completeModelName);
			if (weapon == null)
				return;
		}
		else
			weapon = newWeapon;

		if (weapon.readySurfaceArray.Count == 0)
			weaponModel = Mesher.GenerateModelFromMeshes(weapon);
		else
			weaponModel = Mesher.FillModelFromProcessedData(weapon);
		weaponModel.node.Name = "weapon";
		weaponNode.AddChild(weaponModel.node);
//		weaponModel.go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

		if (!string.IsNullOrEmpty(completeModelName))
		{
			Vector3 OffSet = Vector3.Zero;
			barrel = new Node3D();
			barrel.Name = "barrel_weapon";
			if (newWeapon.readySurfaceArray.Count == 0)
				Mesher.GenerateModelFromMeshes(newWeapon, GameManager.AllPlayerViewMask, barrel);
			else
				Mesher.FillModelFromProcessedData(newWeapon, GameManager.AllPlayerViewMask, barrel);
			weaponModel.node.AddChild(barrel);

			if (weapon.tagsIdbyName.TryGetValue("tag_barrel", out int tagId))
				OffSet = weapon.tagsbyId[tagId][0].origin;
			barrel.Position = OffSet;
//			barrel.transform.SetLocalPositionAndRotation(OffSet, Quaternion.identity);
		}

		upperAnimation = UpperAnimation.Raise;

		if (!string.IsNullOrEmpty(muzzleModelName))
		{
			MD3GodotConverted muzzleUnityConverted;
			Vector3 OffSet = Vector3.Zero;
			MD3 weaponModelTags;
			muzzleFlash = new Node3D();
			muzzleFlash.Name = "muzzle_flash";
			MD3 muzzle = ModelsManager.GetModel(muzzleModelName, true);

			if (muzzle == null)
				return;

			if (muzzle.readySurfaceArray.Count == 0)
				muzzleUnityConverted = Mesher.GenerateModelFromMeshes(muzzle, GameManager.AllPlayerViewMask, muzzleFlash, true);
			else
				muzzleUnityConverted = Mesher.FillModelFromProcessedData(muzzle, GameManager.AllPlayerViewMask, muzzleFlash);

			//Muzzle Flash never cast shadow
//			for (int i = 0; i < muzzle.readyMeshes.Count; i++)
//				muzzleUnityConverted.data[i].meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			//Such Vanity
			if (barrel == null)
			{
				weaponModel.node.AddChild(muzzleFlash);
				weaponModelTags = weapon;
			}
			else
			{
				barrel.AddChild(muzzleFlash);
				weaponModelTags = newWeapon;
			}

			if (weaponModelTags.tagsIdbyName.TryGetValue("tag_flash", out int tagId))
				OffSet = weaponModelTags.tagsbyId[tagId][0].origin;
			muzzleFlash.Position = OffSet;
			muzzleFlash.Visible = false;
//			muzzleFlash.transform.SetLocalPositionAndRotation(OffSet, Quaternion.Identity);
//			muzzleFlash.SetActive(false);
		}

//		GameManager.SetLayerAllChildren(weaponTransform, layer);
	}

	public void UnloadWeapon()
	{
		if (weapon == null)
			return;

		if (weaponModel != null)
			if (weaponModel.node != null)
			{
				weaponModel.node.QueueFree();
				weaponModel.node = null;
			}

		weapon = null;
		barrel = null;
		muzzleFlash = null;

		if (ownerDead)
			return;

		upperAnimation = UpperAnimation.Drop;
	}
	public bool LoadPlayer(string modelName, string SkinName, uint layer, PlayerControls control)
	{
		string playerModelPath = "players/" + modelName;

		string lowerModelName = playerModelPath + "/lower";
		string upperModelName = playerModelPath + "/upper";
		string headModelName = playerModelPath + "/head";
		string animationFile = playerModelPath + "/animation";

		string lowerSkin = playerModelPath + "/lower_" + SkinName;
		string upperSkin = playerModelPath + "/upper_" + SkinName;
		string headSkin = playerModelPath + "/head_" + SkinName;

		lower = ModelsManager.GetModel(lowerModelName);
		if (lower == null)
			return false;
		upper = ModelsManager.GetModel(upperModelName);
		if (upper == null)
			return false;

		head = ModelsManager.GetModel(headModelName);
		if (head == null)
			return false;

		if (!LoadSkin(lower, lowerSkin))
			return false;
		if (!LoadSkin(upper, upperSkin))
			return false;
		if (!LoadSkin(head, headSkin))
			return false;

		upper_tag_torso = upper.tagsIdbyName["tag_torso"];
		lower_tag_torso = lower.tagsIdbyName["tag_torso"];
		upper_tag_head = upper.tagsIdbyName["tag_head"];
		upper_tag_weapon = upper.tagsIdbyName["tag_weapon"];

		LoadAnimations(animationFile, upperAnim, lowerAnim);
		currentUpper = upperAnim[UpperAnimation.Stand];
		currentFrameUpper = currentUpper.startFrame;
		currentLower = lowerAnim[LowerAnimation.Idle];
		currentFrameLower = currentLower.startFrame;

		{
			playerModel = this;
			playerModel.Name = modelName;

			upperBody = new Node3D();
			upperBody.Name = "Upper Body";
			playerModel.AddChild(upperBody);

			tagHeadNode = new Node3D();
			tagHeadNode.Name = "tag_head";
			upperBody.AddChild(tagHeadNode);

			weaponNode = new Node3D();
			weaponNode.Name = "tag_weapon";
			upperBody.AddChild(weaponNode);

			headBody = new Node3D();
			headBody.Name = "Head";
			tagHeadNode.AddChild(headBody);

			if (upper.readySurfaceArray.Count == 0)
				upperModel = Mesher.GenerateModelFromMeshes(upper, meshToSkin);
			else
				upperModel = Mesher.FillModelFromProcessedData(upper, meshToSkin);
			upperModel.node.Name = "upper_body";
			upperBody.AddChild(upperModel.node);

			if (head.readySurfaceArray.Count == 0)
				headModel = Mesher.GenerateModelFromMeshes(head, meshToSkin);
			else
				headModel = Mesher.FillModelFromProcessedData(head, meshToSkin);

			headModel.node.Name = "head";
			headBody.AddChild(headModel.node);

			if (lower.readySurfaceArray.Count == 0)
				lowerModel = Mesher.GenerateModelFromMeshes(lower, meshToSkin);
			else
				lowerModel = Mesher.FillModelFromProcessedData(lower, meshToSkin);
			lowerModel.node.Name = "lower_body";
			lowerNode = lowerModel.node;
			playerModel.AddChild(lowerModel.node);

			loaded = true;
		}
		playerControls = control;

		return true;
	}

	private bool LoadAnimations(string fileName, List<ModelAnimation> upper, List<ModelAnimation> lower)
	{
		StreamReader animFile;

		string path = Directory.GetCurrentDirectory() + "/StreamingAssets/models/" + fileName + ".cfg";
		if (File.Exists(path))
			animFile = new StreamReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.ContainsKey(path = ("models/" + fileName + ".cfg").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			var reader = new ZipReader();
			reader.Open(FileName);
			MemoryStream ms = new MemoryStream(reader.ReadFile(path, false));
			animFile = new StreamReader(ms);
		}
		else
		{
			GD.Print("Unable to load animation file: " + fileName);
			return false;
		}

		animFile.BaseStream.Seek(0, SeekOrigin.Begin);
		ModelAnimation[] animations = new ModelAnimation[TotalAnimation];

		if (animFile.EndOfStream)
		{
			return false;
		}

		string strWord;
		int currentAnim = 0;
		int torsoOffset = 0;
		int legsOffset = (int)LowerAnimation.WalkCR + 1;
		char[] separators = new char[2] { '\t', '(' };
		while (!animFile.EndOfStream)
		{
			strWord = animFile.ReadLine();

			if (strWord.Length == 0)
				continue;

			if (!char.IsDigit(strWord[0]))
			{
				continue;
			}

			string[] values = new string[4] { "", "", "", "" };
			bool lastDigit = true;
			for (int i = 0, j = 0; i < strWord.Length; i++)
			{
				if (char.IsDigit(strWord[i]))
				{
					if (lastDigit)
						values[j] += strWord[i];
					else
					{
						j++;
						values[j] += strWord[i];
						lastDigit = true;
					}
				}
				else
					lastDigit = false;

				if ((j == 3) && (!lastDigit))
					break;
			}

			int startFrame = int.Parse(values[0]);
			int numOfFrames = int.Parse(values[1]);
			int loopingFrames = int.Parse(values[2]);
			int fps = int.Parse(values[3]);

			animations[currentAnim] = new ModelAnimation(currentAnim);
			animations[currentAnim].startFrame = startFrame;
			animations[currentAnim].endFrame = startFrame + numOfFrames;
			animations[currentAnim].loopingFrames = loopingFrames;
			animations[currentAnim].fps = fps;

			string[] name = strWord.Split('/');
			strWord = name[name.Length - 1].Trim();
			name = strWord.Split(separators);
			animations[currentAnim].strName = name[0];

			if (IsInString(animations[currentAnim].strName, "BOTH"))
			{
				upper.Add(animations[currentAnim]);
				lower.Add(animations[currentAnim]);
			}
			else if (IsInString(animations[currentAnim].strName, "TORSO"))
			{
				upper.Add(animations[currentAnim]);
			}
			else if (IsInString(animations[currentAnim].strName, "LEGS"))
			{
				if (torsoOffset == 0)
					torsoOffset = animations[(int)UpperAnimation.Stand2 + 1].startFrame - animations[(int)LowerAnimation.WalkCR].startFrame;

				animations[currentAnim].startFrame -= torsoOffset;
				animations[currentAnim].endFrame -= torsoOffset;
				animations[currentAnim].index -= legsOffset;
				lower.Add(animations[currentAnim]);
			}
			currentAnim++;
		}
		//Add Walk Crounched Back 
		animations[currentAnim] = new ModelAnimation((int)LowerAnimation.WalkCRBack);
		animations[currentAnim].startFrame = lowerAnim[(int)LowerAnimation.WalkCR].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[(int)LowerAnimation.WalkCR].startFrame - 1;
		animations[currentAnim].loopingFrames = lowerAnim[(int)LowerAnimation.WalkCR].loopingFrames;
		animations[currentAnim].fps = lowerAnim[(int)LowerAnimation.WalkCR].fps;
		animations[currentAnim].nextFrame = -1;
		lower.Add(animations[currentAnim++]);

		//Add Fall
		animations[currentAnim] = new ModelAnimation((int)LowerAnimation.Fall);
		animations[currentAnim].startFrame = lowerAnim[(int)LowerAnimation.Land].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[(int)LowerAnimation.Land].endFrame;
		animations[currentAnim].loopingFrames = 0;
		animations[currentAnim].fps = lowerAnim[(int)LowerAnimation.Land].fps;
		lower.Add(animations[currentAnim++]);

		//Add Walk Back
		animations[currentAnim] = new ModelAnimation((int)LowerAnimation.WalkBack);
		animations[currentAnim].startFrame = lowerAnim[(int)LowerAnimation.Walk].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[(int)LowerAnimation.Walk].startFrame - 1;
		animations[currentAnim].loopingFrames = lowerAnim[(int)LowerAnimation.Walk].loopingFrames;
		animations[currentAnim].fps = lowerAnim[(int)LowerAnimation.Walk].fps;
		animations[currentAnim].nextFrame = -1;
		lower.Add(animations[currentAnim++]);

		//Add Fall Back
		animations[currentAnim] = new ModelAnimation((int)LowerAnimation.FallBack);
		animations[currentAnim].startFrame = lowerAnim[(int)LowerAnimation.LandBack].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[(int)LowerAnimation.LandBack].endFrame;
		animations[currentAnim].loopingFrames = 0;
		animations[currentAnim].fps = lowerAnim[(int)LowerAnimation.LandBack].fps;
		lower.Add(animations[currentAnim]);

		animFile.Close();
		return true;
	}
	public bool LoadSkin(MD3 model, string skinName)
	{
		StreamReader SkinFile;

		string path = Directory.GetCurrentDirectory() + "/StreamingAssets/models/" + skinName + ".skin";
		if (File.Exists(path))
			SkinFile = new StreamReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.ContainsKey(path = ("models/" + skinName + ".skin").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			var reader = new ZipReader();
			reader.Open(FileName);
			MemoryStream ms = new MemoryStream(reader.ReadFile(path, false));
			SkinFile = new StreamReader(ms);
		}
		else
		{
			GD.Print("Unable to load skin for model: " + model.name);
			return false;
		}

		SkinFile.BaseStream.Seek(0, SeekOrigin.Begin);

		if (SkinFile.EndOfStream)
		{
			GD.Print("Unable to load skin for model: " + model.name);
			return false;
		}

		// These 2 variables are for reading in each line from the file, then storing
		// the index of where the bitmap name starts after the ',' character.
		string strLine;
		int textureNameStart = 0;

		// Go through every line in the .skin file
		while (!SkinFile.EndOfStream)
		{
			strLine = SkinFile.ReadLine();

			// Loop through all of our objects to test if their name is in this line
			for (int i = 0; i < model.meshes.Count; i++)
			{
				// Check if the name of this mesh appears in this line from the skin file
				if (IsInString(strLine, model.meshes[i].name))
				{
					// To abstract the texture name, we loop through the string, starting
					// at the end of it until we find a '/' character, then save that index + 1.
					for (int j = strLine.Length - 1; j > 0; j--)
					{
						// If this character is a ',', save the index + 1
						if (strLine[j] == ',')
						{
							// Save the index + 1 (the start of the texture name) and break
							textureNameStart = j + 1;
							break;
						}
					}
					string skin = strLine.Substring(textureNameStart);
					//Need to strip extension
					string[] fullName = skin.Split('.');

					//Check if skin texture exist, if not add it
					if (!TextureLoader.HasTexture(fullName[0]))
						TextureLoader.AddNewTexture(fullName[0], false);

					meshToSkin.Add(model.meshes[i].name, fullName[0]);
				}
			}
		}
		SkinFile.Close();
		return true;
	}

	private bool IsInString(string strString, string strSubString)
	{
		// Make sure both of these strings are valid, return false if any are empty
		if (string.IsNullOrEmpty(strString) || string.IsNullOrEmpty(strSubString))
			return false;

		// grab the starting index where the sub string is in the original string
		uint index = (uint)strString.IndexOf(strSubString);

		// Make sure the index returned was valid
		if (index >= 0 && index < strString.Length)
			return true;

		// The sub string does not exist in strString.
		return false;
	}
}
