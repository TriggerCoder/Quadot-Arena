using Godot;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
public static class QShaderManager
{
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

	public static void ReadShaderData(byte[] shaderData)
	{
		MemoryStream ms = new MemoryStream(shaderData);
		StreamReader stream = new StreamReader(ms);
		string strWord;
		char[] line = new char[1024];

		stream.BaseStream.Seek(0, SeekOrigin.Begin);

		int p;
		int stage = -1;
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
								GD.Print("Shader already on the list: " + qShaderData.Name);
								QShaders[qShaderData.Name] = qShaderData;
							}
							else
								QShaders.Add(qShaderData.Name, qShaderData);
							qShaderData = null;
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
			strWord = new string(line);
			
			if (strWord.Length == 0)
				continue;

			if (qShaderData == null)
			{
				if (node == 0)
				{
					qShaderData = new QShaderData();
					qShaderData.Name = strWord.Trim(' ');
				}
				continue;
			}

			strWord = strWord.ToUpper();

			string[] keyValue = strWord.Split(new[] { ' ' }, 2);

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
				keyValue[1] = new string(line);

				if (keyValue[0].Length > 0)
				{
					if (node == 1)
						qShaderData.AddGlobal(keyValue[0], keyValue[1]);
					if (node == 2)
						qShaderData.AddStage(keyValue[0], keyValue[1]);
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

	public void AddStage(string Params, string Value)
	{
		QShaderStage qShaderStage = new QShaderStage();
		qShaderStage.AddStage(Params, Value);
		qShaderStages.Add(qShaderStage);
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
	public List<string> map = null;
	public List<string> clampMap = null;
	public List<string> animMap = null;
	public List<string> blendFunc = null;
	public List<string> rgbGen = null;
	public List<string> alphaGen = null;
	public List<string> tcGen = null;
	public List<string> tcModScale = null;
	public List<string> tcModScroll = null;
	public List<string> tcModTurb = null;
	public List<string> tcModRotate = null;
	public List<string> tcModStretch = null;
	public List<string> tcModTransform = null;
	public List<string> depthFunc = null;
	public List<string> depthWrite = null;
	public List<string> alphaFunc = null;

	public void AddStage(string Params, string Value)
	{
		switch (Params)
		{
			case "MAP":
				if (map == null)
					map = new List<string>();
				map.Add(Value);
			break;
			case "CLAMPMAP":
				if (clampMap == null) 
					clampMap = new List<string>(); 
				clampMap.Add(Value);
			break;
			case "ANIMMAP":
				if (animMap == null) 
					animMap = new List<string>();
				animMap.Add(Value);
			break;
			case "BLENDFUNC":
				if (blendFunc == null) 
					blendFunc = new List<string>();
				blendFunc.Add(Value);
			break;
			case "RGBGEN":
				if (rgbGen == null)
					rgbGen = new List<string>();
				rgbGen.Add(Value);
			break;
			case "ALPHAGEN":
				if (alphaGen == null)
					alphaGen = new List<string>();
				alphaGen.Add(Value);
			break;
			case "TCGEN":
				if (tcGen  == null)
					tcGen = new List<string>();
				tcGen.Add(Value);
			break;
			case "TCMOD":
			{
				string[] keyValue = Value.Split(new[] { ' ' }, 2);
				if (keyValue.Length == 2)
				{
					switch (keyValue[0])
					{
						case "SCALE":
							if (tcModScale == null)
								tcModScale = new List<string>();
							tcModScale.Add(keyValue[1]);
						break;
						case "TURB":
							if (tcModTurb == null)
								tcModTurb = new List<string>();
							tcModTurb.Add(keyValue[1]);
						break;
						case "ROTATE":
							if (tcModRotate == null)
								tcModRotate = new List<string>();
							tcModRotate.Add(keyValue[1]);
						break;
						case "STRETCH":
							if (tcModStretch ==  null)
								tcModStretch = new List<string>();
							tcModStretch.Add(keyValue[1]);
						break;
						case "TRANSFORM":
							if (tcModTransform == null)
								tcModTransform = new List<string>();
							tcModTransform.Add(keyValue[1]);
						break;
						case "SCROLL":
							if (tcModScroll  == null)
								tcModScroll = new List<string>();
							tcModScroll.Add(keyValue[1]);
						break;
					}
				}
			}
			break;
			case "DEPTHFUNC":
				if (depthFunc  == null)
					depthFunc = new List<string>();
				depthFunc.Add(Value);
			break;
			case "DEPTHWRITE":
				if (depthWrite == null)
					depthWrite = new List<string>();
				depthWrite.Add(Value);
			break;
			case "ALPHAFUNC":
				if (alphaFunc == null)
					alphaFunc = new List<string>();
				alphaFunc.Add(Value);
			break;
		}
	}
}

