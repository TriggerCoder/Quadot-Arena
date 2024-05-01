using Godot;
using System;
using System.Collections.Generic;
public partial class ScoreBoard : Node3D
{
	public static ScoreBoard Instance;
	[Export]
	public Sprite3D medalImpressiveIcon;
	[Export]
	public Sprite3D medalGauntletIcon;
	[Export]
	public Sprite3D medalExcellentIcon;

	public List<ScoreBoard> ScoreList = new List<ScoreBoard>();

	private static readonly string medalImpressive = "MENU/MEDALS/MEDAL_IMPRESSIVE";
	private static readonly string medalGauntlet = "MENU/MEDALS/MEDAL_GAUNTLET";
	private static readonly string medalExcellent = "MENU/MEDALS/MEDAL_EXCELLENT";

	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		medalImpressiveIcon.Texture = TextureLoader.GetTextureOrAddTexture(medalImpressive, false);
		medalGauntletIcon.Texture = TextureLoader.GetTextureOrAddTexture(medalGauntlet, false);
		medalExcellentIcon.Texture = TextureLoader.GetTextureOrAddTexture(medalExcellent, false);
	}
}
