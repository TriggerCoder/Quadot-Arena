using Godot;
using System;

public partial class RailTrail : Node3D
{
	[Export]
	public Color modulate = Colors.White;
	[Export]
	public StandardMaterial3D baseMaterial;
	[Export]
	public MeshInstance3D trailMesh;
	[Export]
	public float destroyTimer = 1.5f;
	private float baseTime = 1;

	private StandardMaterial3D trailMaterial;
	private CylinderMesh rail;
	private Color black = Colors.Black;

	private GameManager.FuncState currentState = GameManager.FuncState.None;
	public void Init(Vector3 origin, Vector3 end, Color color)
	{
		rail = new CylinderMesh();
		rail.TopRadius = 0.1f;
		rail.BottomRadius = 0.1f;
		trailMaterial = (StandardMaterial3D)baseMaterial.Duplicate();
		rail.Material = trailMaterial;
		trailMesh.Mesh = rail;
		modulate = color;
		Vector3 direction = (origin - end);
		rail.Height = direction.Length();
		trailMesh.Layers = GameManager.AllPlayerViewMask;

		baseTime = destroyTimer;
		currentState = GameManager.FuncState.Start;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (currentState == GameManager.FuncState.None)
			return;

		float deltaTime = (float)delta;
		destroyTimer -= deltaTime;

		Color color = black.Lerp(modulate, destroyTimer / baseTime);
		trailMaterial.AlbedoColor = color;
		trailMaterial.Emission = color;
		if (destroyTimer < 0)
			QueueFree();
	}
}
