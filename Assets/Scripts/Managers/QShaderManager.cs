using Godot;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public static class QShaderManager
{
	public static Dictionary<string, Shader> Shaders = new Dictionary<string, Shader>();
	public static Dictionary<string, QShaderData> QShaders = new Dictionary<string, QShaderData>();
	public static List<string> QShadersFiles = new List<string>();
	public enum GenFuncType
	{
		RGB,
		Alpha
	}
	public static void ProcessShaders()
	{
		foreach (string shaderFile in QShadersFiles)
		{
			if (PakManager.ZipFiles.ContainsKey(shaderFile))
			{
				string FileName = PakManager.ZipFiles[shaderFile];
				var reader = new ZipReader();
				reader.Open(FileName);
				byte[] rawShader = reader.ReadFile(shaderFile, false);
				ReadShaderData(rawShader);
			}
		}
	}

	public static bool HasShader(string shaderName)
	{
		string upperName = shaderName.ToUpper();
		if (QShaders.ContainsKey(upperName))
			return true;

		return false;
	}
	public static ShaderMaterial GetShadedMaterial(string shaderName, int lm_index, bool alphaIsTransparent = false)
	{
		string code = "";
		string GSHeader = "shader_type spatial;\nrender_mode diffuse_lambert, specular_schlick_ggx, ";
		string GSUniforms = "";
		string GSVertexH = "void vertex()\n{ \n";
		string GSFragmentH = "void fragment()\n{ \n";
		string GSFragmentUvs = "";
		string GSFragmentTcMod = "";
		string GSFragmentTexs = "";
		string GSLateFragmentTexs = "";
		string GSFragmentRGBs = "\tvec4 vertx_color = COLOR;\n";
		string GSFragmentBlends = "";
		string GSFragmentEnd = "\tALBEDO = (color.rgb * vertx_color.rgb);\n";
		string GSAnimation = "";

		List<string> textures = new List<string>();
		Dictionary<string, int> TexIndex = new Dictionary<string, int>();

		string upperName = shaderName.ToUpper();
		if (!QShaders.ContainsKey(upperName))
			return null;

		QShaderData qShader = QShaders[upperName];
		GD.Print("Shader found: " + upperName);

		switch (qShader.qShaderGlobal.sort)
		{
			case QShaderGlobal.SortType.Opaque:
				GSHeader += "depth_draw_opaque, blend_mix, ";
			break;
			case QShaderGlobal.SortType.Additive:
				GSHeader += "depth_draw_always, blend_add, ";
				alphaIsTransparent = true;
			break;
		}
		
		if ((qShader.qShaderGlobal.unShaded) || (qShader.qShaderGlobal.isSky))
			GSHeader += "unshaded, ";

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
				alphaIsTransparent = true;
			break;
		}
		int lightmapStage = -1;
		bool helperRotate = false;
		bool animStages = false;
		int totalStages = qShader.qShaderStages.Count;
		for (int i = 0; i < totalStages; i++)
		{
			QShaderStage qShaderStage = qShader.qShaderStages[i];
			if (qShaderStage.map != null)
			{
				if (qShaderStage.animFreq > 0)
				{
					animStages = true;
					GSLateFragmentTexs += "\tvec4 Stage_" + i + " = animation_" + i + "(Time, ";
					GSLateFragmentTexs += qShaderStage.animFreq.ToString("0.00") + " , " + qShaderStage.map.Length;
					for (int j = 0; j < qShaderStage.map.Length; j++)
						GSLateFragmentTexs += " , Anim_" + i + "_" + j;
					GSLateFragmentTexs += ");\n";
					GSAnimation += "vec4 animation_" + i + "(float Time, ";
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
						GSFragmentTexs += "\tvec4 Stage_" + i + " = texture(" + "Tex_" + index + ", uv_" + i + ");\n";
					else
					{
						GSUniforms += "uniform sampler2D " + "Tex_" + textures.Count;
						if (qShaderStage.clamp)
							GSUniforms += " : repeat_disable;\n";
						else
							GSUniforms += " : repeat_enable;\n";
						GSFragmentTexs += "\tvec4 Stage_" + i + " = texture(" + "Tex_" + textures.Count + ", uv_" + i + ");\n";
						textures.Add(qShaderStage.map[0]);
						TexIndex.Add(qShaderStage.map[0], i);
					}
				}
			}

			GSFragmentUvs += GetTcGen(qShader, i, ref lightmapStage);
			GSFragmentTcMod += GetTcMod(qShader, i, ref helperRotate);
			
			GSFragmentRGBs += GetGenFunc(qShader, i, GenFuncType.RGB);
			GSFragmentRGBs += GetGenFunc(qShader, i, GenFuncType.Alpha);
			GSFragmentRGBs += GetAlphaFunc(qShader, i);
			GSFragmentBlends += GetBlend(qShader, i, ref alphaIsTransparent);
		}

		int totalTex = textures.Count;
		if (qShader.qShaderGlobal.trans)
		{
			alphaIsTransparent = true;
			GD.Print("Current shader is transparent");
		}

		if (qShader.qShaderGlobal.editorImage.Length != 0)
		{
			if (QShaders.ContainsKey(qShader.qShaderGlobal.editorImage))
			{
				QShaderData qeditorShader = QShaders[qShader.qShaderGlobal.editorImage];
				if (qeditorShader.qShaderGlobal.trans)
				{
					alphaIsTransparent = true;
					GD.Print("Current editor shader is transparent");
				}

				if (!TexIndex.ContainsKey(qShader.qShaderGlobal.editorImage))
				{
					GSUniforms += "uniform sampler2D " + "Tex_" + totalTex + " : repeat_enable;\n";
					GSFragmentUvs += "\tvec2 uv_" + totalStages + " = UV;\n";
					GSFragmentTexs += "\tvec4 Stage_" + totalStages + " = texture(" + "Tex_" + totalTex + ", uv_" + totalStages + ");\n";
					textures.Add(qShader.qShaderGlobal.editorImage);
					TexIndex.Add(qShader.qShaderGlobal.editorImage, totalStages);
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
							int texIndex;
							if (TexIndex.TryGetValue(qShaderStage.map[j], out texIndex))
								GSFragmentTexs += "\tvec4 Anim_" + i + "_" + j + " = texture(" + "Tex_" + texIndex + ", uv_" + i + ");\n";
							else
							{
								GSUniforms += "uniform sampler2D " + "Tex_" + totalTex + " : repeat_enable;\n";
								GSFragmentTexs += "\tvec4 Anim_" + i + "_" + j +" = texture(" + "Tex_" + totalTex + ", uv_" + i + ");\n";
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
		code += "global uniform float MsTime;\n";
		code += "instance uniform float OffSetTime = 0.0;\n";
		if (helperRotate)
		{
			code += "\nvec2 rotate(vec2 uv, vec2 pivot, float angle)\n{\n\tmat2 rotation = mat2(vec2(sin(angle), -cos(angle)),vec2(cos(angle), sin(angle)));\n";
			code += "\tuv -= pivot;\n\tuv = uv * rotation;\n\tuv += pivot;\n\treturn uv;\n}\n\n";
		}

		if (animStages)
			code += GSAnimation;

		if (MaterialManager.HasBillBoard.Contains(upperName))
		{
			code += GSVertexH;
			code += "VERTEX = (vec4(VERTEX, 1.0) * MODELVIEW_MATRIX).xyz;\n}\n";
		}
		code += GSFragmentH;
		code += GSFragmentUvs;
		code += "\tfloat Time = (MsTime - OffSetTime);\n";
		code += GSFragmentTcMod;
		code += GSFragmentTexs;
		code += GSLateFragmentTexs;
		code += GSFragmentRGBs;

		if (lightmapStage < 0)
			code += "\tvec4 ambient = vec4("+ GameManager.Instance.mixBrightness.ToString("0.00") + " * " + GameManager.ambientLight.R.ToString("0.00") + ","+ GameManager.Instance.mixBrightness.ToString("0.00") + " * " + GameManager.ambientLight.G.ToString("0.00") + ","+ GameManager.Instance.mixBrightness.ToString("0.00") + " * " + GameManager.ambientLight.B.ToString("0.00") + ", 1.0 );\n";

		if (lightmapStage >= 0)
			code += "\tvec4 color = Stage_" + lightmapStage + ";\n";
		else if (qShader.qShaderGlobal.editorImage.Length != 0)
		{
			int editorIndex;
			if (TexIndex.TryGetValue(qShader.qShaderGlobal.editorImage, out editorIndex))
				code += "\tvec4 color = Stage_" + editorIndex + ";\n";
			else
				code += "\tvec4 color = vec4(0.0, 0.0, 0.0, 0.0);\n";
		}
		else 
			code += "\tvec4 color = vec4(0.0, 0.0, 0.0, 0.0);\n";

		code += "\tvec4 black = vec4(0.0, 0.0, 0.0, 0.0);\n";
		code += "\tvec4 white = vec4(1.0, 1.0, 1.0, 1.0);\n";
		code += GSFragmentBlends;
		code += GSFragmentEnd;

		if (lightmapStage >= 0)
			code += "\tEMISSION = mix((Stage_" + lightmapStage + ".rgb * color.rgb), color.rgb, " + GameManager.Instance.mixBrightness.ToString("0.00") + ");\n";
		else
			code += "\tEMISSION = mix((ambient.rgb * color.rgb), color.rgb, " + GameManager.Instance.mixBrightness.ToString("0.00") + ");\n";

		if (alphaIsTransparent)
			code += "\tALPHA = color.a * vertx_color.a;\n";
		code += "}\n\n";

		if (upperName.Contains("ARMOR"))
			GD.Print(code);

		Shader shader = new Shader();
		shader.Code = code;
		ImageTexture tex;
		ShaderMaterial shaderMaterial = new ShaderMaterial();
		shaderMaterial.Shader = shader;
		for (int i = 0; i < textures.Count; i++)
		{
			if (textures[i].Contains("$LIGHTMAP"))
				tex = MapLoader.lightMaps[lm_index];
			else
				tex = TextureLoader.GetTextureOrAddTexture(textures[i], alphaIsTransparent);
			shaderMaterial.SetShaderParameter("Tex_" + i, tex);
		}

		return shaderMaterial;
	}

	public static string GetTcGen(QShaderData qShader, int currentStage, ref int lightmapStage)
	{
		string TcGen = "";

		if (qShader.qShaderGlobal.skyParms != null)
		{
//			int cloudheight = int.Parse(qShader.qShaderGlobal.skyParms[1]) / 5;
			TcGen += "\tvec4 vot" + currentStage + "  = INV_VIEW_MATRIX * vec4(VERTEX, 0.0);\n";
			TcGen += "\tvot" + currentStage + ".y = 6.0 * (vot" + currentStage + ".y );\n";
			TcGen += "\tvot" + currentStage + " = normalize(vot" + currentStage + ");\n";
			TcGen += "\tvec2 uv_" + currentStage + "= vec2(vot" + currentStage + ".x, vot" + currentStage + ".z);\n";
		}
		else
		{
			if (qShader.qShaderStages[currentStage].environment)
				TcGen = "\tvec2 uv_" + currentStage + " = ((NORMAL * (2.0 * dot(VIEW,NORMAL))) - VIEW).yz * UV;\n";
			else if (qShader.qShaderStages[currentStage].map != null)
			{
				if (qShader.qShaderStages[currentStage].map[0].Contains("$LIGHTMAP"))
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
					TcMod += "\tfloat str_" + currentStage + " = " + basis.ToString("0.00") + " + " + amp.ToString("0.00") + " * (sin((Time)*" + freq.ToString("0.00") + "*6.28)+" + phase.ToString("0.00") + ");\n";
					TcMod += "\tuv_" + currentStage + "  = uv_" + currentStage + " *(str_" + currentStage + ") - vec2(1.0,1.0)*str_" + currentStage + "*0.5 + vec2(0.5,0.5);\n";
				}
				break;
				case QShaderStage.TCModType.Transform:
				{
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

	public static string GetGenFunc(QShaderData qShader, int currentStage, GenFuncType type)
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

		if (GenFunc.Length > 2)
		{
			string RGBFunc = GenFunc[1];
			float offset = TryToParseFloat(GenFunc[2]);
			float amp = TryToParseFloat(GenFunc[3]);
			float phase = TryToParseFloat(GenFunc[4]);
			float freq = TryToParseFloat(GenFunc[5]);
			switch (RGBFunc)
			{
				case "SIN":
					StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
					StageGen += offset.ToString("0.00") + " + sin(6.28 * " + freq.ToString("0.00") + " * (Time +" + phase.ToString("0.00") + "))  * " + amp.ToString("0.00") + "); \n";
				break;
				case "SQUARE":
					StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
					StageGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * round(fract(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))); \n";
				break;
				case "TRIANGLE":
					StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
					StageGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (abs(2.0 * (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(0.5 + Time * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))))); \n";
				break;
				case "SAWTOOTH":
					StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
					StageGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))); \n";
				break;
				case "INVERSESAWTOOTH":
					StageGen = "\tStage_" + currentStage + GenType + " = Stage_" + currentStage + GenType + " * (";
					StageGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (1.0 - (Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + " - floor(Time  * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + ")))); \n";
				break;
			}
		}
		else if (GenFunc.Length == 1)
		{
			string RGBFunc = GenFunc[0];
			if (RGBFunc.Contains("VERTEX"))
				StageGen = "\tStage_" + currentStage + ".rgb = Stage_" + currentStage + ".rgb * vertx_color.rgb ; \n";
		}
		return StageGen;
	}

	public static string GetAlphaFunc(QShaderData qShader, int currentStage)
	{
		string AlphaFunc = "";
		if (qShader.qShaderStages[currentStage].alphaFunc == QShaderStage.AlphaFuncType.NONE)
			return AlphaFunc;

		AlphaFunc = "\tif (Stage_" + currentStage + ".a ";		
		switch (qShader.qShaderStages[currentStage].alphaFunc)
		{
			case QShaderStage.AlphaFuncType.GT0:
				AlphaFunc += "== 1.0)\n";
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
	public static string GetBlend(QShaderData qShader, int currentStage, ref bool alphaIsTransparent)
	{
		string Blend = "";
		if (qShader.qShaderStages[currentStage].blendFunc != null)
		{
			string BlendWhat = qShader.qShaderStages[currentStage].blendFunc[0];
			if (BlendWhat.Contains("ADD"))
			{
				Blend = "\tcolor = Stage_" + currentStage + " + color; \n";
				if (currentStage == 0)
					alphaIsTransparent = true;
			}
			else if (BlendWhat.Contains("FILTER"))
				Blend = "\tcolor = Stage_" + currentStage + " * color; \n";
			else if (BlendWhat.Contains("BLEND"))
			{
				Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * Stage_" + currentStage + ".a + color.rgb * (1.0 - Stage_" + currentStage + ".a); \n";
				Blend += "\tcolor.a = Stage_" + currentStage + ".a *   Stage_" + currentStage + ".a + color.a *  (1.0 -  Stage_" + currentStage + ".a); \n";
			}
			else
			{
				string src = qShader.qShaderStages[currentStage].blendFunc[0];
				string dst = qShader.qShaderStages[currentStage].blendFunc[1];
				string asrc = "";
				string adst = "";
				string csrc = "";
				string cdst = "";
				switch (src)
				{
					case "GL_ONE":
						csrc = " 1.0 ";
						asrc = " 1.0 ";
						break;
					case "GL_ZERO":
						csrc = " 0.0 ";
						asrc = " 0.0 ";
						break;
					case "GL_DST_COLOR":
						csrc = " color.rgb ";
						asrc = " color.a ";
						break;
					case "GL_ONE_MINUS_DST_COLOR":
						csrc = " 1.0 - color.rgb ";
						asrc = " 1.0 - color.a ";
						break;
					case "GL_SRC_ALPHA":
						csrc = "  Stage_" + currentStage + ".a ";
						asrc = "  Stage_" + currentStage + ".a ";
						break;
					case "GL_ONE_MINUS_SRC_ALPHA":
						csrc = " (1.0 -  Stage_" + currentStage + ".a) ";
						asrc = " (1.0 -  Stage_" + currentStage + ".a) ";
						break;
					case "GL_DST_ALPHA":
						csrc = " color.a ";
						asrc = " color.a ";
						break;
					case "GL_ONE_MINUS_DST_ALPHA":
						csrc = " (1.0 - color.a) ";
						asrc = " (1.0 - color.a) ";
						break;
				}
				switch (dst)
				{
					case "GL_ONE":
						cdst = " 1.0 ";
						adst = " 1.0 ";
						//Horrible hack
						if (currentStage == 0)
							alphaIsTransparent = true;
						break;
					case "GL_ZERO":
						cdst = " 0.0 ";
						adst = " 0.0 ";
						break;
					case "GL_SRC_COLOR":
						cdst = " Stage_" + currentStage + ".rgb ";
						adst = " Stage_" + currentStage + ".a ";
						break;
					case "GL_ONE_MINUS_SRC_COLOR":
						cdst = " (1.0 - Stage_" + currentStage + ".rgb) ";
						adst = " (1.0 - Stage_" + currentStage + ".a) ";
						break;
					case "GL_DST_ALPHA":
						cdst = " color.a ";
						adst = " color.a ";
						break;
					case "GL_ONE_MINUS_DST_ALPHA":
						cdst = " (1.0 - color.a) ";
						adst = " (1.0 - color.a) ";
						break;
					case "GL_SRC_ALPHA":
						cdst = "  Stage_" + currentStage + ".a ";
						adst = "  Stage_" + currentStage + ".a ";
						break;
					case "GL_ONE_MINUS_SRC_ALPHA":
						cdst = " (1.0 -  Stage_" + currentStage + ".a) ";
						adst = " (1.0 -  Stage_" + currentStage + ".a) ";
						break;
				}
				//Horrible hack
				if (currentStage == 0)
				{
					if (qShader.qShaderStages[currentStage].environment)
					{
						if ((src == "GL_ONE") && (dst == "GL_ONE"))
						{
							Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * " + csrc + " + color.rgb * " + cdst + "; \n";
							Blend += "\tcolor.a = 0.5; \n";
							alphaIsTransparent = true;
						}
						else
						{
							Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * " + csrc + " + color.rgb * " + cdst + ";\n";
							Blend += "\tcolor.a = Stage_" + currentStage + ".a * " + asrc + " + color.a * " + adst + ";\n";
						}
					}
					else if ((src == "GL_ZERO") && (dst == "GL_ONE_MINUS_SRC_COLOR"))
					{
						Blend = "\tcolor.rgb = Stage_" + currentStage + ".rgb * " + csrc + " + color.rgb * " + cdst + ";\n";
						Blend += "\tcolor.a = Stage_" + currentStage + ".a; \n";
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
			Blend = "\tcolor = Stage_" + currentStage + ";\n";

		Blend += "\tcolor = clamp(color,black,white);\n";
		return Blend;
	}

	public static void ReadShaderData(byte[] shaderData)
	{
		MemoryStream ms = new MemoryStream(shaderData);
		StreamReader stream = new StreamReader(ms);
		string strWord;
		char[] line = new char[1024];

		stream.BaseStream.Seek(0, SeekOrigin.Begin);

		int p;
		int stage = 0;
		int node = 0;

		QShaderData qShaderData = null;
		while (!stream.EndOfStream)
		{
			strWord = stream.ReadLine();
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
						i = strWord.Length;
					}
					break;
					case '}':
					{
						p = 0;
						node--;
						if (node == 0)
						{
							if (QShaders.ContainsKey(qShaderData.Name))
							{
								GD.Print("Shader already on the list: " + qShaderData.Name + " md5: "+ qShaderData.Name.Md5Text());
								QShaders[qShaderData.Name] = qShaderData;
							}
							else
								QShaders.Add(qShaderData.Name, qShaderData);
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
	}
	public static void AddShaderFiles(string FileName)
	{
		if (QShadersFiles.Contains(FileName))
		{
			GD.Print("Shader File "+ FileName + " already on the list ");
			return;
		}
		else
			QShadersFiles.Add(FileName);
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
	public List<string> deformVertexes = null;
	public List<string> fogParms = null;
	public SortType sort = SortType.Opaque;
	public List<string> tessSize = null;
	public List<string> q3map_BackShader = null;
	public List<string> q3map_GlobalTexture = null;
	public List<string> q3map_Sun = null;
	public List<string> q3map_SurfaceLight = null;
	public List<string> q3map_LightImage = null;
	public List<string> q3map_LightSubdivide = null;
	public string editorImage = "";
	public CullType cullType = CullType.Back;
	public bool isSky = false;
	public bool unShaded = false;
	public bool trans = false;
	public bool noPicMip = false;
	public bool portal = false;
	public bool noMipMap = false;
	public bool polygonOffset = false;

	public enum CullType
	{
		Back,
		Front,
		Disable
	}
	public enum SortType
	{
		Portal,
		Sky,
		Opaque,
		Banner,
		Underwater,
		Additive,
		Nearest
	}
	public void AddGlobal(string Params, string Value)
	{
		switch (Params)
		{
			case "SURFACEPARM":
				if (surfaceParms == null)
					surfaceParms = new List<string>();
				surfaceParms.Add(Value);
				switch (Value)
				{
					case "TRANS":
						trans = true;
					break;
					case "NODLIGHT":
						unShaded = true;
					break;
					case "SKY":
						isSky = true;
					break;
				}
				break;
			case "SKYPARMS":
				if (skyParms == null)
					skyParms = new List<string>();
				skyParms.Add(Value);
			break;
			case "CULL":
				switch (Value)
				{
					case "FRONT":
						cullType = CullType.Back;
					break;
					case "BACK":
						cullType = CullType.Front;
					break;
					default:
						cullType = CullType.Disable;
					break;
				}
			break;
			case "DEFORMVERTEXES":
				if (deformVertexes ==  null)
					deformVertexes = new List<string>();
				deformVertexes.Add(Value);
			break;
			case "FOGPARMS":
				if (fogParms == null)
					fogParms = new List<string>();
				fogParms.Add(Value);
			break;
			case "SORT":
				switch (Value)
				{
					case "ADDITIVE":
						sort = SortType.Additive;
					break;
					default:
						sort = SortType.Opaque;
					break;
				}
				break;
			case "TESSSIZE":
				if (tessSize == null)
					tessSize = new List<string>();
				tessSize.Add(Value);
			break;
			case "Q3MAP_BACKSHADER":
				if (q3map_BackShader == null)
					q3map_BackShader = new List<string>();
				q3map_BackShader.Add(Value);	
			break;
			case "Q3MAP_GLOBALTEXTURE":
				if (q3map_GlobalTexture == null)
					q3map_GlobalTexture = new List<string>();
				q3map_GlobalTexture.Add(Value);
			break;
			case "Q3MAP_SUN":
				if (q3map_Sun == null)
					q3map_Sun = new List<string>();
				q3map_Sun.Add(Value);
			break;
			case "Q3MAP_SURFACELIGHT":
				if (q3map_SurfaceLight == null)
					q3map_SurfaceLight = new List<string>();
				q3map_SurfaceLight.Add(Value);
			break;
			case "Q3MAP_LIGHTIMAGE":
				if (q3map_LightImage == null)
					q3map_LightImage = new List<string>();
				q3map_LightImage.Add(Value);
			break;
			case "Q3MAP_LIGHTSUBDIVIDE":
				if (q3map_LightSubdivide == null)
					q3map_LightSubdivide = new List<string>();
				q3map_LightSubdivide.Add(Value);
			break;
			case "NOPICMIP":
				noPicMip = true;
			break;
			case "NOMIPMAP":
				noMipMap = true;
			break;
			case "POLYGONOFFSET":
				polygonOffset = true;
			break;
			case "PORTAL":
				portal = true;
			break;
			case "QER_EDITORIMAGE":
				editorImage = Value.Split('.')[0].ToUpper();
			break;
		}
	}
}
public class QShaderStage
{
	public string[] map = null;
	public bool clamp = false;
	public float animFreq = 0;
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
		switch (Params)
		{
			case "MAP":
				map = new string[1] { Value.Split('.')[0] };
			break;
			case "CLAMPMAP":
				clamp = true;
				map = new string[1] { Value.Split('.')[0] };
			break;
			case "ANIMMAP":
			{
				string[] keyValue = Value.Split(' ');
				animFreq = QShaderManager.TryToParseFloat(keyValue[0]);
				map = new string[keyValue.Length - 1];
				for (int i = 1; i < keyValue.Length; i++)
					map[i-1] = keyValue[i].Split('.')[0];
			}
			break;
			case "BLENDFUNC":
				if (blendFunc == null) 
					blendFunc = Value.Split(' ');
				break;
			case "RGBGEN":
				if (rgbGen == null)
					rgbGen = Value.Split(' ');
			break;
			case "ALPHAGEN":
				if (alphaGen == null)
					alphaGen = Value.Split(' ');
				break;
			case "TCGEN":
				if (tcGen  == null)
					tcGen = Value.Split(' ');

				switch (Value)
				{
					case "ENVIRONMENT":
						environment = true;
					break;
					default:
					break;
				}
			break;
			case "TCMOD":
			{
				string[] keyValue = Value.Split(' ', 2);
				if (keyValue.Length == 2)
				{
					if (tcMod == null)
						tcMod = new List<QShaderTCMod>();
					QShaderTCMod shaderTCMod = new QShaderTCMod();

					switch (keyValue[0])
					{
						case "ROTATE":
							shaderTCMod.type = TCModType.Rotate;
						break;
						case "SCALE":
							shaderTCMod.type = TCModType.Scale;
						break;
						case "SCROLL":
							shaderTCMod.type = TCModType.Scroll;
						break;
						case "STRETCH":
							shaderTCMod.type = TCModType.Stretch;
						break;
						case "TRANSFORM":
							shaderTCMod.type = TCModType.Transform;
						break;
						case "TURB":
							shaderTCMod.type = TCModType.Turb;
						break;
					}
					shaderTCMod.value = keyValue[1].Split(' ');
					tcMod.Add(shaderTCMod);
				}
			}
			break;
			case "DEPTHFUNC":
				switch (Value)
				{
					case "LEQUAL":
						depthFunc = DepthFuncType.LEQUAL;
						break;
					case "EQUAL":
						depthFunc = DepthFuncType.EQUAL;
						break;
				}
				break;
			case "DEPTHWRITE":
				depthWrite = true;
			break;
			case "ALPHAFUNC":
				switch (Value)
				{
					case "GT0":
						alphaFunc = AlphaFuncType.GT0;
					break;
					case "LT128":
						alphaFunc = AlphaFuncType.LT128;
					break;
					case "GE128":
						alphaFunc = AlphaFuncType.GE128;
					break;
				}
				break;
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

