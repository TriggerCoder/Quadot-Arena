using Godot;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

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
		string GSHeader = "shader_type spatial;\n \n";
		string GSUniforms = "";
		string GSVertexH = "void vertex(){ \n";
		string GSFragmentH = "void fragment(){ \n";
		string GSFragmentUvs = "";
		string GSFragmentTcMod = "";
		string GSFragmentTexs = "";
		string GSFragmentRGBs = "";
		string GSFragmentBlends = "vec4 color = vec4(1.0,1.0,1.0,1.0); \n";
		string GSFragmentEnd = "ALBEDO = color.rgb;\n\n ";

		List<string> textures = new List<string>();

		string upperName = shaderName.ToUpper();
		if (!QShaders.ContainsKey(upperName))
			return null;

		QShaderData qShader = QShaders[upperName];
		GD.Print("Shader found: " + upperName);

		for (int i = 0; i < qShader.qShaderStages.Count; i++)
		{
			QShaderStage qShaderStage = qShader.qShaderStages[i];
			if (qShaderStage.map != null)
				textures.Add(qShaderStage.map[0]);
			else if (qShaderStage.clampMap != null)
				textures.Add(qShaderStage.clampMap[0]);
			else if (qShaderStage.animMap != null)
				textures.Add(qShaderStage.animMap[1].Split('.')[0]);
			GSUniforms += "uniform sampler2D " + "stage_" + i + ";\n";
			GSFragmentUvs += GetTcGen(qShader, i);
			GSFragmentTcMod += GetTcMod(qShader, i);
			GSFragmentTexs += "vec4 tex" + i + " = texture(" + "stage_" + i + ", uv" + i + ");\n";
			GSFragmentRGBs += GetRGBGen(qShader, i);
			GSFragmentBlends += GetBlend(qShader, i);
		}

		code += GSHeader;
		code += GSUniforms;
		code += GSFragmentH;
		code += GSFragmentUvs;
		code += GSFragmentTcMod;
		code += GSFragmentTexs;
		code += GSFragmentRGBs;
		code += GSFragmentBlends;
		code += GSFragmentEnd;
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
				tex = TextureLoader.GetTextureOrAddTexture(textures[i]);
			shaderMaterial.SetShaderParameter("stage_" + i, tex);
		}

		return shaderMaterial;
	}

	public static string GetTcGen(QShaderData qShader, int currentStage)
	{
		string TcGen = "";

		if (qShader.qShaderGlobal.skyParms != null)
		{
//			int cloudheight = int.Parse(qShader.qShaderGlobal.skyParms[1]) / 5;
			TcGen = "vec4 vot" + currentStage + "  = INV_VIEW_MATRIX * vec4(VERTEX, 0.0);\n";
			TcGen += "vot" + currentStage + ".y = 6.0 * (vot" + currentStage + ".y );\n";
			TcGen += "vot" + currentStage + " = normalize(vot" + currentStage + ");\n";
			TcGen += "vec2 uv" + currentStage + "= vec2(vot" + currentStage + ".x, vot" + currentStage + ".z);\n";
		}
		else
		{
			if (qShader.qShaderStages[currentStage].map != null)
			{
				if (qShader.qShaderStages[currentStage].map[0].Contains("$LIGHTMAP"))
					TcGen = "vec2 uv" + currentStage + "=UV2;\n";
				else
					TcGen = "vec2 uv" + currentStage + "=UV;\n";
			}
			else
				TcGen = "vec2 uv" + currentStage + "=UV;\n";
		}

		return TcGen;
	}

	public static string GetTcMod(QShaderData qShader, int currentStage)
	{
		string TcMod = "";

		if (qShader.qShaderStages[currentStage].tcModRotate != null)
		{
			float deg = TryToParseFloat(qShader.qShaderStages[currentStage].tcModRotate[0]);
			TcMod = "uv" + currentStage + " *=vec2(cos(" + deg.ToString("0.00") + "),-sin(" + deg.ToString("0.00") + "))* TIME*0.5; \n";
		}
		if (qShader.qShaderStages[currentStage].tcModStretch != null)
		{
			string func = qShader.qShaderStages[currentStage].tcModStretch[0];
			float basis = TryToParseFloat(qShader.qShaderStages[currentStage].tcModStretch[1]);
			float amp = TryToParseFloat(qShader.qShaderStages[currentStage].tcModStretch[2]);
			float phase = TryToParseFloat(qShader.qShaderStages[currentStage].tcModStretch[3]);
			float freq = TryToParseFloat(qShader.qShaderStages[currentStage].tcModStretch[4]);
			TcMod += "float str_" + currentStage + " = " + basis.ToString("0.00") + " + " + amp.ToString("0.00") + " * (sin(fract(TIME)*" + freq.ToString("0.00") + "*6.28)+" + phase.ToString("0.00") + ");\n";
			TcMod += "uv" + currentStage + "  = uv" + currentStage + " *(str_" + currentStage + ") - vec2(1.0,1.0)*str_" + currentStage + "*0.5 + vec2(0.5,0.5);\n";	
		}
		if (qShader.qShaderStages[currentStage].tcModTransform != null)
		{
		}
		if (qShader.qShaderStages[currentStage].tcModTurb != null)
		{
		}
		if (qShader.qShaderStages[currentStage].tcModScale != null)
		{
			float SScale = TryToParseFloat(qShader.qShaderStages[currentStage].tcModScale[0]);
			float TScale = TryToParseFloat(qShader.qShaderStages[currentStage].tcModScale[1]);
			TcMod += "uv" + currentStage + " *=vec2(" + SScale.ToString("0.00") + "," + TScale.ToString("0.00") + "); \n";
		}
		if (qShader.qShaderStages[currentStage].tcModScroll != null)
		{
			float SSpeed = TryToParseFloat(qShader.qShaderStages[currentStage].tcModScroll[0]);
			float TSpeed = TryToParseFloat(qShader.qShaderStages[currentStage].tcModScroll[1]);
			TcMod += "uv" + currentStage + " += vec2(" + SSpeed.ToString("0.00") + "," + TSpeed.ToString("0.00") + ") * TIME*0.5; \n";
		}
		return TcMod;
	}

	public static string GetRGBGen(QShaderData qShader, int currentStage)
	{
		string RGBGen = "tex" + currentStage + ".rgb = tex" + currentStage + ".rgb * 1.0 ; \n";
		if (qShader.qShaderStages[currentStage].rgbGen != null)
		{
			if (qShader.qShaderStages[currentStage].rgbGen.Length > 0)
			{
				string Gen = qShader.qShaderStages[currentStage].rgbGen[0];
			}
			if (qShader.qShaderStages[currentStage].rgbGen.Length > 2)
			{
				string RGBFunc = qShader.qShaderStages[currentStage].rgbGen[1];
				float basis = TryToParseFloat(qShader.qShaderStages[currentStage].rgbGen[2]);
				float amp = TryToParseFloat(qShader.qShaderStages[currentStage].rgbGen[3]);
				float phase = TryToParseFloat(qShader.qShaderStages[currentStage].rgbGen[4]);
				float freq = TryToParseFloat(qShader.qShaderStages[currentStage].rgbGen[5]);
				switch (RGBFunc)
				{
					case "SIN":
						RGBGen = "tex" + currentStage + ".rgb = tex" + currentStage + ".rgb * ";
						RGBGen += basis.ToString("0.00") + " + sin((TIME * " + freq.ToString("0.00") + ")*6.28+" + phase.ToString("0.00") + ")  * " + amp.ToString("0.00") + " ; \n";
					break;
					case "SQUARE":
						RGBGen = "tex" + currentStage + ".rgb = tex" + currentStage + ".rgb * ";
						RGBGen += basis.ToString("0.00") + " + sign(sin((TIME  * " + freq.ToString("0.00") + ")*6.28)+" + phase.ToString("0.00") + ")  * " + amp.ToString("0.00") + " ; \n";
					break;
					case "TRIANGLE":
						RGBGen = "tex" + currentStage + ".rgb = tex" + currentStage + ".rgb * (";
						RGBGen += basis.ToString("0.00") + " + abs( 0.5 - mod(TIME  + " + phase.ToString("0.00") + ", 1.0/" + freq.ToString("0.00") + ")+" + phase.ToString("0.00") + ")  * " + amp.ToString("0.00") + ") ; \n";
					break;
					case "SAWTOOTH":
						RGBGen = "tex" + currentStage + ".rgb = tex" + currentStage + ".rgb * (";
						RGBGen += basis.ToString("0.00") + " + mod(TIME  + " + phase.ToString("0.00") + ", 1.0/" + freq.ToString("0.00") + ")+" + phase.ToString("0.00") + "  * " + amp.ToString("0.00") + ") ; \n";
					break;
					case "INVERSESAWTOOTH":
						RGBGen = "tex" + currentStage + ".rgb = tex" + currentStage + ".rgb * (";
						RGBGen += basis.ToString("0.00") + " + ( 1.0 - mod(TIME  + " + phase.ToString("0.00") + ", 1.0/" + freq.ToString("0.00") + ")+" + phase.ToString("0.00") + ")  * " + amp.ToString("0.00") + ") ; \n";
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
				Blend = "color = color + tex" + currentStage + "; \n";
			else if (BlendWhat.Contains("FILTER"))
				Blend = "color = color * tex" + currentStage + "; \n";
			else if (BlendWhat.Contains("BLEND"))
				Blend = "color.rgb = color.rgb * tex" + currentStage + ".a + tex" + currentStage + ".rgb * (1.0 - tex" + currentStage + ".a); \n";
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
				Blend = "color.rgb = color.rgb * " + fdst + " + tex" + currentStage + ".rgb * " + fsrc + "; \n";
			}
		}
		else
			Blend = "color =  tex" + currentStage + "; \n";
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
								line[p++] = c;
						break;
					}
				}
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
	public string[] tcModScale = null;
	public string[] tcModScroll = null;
	public string[] tcModTurb = null;
	public string[] tcModRotate = null;
	public string[] tcModStretch = null;
	public string[] tcModTransform = null;
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
					animMap = Value.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				}
			break;
			case "BLENDFUNC":
				if (blendFunc == null) 
					blendFunc = Value.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				break;
			case "RGBGEN":
				if (rgbGen == null)
					rgbGen = Value.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
			break;
			case "ALPHAGEN":
				if (alphaGen == null)
					alphaGen = Value.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				break;
			case "TCGEN":
				if (tcGen  == null)
					tcGen = Value.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				break;
			case "TCMOD":
			{
				string[] keyValue = Value.Split(' ', 2).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				if (keyValue.Length == 2)
				{
					switch (keyValue[0])
					{
						case "SCALE":
							if (tcModScale == null)
								tcModScale = keyValue[1].Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
								break;
						case "TURB":
							if (tcModTurb == null)
								tcModTurb = keyValue[1].Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
								break;
						case "ROTATE":
							if (tcModRotate == null)
								tcModRotate = keyValue[1].Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
								break;
						case "STRETCH":
							if (tcModStretch ==  null)
								tcModStretch = keyValue[1].Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
								break;
						case "TRANSFORM":
							if (tcModTransform == null)
								tcModTransform = keyValue[1].Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
								break;
						case "SCROLL":
							if (tcModScroll  == null)
								tcModScroll = keyValue[1].Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
								break;
					}
				}
			}
			break;
			case "DEPTHFUNC":
				if (depthFunc  == null)
					depthFunc = Value.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				break;
			case "DEPTHWRITE":
				if (depthWrite == null)
					depthWrite = Value.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				break;
			case "ALPHAFUNC":
				if (alphaFunc == null)
					alphaFunc = Value.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				break;
		}
	}
}

