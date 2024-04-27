using Godot;
using System.IO;
using System.Collections.Generic;
using ExtensionMethods;

public partial class PlayerModel : RigidBody3D, Damageable
{
	public int rotationFPS = 15;
	public int lowerRotationFPS = 7;

	private MD3 head;
	private MD3 upper;
	private MD3 lower;
	private MD3 weapon;

	public int upperAnimation = UpperAnimation.Stand;
	public int lowerAnimation = LowerAnimation.Idle;

	private int airFrames = 0;
	private const int readyToLand = 25;
	private uint currentLayer;
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
	private bool destroyWeapon = false;
	private bool deadWater = false;
	private bool isMeleeWeapon = false;
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
	private const string defaultModel = "sarge";
	private const string defaultSkin = "default";

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

	private Quaternion QuaternionZero = new Quaternion(0, 0, 0, 0);
	private Quaternion turnTo = new Quaternion(0, 0, 0, 0);
	private List<MeshInstance3D> modelsMeshes = new List<MeshInstance3D>();
	private List<MeshInstance3D> fxMeshes = new List<MeshInstance3D>();
	private int hitpoints = 50;
	private List<MultiMeshData> multiMeshDataList = new List<MultiMeshData>();
	private Vector3 lastGlobalPosition = new Vector3(0, 0, 0);
	private Basis lastGlobalBasis = Basis.Identity;

	//Needed to keep impulse once it turn into ragdoll
	private PlayerControls playerControls;
	private float impulseDampening = 4f;
	private Vector3 impulseVector = Vector3.Zero;

	private PhysicsShapeQueryParameters3D BodyCast;
	private PhysicsDirectSpaceState3D SpaceState;
	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }
	public BloodType BloodColor { get { return BloodType.Red; } }

	protected OmniLight3D FxLight;

	public bool hasQuad = false;
	public override void _Ready()
	{
		FxLight = new OmniLight3D();
		FxLight.Visible = false;
		FxLight.LightCullMask = GameManager.AllPlayerViewMask;
		FxLight.Layers = GameManager.AllPlayerViewMask;
		AddChild(FxLight);
		FxLight.Position = Vector3.Up;
		FxLight.LightColor = new Color(0.2f, 0.2f, 1);


		BodyCast = new PhysicsShapeQueryParameters3D();
		BodyCast.CollisionMask = (1 << GameManager.ColliderLayer);
		BodyCast.Motion = Vector3.Zero;
		SpaceState = GetWorld3D().DirectSpaceState;

		Mass = 80;
	}
	void ApplySimpleMove(float deltaTime)
	{
		float gravityAccumulator;
		Vector3 currentPosition = Position;

		BodyCast.Transform = new Transform3D(Basis.Identity, upperBody.GlobalPosition);
		var hit = SpaceState.GetRestInfo(BodyCast);
		if (hit.Count > 0)
			gravityAccumulator = 0f;
		else if (deadWater)
			gravityAccumulator = GameManager.Instance.waterDeadFall;
		else
			gravityAccumulator = GameManager.Instance.gravity;
		Vector3 gravity = Vector3.Down * gravityAccumulator;
		currentPosition += (gravity + impulseVector) * deltaTime;

		//dampen impulse
		if (impulseVector.LengthSquared() > 0)
		{
			impulseVector = impulseVector.Lerp(Vector3.Zero, impulseDampening * deltaTime);
			if (impulseVector.LengthSquared() < 1f)
				impulseVector = Vector3.Zero;
		}
		Position = (currentPosition);
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (!loaded)
			return;

		float deltaTime = (float)delta;

		if (ragDoll)
		{
//			ApplySimpleMove(deltaTime);
			UpdateMultiMesh();
			return;
		}

		if (turnTo.LengthSquared() > 0)
			playerModel.Quaternion = playerModel.Quaternion.Slerp(turnTo, rotationFPS * deltaTime);

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
							ChangeToRagDoll();
							return;
						case UpperAnimation.Melee:
						case UpperAnimation.Attack:
						case UpperAnimation.Raise:
							if (isMeleeWeapon)
								upperAnimation = UpperAnimation.Stand2;
							else
								upperAnimation = UpperAnimation.Stand;

							nextUpper = upperAnim[upperAnimation];
							nextFrameUpper = nextUpper.startFrame;
							break;
						case UpperAnimation.Drop:
							nextFrameUpper = currentUpper.endFrame;
							if (destroyWeapon)
								DestroyWeapon();
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
							_enableOffset = true;
							break;
						case LowerAnimation.JumpBack:
							lowerAnimation = LowerAnimation.LandBack;
							_enableOffset = true;
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
									playerModel.Quaternion = turnTo;
									turnTo = QuaternionZero;
								}
								lowerAnimation = LowerAnimation.Idle;
							}
							break;
					}
					nextLower = lowerAnim[lowerAnimation];
					nextFrameLower = currentLower.startFrame;
				}
			}
			else
				nextFrameLower = nextLower.startFrame;

			Quaternion upperTorsoRotation = upper.tagsbyId[upper_tag_torso][currentFrameUpper].rotation.Slerp(upper.tagsbyId[upper_tag_torso][nextFrameUpper].rotation, upperCurrentLerpTime).FastNormal();
			Quaternion upperHeadRotation = upper.tagsbyId[upper_tag_head][currentFrameUpper].rotation.Slerp(upper.tagsbyId[upper_tag_head][nextFrameUpper].rotation, upperCurrentLerpTime).FastNormal();
			Quaternion lowerTorsoRotation = lower.tagsbyId[lower_tag_torso][currentFrameLower].rotation.Slerp(lower.tagsbyId[lower_tag_torso][nextFrameLower].rotation, lowerCurrentLerpTime).FastNormal();
			Quaternion weaponRotation = upper.tagsbyId[upper_tag_weapon][currentFrameUpper].rotation.Slerp(upper.tagsbyId[upper_tag_weapon][nextFrameUpper].rotation, upperCurrentLerpTime).FastNormal();

			Vector3 localOrigin = lower.tagsbyId[lower_tag_torso][currentFrameLower].localOrigin.Lerp(lower.tagsbyId[lower_tag_torso][nextFrameLower].localOrigin, lowerCurrentLerpTime);
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
				upperBody.Position = lowerTorsoOrigin;

				Quaternion baseRotation = lowerTorsoRotation;
				currentOffset = baseRotation * upperHeadOrigin;
				currentRotation = baseRotation * upperHeadRotation;

				tagHeadNode.Position = currentOffset;
				tagHeadNode.Basis = new Basis(currentRotation);

				currentOffset = baseRotation * weaponOrigin;
				currentRotation = baseRotation * weaponRotation;

				weaponNode.Position = currentOffset;
				weaponNode.Basis = new Basis(currentRotation);

				Position = localOrigin;

				currentOffset = upperTorsoRotation * upperTorsoOrigin;
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

		if (hasQuad != playerControls.playerInfo.quadDamage)
		{
			hasQuad = playerControls.playerInfo.quadDamage;
			FxLight.Visible = hasQuad;
			GameManager.ChangeQuadFx(fxMeshes,hasQuad);
		}
	}

	public void ChangeView(Vector2 viewDirection, float deltaTime)
	{
		if (ownerDead)
			return;

		//In order to keep proper animation and not offset it by looking at target, otherwise head could go Exorcist-like
		if (!_enableOffset)
			return;

		float vView = -viewDirection.X;
		float hView = viewDirection.Y - Mathf.RadToDeg(Quaternion.GetEuler().Y);

		headBody.Quaternion = headBody.Quaternion.Slerp(Quaternion.FromEuler(new Vector3(0, Mathf.DegToRad(hView), Mathf.DegToRad(vView))), rotationFPS * deltaTime);
		int vAngle = (int)Mathf.Round((vView) / (360) * 32) % 32;
		int hAngle = (int)Mathf.Round((hView) / (360) * 32) % 32;

		upperBody.Quaternion = upperBody.Quaternion.Slerp(Quaternion.FromEuler(new Vector3(0, Mathf.DegToRad(11.25f * hAngle), Mathf.DegToRad(7.5f * vAngle))), rotationFPS * deltaTime);

	}

	public void CheckLegTurn(Vector3 direction)
	{
		if (ownerDead)
			return;

		Vector3 forward = this.ForwardVector();
		int angle = (int)Mathf.Round((Mathf.Atan2(direction.X, direction.Z)) / (Mathf.Pi * 2) * 8) % 8;
		Quaternion dir = Quaternion.FromEuler(new Vector3(0f, Mathf.DegToRad(angle * 45f), 0f));

		angle = (int)Mathf.Round(((Mathf.Atan2((forward.Z * direction.X) - (direction.Z * forward.X), (forward.X * direction.X) + (forward.Z * direction.Z)))) / (Mathf.Pi * 2) * 8) % 8;
		if (angle != 0)
		{
			turnTo = dir;
			if (lowerAnimation == LowerAnimation.Idle)
				lowerAnimation = LowerAnimation.Turn;
		}
	}
	public void Attack()
	{
		if (ownerDead)
			return;
		if (isMeleeWeapon)
			upperAnimation = UpperAnimation.Melee;
		else
			upperAnimation = UpperAnimation.Attack;
	}

	private void ChangeToRagDoll()
	{
		//		uint OwnerShapeId = CreateShapeOwner(this);
		
		for (int i = 0; i < head.meshes.Count; i++)
		{
			CollisionShape3D headCollision = new CollisionShape3D();
			AddChild(headCollision);
			ConcavePolygonShape3D modelColliderShape = new ConcavePolygonShape3D();
			Vector3[] faces = headModel.data[i].arrMesh.GetFaces();
			Quaternion rotation = tagHeadNode.Quaternion;
			for (int j = 0; j < faces.Length; j++)
			{
				faces[j] = rotation * faces[j];
				faces[j] += tagHeadNode.Position + upperBody.Position;
			}
			modelColliderShape.Data = faces;
			headCollision.Shape = modelColliderShape;
//			ShapeOwnerAddShape(OwnerShapeId, modelColliderShape);
		}
		Vector3 headGlobalPos = headBody.GlobalPosition;
		Vector3 headGlobalRot = headBody.GlobalRotation;
		headModel.node.QueueFree();

		Quaternion upperTorsoRotation = upper.tagsbyId[upper_tag_torso][currentFrameUpper].rotation;
		Quaternion lowerTorsoRotation = lower.tagsbyId[lower_tag_torso][currentFrameLower].rotation;
		Vector3 localOrigin = lower.tagsbyId[lower_tag_torso][currentFrameLower].localOrigin;
		Vector3 upperTorsoOrigin = upper.tagsbyId[upper_tag_torso][currentFrameUpper].origin;
		Vector3 currentOffset = lowerTorsoRotation * upperTorsoOrigin;
		Quaternion currentRotation = lowerTorsoRotation * upperTorsoRotation;

		for (int i = 0; i < upper.meshes.Count; i++)
		{
			CollisionShape3D bodyCollision = new CollisionShape3D();
			AddChild(bodyCollision);
			ConcavePolygonShape3D modelColliderShape = new ConcavePolygonShape3D();
			Vector3[] faces = upperModel.data[i].arrMesh.GetFaces();
			Quaternion rotation = upperBody.Quaternion;
			for (int j = 0; j < faces.Length; j++)
			{
				faces[j] = rotation * faces[j];
				faces[j] += upperBody.Position;
			}
			modelColliderShape.Data = faces;
			bodyCollision.Shape = modelColliderShape;
//			ShapeOwnerAddShape(OwnerShapeId, modelColliderShape);
		}
		upperModel.node.QueueFree();
		MD3GodotConverted upperRagDoll = Mesher.GenerateModelFromMeshes(upper, meshToSkin, GameManager.AllPlayerViewMask, true, currentFrameUpper);
		upperRagDoll.node.Name = "upper_body";
		upperBody.AddChild(upperRagDoll.node);
		upperBody.Quaternion = currentRotation;
		upperBody.Position += currentOffset;
		SetMultiMesh(upperRagDoll, upperBody);

		MD3GodotConverted headRagDoll = Mesher.GenerateModelFromMeshes(head, meshToSkin, GameManager.AllPlayerViewMask, true);
		headRagDoll.node.Name = "head";
		headBody.AddChild(headRagDoll.node);
		headBody.GlobalPosition = headGlobalPos;
		headBody.GlobalRotation = headGlobalRot;
		SetMultiMesh(headRagDoll, headBody);

		currentOffset = upperTorsoRotation * upperTorsoOrigin;
		for (int i = 0; i < lower.meshes.Count; i++)
		{
			CollisionShape3D legCollision = new CollisionShape3D();
			AddChild(legCollision);
			ConcavePolygonShape3D modelColliderShape = new ConcavePolygonShape3D();
			modelColliderShape.Data = lowerModel.data[i].arrMesh.GetFaces();
			legCollision.Shape = modelColliderShape;
//			ShapeOwnerAddShape(OwnerShapeId, modelColliderShape);
		}
		lowerModel.node.QueueFree();
		MD3GodotConverted lowerRagDoll = Mesher.GenerateModelFromMeshes(lower, meshToSkin, GameManager.AllPlayerViewMask, true, currentFrameLower);
		lowerRagDoll.node.Name = "lower_body";
		lowerNode = lowerRagDoll.node;
		lowerNode.Position = localOrigin + currentOffset;
		playerModel.AddChild(lowerRagDoll.node);
		SetMultiMesh(lowerRagDoll, playerModel);

		CollisionLayer = (1 << GameManager.RagdollLayer);
		CollisionMask = ((1 << GameManager.ColliderLayer) | (1 << GameManager.RagdollLayer));
		impulseVector = playerControls.impulseVector;
		playerControls.playerThing.CollisionLayer = (1 << GameManager.NoCollisionLayer);
		if (playerControls.playerThing.waterLever > 0)
			deadWater = true;
		ragDoll = true;
		Reparent(GameManager.Instance.TemporaryObjectsHolder);
		playerControls.playerThing.interpolatedTransform.QueueFree();
		playerControls.playerThing.interpolatedTransform = null;
		LinearVelocity = impulseVector;
		CenterOfMassMode = CenterOfMassModeEnum.Custom;
		CenterOfMass = Vector3.Down * .5f;
	}

	public void Die()
	{
		//Need to reset the torso and head view
		headBody.Basis = new Basis(Quaternion.Identity);
		upperBody.Basis = new Basis(Quaternion.Identity);
		lowerNode.Basis = new Basis(Quaternion.Identity);
		int deathNum = 2 * GD.RandRange(0, 2);
		upperAnimation = deathNum;
		lowerAnimation = deathNum;

		FxLight.Visible = false;
		GameManager.ChangeQuadFx(fxMeshes, false);

		ownerDead = true;
	}

	public void Damage(int amount, DamageType damageType = DamageType.Generic, Node3D attacker = null)
	{
		if (!ragDoll)
			return;
	}
	public void Impulse(Vector3 direction, float force)
	{
		if (!ragDoll)
			return;
	}
	public void TurnLegsOnJump(float sideMove, float deltaTime)
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
			rotate = new Quaternion(this.UpVector(), 30f);
		else if (sideMove < 0)
			rotate = new Quaternion(this.UpVector(), -30f);

		lowerNode.Quaternion = lowerNode.Quaternion.Slerp(rotate, lowerRotationFPS * deltaTime);
	}

	public void Swim()
	{
		lowerNode.Quaternion = Quaternion.Identity;
		lowerAnimation = LowerAnimation.Swim;
	}

	public void TurnLegs(int moveType, float sideMove, float forwardMove, float deltaTime)
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
				rotate = new Quaternion(this.UpVector(), Mathf.DegToRad(-30f));
			else if (sideMove < 0)
				rotate = new Quaternion(this.UpVector(), Mathf.DegToRad(30f));
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
				rotate = new Quaternion(this.UpVector(), Mathf.DegToRad(30f));
			else if (sideMove < 0)
				rotate = new Quaternion(this.UpVector(), Mathf.DegToRad(-30f));
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
				rotate = new Quaternion(this.UpVector(), Mathf.DegToRad(-50f));
			else if (sideMove < 0)
				rotate = new Quaternion(this.UpVector(), Mathf.DegToRad(50f));
		}
		else if (lowerAnimation != LowerAnimation.Turn)
		{
			if (nextMoveType == MoveType.Crouch)
				lowerAnimation = LowerAnimation.IdleCR;
			else
				lowerAnimation = LowerAnimation.Idle;
		}
		lowerNode.Quaternion = lowerNode.Quaternion.Slerp(rotate, lowerRotationFPS * deltaTime);
	}
	public void MuzzleFlashSetScale(Vector3 scale)
	{
		if (muzzleFlash == null)
			return;

		muzzleFlash.Scale = scale;
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
		barrel.Quaternion = barrel.Quaternion.Slerp(rotation, speed);
	}

	public void LoadWeapon(MD3 newWeapon, string barrelModelName, string muzzleModelName, bool isMelee)
	{
		if (ownerDead)
			return;
		if (destroyWeapon)
			DestroyWeapon();

		MD3 barrelModel = null;
		MD3 weaponModelTags;
		Node3D barrelTag = null;
		Quaternion Rotation = Quaternion.Identity;
		int tagId;

		if (!string.IsNullOrEmpty(barrelModelName))
		{
			barrelModel = ModelsManager.GetModel(barrelModelName);
			if (barrelModel == null)
				return;
		}

		weapon = newWeapon;
		weaponModel = Mesher.GenerateModelFromMeshes(weapon, currentLayer, true, true, null, false, false);
		weaponModel.node.Name = "weapon";
		weaponNode.AddChild(weaponModel.node);

		if (barrelModel != null)
		{
			Vector3 OffSet = Vector3.Zero;
			barrel = new Node3D();
			barrel.Name = "barrel_weapon";
			if (isMelee)
			{
				barrelTag = new Node3D();
				barrelTag.Name = "Barrel_Tag";
			}
			else
				barrelTag = barrel;

			Mesher.GenerateModelFromMeshes(barrelModel, currentLayer, true, true, barrel, false, false);
			weaponModel.node.AddChild(barrelTag);
			if (isMelee)
				barrelTag.AddChild(barrel);

			if (weapon.tagsIdbyName.TryGetValue("tag_barrel", out tagId))
			{
				OffSet = weapon.tagsbyId[tagId][0].origin;
				Rotation = weapon.tagsbyId[tagId][0].rotation;
			}
			barrelTag.Quaternion = Rotation;
			barrelTag.Position = OffSet;

			weaponModelTags = barrelModel;
		}
		else
			weaponModelTags = weapon;

		isMeleeWeapon = isMelee;
		upperAnimation = UpperAnimation.Raise;

		AddAllMeshInstance3D(weaponNode);

		if (!string.IsNullOrEmpty(muzzleModelName))
		{
			Vector3 OffSet = Vector3.Zero;
			muzzleFlash = new Node3D();
			muzzleFlash.Name = "muzzle_flash";
			MD3 muzzle = ModelsManager.GetModel(muzzleModelName, true);

			if (muzzle == null)
				return;

			Mesher.GenerateModelFromMeshes(muzzle, currentLayer, false, false, muzzleFlash, true, false);

			if (barrel == null)
				weaponModel.node.AddChild(muzzleFlash);
			else
				barrelTag.AddChild(muzzleFlash);

			if (weaponModelTags.tagsIdbyName.TryGetValue("tag_flash", out tagId))
			{
				OffSet = weaponModelTags.tagsbyId[tagId][0].origin;
				Rotation = weaponModelTags.tagsbyId[tagId][0].rotation;
			}
			muzzleFlash.Position = OffSet;
			muzzleFlash.Quaternion = Rotation;
			muzzleFlash.Visible = false;
		}
		AddAllMeshInstance3D(weaponNode, false);
		if (hasQuad)
			GameManager.ChangeQuadFx(fxMeshes,true);
	}

	public void UnloadWeapon()
	{
		if (weapon == null)
			return;

		if (ownerDead)
		{
			if (weaponModel != null)
				if (weaponModel.node != null)
				{
					RemoveAllMeshInstance3D(weaponNode);
					weaponModel.node.QueueFree();
					weaponModel.node = null;
				}

			weapon = null;
			barrel = null;
			muzzleFlash = null;
			return;
		}
		destroyWeapon = true;
		upperAnimation = UpperAnimation.Drop;
	}
	public void AddLightningBolt(Node3D lightningBolt)
	{
		lightningBolt.Hide();
		weaponModel.node.AddChild(lightningBolt);
		lightningBolt.Position = muzzleFlash.Position;
		AddAllMeshInstance3D(lightningBolt, false);
		ChangeLayer(currentLayer);
	}

	public List<MeshInstance3D> GetWeaponModulateMeshes(bool fromMuzzle = false)
	{
		Node parent = weaponModel.node;
		if (fromMuzzle)
			parent = muzzleFlash;
		return GameManager.GetModulateMeshes(parent, fxMeshes);
	}

	public void DestroyWeapon()
	{
		if (weapon == null)
			return;

		if (weaponModel != null)
			if (weaponModel.node != null)
			{
				RemoveAllMeshInstance3D(weaponNode);
				weaponModel.node.QueueFree();
				weaponModel.node = null;
			}

		weapon = null;
		barrel = null;
		muzzleFlash = null;
		destroyWeapon = false;
	}

	public bool LoadPlayer(ref string modelName, ref string SkinName, uint layer, PlayerControls control)
	{
		string playerModelPath = "players/" + modelName;
		string lowerModelName = playerModelPath + "/lower";

		lower = ModelsManager.GetModel(lowerModelName);
		if (lower == null)
		{
			modelName = defaultModel;
			playerModelPath = "players/" + modelName;
			lowerModelName = playerModelPath + "/lower";
			lower = ModelsManager.GetModel(lowerModelName);
			if (lower == null)
				return false;
		}

		string upperModelName = playerModelPath + "/upper";
		string headModelName = playerModelPath + "/head";
		string animationFile = playerModelPath + "/animation";

		upper = ModelsManager.GetModel(upperModelName);
		if (upper == null)
			return false;

		head = ModelsManager.GetModel(headModelName);
		if (head == null)
			return false;

		string lowerSkin = playerModelPath + "/lower_" + SkinName;

		if (!LoadSkin(lower, lowerSkin))
		{
			SkinName = defaultSkin;
			lowerSkin = playerModelPath + "/lower_" + SkinName;
			if (!LoadSkin(lower, lowerSkin))
				return false;
		}

		string upperSkin = playerModelPath + "/upper_" + SkinName;
		string headSkin = playerModelPath + "/head_" + SkinName;

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

			upperModel = Mesher.GenerateModelFromMeshes(upper, meshToSkin, layer);
			upperModel.node.Name = "upper_body";
			upperBody.AddChild(upperModel.node);

			headModel = Mesher.GenerateModelFromMeshes(head, meshToSkin, layer);
			headModel.node.Name = "head";
			headBody.AddChild(headModel.node);

			lowerModel = Mesher.GenerateModelFromMeshes(lower, meshToSkin, layer);
			lowerModel.node.Name = "lower_body";
			lowerNode = lowerModel.node;
			playerModel.AddChild(lowerModel.node);

			loaded = true;
		}
		playerControls = control;
		currentLayer = layer;
		
		AddAllMeshInstance3D(playerModel);

		playerControls.playerInfo.playerPostProcessing.playerHUD.InitHUD(head, meshToSkin);
		return true;
	}
	public void ChangeLayer(uint layer)
	{
		for (int i = 0; i < modelsMeshes.Count; i++)
			modelsMeshes[i].Layers = layer;
		for (int i = 0; i < fxMeshes.Count; i++)
			fxMeshes[i].Layers = layer;
		currentLayer = layer;
	}

	private void AddAllMeshInstance3D(Node parent, bool addFx = true)
	{		
		var Childrens = GameManager.GetAllChildrens(parent);
		foreach(var child in Childrens)
		{
			if (child is MeshInstance3D mesh)
			{
				if (modelsMeshes.Contains(mesh))
					continue;

				//Check if UI Self Shadow
				if (mesh.CastShadow == GeometryInstance3D.ShadowCastingSetting.ShadowsOnly)
					continue;

				if (fxMeshes.Contains(mesh))
					continue;
				modelsMeshes.Add(mesh);

				//UI Self Shadow
				MeshInstance3D shadowMesh = new MeshInstance3D();
				shadowMesh.Mesh = mesh.Mesh;
				shadowMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
				shadowMesh.Layers = playerControls.playerInfo.uiLayer;
				mesh.AddChild(shadowMesh);

				if (!addFx)
					continue;

				//FX Mesh
				MeshInstance3D fxMesh = new MeshInstance3D();
				fxMesh.Mesh = mesh.Mesh;
				fxMesh.Layers = currentLayer;
				fxMesh.Visible = false;
				mesh.AddChild(fxMesh);
				fxMeshes.Add(fxMesh);
			}
		}
	}
	private void RemoveAllMeshInstance3D(Node parent)
	{
		var Childrens = GameManager.GetAllChildrens(parent);
		foreach (var child in Childrens)
		{
			if (child is MeshInstance3D mesh)
			{
				if (fxMeshes.Contains(mesh))
					fxMeshes.Remove(mesh);
				if (modelsMeshes.Contains(mesh))
					modelsMeshes.Remove(mesh);
			}
		}
	}

	private bool LoadAnimations(string file, List<ModelAnimation> upper, List<ModelAnimation> lower)
	{
		StreamReader animFile;
		string FileName;
		string path = Directory.GetCurrentDirectory() + "/StreamingAssets/models/" + file + ".cfg";
		if (File.Exists(path))
			animFile = new StreamReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.TryGetValue(path = ("models/" + file + ".cfg").ToUpper(), out FileName))
		{
			var reader = new ZipReader();
			reader.Open(FileName);
			MemoryStream ms = new MemoryStream(reader.ReadFile(path, false));
			animFile = new StreamReader(ms);
		}
		else
		{
			GameManager.Print("Unable to load animation file: " + file, GameManager.PrintType.Warning);
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
		int legsOffset = LowerAnimation.WalkCR + 1;
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

			if (animations[currentAnim].strName.Contains("BOTH"))
			{
				upper.Add(animations[currentAnim]);
				lower.Add(animations[currentAnim]);
			}
			else if (animations[currentAnim].strName.Contains("TORSO"))
			{
				upper.Add(animations[currentAnim]);
			}
			else if (animations[currentAnim].strName.Contains("LEGS"))
			{
				if (torsoOffset == 0)
					torsoOffset = animations[UpperAnimation.Stand2 + 1].startFrame - animations[LowerAnimation.WalkCR].startFrame;

				animations[currentAnim].startFrame -= torsoOffset;
				animations[currentAnim].endFrame -= torsoOffset;
				animations[currentAnim].index -= legsOffset;
				lower.Add(animations[currentAnim]);
			}
			currentAnim++;
		}
		//Add Walk Crounched Back 
		animations[currentAnim] = new ModelAnimation(LowerAnimation.WalkCRBack);
		animations[currentAnim].startFrame = lowerAnim[LowerAnimation.WalkCR].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[LowerAnimation.WalkCR].startFrame - 1;
		animations[currentAnim].loopingFrames = lowerAnim[LowerAnimation.WalkCR].loopingFrames;
		animations[currentAnim].fps = lowerAnim[LowerAnimation.WalkCR].fps;
		animations[currentAnim].nextFrame = -1;
		lower.Add(animations[currentAnim++]);

		//Add Fall
		animations[currentAnim] = new ModelAnimation(LowerAnimation.Fall);
		animations[currentAnim].startFrame = lowerAnim[LowerAnimation.Land].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[LowerAnimation.Land].endFrame;
		animations[currentAnim].loopingFrames = 0;
		animations[currentAnim].fps = lowerAnim[LowerAnimation.Land].fps;
		lower.Add(animations[currentAnim++]);

		//Add Walk Back
		animations[currentAnim] = new ModelAnimation(LowerAnimation.WalkBack);
		animations[currentAnim].startFrame = lowerAnim[LowerAnimation.Walk].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[LowerAnimation.Walk].startFrame - 1;
		animations[currentAnim].loopingFrames = lowerAnim[LowerAnimation.Walk].loopingFrames;
		animations[currentAnim].fps = lowerAnim[LowerAnimation.Walk].fps;
		animations[currentAnim].nextFrame = -1;
		lower.Add(animations[currentAnim++]);

		//Add Fall Back
		animations[currentAnim] = new ModelAnimation(LowerAnimation.FallBack);
		animations[currentAnim].startFrame = lowerAnim[LowerAnimation.LandBack].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[LowerAnimation.LandBack].endFrame;
		animations[currentAnim].loopingFrames = 0;
		animations[currentAnim].fps = lowerAnim[LowerAnimation.LandBack].fps;
		lower.Add(animations[currentAnim]);

		animFile.Close();
		return true;
	}
	public bool LoadSkin(MD3 model, string skinName)
	{
		StreamReader SkinFile;
		string FileName;
		string path = Directory.GetCurrentDirectory() + "/StreamingAssets/models/" + skinName + ".skin";
		if (File.Exists(path))
			SkinFile = new StreamReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.TryGetValue(path = ("models/" + skinName + ".skin").ToUpper(), out FileName))
		{
			var reader = new ZipReader();
			reader.Open(FileName);
			MemoryStream ms = new MemoryStream(reader.ReadFile(path, false));
			SkinFile = new StreamReader(ms);
		}
		else
		{
			GameManager.Print("Unable to load skin for model: " + model.name, GameManager.PrintType.Warning);
			return false;
		}

		SkinFile.BaseStream.Seek(0, SeekOrigin.Begin);

		if (SkinFile.EndOfStream)
		{
			GameManager.Print("Unable to load skin for model: " + model.name, GameManager.PrintType.Warning);
			return false;
		}

		string strLine;
		int textureNameStart = 0;

		while (!SkinFile.EndOfStream)
		{
			strLine = SkinFile.ReadLine().ToUpper();

			for (int i = 0; i < model.meshes.Count; i++)
			{
				if (strLine.Contains(model.meshes[i].name))
				{
					for (int j = strLine.Length - 1; j > 0; j--)
					{
						if (strLine[j] == ',')
						{
							textureNameStart = j + 1;
							break;
						}
					}
					string skin = strLine.Substring(textureNameStart);
					string[] fullName = skin.Split('.');

					//Check if skin texture exist, if not add it
					if (!TextureLoader.HasTexture(fullName[0]))
					{
						GameManager.Print("Skin: " + fullName[0]);
						TextureLoader.AddNewTexture(fullName[0], false);
					}
					if (!meshToSkin.ContainsKey(model.meshes[i].name))
						meshToSkin.Add(model.meshes[i].name, fullName[0]);
				}
			}
		}
		SkinFile.Close();
		return true;
	}

	public void SetMultiMesh(MD3GodotConverted model, Node3D owner)
	{
		for (int i = 0; i < model.data.Length; i++)
		{
			if (model.data[i] == null)
				continue;
			if (model.data[i].isTransparent)
				continue;

			if (Mesher.MultiMeshes.ContainsKey(model.data[i].multiMesh))
			{
				MultiMeshData multiMeshData = new MultiMeshData();
				multiMeshData.multiMesh = model.data[i].multiMesh;
				Mesher.AddNodeToMultiMeshes(model.data[i].multiMesh, owner);
				multiMeshData.owner = owner;
				multiMeshDataList.Add(multiMeshData);
			}
		}
	}
	void UpdateMultiMesh()
	{
		if (multiMeshDataList.Count == 0)
			return;

		if (((GlobalPosition - lastGlobalPosition).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.X - lastGlobalBasis.X).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.Y - lastGlobalBasis.Y).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.Z - lastGlobalBasis.Z).LengthSquared() > Mathf.Epsilon))
		{

			for (int i = 0; i < multiMeshDataList.Count; i++)
				Mesher.UpdateInstanceMultiMesh(multiMeshDataList[i].multiMesh, multiMeshDataList[i].owner);
		}

		lastGlobalPosition = GlobalPosition;
		lastGlobalBasis = GlobalBasis;
	}
	public void ClearPlayerModel()
	{
		List<MultiMesh> updateMultiMesh = new List<MultiMesh>();
		for (int i = 0; i < multiMeshDataList.Count; i++)
		{
			MultiMesh multiMesh = multiMeshDataList[i].multiMesh;
			List<Node3D> multiMeshList;
			if (Mesher.MultiMeshes.TryGetValue(multiMesh, out multiMeshList))
			{
				if (multiMeshList.Contains(multiMeshDataList[i].owner))
					multiMeshList.Remove(multiMeshDataList[i].owner);
			}
			if (!updateMultiMesh.Contains(multiMesh))
				updateMultiMesh.Add(multiMesh);
		}
		foreach (MultiMesh multiMesh in updateMultiMesh)
			Mesher.MultiMeshUpdateInstances(multiMesh);
	}
}
