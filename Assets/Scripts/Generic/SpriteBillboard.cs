using Godot;
using System;

public partial class SpriteBillboard : Sprite3D
{
	[Export]
	public string spriteName;
	[Export]
	public int spriteRadius = 2;
	[Export]
	public float destroyTimer = 0;

	private float baseTime = 1;
	private ShaderMaterial spriteMaterial;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (string.IsNullOrEmpty(spriteName))
		{
			QueueFree();
			return;
		}

		if (TextureLoader.HasTexture(spriteName))
			spriteMaterial = MaterialManager.GetMaterials(spriteName, -1, Transparent);
		else
		{
			TextureLoader.AddNewTexture(spriteName, Transparent);
			spriteMaterial = MaterialManager.GetMaterials(spriteName, -1, Transparent);
		}
		PixelSize = .5f * 1 / spriteRadius;
		Texture = (Texture2D)spriteMaterial.Get("shader_parameter/Tex_0");
		if (Billboard == BaseMaterial3D.BillboardModeEnum.Disabled)
			MaterialOverride = spriteMaterial;
		if (destroyTimer == 0)
			SetProcess(false);
		else
			baseTime = destroyTimer;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		destroyTimer -= deltaTime;
		Modulate = new Color(1.0f,1.0f, 1.0f, Mathf.Lerp(0.0f, 1.0f, destroyTimer/ baseTime));
		if (destroyTimer < 0)
		{
			QueueFree();
			return;
		}
	}
}
