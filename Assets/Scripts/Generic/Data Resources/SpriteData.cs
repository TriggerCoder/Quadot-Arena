using Godot;
using System.Collections.Generic;

public partial class SpriteData : Resource
{
	[Export]
	public DestroyType destroyType = DestroyType.NoDestroy;
	[Export]
	public float destroyTimer = 0;
	[Export]
	public Color Modulate = Colors.Black;

	public List<Node> destroyNodes;
	public enum DestroyType
	{
		NoDestroy,
		DestroyAfterTime
	}

	public float baseTime = 1;
	private MultiMeshData multiMeshData = null;
	private Vector3 lastGlobalPosition = new Vector3(0, 0, 0);
	private Basis lastGlobalBasis = Basis.Identity;

	private Node3D referenceNode = null;
	private Vector3 startPosition;
	private Vector3 referencePosition;


	public Vector3 GlobalPosition;
	public Basis GlobalBasis;

	public Transform3D GlobalTransform;
	public bool readyToDestroy = false;
	public bool update = false;
	public void SetMultiMeshData(MultiMeshData data)
	{
		multiMeshData = data;
		//Set OffSetTime
		Modulate.A = GameManager.CurrentTimeMsec;
	}

	public void SetReferenceNode(Node3D node)
	{
		referenceNode = node;
		if (referenceNode == null)
			return;
		referencePosition = node.GlobalPosition;
		startPosition = GlobalPosition;
	}

	void CheckReference()
	{
		if (referenceNode == null)
			return;

		//"Parent" node was detroyed
		if (!IsInstanceValid(referenceNode))
		{
			readyToDestroy = true;
			GlobalPosition = MapLoader.mapMinCoord * 2f;
			return;
		}
			
		Vector3 distance = referenceNode.GlobalPosition - referencePosition;
		if (distance.LengthSquared() > Mathf.Epsilon)
			GlobalPosition = startPosition + distance;
	}

	void UpdateMultiMesh()
	{
		if (multiMeshData == null)
			return;

		if (((GlobalPosition - lastGlobalPosition).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.X - lastGlobalBasis.X).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.Y - lastGlobalBasis.Y).LengthSquared() > Mathf.Epsilon) ||
			((GlobalBasis.Z - lastGlobalBasis.Z).LengthSquared() > Mathf.Epsilon))
		{
			update = true;
			lastGlobalPosition = GlobalPosition;
			lastGlobalBasis = GlobalBasis;
			GlobalTransform = new Transform3D(GlobalBasis, GlobalPosition);
		}
	}

	public void Process(float deltaTime)
	{
		CheckReference();
		UpdateMultiMesh();
		if (destroyType == DestroyType.NoDestroy)
			return;

		destroyTimer -= deltaTime;
		if (destroyTimer < 0)
		{
			readyToDestroy = true;
			GlobalPosition = MapLoader.mapMinCoord * 2f;
		}
	}

	public void Destroy()
	{
		MultiMesh multiMesh = multiMeshData.multiMesh;
		List<SpriteData> multiMeshSet;
		if (Mesher.MultiMeshSprites.TryGetValue(multiMesh, out multiMeshSet))
		{
			if (multiMeshSet.Contains(this))
				multiMeshSet.Remove(this);
		}
		
		if (destroyNodes == null)
			return;
		
		for (int i = 0; i < destroyNodes.Count; i++)
		{
			Node node = destroyNodes[i];
			if (IsInstanceValid(node))
				node.QueueFree();
		}
	}
}