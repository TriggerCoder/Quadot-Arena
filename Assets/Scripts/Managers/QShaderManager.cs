using Godot;
using System.IO;
using System.Collections.Generic;
using ExtensionMethods;

public static class QShaderManager
{
	public static Dictionary<string, QShaderData> QShaders = new Dictionary<string, QShaderData>();
	public static Dictionary<string, QShaderData> FogShaders = new Dictionary<string, QShaderData>();
	public enum GenFuncType
	{
		RGB,
		Alpha
	}

	public static ShaderMaterial GetFog(string shaderName, float height)
	{
		const string Density = "shader_parameter/density";
		const string Albedo = "shader_parameter/albedo";
		const string Emission = "shader_parameter/emission";
		const string HeightFalloff = "shader_parameter/height_falloff";

		ShaderMaterial fogMaterial = (ShaderMaterial)MaterialManager.Instance.fogMaterial.Duplicate(true);

		QShaderData Fog;
		if (!FogShaders.TryGetValue(shaderName, out Fog))
		{
			fogMaterial.Set(Density, .15f);
			fogMaterial.Set(Emission, Colors.Black);
			return fogMaterial;
		}

		float R = TryToParseFloat(Fog.qShaderGlobal.fogParms[1]);
		float G = TryToParseFloat(Fog.qShaderGlobal.fogParms[2]);
		float B = TryToParseFloat(Fog.qShaderGlobal.fogParms[3]);
		float OpaqueHeight = TryToParseFloat(Fog.qShaderGlobal.fogParms[5]) * GameManager.sizeDividor;
		if (OpaqueHeight >= height)
			fogMaterial.Set(Density, height / OpaqueHeight);
		else
		{
			fogMaterial.Set(Density, 1);
			fogMaterial.Set(HeightFalloff, .15f / OpaqueHeight);
		}

		fogMaterial.Set(Emission, new Color(R, G, B));


		return fogMaterial;
	}

	public static bool HasShader(string shaderName)
	{
		if (QShaders.ContainsKey(shaderName))
			return true;
		return false;
	}
	public static ShaderMaterial GetShadedMaterial(string shaderName, int lm_index, ref bool alphaIsTransparent, ref bool hasPortal, List<int> multiPassList = null, bool forceView = false, bool canvasShader = false, int numPass = 0)
	{
		string code = "";
		string GSHeader = "shader_type spatial;\nrender_mode diffuse_lambert, specular_schlick_ggx, ";
		if (canvasShader)
			GSHeader = "shader_type canvas_item;\nrender_mode ";
		string GSUniforms = "";
		string GSLigtH = "void light()\n{ \n";
		string GSVertexH = "void vertex()\n{ \n";
		string GSVaryings = "";
		string GSVertexUvs = "";
		string GSFragmentH = "void fragment()\n{ \n";
		string GSFragmentUvs = "";
		string GSFragmentTcMod = "";
		string GSFragmentTexs = "";
		string GSLateFragmentTexs = "";
		string GSFragmentRGBs = "\tvec4 vertx_color = COLOR;\n";
		if (canvasShader)
			GSFragmentRGBs = "\tvec4 vertx_color = vec4(1.0);\n";
		string GSFragmentBlends = "";
		string GSAnimation = "";

		List<string> textures = new List<string>();
		Dictionary<string, int> TexIndex = new Dictionary<string, int>();

		QShaderData qShader;
		if (!QShaders.TryGetValue(shaderName, out qShader))
			return null;

		GameManager.Print("Shader found: " + shaderName);

		GetSunData(qShader);

		int lightmapStage = -1;
		bool helperRotate = false;
		bool animStages = false;
		bool skyMap = false;
		bool depthWrite = false;
		bool entityColor = false;
		bool checkEditorImage = true;
		bool firstPass = true;

		//Needed for Mutipass
		int needMultiPass = 0;
		bool forceAlpha = alphaIsTransparent;
		QShaderGlobal.SortType sortType = qShader.qShaderGlobal.sort;
		int totalStages;

		if (multiPassList == null)
			totalStages = qShader.qShaderStages.Count;
		else
		{
			totalStages = multiPassList.Count;
			if (multiPassList[0] != 0)
			{
				firstPass = false;
				checkEditorImage = false;
			}
		}

		for (int i = 0; i < totalStages; i++)
		{
			int currentStage;
			if (multiPassList == null)
				currentStage = i;
			else
				currentStage = multiPassList[i];
			
			QShaderStage qShaderStage = qShader.qShaderStages[currentStage];

			if (qShaderStage.map != null)
			{
				if (qShaderStage.skyMap)
				{
					skyMap = true;
					int index = textures.Count;
					GSUniforms += "uniform samplerCube " + "Sky_" + currentStage + " : repeat_disable;\n";
					GSFragmentTexs += "\tvec3 sky_uv" + currentStage + " = vot" + currentStage + ".xyz;\n";
					GSFragmentTexs += "\tvec4 Stage_" + currentStage + " = texture(" + "Sky_" + currentStage + ", sky_uv" + currentStage + ");\n";
					TexIndex.Add(qShaderStage.map[0], index);
					textures.Add(qShaderStage.map[0]);
				}
				else if (qShaderStage.animFreq > 0)
				{
					animStages = true;
					GSLateFragmentTexs += "\tvec4 Stage_" + currentStage + " = animation_" + currentStage + "(Time, ";
					GSLateFragmentTexs += qShaderStage.animFreq.ToString("0.00") + " , " + qShaderStage.map.Length;
					for (int j = 0; j < qShaderStage.map.Length; j++)
						GSLateFragmentTexs += " , Anim_" + currentStage + "_" + j;
					GSLateFragmentTexs += ");\n";
					GSAnimation += "vec4 animation_" + currentStage + "(float Time, ";
					GSAnimation += "float freq , int frames";
					for (int j = 0; j < qShaderStage.map.Length; j++)
						GSAnimation += " , vec4 frame_" + j;
					GSAnimation += ")\n";
					GSAnimation += "{\n";
					GSAnimation += "\tvec4 frame["+ qShaderStage.map.Length + "] = { ";
					for (int j = 0; j < qShaderStage.map.Length; j++)
					{
						if (j != 0)
							GSAnimation += " , ";
						GSAnimation += "frame_" + j;
					}
					GSAnimation += "};\n";
					GSAnimation += "\tint currentFrame = int(floor(mod(Time * freq,  float(frames))));\n";
					GSAnimation += "\treturn frame[currentFrame];\n";
					GSAnimation += "}\n";
				}
				else
				{
					int index;
					if (TexIndex.TryGetValue(qShaderStage.map[0], out index))
					{
						if (qShaderStage.isLightmap)
							GSFragmentTexs += "\tvec4 Stage_" + currentStage + " = texture(" + "LightMap, uv_" + currentStage + ");\n";
						else
							GSFragmentTexs += "\tvec4 Stage_" + currentStage + " = texture(" + "Tex_" + index + ", uv_" + currentStage + ");\n";
					}
					else
					{
						index = textures.Count;
						if (qShaderStage.isLightmap)
						{
							GSUniforms += "uniform sampler2D " + "LightMap";
							GSFragmentTexs += "\tvec4 Stage_" + currentStage + " = texture(" + "LightMap, uv_" + currentStage + ");\n";
						}
						else
						{
							GSUniforms += "uniform sampler2D " + "Tex_" + index;
							GSFragmentTexs += "\tvec4 Stage_" + currentStage + " = texture(" + "Tex_" + index + ", uv_" + currentStage + ");\n";
						}

						if (qShaderStage.clamp)
							GSUniforms += " : repeat_disable;\n";
						else
							GSUniforms += " : repeat_enable;\n";

						TexIndex.Add(qShaderStage.map[0], index);
						//Lightmap will be added outside of the shader creation
						if (qShaderStage.isLightmap)
							textures.Add("");
						else
							textures.Add(qShaderStage.map[0]);
					}
				}
			}
			else
			{
				GameManager.Print("Shader: '" + shaderName + "' Stage: " + currentStage + " is invalid, skipping", GameManager.PrintType.Warning);
				continue;
			}

			GSVertexUvs += GetUVGen(qShader, currentStage, ref GSVaryings);
			GSFragmentUvs += GetTcGen(qShader, currentStage, ref lightmapStage);
			GSFragmentTcMod += GetTcMod(qShader, currentStage, ref helperRotate);
			
			GSFragmentRGBs += GetGenFunc(qShader, currentStage, GenFuncType.RGB, ref entityColor);
			GSFragmentRGBs += GetGenFunc(qShader, currentStage, GenFuncType.Alpha, ref entityColor);
			GSFragmentBlends += GetAlphaFunc(qShader, currentStage, multiPassList, ref needMultiPass);
			GSFragmentBlends += GetBlend(qShader, currentStage, i, multiPassList, ref needMultiPass, ref alphaIsTransparent);
			if (qShaderStage.depthWrite)
			{
				if (i == 0)
					depthWrite = true;
				else
					needMultiPass = i;
			}
		}

		if (needMultiPass > 0)
		{
			GameManager.Print("Shader needs multipass " + needMultiPass);
			List<int> matPass = new List<int>();
			for (int i = 0; i < needMultiPass; i++)
			{
				int currentStage;
				if (multiPassList == null)
					currentStage = i;
				else
					currentStage = multiPassList[i];
				matPass.Add(currentStage);
			}
			if (lightmapStage >= 0)
				if (!matPass.Contains(lightmapStage))
					matPass.Add(lightmapStage);
			bool baseAlpha = forceAlpha;

			ShaderMaterial passZeroMaterial = GetShadedMaterial(shaderName, lm_index, ref baseAlpha, ref hasPortal, matPass, forceView, canvasShader, numPass);
			ShaderMaterial lastMaterial = passZeroMaterial;

			while (lastMaterial.NextPass != null)
			{
				numPass++;
				lastMaterial = (ShaderMaterial)lastMaterial.NextPass;
			}

			matPass = new List<int>();
			if (multiPassList == null)
				totalStages = qShader.qShaderStages.Count;
			else
				totalStages = multiPassList.Count;
			for (int i = needMultiPass; i < totalStages; i++)
			{
				int currentStage;
				if (multiPassList == null)
					currentStage = i;
				else
					currentStage = multiPassList[i];
				matPass.Add(currentStage);
			}
			if (lightmapStage >= 0)
				if (!matPass.Contains(lightmapStage))
					matPass.Add(lightmapStage);

			qShader.qShaderGlobal.sort = sortType;

			numPass++;
			ShaderMaterial passOneMaterial = GetShadedMaterial(shaderName, lm_index, ref forceAlpha, ref hasPortal, matPass, forceView, canvasShader, numPass);
			lastMaterial.NextPass = passOneMaterial;
			alphaIsTransparent = forceAlpha | baseAlpha;

			return passZeroMaterial;
		}

		if (canvasShader)
		{
			switch (qShader.qShaderGlobal.sort)
			{
				case QShaderGlobal.SortType.Opaque:
					GSHeader += "blend_mix";
				break;
				case QShaderGlobal.SortType.Additive:
					GSHeader += "blend_add";
				break;
				case QShaderGlobal.SortType.Multiplicative:
					GSHeader += "blend_mul";
					qShader.qShaderGlobal.unShaded = true;
				break;
			}
		}
		else
		{
			switch (qShader.qShaderGlobal.sort)
			{
				case QShaderGlobal.SortType.Opaque:
				{
					if (alphaIsTransparent)
					{
						if (multiPassList != null)
							GSHeader += "depth_prepass_alpha, ";
					}
					if ((depthWrite) || (forceAlpha))
						GSHeader += "depth_draw_always, blend_mix, ";
					else
						GSHeader += "depth_draw_opaque, blend_mix, ";
				}
				break;
				case QShaderGlobal.SortType.Additive:
					if (depthWrite)
						GSHeader += "depth_draw_always, blend_add, ";
					else
						GSHeader += "depth_draw_opaque, blend_add, ";
				break;
				case QShaderGlobal.SortType.Multiplicative:
					if (depthWrite)
						GSHeader += "depth_draw_always, blend_mul, ";
					else
						GSHeader += "depth_draw_opaque, blend_mul, ";
					qShader.qShaderGlobal.unShaded = true;
				break;
			}
		}

		if (qShader.qShaderGlobal.isSky)
		{
			qShader.qShaderGlobal.unShaded = true;
			checkEditorImage = false;
		}

		if (qShader.qShaderGlobal.unShaded)
		{
			if (canvasShader)
				GSHeader += ", unshaded";
			else
				GSHeader += "unshaded, ";
		}

		if (canvasShader)
			GSHeader += ";\n \n";
		else
		{
			switch (qShader.qShaderGlobal.cullType)
			{
				case QShaderGlobal.CullType.Back:
					GSHeader += "cull_back;\n \n";
				break;
				case QShaderGlobal.CullType.Front:
					GSHeader += "cull_front;\n \n";
				break;
				case QShaderGlobal.CullType.Disable:
					GSHeader += "cull_disabled;\n \n";
				break;
			}
		}

		int totalTex = textures.Count;
		if (qShader.qShaderGlobal.trans)
		{
			alphaIsTransparent = true;
			GameManager.Print("Current shader is transparent");
		}

		if ((checkEditorImage) && (qShader.qShaderGlobal.editorImage.Length != 0))
		{
			if (QShaders.TryGetValue(qShader.qShaderGlobal.editorImage, out QShaderData qeditorShader))
			{
				if (qeditorShader.qShaderGlobal.trans)
				{
					alphaIsTransparent = true;
					GameManager.Print("Current editor shader is transparent");
				}
			}

			if (TextureLoader.HasTextureOrAddTexture(qShader.qShaderGlobal.editorImage, alphaIsTransparent))
			{
				int lastStage = totalStages;
				if (multiPassList != null)
					lastStage = multiPassList[lastStage-1] + 1;

				if (TexIndex.TryGetValue(qShader.qShaderGlobal.editorImage, out int editorIndex))
				{
					GSFragmentUvs += "\tvec2 uv_" + lastStage + " = UV;\n";
					GSFragmentTexs += "\tvec4 Stage_" + lastStage + " = texture(" + "Tex_" + editorIndex + ", uv_" + lastStage + ");\n";
				}
				else
				{
					GSUniforms += "uniform sampler2D " + "Tex_" + totalTex + " : repeat_enable;\n";
					GSFragmentUvs += "\tvec2 uv_" + lastStage + " = UV;\n";
					GSFragmentTexs += "\tvec4 Stage_" + lastStage + " = texture(" + "Tex_" + totalTex + ", uv_" + lastStage + ");\n";
					textures.Add(qShader.qShaderGlobal.editorImage);
					TexIndex.Add(qShader.qShaderGlobal.editorImage, totalTex);
					totalTex++;
				}
			}
		}

		if (animStages)
		{
			for (int i = 0; i < totalStages; i++)
			{
				QShaderStage qShaderStage = qShader.qShaderStages[i];
				if (qShaderStage.map != null)
				{
					if (qShaderStage.animFreq > 0)
					{
						for (int j = 0; j < qShaderStage.map.Length; j++)
						{
							if (TexIndex.TryGetValue(qShaderStage.map[j], out int texIndex))
								GSFragmentTexs += "\tvec4 Anim_" + i + "_" + j + " = texture(" + "Tex_" + texIndex + ", uv_" + i + ");\n";
							else
							{
								GSUniforms += "uniform sampler2D " + "Tex_" + totalTex + " : repeat_enable;\n";
								GSFragmentTexs += "\tvec4 Anim_" + i + "_" + j + " = texture(" + "Tex_" + totalTex + ", uv_" + i + ");\n";
								TexIndex.Add(qShaderStage.map[j], totalTex);
								textures.Add(qShaderStage.map[j]);
								totalTex++;
							}
						}
					}
				}
			}
		}

		code += GSHeader;
		code += GSUniforms;
		code += GSVaryings;
		code += "global uniform float ViewCameraFOV;\n";
		code += "global uniform float MsTime;\n";
		code += "global uniform vec4 AmbientColor: source_color;\n";
		code += "global uniform float mixBrightness;\n";
		if (canvasShader)
			code += "uniform float OffSetTime = 0.0;\n";
		else
			code += "instance uniform float OffSetTime = 0.0;\n";
		if (entityColor)
		{
			code += "uniform bool UseModulation = true;\n";
			if (canvasShader)
				code += "uniform vec4 modulate: source_color = vec4(1.0, 1.0, 1.0, 1.0);\n";
			else
				code += "instance uniform vec4 modulate: source_color = vec4(1.0, 1.0, 1.0, 1.0);\n";
		}
		if ((multiPassList == null) && (canvasShader == false) && (qShader.qShaderGlobal.isSky == false))
		{
			code += "instance uniform float ShadowIntensity : hint_range(0, 1) = 0.0;\n";
			code += "instance uniform bool ViewModel = false;\n";
			code += "instance uniform bool UseLightVol = false;\n";
			code += "global uniform vec3 LightVolNormalize;\n";
			code += "global uniform vec3 LightVolOffset;\n";
			code += "global uniform sampler3D LightVolAmbient;\n";
			code += "global uniform sampler3D LightVolDirectonal;\n";
			code += "varying vec3 ambientColor;\n";
			code += "varying vec3 dirColor;\n";
			code += "varying vec3 dirVector;\n\n";
			code += "vec3 GetTextureCoordinates(vec3 Position)\n";
			code += "{\n\tPosition -= LightVolOffset;\n";
			code += "\tvec3 Q3Pos = vec3(Position.x / -LightVolNormalize.x, Position.z / LightVolNormalize.y, Position.y / LightVolNormalize.z);\n";
			code += "\treturn Q3Pos;\n}\n";
		}
		if (helperRotate)
		{
			code += "\nvec2 rotate(vec2 uv, vec2 pivot, float angle)\n{\n\tmat2 rotation = mat2(vec2(sin(angle), -cos(angle)),vec2(cos(angle), sin(angle)));\n";
			code += "\tuv -= pivot;\n\tuv = uv * rotation;\n\tuv += pivot;\n\treturn uv;\n}\n\n";
		}

		if (animStages)
			code += GSAnimation;

		//Vertex
		if ((canvasShader == false) && (qShader.qShaderGlobal.isSky == false))
		{
			float Value = 0.001f;
			bool offSet = qShader.qShaderGlobal.polygonOffset;
			if (offSet)
				numPass++;
			code += GSVertexH;
			if (multiPassList == null)
			{
				code += "\tvec3 WorldPos = GetTextureCoordinates((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz);\n";
				code += "\tvec4 ambient = texture(LightVolAmbient, WorldPos);\n";
				code += "\tvec4 dir = texture(LightVolDirectonal, WorldPos);\n";
				code += "\tambientColor = ambient.rgb;\n";
				code += "\tdirColor = dir.rgb;\n";
				code += "\tfloat lng = ambient.a * (PI) / 128.0f;\n";
				code += "\tfloat lat = dir.a * (PI) / 128.0f;\n";
				code += "\tdirVector = vec3(-cos(lat) * sin(lng), cos(lng), sin(lat) * sin(lng));\n";
			}
			else if (!firstPass)
				offSet = true;

			if (offSet)
				Value *= numPass;
			code += GetVertex(qShader, (multiPassList == null), forceView, offSet, Value);
			code += GSVertexUvs + "}\n";
		}

		//Lightning
		if ((multiPassList == null) && (qShader.qShaderGlobal.unShaded == false) && (canvasShader == false))
		{
			code += GSLigtH;
			code += GetDiffuseLightning();
			code += "}\n";
		}

		code += GSFragmentH;
		code += GSFragmentUvs;
		code += "\tfloat Time = (MsTime - OffSetTime);\n";
		code += GSFragmentTcMod;
		code += GSFragmentTexs;
		code += GSLateFragmentTexs;
		code += GSFragmentRGBs;

		if (qShader.qShaderGlobal.isSky)
			code += "\tvec3 ambient = AmbientColor.rgb;\n";
		else if (lightmapStage < 0)
			code += "\tvec3 ambient = AmbientColor.rgb * mixBrightness;\n";

		if ((qShader.qShaderGlobal.isSky) || (!firstPass) || (qShader.qShaderGlobal.sort == QShaderGlobal.SortType.Additive))
			code += "\tvec4 color = vec4(0.0, 0.0, 0.0, 0.0);\n";
		else if (lightmapStage >= 0)
			code += "\tvec4 color = Stage_" + lightmapStage + ";\n";
		else if (qShader.qShaderGlobal.editorImage.Length != 0)
		{
			if (TexIndex.ContainsKey(qShader.qShaderGlobal.editorImage))
				code += "\tvec4 color = Stage_" + totalStages + ";\n";
			else
				code += "\tvec4 color = vec4(0.0, 0.0, 0.0, 0.0);\n";
		}
		else 
			code += "\tvec4 color = vec4(0.0, 0.0, 0.0, 0.0);\n";

		code += "\tvec4 black = vec4(0.0, 0.0, 0.0, 0.0);\n";
		code += "\tvec4 white = vec4(1.0, 1.0, 1.0, 1.0);\n";
		code += GSFragmentBlends;

		if (qShader.qShaderGlobal.isSky)
		{
			if (canvasShader)
				code += "\tCOLOR.rgb = (color.rgb * ambient);\n";
			else
			{
				code += "\tALBEDO = (color.rgb * ambient);\n";
				code += "\tEMISSION = ambient;\n";
			}
		}
		else if (qShader.qShaderGlobal.sort == QShaderGlobal.SortType.Multiplicative)
		{
			if (canvasShader)
				code += "\tCOLOR.rgb = color.rgb;\n";
			else
				code += "\tALBEDO = color.rgb;\n";
		}
		else
		{
			code += "\tvec3 albedo = color.rgb * vertx_color.rgb;\n";
			code += "\tvec3 emission = color.rgb;\n";
			if (lightmapStage >= 0)
				code += "\temission = mix(Stage_" + lightmapStage + ".rgb * color.rgb, color.rgb, mixBrightness);\n";
			else
			{
				code += "\tvec3 defaultEmission = mix(emission * ambient, emission, mixBrightness);\n";
				if ((multiPassList == null) && (canvasShader == false))
				{
					code += "\tvec3 useLightVolEmission = emission * mix(ambientColor, emission, mixBrightness);\n";
					code += "\temission = mix(defaultEmission, useLightVolEmission, float(UseLightVol));\n";
				}
				else
					code += "\temission = defaultEmission;\n";
			}

			//FOG gets removed from sprites
			if ((MaterialManager.HasBillBoard.Contains(qShader.Name)) && (qShader.qShaderGlobal.sort == QShaderGlobal.SortType.Additive))
				code += "\talbedo -= FOG.rgb;\n";
			if (canvasShader)
				code += "\tCOLOR.rgb = albedo;\n";
			else
			{
				code += "\tALBEDO = albedo;\n";
				code += "\tEMISSION = emission;\n";
			}
		}
		if (forceView)
			code += "\tDEPTH = mix(FRAGCOORD.z, 1.0, 0.999);\n";
		else if ((multiPassList == null) && (canvasShader == false) && (qShader.qShaderGlobal.isSky == false))
			code += "\tDEPTH = mix(FRAGCOORD.z, mix(FRAGCOORD.z, 1.0, 0.999), float(ViewModel));\n";

		if (qShader.qShaderGlobal.portal)
		{
			hasPortal = true;
			alphaIsTransparent = false;
		}

		if (alphaIsTransparent)
		{
			if (canvasShader)
				code += "\tCOLOR = COLOR.rgb + color.a;\n";
			else if ((multiPassList == null) || (firstPass))
				code += "\tALPHA = color.a;\n";
			else
			{
				code += "\tALPHA_HASH_SCALE = 0.5;\n";
				code += "\tALPHA = color.a;\n";
			}
		}
		code += "}\n\n";

//		if (shaderName.Contains("RAILGUN"))
//			GameManager.Print(code);

		Shader shader = new Shader();
		shader.Code = code;
		ImageTexture tex;
		ShaderMaterial shaderMaterial = new ShaderMaterial();
		shaderMaterial.Shader = shader;
		for (int i = 0; i < textures.Count; i++)
		{
			if (textures[i].Length > 0)
			{
				//CubeMap Skybox will always be the first stage
				if ((i == 0) && (skyMap))
				{
					Cubemap cube = TextureLoader.GetCubeMap(textures[i]);
					shaderMaterial.SetShaderParameter("Sky_" + i, cube);
				}
				//Check for Quake Live Ads
				else if (textures[i] == MaterialManager.advertisementTexture)
				{
					shaderMaterial.SetShaderParameter("Tex_" + i, GameManager.Instance.AdvertisementViewPort.GetTexture());
					if (!MaterialManager.AdsMaterials.Contains(shaderName))
						MaterialManager.AdsMaterials.Add(shaderName);
				}
				else
				{
					tex = TextureLoader.GetTextureOrAddTexture(textures[i], alphaIsTransparent);
					shaderMaterial.SetShaderParameter("Tex_" + i, tex);
				}
			}
		}

		if (hasPortal)
		{
			ShaderMaterial portalMaterial = PortalMaterial(qShader);
			shaderMaterial.NextPass = portalMaterial;
		}

		if (lightmapStage >= 0)
		{
			//If Lightmap is needed but there is no lightmapIndex, then the material is broken
			if (lm_index == -1)
			{
				GameManager.Print("Requested Lightmap, but there is no lightmapIndex", GameManager.PrintType.Warning);
				return MaterialManager.Instance.illegal;
			}
		}

		return shaderMaterial;
	}

	public static void GetSunData(QShaderData qShader)
	{
		if (qShader.qShaderGlobal.sunParams == null)
			return;

		string[] SunData = qShader.qShaderGlobal.sunParams;
		float degrees = TryToParseFloat(SunData[4]);
		float elevation = TryToParseFloat(SunData[5]);
		GameManager.Instance.Sun.RotationDegrees = new Vector3(-elevation, degrees - 90, 0); 

	}
	public static ShaderMaterial PortalMaterial(QShaderData qShader)
	{
		string code = "shader_type spatial;\nrender_mode diffuse_lambert, specular_schlick_ggx, depth_draw_always, blend_mix, cull_back;\n\n";
		code += "uniform sampler2D Tex_0 : repeat_enable;\n";
		code += "uniform float Transparency : hint_range(0, 1) = 0.0;\n";
		code += "global uniform float MsTime;\n";
		code += "global uniform vec4 AmbientColor: source_color;\n";
		code += "global uniform float mixBrightness;\n";
		code += "instance uniform float OffSetTime = 0.0;\n";
		code += "const bool ViewModel = false;\n";
		code += "void vertex()\n{\n";
		code += GetVertex(qShader, false, false, qShader.qShaderGlobal.polygonOffset) + "}\n";
		code += "void fragment()\n{\n\tvec2 uv_0 = SCREEN_UV;\n\tvec4 Stage_0 = texture(Tex_0, uv_0);\n";
		code += "\tvec4 ambient = AmbientColor * mixBrightness;\n";
		code += "\tvec4 vertx_color = COLOR;\n";
		code += "\tvec4 color = vec4(0.0, 0.0, 0.0, 0.0);\n";
		code += "\tcolor = Stage_0;\n";
		code += "\tALBEDO = (color.rgb * vertx_color.rgb);\n";
		code += "\tEMISSION = mix((ambient.rgb * color.rgb), color.rgb, mixBrightness);\n";
		code += "\tALPHA = Transparency;\n";
		code += "}\n\n";
		Shader shader = new Shader();
		shader.Code = code;
		ShaderMaterial shaderMaterial = new ShaderMaterial();
		shaderMaterial.Shader = shader;
		shaderMaterial.RenderPriority = 1;
		return shaderMaterial;
	}
	public static ShaderMaterial MirrorShader(string shaderName)
	{
		QShaderData qShader;
		if (!QShaders.TryGetValue(shaderName, out qShader))
			qShader = null;

		string code = "shader_type spatial;\nrender_mode diffuse_lambert, specular_schlick_ggx, depth_draw_opaque, blend_mix, cull_back;\n\n";
		code += "uniform sampler2D Tex_0 : repeat_enable;\n";
		code += "uniform float InvertX : hint_range(0, 1) = 1.0;\n";
		code += "uniform float InvertY : hint_range(0, 1) = 1.0;\n";
		code += "global uniform float MsTime;\n";
		code += "global uniform vec4 AmbientColor: source_color;\n";
		code += "global uniform float mixBrightness;\n";
		code += "instance uniform float OffSetTime = 0.0;\n";
		code += "const bool ViewModel = false;\n";
		code += "void vertex()\n{\n";
		code += "\tUV2.x = mix(UV2.x, 1.0 - UV2.x, InvertX);\n";
		code += "\tUV2.y = mix(UV2.y, 1.0 - UV2.y, InvertY);\n";
		if (qShader != null)
			code += GetVertex(qShader, false, false, qShader.qShaderGlobal.polygonOffset);
		code += "}\nvoid fragment()\n{\n\tvec2 uv_0 = UV2;\n\tvec4 Stage_0 = texture(Tex_0, uv_0);\n";
		code += "\tvec4 ambient = AmbientColor * mixBrightness;\n";
		code += "\tvec4 vertx_color = COLOR;\n";
		code += "\tvec4 color = vec4(0.0, 0.0, 0.0, 0.0);\n";
		code += "\tcolor = Stage_0;\n";
		code += "\tALBEDO = (color.rgb * vertx_color.rgb);\n";
		code += "\tEMISSION = mix((ambient.rgb * color.rgb), color.rgb, mixBrightness);\n";
		code += "}\n\n";
		Shader shader = new Shader();
		shader.Code = code;
		ShaderMaterial shaderMaterial = new ShaderMaterial();
		shaderMaterial.Shader = shader;
		return shaderMaterial;
	}
	public static string GetDiffuseLightning()
	{
		string DiffuseLight;

		DiffuseLight = "\tfloat isLightDir = float(LIGHT_IS_DIRECTIONAL);\n";
		DiffuseLight += "\tfloat useLightVol = float(UseLightVol);\n";
		DiffuseLight += "\tfloat mul = mix(1.0, 0.4, useLightVol);\n";
		DiffuseLight += "\tDIFFUSE_LIGHT += ShadowIntensity * mul * vec3(ATTENUATION - 1.0) * isLightDir;\n";
		DiffuseLight += "\tDIFFUSE_LIGHT += clamp(dot(NORMAL, dirVector), 0.0, 1.0) * dirColor * isLightDir * useLightVol;\n";
		DiffuseLight += "\tDIFFUSE_LIGHT += clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION * LIGHT_COLOR * (1.0 - isLightDir);\n";

		return DiffuseLight;
	}

	public static string GetVertex(QShaderData qShader, bool useView, bool forceView, bool polygonOffSet, float Value = 0.001f)
	{
		string Vertex = "";

		//This is important as we added Billboard outside the shaders
		if (MaterialManager.HasBillBoard.Contains(qShader.Name))
		{
			switch (qShader.qShaderGlobal.billboard)
			{
				default:
					Vertex = "\tVERTEX = (vec4(VERTEX, 1.0) * MODELVIEW_MATRIX).xyz;\n";
				break;
				case QShaderGlobal.SpriteType.FixedY:
					Vertex = "\tivec2 alignment = ivec2(1,0);\n";
					Vertex += "\tvec3 local_up = MODEL_MATRIX[alignment.x].xyz;\n";
					Vertex += "\tvec4 ax = vec4(normalize(cross(local_up, INV_VIEW_MATRIX[2].xyz)), 0.0);\n";
					Vertex += "\tvec4 ay = vec4(local_up.xyz, 0.0);\n";
					Vertex += "\tvec4 az = vec4(normalize(cross(INV_VIEW_MATRIX[alignment.y].xyz, local_up)), 0.0);\n";
					Vertex += "\tMODELVIEW_MATRIX = VIEW_MATRIX * mat4(ax, ay, az, MODEL_MATRIX[3]);\n";
					Vertex += "\tMODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);\n";
				break;
			}
		}

		if (qShader.qShaderGlobal.deformVertexes == null)
		{
			if (useView)
			{
				Vertex += "\tfloat InvTanFOV = 1.0f / tan(0.5f * (ViewCameraFOV * PI / 180.0f));\n";
				Vertex += "\tfloat Aspect = VIEWPORT_SIZE.x / VIEWPORT_SIZE.y;\n";
				Vertex += "\tPROJECTION_MATRIX[1][1] = mix(PROJECTION_MATRIX[1][1], -InvTanFOV, float(ViewModel));\n";
				Vertex += "\tPROJECTION_MATRIX[0][0] = mix(PROJECTION_MATRIX[0][0], InvTanFOV / Aspect, float(ViewModel));\n";
			}
			else if (forceView)
			{
				Vertex += "\tfloat InvTanFOV = 1.0f / tan(0.5f * (ViewCameraFOV * PI / 180.0f));\n";
				Vertex += "\tfloat Aspect = VIEWPORT_SIZE.x / VIEWPORT_SIZE.y;\n";
				Vertex += "\tPROJECTION_MATRIX[1][1] = -InvTanFOV;\n";
				Vertex += "\tPROJECTION_MATRIX[0][0] =  InvTanFOV / Aspect;\n";
			}
			Vertex += "\tPOSITION = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);\n";
			if (polygonOffSet)
				Vertex += "\tPOSITION.z = mix(POSITION.z, 1.0, "+ Value.ToString("0.000") + ");\n";
			return Vertex;
		}

		Vertex += "\tfloat Time = (MsTime - OffSetTime);\n";
		string Vars = "";
		string Verts = "";
		for (int i = 0; i < qShader.qShaderGlobal.deformVertexes.Count; i++)
		{
			string[] VertexFunc = qShader.qShaderGlobal.deformVertexes[i];
			if (VertexFunc[0] == "WAVE")
			{
				float div = TryToParseFloat(VertexFunc[1]) * GameManager.sizeDividor;
				string DeformFunc = VertexFunc[2];
				float offset = TryToParseFloat(VertexFunc[3]);
				float amp = TryToParseFloat(VertexFunc[4]);
				float phase = TryToParseFloat(VertexFunc[5]);
				float freq = TryToParseFloat(VertexFunc[6]);

				if (div > 0)
					div = 1.0f / div;
				else
					div = 100 * GameManager.sizeDividor;

				Vars += "\tfloat OffSet_" + i + " = (VERTEX.x + VERTEX.y + VERTEX.z) * " + div.ToString("0.00") + ";\n";
				if (DeformFunc == "SIN")
				{
					Verts += "\tVERTEX += NORMAL * " + GameManager.sizeDividor.ToString("0.00") + " * (";
					Verts += offset.ToString("0.00") + " + sin(6.28 * " + freq.ToString("0.00") + " * (Time +" + phase.ToString("0.00") + " +  OffSet_" + i + "))  * " + amp.ToString("0.00") + "); \n";
				}
				else if (DeformFunc == "SQUARE")
				{
					Verts += "\tVERTEX += NORMAL * " + GameManager.sizeDividor.ToString("0.00") + " * (";
					Verts += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * round(fract(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " +  OffSet_" + i + "))); \n";
				}
				else if (DeformFunc == "TRIANGLE")
				{

					Verts += "\tVERTEX += NORMAL * " + GameManager.sizeDividor.ToString("0.00") + " * (";
					Verts += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (abs(2.0 * (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " +  OffSet_" + i + " - floor(0.5 + Time * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " +  OffSet_" + i + "))))); \n";
				}
				else if (DeformFunc == "SAWTOOTH")
				{
					Verts += "\tVERTEX += NORMAL * " + GameManager.sizeDividor.ToString("0.00") + " * (";
					Verts += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " +  OffSet_" + i + " - floor(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " +  OffSet_" + i + "))); \n";
				}
				else if (DeformFunc == "INVERSESAWTOOTH")
				{
					Verts += "\tVERTEX += NORMAL * " + GameManager.sizeDividor.ToString("0.00") + " * (";
					Verts += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (1.0 - (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " +  OffSet_" + i + " - floor(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " +  OffSet_" + i + ")))); \n";
				}
			}
			else if (VertexFunc[0] == "MOVE")
			{
				Vector3 move = new Vector3(-1.0f * TryToParseFloat(VertexFunc[1]), TryToParseFloat(VertexFunc[3]), TryToParseFloat(VertexFunc[2])) * GameManager.sizeDividor;
				string DeformFunc = VertexFunc[4];
				float offset = TryToParseFloat(VertexFunc[5]);
				float amp = TryToParseFloat(VertexFunc[6]);
				float phase = TryToParseFloat(VertexFunc[7]);
				float freq = TryToParseFloat(VertexFunc[8]);

				Vars += "\tvec3 OffSet_" + i + " = vec3(" + move.X.ToString("0.00") + ", " + move.Y.ToString("0.00") + ", " + move.Z.ToString("0.00") + ");\n";
				if (DeformFunc == "SIN")
				{
					Verts += "\tVERTEX += OffSet_" + i + " * (";
					Verts += offset.ToString("0.00") + " + sin(6.28 * " + freq.ToString("0.00") + " * (Time +" + phase.ToString("0.00") + "))  * " + amp.ToString("0.00") + "); \n";
				}
				else if (DeformFunc == "SQUARE")
				{
					Verts += "\tVERTEX += OffSet_" + i + " * (";
					Verts += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * round(fract(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))); \n";
				}
				else if (DeformFunc == "TRIANGLE")
				{
					Verts += "\tVERTEX += OffSet_" + i + " * (";
					Verts += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (abs(2.0 * (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(0.5 + Time * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))))); \n";
				}
				else if (DeformFunc == "SAWTOOTH")
				{

					Verts += "\tVERTEX += OffSet_" + i + " * (";
					Verts += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))); \n";
				}
				else if (DeformFunc == "INVERSESAWTOOTH")
				{
					Verts += "\tVERTEX += OffSet_" + i + " * (";
					Verts += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (1.0 - (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + ")))); \n";
				}
			}
		}
		Vertex += Vars;
		Vertex += Verts;
		if (useView)
		{
			Vertex += "\tfloat InvTanFOV = 1.0f / tan(0.5f * (ViewCameraFOV * PI / 180.0f));\n";
			Vertex += "\tfloat Aspect = VIEWPORT_SIZE.x / VIEWPORT_SIZE.y;\n";
			Vertex += "\tPROJECTION_MATRIX[1][1] = mix(PROJECTION_MATRIX[1][1], -InvTanFOV, float(ViewModel));\n";
			Vertex += "\tPROJECTION_MATRIX[0][0] = mix(PROJECTION_MATRIX[0][0], InvTanFOV / Aspect, float(ViewModel));\n";
		}
		else if (forceView)
		{
			Vertex += "\tfloat InvTanFOV = 1.0f / tan(0.5f * (ViewCameraFOV * PI / 180.0f));\n";
			Vertex += "\tfloat Aspect = VIEWPORT_SIZE.x / VIEWPORT_SIZE.y;\n";
			Vertex += "\tPROJECTION_MATRIX[1][1] = -InvTanFOV;\n";
			Vertex += "\tPROJECTION_MATRIX[0][0] =  InvTanFOV / Aspect;\n";
		}
		Vertex += "\tPOSITION = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);\n";
		if (polygonOffSet)
			Vertex += "\tPOSITION.z = mix(POSITION.z, 1.0, "+ Value.ToString("0.000") + ");\n";
		return Vertex;
	}
	public static string GetUVGen(QShaderData qShader, int currentStage, ref string GSVaryings)
	{
		string UVGen = "";
		if (qShader.qShaderStages[currentStage].environment)
		{
			GSVaryings += "varying vec2 UV_" + currentStage + ";\n";
			UVGen = "\tvec3 viewer_" + currentStage + " = normalize((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz - CAMERA_POSITION_WORLD);\n";
			UVGen += "\tvec3 normal_" + currentStage + " = normalize(MODEL_NORMAL_MATRIX * NORMAL);\n";
			UVGen += "\tvec3 reflect_" + currentStage + " = reflect(viewer_" + currentStage + ", normal_" + currentStage + ");\n";
			UVGen += "\tUV_" + currentStage + " = reflect_" + currentStage + ".yz;\n";
		}
		return UVGen;
	}
		
	public static string GetTcGen(QShaderData qShader, int currentStage, ref int lightmapStage)
	{
		string TcGen = "";

		if (qShader.qShaderGlobal.skyParms != null)
		{
			if (qShader.qShaderGlobal.skyParms[0] == "-")
			{
//				int cloudheight = int.Parse(qShader.qShaderGlobal.skyParms[1]) / 5;
				TcGen += "\tvec4 vot" + currentStage + " = INV_VIEW_MATRIX * vec4(VERTEX, 0.0);\n";
				TcGen += "\tvot" + currentStage + ".y = 5.0 * (vot" + currentStage + ".y);\n";
				TcGen += "\tvot" + currentStage + " = normalize(vot" + currentStage + ");\n";
				TcGen += "\tvec2 uv_" + currentStage + " = vec2(vot" + currentStage + ".x, vot" + currentStage + ".z);\n";
			}
			else
			{
				if (currentStage == 0)
				{
					TcGen += "\tvec4 vot" + currentStage + " = INV_VIEW_MATRIX * vec4(VERTEX, 0.0);\n";
					TcGen += "\tvec4 sky_vol" + currentStage + " = vot" + currentStage + ";\n";
					TcGen += "\tsky_vol" + currentStage + ".y = 5.0 * (vot" + currentStage + ".y);\n";
					TcGen += "\tsky_vol" + currentStage + " = normalize(sky_vol" + currentStage + ");\n";
					TcGen += "\tvot" + currentStage + " = normalize(vot" + currentStage + ");\n";
					TcGen += "\tvec2 uv_" + currentStage + " = vec2(sky_vol" + currentStage + ".x, sky_vol" + currentStage + ".z);\n";
				}
				else
					TcGen += "\tvec2 uv_" + currentStage + " = uv_0;\n";
			}
		}
		else
		{
			if (qShader.qShaderStages[currentStage].environment)
			{
				TcGen += "\tvec2 uv_" + currentStage + ";\n";
				TcGen += "\tuv_" + currentStage + ".x = 0.5 + UV_" + currentStage + ".y * 0.5;\n";
				TcGen += "\tuv_" + currentStage + ".y = 0.5 - UV_" + currentStage + ".x * 0.5;\n";
			}
			else if (qShader.qShaderStages[currentStage].map != null)
			{
				if (qShader.qShaderStages[currentStage].isLightmap)
				{
					lightmapStage = currentStage;
					TcGen = "\tvec2 uv_" + currentStage + " = UV2;\n";
				}
				else
					TcGen = "\tvec2 uv_" + currentStage + " = UV;\n";
			}
			else
				TcGen = "\tvec2 uv_" + currentStage + " = UV;\n";
		}

		return TcGen;
	}

	public static string GetTcMod(QShaderData qShader, int currentStage, ref bool helperRotate)
	{
		string TcMod = "";

		if (qShader.qShaderStages[currentStage].tcMod == null)
			return TcMod;

		for (int i = 0; i < qShader.qShaderStages[currentStage].tcMod.Count; i++)
		{
			QShaderStage.QShaderTCMod shaderTCMod = qShader.qShaderStages[currentStage].tcMod[i];

			switch(shaderTCMod.type)
			{
				case QShaderStage.TCModType.Rotate:
				{
					if (helperRotate == false)
						helperRotate = true;
					float deg = TryToParseFloat(shaderTCMod.value[0]);
					TcMod += "\tuv_" + currentStage + " = rotate(uv_" + currentStage + ", vec2(0.5), radians(" + deg.ToString("0.00") + ") * Time*0.5);\n";
				}
				break;
				case QShaderStage.TCModType.Scale:
				{
					float SScale = TryToParseFloat(shaderTCMod.value[0]);
					float TScale = TryToParseFloat(shaderTCMod.value[1]);
					TcMod += "\tuv_" + currentStage + " *= vec2(" + SScale.ToString("0.00") + "," + TScale.ToString("0.00") + "); \n";
				}
				break;
				case QShaderStage.TCModType.Scroll:
				{
					float SSpeed = TryToParseFloat(shaderTCMod.value[0]);
					float TSpeed = TryToParseFloat(shaderTCMod.value[1]);
					TcMod += "\tuv_" + currentStage + " += vec2(" + SSpeed.ToString("0.00") + "," + TSpeed.ToString("0.00") + ") * Time*0.5; \n";
				}
				break;
				case QShaderStage.TCModType.Stretch:
				{
					string func = shaderTCMod.value[0];
					float basis = TryToParseFloat(shaderTCMod.value[1]);
					float amp = TryToParseFloat(shaderTCMod.value[2]);
					float phase = TryToParseFloat(shaderTCMod.value[3]);
					float freq = TryToParseFloat(shaderTCMod.value[4]);
					TcMod += "\tfloat str_" + currentStage + " = 1.0 / (" + basis.ToString("0.00") + " + " + amp.ToString("0.00") + " * (sin((Time)*" + freq.ToString("0.00") + "*6.28)+" + phase.ToString("0.00") + "));\n";
					TcMod += "\tuv_" + currentStage + "  = uv_" + currentStage + " *(str_" + currentStage + ") - vec2(1.0,1.0)*str_" + currentStage + "*0.5 + vec2(0.5,0.5);\n";
				}
				break;
				case QShaderStage.TCModType.Transform:
				{
					float M00 = TryToParseFloat(shaderTCMod.value[0]);
					float M01 = TryToParseFloat(shaderTCMod.value[1]);
					float M10 = TryToParseFloat(shaderTCMod.value[2]);
					float M11 = TryToParseFloat(shaderTCMod.value[3]);
					float T0 = TryToParseFloat(shaderTCMod.value[4]);
					float T1 = TryToParseFloat(shaderTCMod.value[5]);
					if (qShader.qShaderGlobal.skyParms != null)
					{
						T0 *= 5;
						T1 *= 5;
					}
					string transX = "(uv_" + currentStage + ".x * " + M00.ToString("0.00") + " + uv_" + currentStage + ".y * " + M10.ToString("0.00") + " + " + T0.ToString("0.00") + ")";
					string transY = "(uv_" + currentStage + ".x * " + M01.ToString("0.00") + " + uv_" + currentStage + ".y * " + M11.ToString("0.00") + " + " + T1.ToString("0.00") + ")";
					TcMod += "\tuv_" + currentStage + " = vec2(" + transX + "," + transY + "); \n";
				}
				break;
				case QShaderStage.TCModType.Turb:
				{
//					float basis = TryToParseFloat(shaderTCMod.value[0]);
					float amp = TryToParseFloat(shaderTCMod.value[1]);
					float phase = TryToParseFloat(shaderTCMod.value[2]);
					float freq = TryToParseFloat(shaderTCMod.value[3]);
					string turbX = "(sin( (2.0 *" + freq.ToString("0.00") + ") * (Time * 6.28) + " + phase.ToString("0.00") + ") * " + amp.ToString("0.00") + " )";
					string turbY = "(cos( (2.0 *" + freq.ToString("0.00") + ") * (Time * 6.28) + " + phase.ToString("0.00") + ") * " + amp.ToString("0.00") + " )";
					TcMod += "\tuv_" + currentStage + " += vec2(" + turbX + "," + turbY + "); \n";
				}
				break;
			}
		}
		return TcMod;
	}

	public static string GetGenFunc(QShaderData qShader, int currentStage, GenFuncType type, ref bool entityColor)
	{
		string GenType = ".rgb";
		string[] GenFunc = qShader.qShaderStages[currentStage].rgbGen;
		
		if (type == GenFuncType.Alpha)
		{
			GenType = ".a";
			GenFunc = qShader.qShaderStages[currentStage].alphaGen;
		}

		string StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType +" * 1.0 ; \n";
		if (GenFunc == null)
			return StageGen;

		if (GenFunc[0] == "WAVE")
		{
			string RGBFunc = GenFunc[1];
			float offset = TryToParseFloat(GenFunc[2]);
			float amp = TryToParseFloat(GenFunc[3]);
			float phase = TryToParseFloat(GenFunc[4]);
			float freq = TryToParseFloat(GenFunc[5]);
			if (RGBFunc == "SIN")
			{
				StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
				StageGen += offset.ToString("0.00") + " + sin(6.28 * " + freq.ToString("0.00") + " * (Time +" + phase.ToString("0.00") + "))  * " + amp.ToString("0.00") + "); \n";
			}
			else if (RGBFunc == "SQUARE")
			{
				StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
				StageGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * round(fract(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))); \n";
			}
			else if (RGBFunc == "TRIANGLE")
			{
				StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
				StageGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (abs(2.0 * (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(0.5 + Time * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))))); \n";
			}
			else if (RGBFunc == "SAWTOOTH")
			{
				StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
				StageGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))); \n";
			}
			else if (RGBFunc == "INVERSESAWTOOTH")
			{
				StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
				StageGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (1.0 - (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + ")))); \n";
			}
		}
		else if (GenFunc.Length > 0)
		{
			string Func = GenFunc[0];
			if (Func == "VERTEX")
			{
				if (type == GenFuncType.RGB)
					StageGen = "\tStage_" + currentStage + ".rgb = Stage_" + currentStage + ".rgb * vertx_color.rgb; \n";
				else
					StageGen = "\tStage_" + currentStage + ".a = vertx_color.a; \n";
			}
			else if (Func == "ENTITY")
			{
				StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * modulate" + GenType + " ; \n";
				entityColor = true;
			}
			if (Func == "CONST")
			{
				if (type == GenFuncType.RGB)
				{
					float r = TryToParseFloat(GenFunc[2]);
					float g = TryToParseFloat(GenFunc[3]);
					float b = TryToParseFloat(GenFunc[4]);
					StageGen = "\tvec3 ColorGen_" + currentStage + " = vec3(" + r.ToString("0.00") + ", " + g.ToString("0.00") + ", " + b.ToString("0.00") + ");\n";
					StageGen += "\tStage_" + currentStage + ".rgb = Stage_" + currentStage + ".rgb * ColorGen_" + currentStage + ";\n";
				}
				else
				{
					float a = TryToParseFloat(GenFunc[1]);
					StageGen = "\tStage_" + currentStage + ".a = " + a.ToString("0.00") + "; \n";
				}
			}
		}
		return StageGen;
	}

	public static string GetAlphaFunc(QShaderData qShader, int currentStage, List<int> multiPassList, ref int needMultiPass)
	{
		string AlphaFunc = "";
		if (qShader.qShaderStages[currentStage].alphaFunc == QShaderStage.AlphaFuncType.NONE)
			return AlphaFunc;

		int fistStage = 0;
		if (multiPassList != null)
			fistStage = multiPassList[0];
		if (currentStage != fistStage)
		{
			needMultiPass = currentStage;
			return AlphaFunc;
		}


		AlphaFunc = "\tif (Stage_" + currentStage + ".a ";		
		switch (qShader.qShaderStages[currentStage].alphaFunc)
		{
			case QShaderStage.AlphaFuncType.GT0:
				AlphaFunc += "== 0.0)\n";
			break;
			case QShaderStage.AlphaFuncType.LT128:
				AlphaFunc += ">= 0.5)\n";
			break;
			case QShaderStage.AlphaFuncType.GE128:
				AlphaFunc += "< 0.5)\n";
			break;
		}
		AlphaFunc += "\t{\n\t\tdiscard;\n\t}\n";

		return AlphaFunc;
	}
	public static string GetBlend(QShaderData qShader, int currentStage, int numStage, List<int> multiPassList, ref int needMultiPass, ref bool alphaIsTransparent)
	{
		string Blend = "";
		if (qShader.qShaderStages[currentStage].blendFunc != null)
		{
			int firstStage = 0;
			if (multiPassList != null)
				firstStage = multiPassList[0];

			string BlendWhat = qShader.qShaderStages[currentStage].blendFunc[0];
			if (BlendWhat == "ADD")
			{
				Blend = "\tcolor = Stage_" + currentStage + " + color; \n";
				if (currentStage == firstStage)
					qShader.qShaderGlobal.sort = QShaderGlobal.SortType.Additive;
			}
			else if (BlendWhat == "FILTER")
			{

				Blend = "\tcolor = Stage_" + currentStage + " * color; \n";
				if (currentStage == firstStage)
				{
					Blend = "\tcolor = Stage_" + currentStage + "; \n";
					qShader.qShaderGlobal.sort = QShaderGlobal.SortType.Additive;
				}
			}
			else if (BlendWhat == "BLEND")
			{
				Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * Stage_" + currentStage + ".a + color.rgb * (1.0 - Stage_" + currentStage + ".a); \n";
				Blend += "\tcolor.a = Stage_" + currentStage + ".a *   Stage_" + currentStage + ".a + color.a *  (1.0 -  Stage_" + currentStage + ".a); \n";
				if (currentStage == firstStage)
				{
					if (firstStage == 0)
					{
						Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * Stage_" + currentStage + ".a + color.rgb * (1.0 - Stage_" + currentStage + ".a); \n";
						Blend += "\tcolor.a = Stage_" + currentStage + ".a *   Stage_" + currentStage + ".a;\n";
					}
					else
						Blend = "\tcolor = Stage_" + currentStage + ";\n";
					alphaIsTransparent = true;
				}
			}
			else
			{
				string src = qShader.qShaderStages[currentStage].blendFunc[0];
				string dst = qShader.qShaderStages[currentStage].blendFunc[1];
				string asrc = "";
				string adst = "";
				string csrc = "";
				string cdst = "";

				//SOURCE
				if (src == "GL_ONE")
				{
					csrc = " 1.0 ";
					asrc = " 1.0 ";
				}
				else if (src == "GL_ZERO")
				{
					csrc = " 0.0 ";
					asrc = " 0.0 ";
				}
				else if (src == "GL_DST_COLOR")
				{
					csrc = " color.rgb ";
					asrc = " color.a ";
				}
				else if (src == "GL_ONE_MINUS_DST_COLOR")
				{
					csrc = " 1.0 - color.rgb ";
					asrc = " 1.0 - color.a ";
				}
				else if (src == "GL_SRC_ALPHA")
				{
					csrc = "  Stage_" + currentStage + ".a ";
					asrc = "  Stage_" + currentStage + ".a ";
				}
				else if (src == "GL_ONE_MINUS_SRC_ALPHA")
				{
					csrc = " (1.0 -  Stage_" + currentStage + ".a) ";
					asrc = " (1.0 -  Stage_" + currentStage + ".a) ";
				}
				else if (src == "GL_DST_ALPHA")
				{
					csrc = " color.a ";
					asrc = " color.a ";
				}
				else if (src == "GL_ONE_MINUS_DST_ALPHA")
				{
					csrc = " (1.0 - color.a) ";
					asrc = " (1.0 - color.a) ";
				}

				//DEST
				if (dst == "GL_ONE")
				{
					cdst = " 1.0 ";
					adst = " 1.0 ";
					if (currentStage == firstStage)
						qShader.qShaderGlobal.sort = QShaderGlobal.SortType.Additive;
				}
				else if (dst == "GL_ZERO")
				{
					cdst = " 0.0 ";
					adst = " 0.0 ";
				}
				else if (dst == "GL_SRC_COLOR")
				{
					cdst = " Stage_" + currentStage + ".rgb ";
					adst = " Stage_" + currentStage + ".a ";
				}
				else if (dst == "GL_ONE_MINUS_SRC_COLOR")
				{
					cdst = " (1.0 - Stage_" + currentStage + ".rgb) ";
					adst = " (1.0 - Stage_" + currentStage + ".a) ";
				}
				else if (dst == "GL_DST_ALPHA")
				{
					cdst = " color.a ";
					adst = " color.a ";
				}
				else if (dst == "GL_ONE_MINUS_DST_ALPHA")
				{
					cdst = " (1.0 - color.a) ";
					adst = " (1.0 - color.a) ";
				}
				else if (dst == "GL_SRC_ALPHA")
				{
					cdst = "  Stage_" + currentStage + ".a ";
					adst = "  Stage_" + currentStage + ".a ";
				}
				else if (dst == "GL_ONE_MINUS_SRC_ALPHA")
				{
					cdst = " (1.0 -  Stage_" + currentStage + ".a) ";
					adst = " (1.0 -  Stage_" + currentStage + ".a) ";
				}

				//Check FirstStage
				if (currentStage == firstStage)
				{
					if (src == "GL_ZERO")
					{
						if (dst == "GL_SRC_COLOR")
						{
							Blend = "\tcolor = Stage_" + currentStage + "; \n";
							qShader.qShaderGlobal.sort = QShaderGlobal.SortType.Additive;
						}
						else if (dst == "GL_ONE_MINUS_SRC_COLOR")
						{
							Blend = "\tcolor.rgb = " + cdst + ";\n";
							qShader.qShaderGlobal.sort = QShaderGlobal.SortType.Multiplicative;
						}
						else
						{
							Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * " + csrc + " + color.rgb * " + cdst + ";\n";
							Blend += "\tcolor.a = Stage_" + currentStage + ".a * " + asrc + " + color.a * " + adst + ";\n";
						}
					}
					else if (src == "GL_DST_COLOR")
					{
						{
							if (dst == "GL_ZERO")
							{
								Blend = "\tcolor = Stage_" + currentStage + "; \n";
								qShader.qShaderGlobal.sort = QShaderGlobal.SortType.Additive;
							}
							else
							{
								Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * " + csrc + " + color.rgb * " + cdst + ";\n";
								Blend += "\tcolor.a = Stage_" + currentStage + ".a * " + asrc + " + color.a * " + adst + ";\n";
							}
						}
					}
					else if (src == "GL_SRC_ALPHA")
					{
						if (dst == "GL_ONE_MINUS_SRC_ALPHA")
						{
							if (firstStage == 0)
							{
								Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * Stage_" + currentStage + ".a + color.rgb * (1.0 - Stage_" + currentStage + ".a); \n";
								Blend += "\tcolor.a = Stage_" + currentStage + ".a *   Stage_" + currentStage + ".a;\n";
							}
							else
								Blend = "\tcolor = Stage_" + currentStage + ";\n";
							alphaIsTransparent = true;
						}
						else
						{
							Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * " + csrc + " + color.rgb * " + cdst + ";\n";
							Blend += "\tcolor.a = Stage_" + currentStage + ".a * " + asrc + " + color.a * " + adst + ";\n";
						}
					}
					else
					{
						Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * " + csrc + " + color.rgb * " + cdst + ";\n";
						Blend += "\tcolor.a = Stage_" + currentStage + ".a * " + asrc + " + color.a * " + adst + ";\n";
					}
				}
				else
				{
					Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * " + csrc + " + color.rgb * " + cdst + ";\n";
					Blend += "\tcolor.a = Stage_" + currentStage + ".a * " + asrc + " + color.a * " + adst + ";\n";
				}
			}
		}
		else
		{
			if (numStage != 0)
				needMultiPass = currentStage;
			Blend = "\tcolor = Stage_" + currentStage + ";\n";
		}

		Blend += "\tcolor = clamp(color,black,white);\n";
		return Blend;
	}

	public static void ReadShaderData(byte[] shaderData)
	{
		MemoryStream ms = new MemoryStream(shaderData);
		ReadShaderData(ms);
		ms.Close();
	}

	public static void ReadShaderData(Stream ms)
	{
		StreamReader stream = new StreamReader(ms);
		string strWord;
		char[] line = new char[1024];

		int p;
		int stage = 0;
		int node = 0;

		QShaderData qShaderData = null;
		while (!stream.EndOfStream)
		{
			strWord = stream.ReadLine();
			if (string.IsNullOrEmpty(strWord))
				continue;

			p = 0;
			for (int i = 0; i < strWord.Length; i++)
			{
				char c = strWord[i];
				switch (c)
				{
					default:
						line[p++] = c;
					break;
					case '{':
					{
						p = 0;
						node++;
						if (node == 2)
							stage++;
					}
					break;
					case '}':
					{
						p = 0;
						node--;
						if (node == 0)
						{
							if (qShaderData.qShaderGlobal.isFog)
							{
								if (FogShaders.ContainsKey(qShaderData.Name))
								{
//									GameManager.Print("Fog Shader already on the list: " + qShaderData.Name, GameManager.PrintType.Info);
									FogShaders[qShaderData.Name] = qShaderData;
								}
								else
									FogShaders.Add(qShaderData.Name, qShaderData);
								MaterialManager.AddFog(qShaderData.Name);
							}
							else
							{
								if (qShaderData.qShaderGlobal.skyParms != null)
								{
									if (qShaderData.qShaderGlobal.skyParms[0] != "-")
									{
										GameManager.Print("ADDING SKY: "+ qShaderData.qShaderGlobal.skyParms[0] + " TO SHADER: " + qShaderData.Name);
										qShaderData.AddFirstStage("SKYMAP", qShaderData.qShaderGlobal.skyParms[0]);										}
									}
								if (QShaders.ContainsKey(qShaderData.Name))
								{
//									GameManager.Print("Shader already on the list: " + qShaderData.Name, GameManager.PrintType.Info);
									QShaders[qShaderData.Name] = qShaderData;
								}
								else
									QShaders.Add(qShaderData.Name, qShaderData);
								if (qShaderData.qShaderGlobal.billboard != QShaderGlobal.SpriteType.Disabled)
									MaterialManager.AddBillBoard(qShaderData.Name);
								if (qShaderData.qShaderGlobal.portal)
									MaterialManager.AddPortalMaterial(qShaderData.Name);
							}
							qShaderData = null;
							stage = 0;
						}
						i = strWord.Length;
					}
					break;
					case '\t':
						if (p > 0)
							line[p++] = ' ';
					break;
					case ' ':
						if (p > 0)
							line[p++] = c;
					break;
					case '/':
					{
						int next = i + 1;
						if (next < strWord.Length)
						{
							if (strWord[next] == '/')
								i = strWord.Length;
							else
								line[p++] = c;
						}
						else
							line[p++] = c;
					}
					break;
				}
			}

			if (p == 0)
				continue;

			line[p] = '\0';
			strWord = new string(line, 0 , p);
			
			if (strWord.Length == 0)
				continue;

			strWord = strWord.ToUpper();

			if (qShaderData == null)
			{
				if (node == 0)
				{
					qShaderData = new QShaderData();
					qShaderData.Name = strWord.Trim(' ');
				}
				continue;
			}

			string[] keyValue = strWord.Split(' ' , 2);

			if (keyValue.Length < 2)
				keyValue = new string[2] { keyValue[0], "" };

			if (keyValue.Length == 2)
			{
				//Sanitizing extra spaces 
				p = 0;
				for (int i = 0; i < keyValue[1].Length; i++)
				{
					char c = keyValue[1][i];
					switch (c)
					{
						default:
							line[p++] = c;
						break;
						case ' ':
							if (p > 0)
							{
								if ((line[p - 1]) != ' ')
									line[p++] = c;
							}
						break;
					}
				}
				if ((p > 0) && ((line[p - 1]) == ' '))
					p--;

				line[p] = '\0';
				keyValue[1] = new string(line, 0, p);

				if (keyValue[0].Length > 0)
				{
					if (node == 1)
						qShaderData.AddGlobal(keyValue[0], keyValue[1]);
					if (node == 2)
						qShaderData.AddStage(stage, keyValue[0], keyValue[1]);
				}
			}
		}
		stream.Close();
	}

	public static float TryToParseFloat(string Number)
	{
		int inum;
		float fNum;

		if (int.TryParse(Number, out inum))
			fNum = inum;
		else
			fNum = float.Parse(Number);
		return fNum;
	}
}

public class QShaderData
{
	public string Name;
	public QShaderGlobal qShaderGlobal = new QShaderGlobal();
	public List<QShaderStage> qShaderStages = new List<QShaderStage>();

	public void AddGlobal(string Params, string Value)
	{
		qShaderGlobal.AddGlobal(Params, Value);
	}

	public void AddFirstStage(string Params, string Value)
	{
		QShaderStage qShaderStage;
		qShaderStage = new QShaderStage();
		qShaderStage.AddStageParams(Params, Value);
		if (qShaderStages.Count == 0)
			qShaderStages.Add(qShaderStage);
		else
		{
			List<QShaderStage> shaderStages = new List<QShaderStage>()
			{
				qShaderStage
			};
			shaderStages.AddRange(qShaderStages);
			qShaderStages = shaderStages;
		}
	}

	public void AddStage(int currentStage, string Params, string Value)
	{
		QShaderStage qShaderStage;
		if (qShaderStages.Count < currentStage)
		{
			qShaderStage = new QShaderStage();
			qShaderStage.AddStageParams(Params, Value);
			qShaderStages.Add(qShaderStage);
		}
		else
		{
			qShaderStage = qShaderStages[currentStage-1];
			qShaderStage.AddStageParams(Params, Value);
		}
	}
}

public class QShaderGlobal
{
	public List<string> surfaceParms = null;
	public List<string> skyParms = null;
	public List<string[]> deformVertexes = null;
	public string[] fogParms = null;
	public SortType sort = SortType.Opaque;
	public string[] sunParams = null;
	public List<string> tessSize = null;
	public List<string> q3map_BackShader = null;
	public List<string> q3map_GlobalTexture = null;
	public List<string> q3map_SurfaceLight = null;
	public List<string> q3map_LightImage = null;
	public List<string> q3map_LightSubdivide = null;
	public string editorImage = "";
	public CullType cullType = CullType.Back;
	public SpriteType billboard = SpriteType.Disabled;
	public bool isSky = false;
	public bool isFog = false;
	public bool unShaded = false;
	public bool trans = false;
	public bool lava = false;
	public bool water = false;
	public bool noPicMip = false;
	public bool portal = false;
	public bool noMipMap = false;
	public bool polygonOffset = false;

	public enum SpriteType
	{
		Disabled,
		FixedY,
		Enabled
	}
	public enum CullType
	{
		Back,
		Front,
		Disable
	}
	public enum SortType
	{
		Opaque,
		Additive,
		Multiplicative
	}
	public void AddGlobal(string Params, string Value)
	{
		if (Params == "SURFACEPARM")
		{
			if (surfaceParms == null)
				surfaceParms = new List<string>();
			surfaceParms.Add(Value);
			if (Value == "TRANS")
			{
				if (!lava)
					trans = true;
			}
			else if (Value == "NODLIGHT")
				unShaded = true;
			else if (Value == "SKY")
				isSky = true;
			else if (Value == "LAVA")
			{
				lava = true;
				if (trans)
					trans = false;
			}
			else if (Value == "WATER")
				water = true;
		}
		else if (Params == "SKYPARMS")
		{
//			Some shader overwrite their first skyparams as those are invalid
//			if (skyParms == null)
				skyParms = new List<string>();
			skyParms.AddRange(Value.Split(' '));
			if ((skyParms[0] == "FULL") || (skyParms[0] == "HALF"))
				skyParms[0] = "-";
		}
		else if (Params == "CULL")
		{
			if (Value == "FRONT")
				cullType = CullType.Back;
			else if (Value == "BACK")
				cullType = CullType.Front;
			else
				cullType = CullType.Disable;
		}
		else if (Params == "DEFORMVERTEXES")
		{
			if (Value.Contains("AUTOSPRITE"))
			{
				if (Value.Contains('2'))
					billboard = SpriteType.FixedY;
				else
					billboard = SpriteType.Enabled;
			}
			else
			{
				if (deformVertexes == null)
					deformVertexes = new List<string[]>();
				deformVertexes.Add(Value.Split(' '));
			}
		}
		else if (Params == "FOGPARMS")
		{
			fogParms = Value.Split(' ');
			isFog = true;
		}
		else if (Params == "SORT")
		{
			if (Value == "ADDITIVE")
				sort = SortType.Additive;
			else
				sort = SortType.Opaque;
		}
		else if (Params == "TESSSIZE")
		{
			if (tessSize == null)
				tessSize = new List<string>();
			tessSize.Add(Value);
		}
		else if (Params == "Q3MAP_BACKSHADER")
		{
			if (q3map_BackShader == null)
				q3map_BackShader = new List<string>();
			q3map_BackShader.Add(Value);
		}
		else if (Params == "Q3MAP_GLOBALTEXTURE")
		{
			if (q3map_GlobalTexture == null)
				q3map_GlobalTexture = new List<string>();
			q3map_GlobalTexture.Add(Value);
		}
		else if (Params == "Q3MAP_SUN")
		{
			if (sunParams == null)
				sunParams = Value.Split(' ');
		}
		else if (Params == "Q3MAP_SURFACELIGHT")
		{
			if (q3map_SurfaceLight == null)
				q3map_SurfaceLight = new List<string>();
			q3map_SurfaceLight.Add(Value);
		}
		else if (Params == "Q3MAP_LIGHTIMAGE")
		{
			if (q3map_LightImage == null)
				q3map_LightImage = new List<string>();
			q3map_LightImage.Add(Value);
		}
		else if (Params == "Q3MAP_LIGHTSUBDIVIDE")
		{
			if (q3map_LightSubdivide == null)
				q3map_LightSubdivide = new List<string>();
			q3map_LightSubdivide.Add(Value);
		}
		else if (Params == "NOPICMIP")
			noPicMip = true;
		else if (Params == "NOMIPMAP")
			noMipMap = true;
		else if (Params == "POLYGONOFFSET")
			polygonOffset = true;
		else if (Params == "PORTAL")
			portal = true;
		else if (Params == "QER_EDITORIMAGE")
			editorImage = Value.StripExtension().ToUpper();
	}
}
public class QShaderStage
{
	public string[] map = null;
	public bool clamp = false;
	public bool isLightmap = false;
	public float animFreq = 0;
	public bool skyMap = false;
	public string[] blendFunc = null;
	public string[] rgbGen = null;
	public string[] alphaGen = null;
	public string[] tcGen = null;
	public List<QShaderTCMod> tcMod = null;
	public DepthFuncType depthFunc = DepthFuncType.LEQUAL;
	public bool depthWrite = false;
	public bool environment = false;
	public AlphaFuncType alphaFunc = AlphaFuncType.NONE;

	public void AddStageParams(string Params, string Value)
	{
		if (Params == "MAP")
		{
			map = new string[1] { Value.StripExtension() };
			if (map[0].Contains("$LIGHTMAP"))
				isLightmap = true;
		}
		else if (Params == "CLAMPMAP")
		{
			clamp = true;
			map = new string[1] { Value.StripExtension() };
		}
		else if (Params == "ANIMMAP")
		{
			string[] keyValue = Value.Split(' ');
			animFreq = QShaderManager.TryToParseFloat(keyValue[0]);
			map = new string[keyValue.Length - 1];
			for (int i = 1; i < keyValue.Length; i++)
				map[i - 1] = keyValue[i].StripExtension();
		}
		else if (Params == "SKYMAP")
		{
			skyMap = true;
			map = new string[1] { Value.StripExtension() };
		}
		else if (Params == "BLENDFUNC")
		{
			if (blendFunc == null)
				blendFunc = Value.Split(' ');
		}
		else if (Params == "RGBGEN")
		{
			if (rgbGen == null)
				rgbGen = Value.Split(' ');
		}
		else if (Params == "ALPHAGEN")
		{
			if (alphaGen == null)
				alphaGen = Value.Split(' ');
		}
		else if (Params == "TCGEN")
		{
			if (tcGen == null)
				tcGen = Value.Split(' ');

			if (Value == "ENVIRONMENT")
				environment = true;
		}
		else if (Params == "TCMOD")
		{
			string[] keyValue = Value.Split(' ', 2);
			if (keyValue.Length == 2)
			{
				if (tcMod == null)
					tcMod = new List<QShaderTCMod>();
				QShaderTCMod shaderTCMod = new QShaderTCMod();
				string func = keyValue[0];
				if (func == "ROTATE")
					shaderTCMod.type = TCModType.Rotate;
				else if (func == "SCALE")
					shaderTCMod.type = TCModType.Scale;
				else if (func == "SCROLL")
					shaderTCMod.type = TCModType.Scroll;
				else if (func == "STRETCH")
					shaderTCMod.type = TCModType.Stretch;
				else if (func == "TRANSFORM")
					shaderTCMod.type = TCModType.Transform;
				else if (func == "TURB")
					shaderTCMod.type = TCModType.Turb;
				shaderTCMod.value = keyValue[1].Split(' ');
				tcMod.Add(shaderTCMod);
			}
		}
		else if (Params == "DEPTHFUNC")
		{
			if (Value == "LEQUAL")
				depthFunc = DepthFuncType.LEQUAL;
			else if (Value == "EQUAL")
				depthFunc = DepthFuncType.EQUAL;
		}
		else if (Params == "DEPTHWRITE")
			depthWrite = true;
		else if (Params == "ALPHAFUNC")
		{
			if (Value == "GT0")
				alphaFunc = AlphaFuncType.GT0;
			else if (Value == "LT128")
				alphaFunc = AlphaFuncType.LT128;
			else if (Value == "GE128")
				alphaFunc = AlphaFuncType.GE128;
		}
	}
	public class QShaderTCMod
	{
		public TCModType type;
		public string[] value = null;
	}
	public enum TCModType
	{
		Rotate,
		Scale,
		Scroll,
		Stretch,
		Transform,
		Turb
	}
	public enum AlphaFuncType
	{
		NONE,
		GT0,
		LT128,
		GE128
	}
	public enum DepthFuncType
	{
		LEQUAL,
		EQUAL
	}
	
}

