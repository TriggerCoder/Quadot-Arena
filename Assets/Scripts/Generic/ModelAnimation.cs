using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class ModelAnimation : Node3D
{
	[Export]
	public string modelName = "";
	[Export]
	public string shaderName = "";
	[Export]
	public bool isTransparent = false;
	[Export]
	public bool castShadow = true;
	[Export]
	public bool useLowCountMultiMesh = true;

	private MD3 md3Model;

	private MD3GodotConverted model;
	[Export]
	public AnimData modelAnimation;
	[Export]
	public AnimData textureAnimation;
	[Export]
	public DestroyType destroyType;

	public List<MultiMeshData> multiMeshDataList = new List<MultiMeshData>();
	private List<int> modelAnim = new List<int>();
	private List<int> textureAnim = new List<int>();
	private Dictionary<int, ShaderMaterial[]> materials = new Dictionary<int, ShaderMaterial[]>();

	private int modelCurrentFrame;
	private List<int> textureCurrentFrame = new List<int>();

	private Vector3 currentOrigin;
	private float height;

	private float ModelLerpTime = 0;
	private float ModelCurrentLerpTime = 0;

	private float TextureLerpTime = 0;
	private float TextureCurrentLerpTime = 0;

	private GameManager.FuncState currentState = GameManager.FuncState.None;
	public enum DestroyType
	{
		NoDestroy,
		DestroyAfterTime,
		DestroyAfterModelLastFrame,
		DestroyAfterTextureLastFrame
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Init();
	}

	public void Init()
	{
		currentState = GameManager.FuncState.Ready;
		if (string.IsNullOrEmpty(modelName))
			return;

		md3Model = ModelsManager.GetModel(modelName, isTransparent);
		if (md3Model == null)
			return;

		Node3D currentObject = this;
		Dictionary<string, string> meshToSkin = null;
		if (!string.IsNullOrEmpty(shaderName))
		{
			meshToSkin = new Dictionary<string, string>
			{
				{ md3Model.meshes[0].name, shaderName }
			};
		}

		if (md3Model.readySurfaceArray.Count == 0)
			model = Mesher.GenerateModelFromMeshes(md3Model, GameManager.AllPlayerViewMask, currentObject, isTransparent, ((modelAnimation.fps == 0) && !isTransparent), meshToSkin, useLowCountMultiMesh);
		else
			model = Mesher.FillModelFromProcessedData(md3Model, GameManager.AllPlayerViewMask, currentObject, ((modelAnimation.fps == 0) && !isTransparent));

		for (int i = 0; i < md3Model.meshes.Count; i++)
		{
			var modelMesh = md3Model.meshes[i];
			if (modelMesh.numFrames > 1)
				modelAnim.Add(i);

			if (!string.IsNullOrEmpty(shaderName))
				continue;

			if (modelMesh.numSkins > 1)
			{
				ShaderMaterial[] frames = new ShaderMaterial[modelMesh.numSkins];
				for (int j = 0; j < modelMesh.numSkins; j++)
				{
					string texName = modelMesh.skins[j].name;
					bool currentTransparent = isTransparent;
					if (TextureLoader.HasTexture(texName))
						frames[j] = MaterialManager.GetMaterials(texName, -1, ref currentTransparent);
					else
					{
						TextureLoader.AddNewTexture(texName, isTransparent);
						frames[j] = MaterialManager.GetMaterials(texName, -1, ref currentTransparent);
					}
				}
				textureAnim.Add(i);
				textureCurrentFrame.Add(0);
				materials.Add(i, frames);
			}
		}

		modelCurrentFrame = 0;
	}
	void AnimateModel(float deltaTime)
	{
		int currentFrame = modelCurrentFrame;
		int nextFrame = currentFrame + 1;
		
		if (nextFrame >= md3Model.numFrames)
			nextFrame = 0;
		if ((nextFrame == 0) && (destroyType == DestroyType.DestroyAfterModelLastFrame))
		{
			QueueFree();
			return;
		}

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

		ModelLerpTime = modelAnimation.fps * deltaTime;
		ModelCurrentLerpTime += ModelLerpTime;

		if (ModelCurrentLerpTime >= 1.0f)
		{
			ModelCurrentLerpTime -= 1.0f;
			modelCurrentFrame = nextFrame;
		}
	}
	void AnimateTexture(float deltaTime)
	{
		TextureLerpTime = textureAnimation.fps * deltaTime;
		TextureCurrentLerpTime += TextureLerpTime;

		for (int i = 0; i < textureAnim.Count; i++)
		{
			MD3Mesh currentMesh = md3Model.meshes[textureAnim[i]];

			int currentFrame = textureCurrentFrame[i];
			int nextFrame = currentFrame + 1;
			if (nextFrame >= currentMesh.numSkins)
				nextFrame = 0;
			if (TextureCurrentLerpTime >= 1.0f)
			{
				model.data[currentMesh.meshNum].meshDataTool.SetMaterial(materials[i][nextFrame]);
				model.data[currentMesh.meshNum].arrMesh.SurfaceSetMaterial(0, materials[i][nextFrame]);
				textureCurrentFrame[i] = nextFrame;
			}
		}

		if (TextureCurrentLerpTime >= 1.0f)
			TextureCurrentLerpTime -= 1.0f;
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
			GD.Print("Model not found: " + modelName);
			QueueFree();
			return;
		}

		if (modelAnimation.fps == 0)
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
					 Mesher.AddNodeToMultiMeshes(model.data[i].multiMesh, this);
					multiMeshData.owner = this;
					multiMeshDataList.Add(multiMeshData);
				}
			}
		}

		currentState = GameManager.FuncState.Start;
	}

	void UpdateMultiMesh()
	{
		for (int i = 0; i < multiMeshDataList.Count; i++)
			Mesher.UpdateInstanceMultiMesh(multiMeshDataList[i].multiMesh, this);
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		switch (currentState)
		{
			default:
			break;
			case GameManager.FuncState.Ready:
				Start();
			break;
		}

		float deltaTime = (float)delta;

		AnimateModel(deltaTime);
		AnimateTexture(deltaTime);
		UpdateMultiMesh();
	}
}
