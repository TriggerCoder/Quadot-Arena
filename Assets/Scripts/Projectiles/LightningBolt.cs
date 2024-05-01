using Godot;
using System;

public partial class LightningBolt : Node3D
{
	[Export]
	public MeshInstance3D[] Arcs;
	[Export]
	public ShaderMaterial boltMaterial;

	private float BoltLenght = 24f;
	private float BoltWidht = 2f;
	private ArrayMesh mesh;
	private MeshDataTool meshDataTool = new MeshDataTool();
	public override void _Ready()
	{
		mesh = Mesher.GenerateQuadMesh(BoltWidht, BoltLenght, 0.5f, 0f);
		mesh.SurfaceSetMaterial(0, boltMaterial);
		meshDataTool.CreateFromSurface(mesh, 0);
		meshDataTool.SetVertex(2, new Vector3(-.5f * BoltWidht, BoltLenght, 0));
		meshDataTool.SetVertex(3, new Vector3(.5f * BoltWidht, BoltLenght, 0));
		meshDataTool.SetVertexUV(2, new Vector2(0, BoltLenght / 2f));
		meshDataTool.SetVertexUV(3, new Vector2(1, BoltLenght / 2f));
		mesh.ClearSurfaces();
		meshDataTool.CommitToSurface(mesh);

		for (int i = 0; i<Arcs.Length; i++)
			Arcs[i].Mesh = mesh;

	}

	public void SetArcsColors(Color color)
	{
		for (int i = 0; i < Arcs.Length; i++)
			Arcs[i].SetInstanceShaderParameter("lightning_color", color);
	}

	public void SetArcsLayers(uint layer)
	{
		for (int i = 0; i < Arcs.Length; i++)
			Arcs[i].Layers = layer;
	}

	public void SetBoltLenght(float lenght)
	{
		BoltLenght = lenght;
		meshDataTool.SetVertex(2, new Vector3(-.5f * BoltWidht, BoltLenght, 0));
		meshDataTool.SetVertex(3, new Vector3(.5f * BoltWidht, BoltLenght, 0));
		meshDataTool.SetVertexUV(2, new Vector2(0, BoltLenght / 2f));
		meshDataTool.SetVertexUV(3, new Vector2(1, BoltLenght / 2f));
		mesh.ClearSurfaces();
		meshDataTool.CommitToSurface(mesh);
	}

	public void SetBoltMesh(Vector3 origin, Vector3 end)
	{
		Vector3 direction = (origin - end);
		BoltLenght = direction.Length();
		meshDataTool.SetVertex(2, new Vector3(-.5f * BoltWidht, BoltLenght, 0));
		meshDataTool.SetVertex(3, new Vector3(.5f * BoltWidht, BoltLenght, 0));
		meshDataTool.SetVertexUV(2, new Vector2(0, BoltLenght / 2f));
		meshDataTool.SetVertexUV(3, new Vector2(1, BoltLenght / 2f));
		mesh.ClearSurfaces();
		meshDataTool.CommitToSurface(mesh);
	}
}
