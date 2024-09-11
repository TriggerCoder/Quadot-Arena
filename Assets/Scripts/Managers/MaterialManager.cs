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
	public ShaderMaterial defaultMaterial;
	[Export]
	public ShaderMaterial defaultTransparentMaterial;
	[Export]
	public ShaderMaterial defaultLightMapMaterial;
	[Export]
	public ShaderMaterial defaultTransparentLightMapMaterial;
	[Export]
	public ShaderMaterial decalsMapMaterial;
	[Export]
	public ShaderMaterial baseCameraMaterial;
	[Export]
	public ShaderMaterial underWaterMaterial;
	[Export]
	public ShaderMaterial painModelMaterial;
	[Export]
	public Resource[] _extraTextures;
	[Export]
	public string[] _decalsNames;
	[Export]
	public string[] _extraQShaders;

	//PowerUps FX Material
	public static ShaderMaterial quadFxMaterial;
	public static string quadFxShader = "POWERUPS/QUAD";
	public static ShaderMaterial quadWeaponFxMaterial;
	public static string quadWeaponFxShader = "POWERUPS/QUADWEAPON";
	public static ShaderMaterial battleSuitFxMaterial;
	public static string battleSuitFxShader = "POWERUPS/BATTLESUIT";
	public static ShaderMaterial battleSuitWeaponFxMaterial;
	public static string battleSuitWeaponFxShader = "POWERUPS/BATTLEWEAPON";
	public static ShaderMaterial battleSuitAndQuadFxMaterial;
	public static ShaderMaterial battleSuitAndQuadWeaponFxMaterial;
	public static ShaderMaterial invisFxMaterial;
	public static ShaderMaterial invisWeaponFxMaterial;
	public static string invisFxShader = "POWERUPS/INVISIBILITY";
	public static ShaderMaterial regenFxMaterial;
	public static string regenFxShader = "POWERUPS/REGEN";
	public static ShaderMaterial regenWeaponFxMaterial;
	public static string regenWeaponFxShader = "POWERUPS/REGENWEAPON";

	public static ShaderMaterial consoleMaterial;
	public static string consoleShader = "CONSOLE";
	public static FogMaterial waterFogMaterial;
	public static FogMaterial lavaFogMaterial;
	public static FogMaterial slimeFogMaterial;

	[Export]
	public bool applyLightmaps = true;

	public static string shadowProperty = "ShadowIntensity";
	public static string lightMapProperty = "shader_parameter/LightMap";
	public static string opaqueTexProperty = "shader_parameter/Tex_0";
	public static string screenTexure = "screen_texture";
	public static string normalDepthTexture = "normal_depth_texture";
	public static string painTexure = "pain_texture";
	public static string pickUpTexture = "pick_up_texture";
	public static string advertisementTexture = "TEXTURES/AD_CONTENT/AD2X1";

	public static List<string> Decals = new List<string>();
	public static List<string> FogShaders = new List<string>();
	public static HashSet<string> HasBillBoard = new HashSet<string>() { "FLARESHADER" };
	public static List<string> PortalMaterials = new List<string>();
	public static List<ShaderMaterial> AllMaterials = new List<ShaderMaterial>();
	public static HashSet<string> AdsMaterials = new HashSet<string>();
	public static Dictionary<string, ShaderMaterial> Materials = new Dictionary<string, ShaderMaterial>();
	public static Dictionary<string, ShaderMaterial> MirrorMaterials = new Dictionary<string, ShaderMaterial>();
	public static Dictionary<string, QShader> AditionalTextures = new Dictionary<string, QShader>();

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

		waterFogMaterial = new FogMaterial();
		waterFogMaterial.Density = .25f;
		waterFogMaterial.Albedo = new Color(.16f, .46f, .52f);
		waterFogMaterial.Emission = new Color(.3f, .3f, .35f);

		lavaFogMaterial = new FogMaterial();
		lavaFogMaterial.Density = .3f;
		lavaFogMaterial.Albedo = new Color(.91f, .18f, .18f);
		lavaFogMaterial.Emission = new Color(.75f, .38f, .0f);

		slimeFogMaterial = new FogMaterial();
		slimeFogMaterial.Density = .3f;
		slimeFogMaterial.Albedo = new Color(.12f, .45f, .25f);
		slimeFogMaterial.Emission = new Color(.10f, .87f, .40f);

		foreach (Resource res in _extraTextures)
			TextureLoader.LoadTexturesFromResource(res);

		TextureLoader.CreateWhiteImage();
	}

	public void AddExtraShaders()
	{
		foreach (string FileName in _extraQShaders)
		{
			var file = FileAccess.Open(FileName, FileAccess.ModeFlags.Read);
			if (file != null)
			{
				byte[] content = file.GetBuffer((long)file.GetLength());
				QShaderManager.ReadShaderData(content);
			}
		}
	}

	public static void SetAmbient()
	{
		RenderingServer.GlobalShaderParameterSet("AmbientColor", GameManager.Instance.ambientLightColor);
		RenderingServer.GlobalShaderParameterSet("mixBrightness", GameManager.Instance.mixBrightness);
	}

	public static void LoadFXShaders()
	{
		bool useAlpha = false;
		bool hasPortal = false;
		List<int> stage = new List<int>
		{
			0
		};
		quadFxMaterial = QShaderManager.GetShadedMaterial(quadFxShader, -1, ref useAlpha, ref hasPortal, stage);
		quadWeaponFxMaterial = QShaderManager.GetShadedMaterial(quadWeaponFxShader, -1, ref useAlpha, ref hasPortal, stage, true);
		battleSuitFxMaterial = QShaderManager.GetShadedMaterial(battleSuitFxShader, -1, ref useAlpha, ref hasPortal, stage);
		battleSuitWeaponFxMaterial = QShaderManager.GetShadedMaterial(battleSuitWeaponFxShader, -1, ref useAlpha, ref hasPortal, stage, true);
		invisFxMaterial = QShaderManager.GetShadedMaterial(invisFxShader, -1, ref useAlpha, ref hasPortal, stage);
		invisWeaponFxMaterial = QShaderManager.GetShadedMaterial(invisFxShader, -1, ref useAlpha, ref hasPortal, stage, true);
		regenFxMaterial = QShaderManager.GetShadedMaterial(regenFxShader, -1, ref useAlpha, ref hasPortal, stage);
		regenWeaponFxMaterial = QShaderManager.GetShadedMaterial(regenWeaponFxShader, -1, ref useAlpha, ref hasPortal, stage, true);

		consoleMaterial = QShaderManager.GetShadedMaterial(consoleShader, -1, ref useAlpha, ref hasPortal, null, false, true);

		battleSuitAndQuadFxMaterial = (ShaderMaterial)battleSuitFxMaterial.Duplicate(true);
		battleSuitAndQuadFxMaterial.NextPass = quadFxMaterial;

		battleSuitAndQuadWeaponFxMaterial = (ShaderMaterial)battleSuitWeaponFxMaterial.Duplicate(true);
		battleSuitAndQuadWeaponFxMaterial.NextPass = quadWeaponFxMaterial;
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
	public static ShaderMaterial GetMaterials(string textureName, int lm_index , ref bool forceSkinAlpha, ref bool hasPortal)
	{
		ShaderMaterial mat;
		ImageTexture lmap = null;

		// Lightmapping is on, so calc the lightmaps
		if (lm_index >= 0 && Instance.applyLightmaps)
			lmap = MapLoader.lightMaps[lm_index];

		if (Materials.TryGetValue(textureName, out ShaderMaterial sourceMat))
		{
			if (hasPortal = IsPortalMaterial(textureName))
			{
				mat = (ShaderMaterial)sourceMat.Duplicate(true);
				if (lm_index >= 0 && Instance.applyLightmaps)
					mat.Set(lightMapProperty, lmap);
				return mat;
			}

			if (lm_index >= 0 && Instance.applyLightmaps)
			{
				mat = (ShaderMaterial)sourceMat.Duplicate(true);
				ShaderMaterial lastMaterial = mat;
				while ((lastMaterial != null) && (lm_index >= 0 && Instance.applyLightmaps))
				{
					lastMaterial.Set(lightMapProperty, lmap);
					lastMaterial = (ShaderMaterial)lastMaterial.NextPass;
				}
			}
			else
				mat = sourceMat;
			return mat;
		}

		if (Decals.Contains(textureName))
		{
			GD.Print("Decal found: " + textureName);
			if (!TextureLoader.Textures.ContainsKey(textureName))
				TextureLoader.AddNewTexture(textureName, false);
			ImageTexture tex = TextureLoader.GetTexture(textureName);
			mat = (ShaderMaterial)Instance.decalsMapMaterial.Duplicate(true);
			mat.Set(opaqueTexProperty, tex);
		}
		else
		{
			mat = QShaderManager.GetShadedMaterial(textureName, lm_index, ref forceSkinAlpha, ref hasPortal);
			ShaderMaterial lastMaterial = mat;
			while ((lastMaterial != null) && (lm_index >= 0 && Instance.applyLightmaps))
			{
				lastMaterial.Set(lightMapProperty, lmap);
				lastMaterial = (ShaderMaterial)lastMaterial.NextPass;
			}
		}
		if (mat == null)
		{
			if (lm_index >= 0 && Instance.applyLightmaps)
			{
				if (forceSkinAlpha)
					mat = (ShaderMaterial)Instance.defaultTransparentLightMapMaterial.Duplicate(true);
				else
					mat = (ShaderMaterial)Instance.defaultLightMapMaterial.Duplicate(true);
				mat.Set(lightMapProperty, lmap);
			}
			else
			{
				if (forceSkinAlpha)
					mat = (ShaderMaterial)Instance.defaultTransparentMaterial.Duplicate(true);
				else
					mat = (ShaderMaterial)Instance.defaultMaterial.Duplicate(true);
			}

			ImageTexture tex = TextureLoader.GetTexture(textureName);
			mat.Set(opaqueTexProperty, tex);
		}
		else if (lm_index >= 0 && Instance.applyLightmaps)
			mat.Set(lightMapProperty, lmap);

		//Don't reuse Quake Live Ads Materials
		if (!AdsMaterials.Contains(textureName))
			Materials.Add(textureName, mat);
		return mat;
	}
	public static ShaderMaterial GetMirrorMaterial(string shaderName)
	{
		ShaderMaterial mat;
		if (MirrorMaterials.TryGetValue(shaderName, out mat))
			return (ShaderMaterial)mat.Duplicate(true);

		mat = QShaderManager.MirrorShader(shaderName);
		MirrorMaterials.Add(shaderName, mat);
		return mat;
	}
}
