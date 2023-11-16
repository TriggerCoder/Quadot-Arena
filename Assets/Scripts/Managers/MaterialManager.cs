using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class MaterialManager : Node
{
	public static MaterialManager Instance;
	[Export]
	public ShaderMaterial illegal;
	[Export]
	public ShaderMaterial opaqueMaterial;
	[Export]
	public ShaderMaterial defaultMaterial;
	[Export]
	public ShaderMaterial billBoardMaterial;
	[Export]
	public ShaderMaterial spriteMaterial;
	[Export]
	public ShaderMaterial defaultTransparentMaterial;
	[Export]
	public ShaderMaterial defaultLightMapMaterial;
	[Export]
	public ShaderMaterial defaultTransparentLightMapMaterial;
	[Export]
	public ShaderMaterial decalsMapMaterial;
	[Export]
	public string[] _decalsNames;

	public bool applyLightmaps = true;

	public static string lightMapProperty = "shader_parameter/LightMap";
	public static string opaqueTexProperty = "shader_parameter/Tex_0";
	public static string colorProperty = "shader_parameter/AmbientColor";
	public static string mixBrightness = "shader_parameter/mixBrightness";

	public static List<string> Decals = new List<string>();
	public static List<string> HasBillBoard = new List<string>();
	public static List<ShaderMaterial> AllMaterials = new List<ShaderMaterial>();	
	public static Dictionary<string, ShaderMaterial> Materials = new Dictionary<string, ShaderMaterial>();
	public static Dictionary<string, QShader> AditionalTextures = new Dictionary<string, QShader>();

	public static readonly string[] rgbGenTextures = { "_S_Texture", "_W_Texture", "_IW_Texture" };
	public static readonly string[] rgbGenBase = { "_S_Base", "_W_Base", "_IW_Base" };
	public static readonly string[] rgbGenAmp = { "_S_Amp", "_W_Amp", "_IW_Amp" };
	public static readonly string[] rgbGenPhase = { "_S_Phase", "_W_Phase", "_IW_Phase" };
	public static readonly string[] rgbGenFreq = { "_S_Freq", "_W_Freq", "_IW_Freq" };

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		foreach (string name in _decalsNames)
		{
			string upperName = name.ToUpper();
			GD.Print("Decal Texture Name: " + upperName);
			Decals.Add(upperName);
		}
	}

	public static void AddBillBoard(string shaderName)
	{
		shaderName = shaderName.ToUpper();
		if (HasBillBoard.Contains(shaderName))
			return;
		HasBillBoard.Add(shaderName);
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
	public static ShaderMaterial GetMaterials(string textureName, int lm_index = -1)
	{
		bool forceSkinAlpha = false;
		return GetMaterials(textureName, lm_index, ref forceSkinAlpha);
	}

	public static ShaderMaterial GetMaterials(string textureName, int lm_index , ref bool forceSkinAlpha)
	{
//		if (IsSkyTexture(textureName))
//			return Instance.skyHole;

		// Load the primary texture for the surface from the texture lump
		// The texture lump itself will have already looked over all
		// available .pk3 files and compiled a dictionary of textures for us.
		ImageTexture tex = TextureLoader.GetTexture(textureName);

		ShaderMaterial mat;
		// Lightmapping is on, so calc the lightmaps
		if (lm_index >= 0 && Instance.applyLightmaps)
		{
			if (Materials.ContainsKey(textureName + lm_index.ToString()))
				return Materials[textureName + lm_index.ToString()];
			bool useTransparent = false;
			mat = QShaderManager.GetShadedMaterial(textureName, lm_index, ref useTransparent);
			if (mat == null)
			{
				// Lightmapping
				ImageTexture lmap = MapLoader.lightMaps[lm_index];
				if (forceSkinAlpha)
					mat = (ShaderMaterial)Instance.defaultTransparentLightMapMaterial.Duplicate(true);
				else
					mat = (ShaderMaterial)Instance.defaultLightMapMaterial.Duplicate(true);
			
				mat.Set(opaqueTexProperty, tex);
				mat.Set(lightMapProperty, lmap);
				mat.Set(mixBrightness, GameManager.Instance.mixBrightness);
			}
			forceSkinAlpha = useTransparent;
			Materials.Add(textureName + lm_index.ToString(), mat);
			return mat;
		}

		if (Materials.ContainsKey(textureName))
			return Materials[textureName];

		if (Decals.Contains(textureName))
		{
			GD.Print("Decal found: " + textureName);
			if (!TextureLoader.Textures.ContainsKey(textureName))
			{
				TextureLoader.AddNewTexture(textureName, false);
				tex = TextureLoader.GetTexture(textureName);
			}
			mat = (ShaderMaterial)Instance.decalsMapMaterial.Duplicate(true);
			mat.Set(opaqueTexProperty, tex);
			mat.Set(colorProperty, GameManager.ambientLight);
			mat.Set(mixBrightness, GameManager.Instance.mixBrightness);
		}
		else
			mat = QShaderManager.GetShadedMaterial(textureName, 0, ref forceSkinAlpha);
		if (mat == null)
		{
			// Lightmapping is off, so don't.
			if (forceSkinAlpha)
				mat = (ShaderMaterial)Instance.defaultTransparentMaterial.Duplicate(true);
			else
				mat = (ShaderMaterial)Instance.defaultMaterial.Duplicate(true);
			mat.Set(opaqueTexProperty, tex);
			mat.Set(colorProperty, GameManager.ambientLight);
			mat.Set(mixBrightness, GameManager.Instance.mixBrightness);
		}
		Materials.Add(textureName, mat);
		return mat;
	}
}
