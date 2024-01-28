using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class MaterialManager : Node
{
	public static MaterialManager Instance;
	[Export]
	public ShaderMaterial illegal;
	[Export]
	public ShaderMaterial fogMaterial;
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

	//PowerUps FX Material
	public static ShaderMaterial quadFxMaterial;
	public static string quadFxShader = "powerups/quad";
	public static ShaderMaterial quadWeaponFxMaterial;
	public static string quadWeaponFxShader = "powerups/quadWeapon";

	[Export]
	public bool applyLightmaps = true;

	public static string shadowProperty = "ShadowIntensity";
	public static string lightMapProperty = "shader_parameter/LightMap";
	public static string opaqueTexProperty = "shader_parameter/Tex_0";
	public static string colorProperty = "shader_parameter/AmbientColor";
	public static string mixBrightness = "shader_parameter/mixBrightness";

	public static List<string> Decals = new List<string>();
	public static List<string> FogShaders = new List<string>();
	public static List<string> HasBillBoard = new List<string>() { "FLARESHADER" };
	public static List<string> PortalMaterials = new List<string>();
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
		Image image = ((CompressedTexture2D)illegal.Get("shader_parameter/Tex_0")).GetImage();
		TextureLoader.illegal = ImageTexture.CreateFromImage(image);
		foreach (string name in _decalsNames)
		{
			string upperName = name.ToUpper();
			GameManager.Print("Decal Texture Name: " + upperName);
			Decals.Add(upperName);
		}
	}

	public static void LoadFXShaders()
	{
		bool useAlpha = true;
		bool hasPortal = false;
		List<int> stage = new List<int>
		{
			0
		};
		quadFxShader = quadFxShader.ToUpper();
		quadWeaponFxShader = quadWeaponFxShader.ToUpper();

		quadFxMaterial = QShaderManager.GetShadedMaterial(quadFxShader, -1, ref useAlpha, ref hasPortal, stage);
		quadWeaponFxMaterial = QShaderManager.GetShadedMaterial(quadWeaponFxShader, -1, ref useAlpha, ref hasPortal, stage, true);
	}

	public static void AddBillBoard(string shaderName)
	{
		if (HasBillBoard.Contains(shaderName))
			return;
		HasBillBoard.Add(shaderName);
	}

	public static void AddFog(string shaderName)
	{
		if (FogShaders.Contains(shaderName))
			return;
		FogShaders.Add(shaderName);
	}

	public static void AddPortalMaterial(string shaderName)
	{
		if (PortalMaterials.Contains(shaderName))
			return;
		PortalMaterials.Add(shaderName);
	}
	public static bool IsPortalMaterial(string shaderName)
	{
		if (PortalMaterials.Contains(shaderName))
			return true;
		return false;
	}
	public static bool IsSkyTexture(string textureName)
	{
		if (textureName.Contains("/SKIES/"))
			return true;
		return false;
	}

	public static bool IsFogMaterial(string shaderName)
	{
		if (FogShaders.Contains(shaderName))
			return true;
		return false;
	}

	void AddAditionalTextures(string textureName, bool addAlpha = false)
	{
		QShader shader = new QShader(textureName, 0, 0, addAlpha);
		if (!AditionalTextures.ContainsKey(textureName))
			AditionalTextures.Add(textureName, shader);
	}
	public static ShaderMaterial GetMaterials(string textureName, int lm_index)
	{
		bool forceSkinAlpha = false;
		bool hasPortal = false;
		return GetMaterials(textureName, lm_index, ref forceSkinAlpha, ref hasPortal);
	}
	public static ShaderMaterial GetMaterials(string textureName, int lm_index, ref bool forceSkinAlpha)
	{
		bool hasPortal = false;
		return GetMaterials(textureName, lm_index, ref forceSkinAlpha, ref hasPortal);
	}
	public static ShaderMaterial GetMaterials(string textureName, int lm_index , ref bool forceSkinAlpha, ref bool hasPortal, bool forceView = false)
	{
		ShaderMaterial mat;

		// Lightmapping is on, so calc the lightmaps
		if (lm_index >= 0 && Instance.applyLightmaps)
		{
			if (Materials.ContainsKey(textureName + lm_index.ToString()))
			{
				if (hasPortal = IsPortalMaterial(textureName + lm_index.ToString()))
					return (ShaderMaterial)Materials[textureName + lm_index.ToString()].Duplicate(true);
				return Materials[textureName + lm_index.ToString()];
			}
			bool useTransparent = false;
			mat = QShaderManager.GetShadedMaterial(textureName, lm_index, ref useTransparent, ref hasPortal);
			if (mat == null)
			{
				// Lightmapping
				ImageTexture lmap = MapLoader.lightMaps[lm_index];
				if (forceSkinAlpha)
					mat = (ShaderMaterial)Instance.defaultTransparentLightMapMaterial.Duplicate(true);
				else
					mat = (ShaderMaterial)Instance.defaultLightMapMaterial.Duplicate(true);
				ImageTexture tex = TextureLoader.GetTexture(textureName);
				mat.Set(opaqueTexProperty, tex);
				mat.Set(lightMapProperty, lmap);
				mat.Set(mixBrightness, GameManager.Instance.mixBrightness);
			}
			else if (hasPortal)
				AddPortalMaterial(textureName + lm_index.ToString());

			forceSkinAlpha = useTransparent;
			Materials.Add(textureName + lm_index.ToString(), mat);
			return mat;
		}

		if (Materials.ContainsKey(textureName))
		{
			if (hasPortal = IsPortalMaterial(textureName))
				return (ShaderMaterial)Materials[textureName].Duplicate(true);
			return Materials[textureName];
		}

		if (Decals.Contains(textureName))
		{
			GD.Print("Decal found: " + textureName);
			if (!TextureLoader.Textures.ContainsKey(textureName))
				TextureLoader.AddNewTexture(textureName, false);
			ImageTexture tex = TextureLoader.GetTexture(textureName);
			mat = (ShaderMaterial)Instance.decalsMapMaterial.Duplicate(true);
			mat.Set(opaqueTexProperty, tex);
			mat.Set(colorProperty, GameManager.ambientLight);
			mat.Set(mixBrightness, GameManager.Instance.mixBrightness);
		}
		else
			mat = QShaderManager.GetShadedMaterial(textureName, 0, ref forceSkinAlpha, ref hasPortal);
		if (mat == null)
		{
			// Lightmapping is off, so don't.
			if (forceSkinAlpha)
				mat = (ShaderMaterial)Instance.defaultTransparentMaterial.Duplicate(true);
			else
				mat = (ShaderMaterial)Instance.defaultMaterial.Duplicate(true);
			ImageTexture tex = TextureLoader.GetTexture(textureName);
			mat.Set(opaqueTexProperty, tex);
			mat.Set(colorProperty, GameManager.ambientLight);
			mat.Set(mixBrightness, GameManager.Instance.mixBrightness);
		}
		else if (hasPortal)
			AddPortalMaterial(textureName);
		Materials.Add(textureName, mat);
		return mat;
	}
}
