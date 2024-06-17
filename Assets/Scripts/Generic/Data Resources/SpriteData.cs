using Godot;
using System;
using System.Collections.Generic;

public partial class SpriteData : Resource
{
	[Export]
	public bool alphaFade = false;
	[Export]
	public DestroyType destroyType = DestroyType.NoDestroy;
	[Export]
	public float destroyTimer = 0;
	[Export]
	public Color Modulate = Colors.Black;
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

		//Node was detroyed
		if (!IsInstanceValid(referenceNode))
		{
			readyToDestroy = true;
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

		if ((alphaFade) ||
			((GlobalPosition - lastGlobalPosition).LengthSquared() > Mathf.Epsilon) ||
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
		if (alphaFade)
		{
			float alphaValue = Mathf.Lerp(1.0f, 0.0f, destroyTimer / baseTime);
			Modulate = new Color(alphaValue, alphaValue, alphaValue);
		}
		if (destroyType == DestroyType.NoDestroy)
			return;

		destroyTimer -= deltaTime;
		if (destroyTimer < 0)
			readyToDestroy = true;
	}

	public void Destroy()
	{
		MultiMesh multiMesh = multiMeshData.multiMesh;
		HashSet<SpriteData> multiMeshSet;
		if (Mesher.MultiMeshSprites.TryGetValue(multiMesh, out multiMeshSet))
		{
			if (multiMeshSet.Contains(this))
				multiMeshSet.Remove(this);
		}
	}
}
