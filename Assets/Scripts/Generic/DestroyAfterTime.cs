using Godot;
using System.Collections;
using System.Collections.Generic;
public partial class DestroyAfterTime : Node
{
	[Export]
	public float destroyTimer = 10;

	private Node parent;
	private List<ModelController> modelList = new List<ModelController>();
	private GameManager.FuncState currentState = GameManager.FuncState.None;
	public override void _Ready()
	{
		currentState = GameManager.FuncState.Ready;
	}

	private void AddAllMultiMeshModels(Node parent)
	{
		var Childrens = GameManager.GetAllChildrens(parent);
		foreach (var child in Childrens)
		{
			if (child is ModelController mesh)
				modelList.Add(mesh);
		}
	}
	public void Start()
	{
		parent = GetParent();
		AddAllMultiMeshModels(parent);
		currentState = GameManager.FuncState.Start;
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
		destroyTimer -= deltaTime;
		if (destroyTimer < 0)
			Destroy();
			
	}

	public void Destroy()
	{
		List<MultiMesh> updateMultiMesh = new List<MultiMesh>();
		foreach(ModelController modelController in modelList)
		{
			for (int i = 0; i < modelController.multiMeshDataList.Count; i++)
			{
				MultiMesh multiMesh = modelController.multiMeshDataList[i].multiMesh;
				if (Mesher.MultiMeshes.ContainsKey(multiMesh))
				{
					List<Node3D> multiMeshList = Mesher.MultiMeshes[multiMesh];
					if (multiMeshList.Contains(modelController.multiMeshDataList[i].owner))
						multiMeshList.Remove(modelController.multiMeshDataList[i].owner);
				}
				if (!updateMultiMesh.Contains(multiMesh))
					updateMultiMesh.Add(multiMesh);
			}
		}
		foreach (MultiMesh multiMesh in updateMultiMesh)
			Mesher.MultiMeshUpdateInstances(multiMesh);
		
		parent.QueueFree();
	}
}
