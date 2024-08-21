using Godot;
using System.Collections.Generic;

public partial class ModelController : Node3D
{
	[Export]
	public string modelName = "";
	[Export]
	public string shaderName = "";
	[Export]
	public string tagName = "";
	[Export]
	public bool useCommon = true;
	[Export]
	public bool isTransparent = false;
	[Export]
	public bool receiveShadows = false;
	[Export]
	public bool castShadows = false;
	[Export]
	public bool useLowCountMultiMesh = true;
	[Export]
	public bool alphaFade = false;
	[Export]
	public bool isViewModel = false;

	public uint currentLayer = GameManager.AllPlayerViewMask;
	public MD3 Model { get { return md3Model; } }
	private MD3 md3Model = null;

	private MeshProcessed model;
	[Export]
	public float modelAnimationFPS = 0;
	[Export]
	public DestroyType destroyType;
	[Export]
	public float destroyTimer = 0;
	[Export]
	public GameManager.FuncState currentState = GameManager.FuncState.Ready;

	private List<MultiMeshData> multiMeshDataList = new List<MultiMeshData>();
	private List<Node3D> destroyNodes = new List<Node3D>();
	private List<int> modelAnim = new List<int>();

	private int modelCurrentFrame;

	private Vector3 currentOrigin;
	private float height;

	private float ModelLerpTime = 0;
	private float ModelCurrentLerpTime = 0;

	private Color Modulate = Colors.Black;
	private float baseTime = 1;
	public enum DestroyType
	{
		NoDestroy,
		DestroyAfterTime,
		DestroyAfterModelLastFrame
	}

	private Vector3 lastGlobalPosition = new Vector3(0, 0, 0);
	private Basis	lastGlobalBasis = Basis.Identity;	

	public void AddDestroyNode(Node3D node)
	{
		if (!destroyNodes.Contains(node))
			destroyNodes.Add(node);
	}

	public override void _Ready()
	{
		Init();
	}

	public void Init()
	{
		if (currentState == GameManager.FuncState.None)
			return;

		if (string.IsNullOrEmpty(modelName))
			return;

		md3Model = ModelsManager.GetModel(modelName, isTransparent);
		if (md3Model == null)
			return;

		Node3D currentObject = this;
		Dictionary<string, string> meshToSkin = null;
		if (!string.IsNullOrEmpty(shaderName))
		{
			shaderName = shaderName.ToUpper();
			meshToSkin = new Dictionary<string, string>
			{
				{ md3Model.meshes[0].name, shaderName }
			};
			if (!TextureLoader.HasTexture(shaderName))
				TextureLoader.AddNewTexture(shaderName, isTransparent);
		}

		model = Mesher.GenerateModelFromMeshes(md3Model, currentLayer, receiveShadows, castShadows, currentObject, isTransparent, ((modelAnimationFPS == 0) && !isTransparent) && useCommon, meshToSkin, useLowCountMultiMesh, alphaFade, isViewModel);

		for (int i = 0; i < md3Model.meshes.Count; i++)
		{
			var modelMesh = md3Model.meshes[i];
			if (modelMesh.numFrames > 1)
				modelAnim.Add(i);
		}

		modelCurrentFrame = 0;
		if (destroyTimer != 0)
			baseTime = destroyTimer;
		ModelsManager.AddModel(this);

	}

	void SetTagPosition()
	{
		Node parent = GetParent();
		if (parent is ModelController source)
		{
			if (source.Model.tagsIdbyName.TryGetValue(tagName, out int tagId))
			{
				Position = source.Model.tagsbyId[tagId][0].origin;
				Quaternion = source.Model.tagsbyId[tagId][0].rotation;
			}
		}
	}

	void AnimateModel(float deltaTime)
	{
		if (modelAnimationFPS == 0)
			return;

		int currentFrame = modelCurrentFrame;
		int nextFrame = currentFrame + 1;
		
		if (nextFrame >= md3Model.numFrames)
			nextFrame = 0;
		if ((nextFrame == 0) && (destroyType == DestroyType.DestroyAfterModelLastFrame))
		{
			QueueFree();
			return;
		}

		if (currentFrame == nextFrame)
			return;

		for (int i = 0; i < modelAnim.Count; i++)
		{
			MD3Mesh currentMesh = md3Model.meshes[modelAnim[i]];
			for (int j = 0; j < currentMesh.numVertices; j++)
			{
				Vector3 newVertex = currentMesh.verts[currentFrame][j].Lerp(currentMesh.verts[nextFrame][j], ModelCurrentLerpTime);
				Vector3 newNormal = currentMesh.normals[currentFrame][j].Lerp(currentMesh.normals[nextFrame][j], ModelCurrentLerpTime);
				model.data[i].meshDataTool.SetVertex(j, newVertex);
				model.data[i].meshDataTool.SetVertexNormal(j, newNormal);
			}
			model.data[i].arrMesh.ClearSurfaces();
			model.data[i].meshDataTool.CommitToSurface(model.data[i].arrMesh);
		}

		ModelLerpTime = modelAnimationFPS * deltaTime;
		ModelCurrentLerpTime += ModelLerpTime;

		if (ModelCurrentLerpTime >= 1.0f)
		{
			ModelCurrentLerpTime -= 1.0f;
			modelCurrentFrame = nextFrame;
		}
	}
	public void Start()
	{
		if (string.IsNullOrEmpty(modelName))
		{
			QueueFree();
			return;
		}

		if (md3Model == null)
		{
			GameManager.Print("Model not found: " + modelName, GameManager.PrintType.Warning);
			QueueFree();
			return;
		}

		if (!string.IsNullOrEmpty(tagName))
			SetTagPosition();

		if ((modelAnimationFPS == 0) && useCommon)
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
					Mesher.AddNodeToMultiMeshes(model.data[i].multiMesh, this, Modulate);
					multiMeshData.owner = this;
					multiMeshDataList.Add(multiMeshData);
				}
			}
		}

		currentState = GameManager.FuncState.Start;
	}

	void UpdateMultiMesh()
	{
		if (multiMeshDataList.Count == 0)
			return;

		if ((alphaFade) ||
			((GlobalPosition - lastGlobalPosition).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.X - lastGlobalBasis.X).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.Y - lastGlobalBasis.Y).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.Z - lastGlobalBasis.Z).LengthSquared() > Mathf.Epsilon))
		{

			for (int i = 0; i < multiMeshDataList.Count; i++)
			{
				if (alphaFade)
					Mesher.UpdateInstanceMultiMesh(multiMeshDataList[i].multiMesh, this, Modulate);
				else
					Mesher.UpdateInstanceMultiMesh(multiMeshDataList[i].multiMesh, this);
			}
		}

		lastGlobalPosition = GlobalPosition;
		lastGlobalBasis = GlobalBasis;	
	}

	public override void _Notification(int what)
	{
		if (what == NotificationPredelete)
			Destroy();
	}
	public void Destroy()
	{
		List<MultiMesh> updateMultiMesh = new List<MultiMesh>();
		for (int i = 0; i < multiMeshDataList.Count; i++)
		{
			MultiMesh multiMesh = multiMeshDataList[i].multiMesh;
			Dictionary<Node3D, int> multiMeshSet;

			multiMeshDataList[i].owner.Hide();
			Mesher.UpdateInstanceMultiMesh(multiMesh, multiMeshDataList[i].owner);
			if (Mesher.MultiMeshes.TryGetValue(multiMesh, out multiMeshSet))
			{
				if (multiMeshSet.ContainsKey(multiMeshDataList[i].owner))
					multiMeshSet.Remove(multiMeshDataList[i].owner);
			}
			if (!updateMultiMesh.Contains(multiMesh))
				updateMultiMesh.Add(multiMesh);
		}

		//No need to update or detroy other nodes if changing map
		if (GameManager.CurrentState != GameManager.FuncState.Start)
			return;

		foreach (MultiMesh multiMesh in updateMultiMesh)
			Mesher.MultiMeshUpdateInstances(multiMesh);

		foreach (Node3D node in destroyNodes)
		{
			if (IsInstanceValid(node))
				node.QueueFree();
		}

		ModelsManager.RemoveModel(this);
	}
	public void Process(float deltaTime)
	{
		if (currentState == GameManager.FuncState.None)
			return;

		if (currentState == GameManager.FuncState.Ready)
			Start();

		AnimateModel(deltaTime);
		UpdateMultiMesh();
		if (alphaFade)
		{
			float alphaValue = Mathf.Lerp(1.0f, 0.0f, destroyTimer / baseTime);
			Modulate = new Color(alphaValue, alphaValue, alphaValue);
		}	
		if (destroyType == DestroyType.DestroyAfterTime)
		{
			destroyTimer -= deltaTime;
			if (destroyTimer < 0)
				QueueFree();
		}
	}
}
