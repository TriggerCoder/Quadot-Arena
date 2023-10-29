using Godot;
using System;

public partial class SpriteBillboard : Sprite3D
{
	[Export]
	public string spriteName;
	[Export]
	public bool isTransparent = false;

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
			spriteMaterial = MaterialManager.GetMaterials(spriteName, -1, isTransparent);
		else
		{
			TextureLoader.AddNewTexture(spriteName, isTransparent);
			spriteMaterial = MaterialManager.GetMaterials(spriteName, -1, isTransparent);
		}
		Texture2D MainTex = (Texture2D)spriteMaterial.Get("shader_parameter/Tex_0");
		int height = MainTex.GetHeight();
		int width = MainTex.GetWidth();
		Axis = Vector3.Axis.Z;
		PixelSize = .05f;
		Texture = MainTex;
		Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
		MaterialOverride = spriteMaterial;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
