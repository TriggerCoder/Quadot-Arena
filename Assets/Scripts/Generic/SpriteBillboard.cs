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
	[Export]
	public bool alphaFade = false;

	private float baseTime = 1;
	private ShaderMaterial spriteMaterial;
	
	public override void _Ready()
	{
		if (string.IsNullOrEmpty(spriteName))
		{
			QueueFree();
			return;
		}

		if ((QShaderManager.HasShader(spriteName)) ||(TextureLoader.HasTexture(spriteName)))
			spriteMaterial = MaterialManager.GetMaterials(spriteName, -1, Transparent);
		else
		{
			TextureLoader.AddNewTexture(spriteName, Transparent);
			spriteMaterial = MaterialManager.GetMaterials(spriteName, -1, Transparent);
		}
		PixelSize = .5f * 1 / spriteRadius;
		Texture = (Texture2D)spriteMaterial.Get("shader_parameter/Tex_0");
		MaterialOverride = spriteMaterial;
		if (destroyTimer == 0)
			SetProcess(false);
		else
			baseTime = destroyTimer;
		SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		destroyTimer -= deltaTime;
		if (destroyTimer < 0)
		{
			QueueFree();
			return;
		}
		if (alphaFade)
			Modulate = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.0f, 1.0f, destroyTimer / baseTime));
	}
}
