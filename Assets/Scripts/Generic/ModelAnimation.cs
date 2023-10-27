using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class ModelAnimation : Node3D
{
	[Export]
	public string modelName;
	[Export]
	public bool isTransparent = false;
	[Export]
	public bool castShadow = true;

	private MD3 md3Model;

	private MD3GodotConverted model;
	[Export]
	public AnimData modelAnimation;
	[Export]
	public AnimData textureAnimation;

	private List<int> modelAnim = new List<int>();
	private List<int> textureAnim = new List<int>();
	private Dictionary<int, ShaderMaterial[]> materials = new Dictionary<int, ShaderMaterial[]>();

	private int modelCurrentFrame;
	private List<int> textureCurrentFrame = new List<int>();

	private Vector3 currentOrigin;
	private float height;
	private float timer = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (string.IsNullOrEmpty(modelName))
		{
			QueueFree();
			return;
		}

		md3Model = ModelsManager.GetModel(modelName, isTransparent);
		if (md3Model == null)
		{
			GD.Print("Model not found: " + modelName);
//			SetProcess(false);
			return;
		}

		Node3D currentObject = this;
		model = Mesher.GenerateModelFromMeshes(md3Model, GameManager.AllPlayerViewMask, currentObject, isTransparent, null);

		for (int i = 0; i < md3Model.meshes.Count; i++)
		{
			var modelMesh = md3Model.meshes[i];
			if (modelMesh.numFrames > 1)
				modelAnim.Add(i);
			if (modelMesh.numSkins > 1)
			{
				ShaderMaterial[] frames = new ShaderMaterial[modelMesh.numSkins];
				for (int j = 0; j < modelMesh.numSkins; j++)
				{
					string texName = modelMesh.skins[j].name;
					if (TextureLoader.HasTexture(texName))
						frames[j] = MaterialManager.GetMaterials(texName, -1, isTransparent);
					else
					{
						TextureLoader.AddNewTexture(texName, isTransparent);
						frames[j] = MaterialManager.GetMaterials(texName, -1, isTransparent);
					}
				}
				textureAnim.Add(i);
				textureCurrentFrame.Add(0);
				materials.Add(i, frames);
			}
		}

		modelCurrentFrame = 0;
		modelAnimation.currentLerpTime = 0;
		textureAnimation.currentLerpTime = 0;
	}

	void AnimateModel(float deltaTime)
	{
		int currentFrame = modelCurrentFrame;
		int nextFrame = currentFrame + 1;
		float t = modelAnimation.currentLerpTime;
		if (nextFrame >= md3Model.numFrames)
			nextFrame = 0;

		for (int i = 0; i < modelAnim.Count; i++)
		{
			MD3Mesh currentMesh = md3Model.meshes[modelAnim[i]];
			List<Vector3> lerpVertex = new List<Vector3>(currentMesh.numVertices);
			List<Vector3> lerpNormals = new List<Vector3>(currentMesh.numVertices);
			for (int j = 0; j < currentMesh.numVertices; j++)
			{
				Vector3 newVertex = currentMesh.verts[currentFrame][j].Lerp(currentMesh.verts[nextFrame][j], t);
				Vector3 newNormal = currentMesh.normals[currentFrame][j].Lerp(currentMesh.normals[nextFrame][j], t);

				lerpVertex.Add(newVertex);
				lerpNormals.Add(newNormal);
			}
			Mesher.UpdateVertices(model.data[i], lerpVertex, lerpNormals);
//			Mesher.RecalculateNormals((ArrayMesh)model.meshInstance[i].Mesh,model.arrMesh[i]);
		}

		modelAnimation.lerpTime = modelAnimation.fps * deltaTime;
		modelAnimation.currentLerpTime += modelAnimation.lerpTime;

		if (modelAnimation.currentLerpTime >= 1.0f)
		{
			modelAnimation.currentLerpTime -= 1.0f;
			modelCurrentFrame = nextFrame;
		}
	}
	void AnimateTexture(float deltaTime)
	{
		textureAnimation.lerpTime = textureAnimation.fps * deltaTime;
		textureAnimation.currentLerpTime += textureAnimation.lerpTime;

		for (int i = 0; i < textureAnim.Count; i++)
		{
			MD3Mesh currentMesh = md3Model.meshes[textureAnim[i]];

			int currentFrame = textureCurrentFrame[i];
			int nextFrame = currentFrame + 1;
			if (nextFrame >= currentMesh.numSkins)
				nextFrame = 0;
			if (textureAnimation.currentLerpTime >= 1.0f)
			{
				model.data[currentMesh.meshNum].arrMesh.SurfaceSetMaterial(0, materials[i][nextFrame]);
				textureCurrentFrame[i] = nextFrame;
			}
		}

		if (textureAnimation.currentLerpTime >= 1.0f)
			textureAnimation.currentLerpTime -= 1.0f;
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (md3Model == null)
			_Ready();
		
		float deltaTime = (float)delta;

		AnimateModel(deltaTime);
		AnimateTexture(deltaTime);
	}
}
