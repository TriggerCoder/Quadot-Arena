using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class MaterialManager : Node
{
	public static MaterialManager Instance;
	[Export]
	public Material illegal;
	[Export]
	public Material skyHole;
	[Export]
	public Material opaqueMaterial;
	[Export]
	public Material defaultMaterial;
	[Export]
	public Material billBoardMaterial;
	[Export]
	public Material spriteMaterial;
	[Export]
	public Material defaultTransparentMaterial;
	[Export]
	public Material defaultLightMapMaterial;
	[Export]
	public Material defaultTransparentLightMapMaterial;
	[Export]
	public Material debug;
	[Export]
	public Material rgbGenIdentity;
	[Export]
	public Material tcGenEnvironment;
	[Export]
	public Material tcModRotate;
	[Export]
	public Material tcModScroll;

	public MaterialOverride[] _OverrideMaterials = new MaterialOverride[0];

	public bool applyLightmaps = true;

	public static string lightMapProperty = "shader_parameter/LightMap";
	public static string opaqueTexProperty = "shader_parameter/MainTex";
	public static string colorProperty = "shader_parameter/AmbientColor";

	public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();
	public static Dictionary<string, MaterialOverride> OverrideMaterials = new Dictionary<string, MaterialOverride>();
	public static Dictionary<string, QShader> AditionalTextures = new Dictionary<string, QShader>();

	public static readonly string[] rgbGenTextures = { "_S_Texture", "_W_Texture", "_IW_Texture" };
	public static readonly string[] rgbGenBase = { "_S_Base", "_W_Base", "_IW_Base" };
	public static readonly string[] rgbGenAmp = { "_S_Amp", "_W_Amp", "_IW_Amp" };
	public static readonly string[] rgbGenPhase = { "_S_Phase", "_W_Phase", "_IW_Phase" };
	public static readonly string[] rgbGenFreq = { "_S_Freq", "_W_Freq", "_IW_Freq" };

	[System.Serializable]
	public struct MaterialOverride
	{
		public string overrideName;
		public bool opaque;
		public string opaqueTextureName;
		public Material material;
		public MaterialAnimation[] animation;
	}

	[System.Serializable]
	public struct MaterialAnimation
	{
		public string[] textureFrames;
		public bool addAlpha;
		public int fps;
		public float Base;
		public float Amp;
		public float Phase;
		public float Freq;
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}
	public static bool IsSkyTexture(string textureName)
	{
		if (textureName.ToUpper().Contains("/SKIES/"))
			return true;
		return false;
	}
	void AddAditionalTextures(string textureName, bool addAlpha = false)
	{
		QShader shader = new QShader(textureName, 0, 0, addAlpha);
		if (!AditionalTextures.ContainsKey(textureName))
			AditionalTextures.Add(textureName, shader);
	}
	
	public static Material GetMaterials(string textureName, int lm_index, bool forceSkinAlpha = false)
	{
		if (IsSkyTexture(textureName))
			return Instance.skyHole;

		// Load the primary texture for the surface from the texture lump
		// The texture lump itself will have already looked over all
		// available .pk3 files and compiled a dictionary of textures for us.
		ImageTexture tex = TextureLoader.GetTexture(textureName);

		Material mat;
		// Lightmapping is on, so calc the lightmaps
		if (lm_index >= 0 && Instance.applyLightmaps)
		{
			if (Materials.ContainsKey(textureName + lm_index.ToString()))
				return Materials[textureName + lm_index.ToString()];

			// Lightmapping
			ImageTexture lmap = MapLoader.lightMaps[lm_index];

			if (forceSkinAlpha)
				mat = (Material)Instance.defaultLightMapMaterial.Duplicate(true);
			else
				mat = (Material)Instance.defaultLightMapMaterial.Duplicate(true);

			mat.Set(opaqueTexProperty, tex);
			mat.Set(lightMapProperty, lmap);
			
			Materials.Add(textureName + lm_index.ToString(), mat);
			return mat;
		}

		if (Materials.ContainsKey(textureName))
			return Materials[textureName];
		// Lightmapping is off, so don't.
		if (forceSkinAlpha)
			mat = (Material)Instance.defaultTransparentMaterial.Duplicate(true);
		else
			mat = (Material)Instance.defaultMaterial.Duplicate(true);
		mat.Set(opaqueTexProperty, tex);
		mat.Set(colorProperty, GameManager.ambientLight);
		Materials.Add(textureName, mat);
		return mat;
	}
}
