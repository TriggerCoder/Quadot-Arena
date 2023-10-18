using Godot;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public static class QShaderManager
{
	public static Dictionary<string, Shader> Shaders = new Dictionary<string, Shader>();
	public static Dictionary<string, QShaderData> QShaders = new Dictionary<string, QShaderData>();
	public static List<string> QShadersFiles = new List<string>();
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
//				GD.Print(shaderFile);
				ReadShaderData(rawShader);
			}
		}
	}
	public static ShaderMaterial GetShadedMaterial(string shaderName, int lm_index)
	{
		string code = "";
		string GSHeader = "shader_type spatial;\nrender_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_schlick_ggx;\n \n";
		string GSUniforms = "";
		string GSVertexH = "void vertex()\n{ \n";
		string GSFragmentH = "void fragment()\n{ \n";
		string GSFragmentUvs = "";
		string GSFragmentTcMod = "";
		string GSFragmentTexs = "";
		string GSFragmentRGBs = "";
		string GSFragmentBlends = "\tvec4 vertx_color = COLOR;\n";
		string GSFragmentEnd = "\t//if (length(color.rgb) == 1.0)\n\t//{\n\t\t//discard;\n\t//}\n\tALBEDO = (color.rgb * vertx_color.rgb);\n";
		bool alphaIsTransparent = false;
		bool useEditorImage = false;

		List<string> textures = new List<string>();

		string upperName = shaderName.ToUpper();
		if (!QShaders.ContainsKey(upperName))
			return null;

		QShaderData qShader = QShaders[upperName];
		GD.Print("Shader found: " + upperName);

		int lightmapStage = -1;
		bool helperRotate = false;
		for (int i = 0; i < qShader.qShaderStages.Count; i++)
		{
			QShaderStage qShaderStage = qShader.qShaderStages[i];
			GSUniforms += "uniform sampler2D " + "stage_" + i;
			if (qShaderStage.map != null)
			{
				textures.Add(qShaderStage.map[0]);
				GSUniforms += " : repeat_enable;\n";
			}
			else if (qShaderStage.clampMap != null)
			{
				textures.Add(qShaderStage.clampMap[0]);
				GSUniforms += " : repeat_disable;\n";
			}
			else if (qShaderStage.animMap != null)
			{
				textures.Add(qShaderStage.animMap[1].Split('.')[0]);
				GSUniforms += " : repeat_enable;\n";
			}
			GSFragmentUvs += GetTcGen(qShader, i, ref lightmapStage);
			GSFragmentTcMod += GetTcMod(qShader, i, ref helperRotate);
			GSFragmentTexs += "\tvec4 tex" + i + " = texture(" + "stage_" + i + ", uv" + i + ");\n";
			GSFragmentRGBs += GetRGBGen(qShader, i);
			GSFragmentBlends += GetBlend(qShader, i);
		}

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
				useEditorImage = true;
				textures.Add(qShader.qShaderGlobal.editorImage);
				int index = qShader.qShaderStages.Count;
				GSUniforms += "uniform sampler2D " + "stage_" + index + " : repeat_enable;\n";
				GSFragmentUvs += "\tvec2 uv" + index + " = UV;\n";
				GSFragmentTexs += "\tvec4 tex" + index + " = texture(" + "stage_" + index + ", uv" + index + ");\n";
				if (qeditorShader.qShaderGlobal.trans)
				{
					alphaIsTransparent = true;
					GD.Print("Current editor shader is transparent");
				}
			}
		}

		code += GSHeader;
		code += GSUniforms;

		if (helperRotate)
		{
			code += "\nvec2 rotate(vec2 uv, vec2 pivot, float angle)\n{\n\tmat2 rotation = mat2(vec2(sin(angle), -cos(angle)),vec2(cos(angle), sin(angle)));\n";
			code += "\tuv -= pivot;\n\tuv = uv * rotation;\n\tuv += pivot;\n\treturn uv;\n}\n\n";
		}

		code += GSFragmentH;
		code += GSFragmentUvs;
		code += GSFragmentTcMod;
		code += GSFragmentTexs;
		code += GSFragmentRGBs;

		if (lightmapStage >= 0)
			code += "\tvec4 color = tex" + lightmapStage + ";\n";
		else if (useEditorImage)
			code += "\tvec4 color = tex" + qShader.qShaderStages.Count + ";\n";
		else 
			code += "\tvec4 color = vec4(0.0, 0.0, 0.0, 0.0);\n";

		code += GSFragmentBlends;

		code += GSFragmentEnd;
		if (lightmapStage >= 0)
		{
			code += "\tEMISSION = mix((tex" + lightmapStage + ".rgb * color.rgb), color.rgb, " + GameManager.Instance.mixBrightness.ToString("0.00") + ");\n\n ";
		}
		if (alphaIsTransparent)
		{
//			code += "\tALPHA_SCISSOR_THRESHOLD = 0.5;\n";
			code += "\tALPHA = color.a;\n";
		}
		code += "\n}\n\n";

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
			shaderMaterial.SetShaderParameter("stage_" + i, tex);
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
			TcGen += "\tvec2 uv" + currentStage + "= vec2(vot" + currentStage + ".x, vot" + currentStage + ".z);\n";
		}
		else
		{
			if (qShader.qShaderStages[currentStage].map != null)
			{
				if (qShader.qShaderStages[currentStage].map[0].Contains("$LIGHTMAP"))
				{
					lightmapStage = currentStage;
					TcGen = "\tvec2 uv" + currentStage + " = UV2;\n";
				}
				else
					TcGen = "\tvec2 uv" + currentStage + " = UV;\n";
			}
			else
				TcGen = "\tvec2 uv" + currentStage + " = UV;\n";
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
					TcMod += "\tuv" + currentStage + " = rotate(uv" + currentStage + ", vec2(0.5), radians(" + deg.ToString("0.00") + ") * TIME*0.5);\n";
				}
				break;
				case QShaderStage.TCModType.Scale:
				{
					float SScale = TryToParseFloat(shaderTCMod.value[0]);
					float TScale = TryToParseFloat(shaderTCMod.value[1]);
					TcMod += "\tuv" + currentStage + " *= vec2(" + SScale.ToString("0.00") + "," + TScale.ToString("0.00") + "); \n";
				}
				break;
				case QShaderStage.TCModType.Scroll:
				{
					float SSpeed = TryToParseFloat(shaderTCMod.value[0]);
					float TSpeed = TryToParseFloat(shaderTCMod.value[1]);
					TcMod += "\tuv" + currentStage + " += vec2(" + SSpeed.ToString("0.00") + "," + TSpeed.ToString("0.00") + ") * TIME*0.5; \n";
				}
				break;
				case QShaderStage.TCModType.Stretch:
				{
					string func = shaderTCMod.value[0];
					float basis = TryToParseFloat(shaderTCMod.value[1]);
					float amp = TryToParseFloat(shaderTCMod.value[2]);
					float phase = TryToParseFloat(shaderTCMod.value[3]);
					float freq = TryToParseFloat(shaderTCMod.value[4]);
					TcMod += "\tfloat str_" + currentStage + " = " + basis.ToString("0.00") + " + " + amp.ToString("0.00") + " * (sin((TIME)*" + freq.ToString("0.00") + "*6.28)+" + phase.ToString("0.00") + ");\n";
					TcMod += "\tuv" + currentStage + "  = uv" + currentStage + " *(str_" + currentStage + ") - vec2(1.0,1.0)*str_" + currentStage + "*0.5 + vec2(0.5,0.5);\n";
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
					string turbX = "(sin( (2.0 /" + freq.ToString("0.00") + ") * (TIME * 6.28) + " + phase.ToString("0.00") + ") * " + amp.ToString("0.00") + " )";
					string turbY = "(sin( (2.0 /" + freq.ToString("0.00") + ") * (TIME * 6.28) + " + phase.ToString("0.00") + ") * " + amp.ToString("0.00") + " )";
					TcMod += "\tuv" + currentStage + " += vec2(" + turbX + "," + turbY + "); \n";				
				}
				break;
			}
		}
		return TcMod;
	}

	public static string GetRGBGen(QShaderData qShader, int currentStage)
	{
		string RGBGen = "\ttex" + currentStage + ".rgb = tex" + currentStage + ".rgb * 1.0 ; \n";
		if (qShader.qShaderStages[currentStage].rgbGen != null)
		{
			if (qShader.qShaderStages[currentStage].rgbGen.Length > 0)
			{
				string Gen = qShader.qShaderStages[currentStage].rgbGen[0];
			}
			if (qShader.qShaderStages[currentStage].rgbGen.Length > 2)
			{
				string RGBFunc = qShader.qShaderStages[currentStage].rgbGen[1];
				float offset = TryToParseFloat(qShader.qShaderStages[currentStage].rgbGen[2]);
				float amp = TryToParseFloat(qShader.qShaderStages[currentStage].rgbGen[3]);
				float phase = TryToParseFloat(qShader.qShaderStages[currentStage].rgbGen[4]);
				float freq = TryToParseFloat(qShader.qShaderStages[currentStage].rgbGen[5]);
				switch (RGBFunc)
				{
					case "SIN":
						RGBGen = "\ttex" + currentStage + ".rgb = tex" + currentStage + ".rgb * (";
						RGBGen += offset.ToString("0.00") + " + sin(6.28 * " + freq.ToString("0.00") + " * (TIME +" + phase.ToString("0.00") + "))  * " + amp.ToString("0.00") + "); \n";
					break;
					case "SQUARE":
						RGBGen = "\ttex" + currentStage + ".rgb = tex" + currentStage + ".rgb * (";
						RGBGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * round(fract(TIME  * " + freq.ToString("0.00") + "+ " + phase.ToString("0.00") + "))); \n";
					break;
					case "TRIANGLE":
						RGBGen = "\ttex" + currentStage + ".rgb = tex" + currentStage + ".rgb * (";
						RGBGen += offset.ToString("0.00") + " + " + amp.ToString("0.00") + " * (abs(2.0 * (TIME  * " + freq.ToString("0.00") + "+ " + phase.ToString("0.00") + " - floor(0.5 + TIME * " + freq.ToString("0.00") + " + " + phase.ToString("0.00") + "))))); \n";
						break;
					case "SAWTOOTH":
						RGBGen = "\ttex" + currentStage + ".rgb = tex" + currentStage + ".rgb * (";
						RGBGen += offset.ToString("0.00") + "+ " + amp.ToString("0.00") + " * (TIME  * " + freq.ToString("0.00") + "+ " + phase.ToString("0.00") + " - floor(TIME  * " + freq.ToString("0.00") + "+ " + phase.ToString("0.00") + "))); \n";
						break;
					case "INVERSESAWTOOTH":
						RGBGen = "\ttex" + currentStage + ".rgb = tex" + currentStage + ".rgb * (";
						RGBGen += offset.ToString("0.00") + "+ " + amp.ToString("0.00") + " * (1.0 - (TIME  * " + freq.ToString("0.00") + "+ " + phase.ToString("0.00") + " - floor(TIME  * " + freq.ToString("0.00") + "+ " + phase.ToString("0.00") + ")))); \n";
					break;
				}
			}
		}
		return RGBGen;
	}

	public static string GetBlend(QShaderData qShader, int currentStage)
	{
		string Blend = "";
		if (qShader.qShaderStages[currentStage].blendFunc != null)
		{
			string BlendWhat = qShader.qShaderStages[currentStage].blendFunc[0];
			if (BlendWhat.Contains("ADD"))
				Blend = "\tcolor = tex" + currentStage + " + color; \n";
			else if (BlendWhat.Contains("FILTER"))
				Blend = "\tcolor = tex" + currentStage + " * color; \n";
			else if (BlendWhat.Contains("BLEND"))
				Blend = "\tcolor.rgb = tex" + currentStage + ".rgb * tex" + currentStage + ".a + color.rgb * (1.0 - tex" + currentStage + ".a); \n";
			else
			{
				string src = qShader.qShaderStages[currentStage].blendFunc[0];
				string dst = qShader.qShaderStages[currentStage].blendFunc[1];
				string fsrc = "";
				string fdst = "";
				switch (src)
				{
					case "GL_ONE":
						fsrc = " 1.0 ";
						break;
					case "GL_ZERO":
						fsrc = " 0.0 ";
						break;
					case "GL_DST_COLOR":
						fsrc = " color.rgb ";
						break;
					case "GL_ONE_MINUS_DST_COLOR":
						fsrc = " 1.0 - color.rgb ";
						break;
					case "GL_SRC_ALPHA":
						fsrc = "  tex" + currentStage + ".a ";
						break;
					case "GL_ONE_MINUS_SRC_ALPHA":
						fsrc = " (1.0 -  tex" + currentStage + ".a) ";
						break;
					case "GL_DST_ALPHA":
						fsrc = " color.a ";
						break;
					case "GL_ONE_MINUS_DST_ALPHA":
						fsrc = " (1.0 - color.a) ";
						break;
				}
				switch (dst)
				{
					case "GL_ONE":
						fdst = " 1.0 ";
						break;
					case "GL_ZERO":
						fdst = " 0.0 ";
						break;
					case "GL_SRC_COLOR":
						fdst = " tex" + currentStage + ".rgb ";
						break;
					case "GL_ONE_MINUS_SRC_COLOR":
						fdst = " (1.0 - tex" + currentStage + ".rgb) ";
						break;
					case "GL_DST_ALPHA":
						fdst = " color.a ";
						break;
					case "GL_ONE_MINUS_DST_ALPHA":
						fdst = " (1.0 - color.a) ";
						break;
					case "GL_SRC_ALPHA":
						fdst = "  tex" + currentStage + ".a ";
						break;
					case "GL_ONE_MINUS_SRC_ALPHA":
						fdst = " (1.0 -  tex" + currentStage + ".a) ";
						break;
				}
				Blend = "\tcolor.rgb = tex" + currentStage + ".rgb * " + fsrc + " + color.rgb * " + fdst + "; \n";
			}
		}
		else
			Blend = "\tcolor =  tex" + currentStage + "; \n";
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
	public List<string> cull = null;
	public List<string> deformVertexes = null;
	public List<string> fogParms = null;
	public List<string> sort = null;
	public List<string> tessSize = null;
	public List<string> q3map_BackShader = null;
	public List<string> q3map_GlobalTexture = null;
	public List<string> q3map_Sun = null;
	public List<string> q3map_SurfaceLight = null;
	public List<string> q3map_LightImage = null;
	public List<string> q3map_LightSubdivide = null;
	public string editorImage = "";
	public bool trans = false;
	public bool noPicMip = false;
	public bool portal = false;
	public bool noMipMap = false;
	public bool polygonOffset = false;

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
				}
				break;
			case "SKYPARMS":
				if (skyParms == null)
					skyParms = new List<string>();
				skyParms.Add(Value);
			break;
			case "CULL":
				if (cull == null)
					cull = new List<string>();
				cull.Add(Value);	
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
				if (sort ==  null)
					sort = new List<string>();
				sort.Add(Value);
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
	public string[] clampMap = null;
	public string[] animMap = null;
	public string[] blendFunc = null;
	public string[] rgbGen = null;
	public string[] alphaGen = null;
	public string[] tcGen = null;
	public List<QShaderTCMod> tcMod = null;
	public string[] depthFunc = null;
	public string[] depthWrite = null;
	public string[] alphaFunc = null;

	public void AddStageParams(string Params, string Value)
	{
		switch (Params)
		{
			case "MAP":
				if (map == null)
					map = Value.Split('.');
				break;
			case "CLAMPMAP":
				if (clampMap == null) 
					clampMap = Value.Split('.');
				break;
			case "ANIMMAP":
			{
				if (animMap == null)
					animMap = Value.Split(' ');
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
				if (depthFunc  == null)
					depthFunc = Value.Split(' ');
				break;
			case "DEPTHWRITE":
				if (depthWrite == null)
					depthWrite = Value.Split(' ');
				break;
			case "ALPHAFUNC":
				if (alphaFunc == null)
					alphaFunc = Value.Split(' ');
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
}

