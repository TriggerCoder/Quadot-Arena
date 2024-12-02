using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Win32;
using ExtensionMethods;

public static class PakManager
{
	public static Dictionary<string, string> ZipFiles = new Dictionary<string, string>();
	public static Dictionary<string, FileStream> QuakeFiles = new Dictionary<string, FileStream>();
	private static Dictionary<string, ZipArchive> OpenedZippedFiles = new Dictionary<string, ZipArchive>();
	private static List<FileStream> OpenedPK3Files = new List<FileStream>();
	public static Dictionary<string, int> EntryByIndex = new Dictionary<string, int>();

	public static List<string> mapList = new List<string>();
	public static List<string> playerModelList = new List<string>();
	public static List<string> playerSkinList = new List<string>();

	private const string pak0Demo = "0613b3d4ef05e613a2b470571498690f";		//pak0.pk3 QUAKE 3 DEMO
	private const string pak0Retail = "1197ca3df1e65f3c380f8abc10ca43bf";	//pak0.pk3 QUAKE 3
	private const string pak1Patch = "48911719d91be25adb957f2d325db4a0";	//pak1.pk3 QUAKE 3
	private const string pak2Patch = "d550ce896130c47166ca44b53f8a670a";	//pak2.pk3 QUAKE 3
	private const string pak3Patch = "968dfd0f30dad67056115c8e92344ddc";	//pak3.pk3 QUAKE 3
	private const string pak4Patch = "24bb1f4fcabd95f6e320c0e2f62f19ca";	//pak4.pk3 QUAKE 3
	private const string pak5Patch = "734dcd06d2cbc7a16432ff6697f1c5ba";	//pak5.pk3 QUAKE 3
	private const string pak6Patch = "873888a73055c023f6c38b8ca3f2ce05";	//pak6.pk3 QUAKE 3
	private const string pak7Patch = "8fd38c53ed814b64f6ab03b5290965e4";	//pak7.pk3 QUAKE 3
	private const string pak8Patch = "d8b96d429ca4a9c289071cb7e77e14d2";	//pak8.pk3 QUAKE 3
	private const string pak0TA = "e8ba9e3bf06210930bc0e7fdbcdd01c2";		//pak0.pk3 QUAKE 3 TEAM ARENA
	private const string pak00Live = "75aaae7c836b9ebdb1d4cfd53ba1c958";	//pak00.pk3 QUAKE LIVE

	private static readonly string[] basePaks = { pak0Demo, pak0Retail, pak1Patch, pak2Patch, pak3Patch, pak4Patch, pak5Patch, pak6Patch, pak7Patch, pak8Patch, pak0TA, pak00Live};
	public static GameManager.BasePak basePak = GameManager.BasePak.All;
	public static void LoadPK3Files()
	{
		string filePath;
		DirectoryInfo dir;
		FileInfo[] files;

		//Try to get Steam Pk3 First
		if (OperatingSystem.IsWindows())
		{
			string SteamPath = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null);
			if (SteamPath != null)
			{
				SteamPath = SteamPath.Replace("\\", "/");
				//Check Quake Live
				if (File.Exists(SteamPath + "/steamapps/appmanifest_282440.acf"))
				{
					filePath = SteamPath + "/steamapps/common/Quake Live/baseq3/";
					GameManager.Print("Open Directory " + filePath);
					dir = new DirectoryInfo(filePath);
					if (dir.Exists)
					{
						files = dir.GetFiles("*.pk3");
						CheckPK3Files(filePath, files);
					}
				}
				//Check Quake 3
				if (File.Exists(SteamPath + "/steamapps/appmanifest_2200.acf"))
				{
					filePath = SteamPath + "/steamapps/common/Quake 3 Arena/baseq3/";
					GameManager.Print("Open Directory " + filePath);
					dir = new DirectoryInfo(filePath);
					if (dir.Exists)
					{
						files = dir.GetFiles("*.pk3");
						CheckPK3Files(filePath, files);
					}
					//Check TeamArena
					filePath = SteamPath + "/steamapps/common/Quake 3 Arena/missionpack/";
					GameManager.Print("Open Directory " + filePath);
					dir = new DirectoryInfo(filePath);
					if (dir.Exists)
					{
						files = dir.GetFiles("*.pk3");
						CheckPK3Files(filePath, files);
					}
				}
			}
		}

		filePath = Directory.GetCurrentDirectory() + "/StreamingAssets/";
		GameManager.Print("Open Directory " + filePath);
		dir = new DirectoryInfo(filePath);
		files = dir.GetFiles("*.pk3");
		CheckPK3Files(filePath, files);

		int start = 0;
		int end = basePaks.Length;
		if (GameManager.Instance.gameConfig.GameSelect == GameManager.BasePak.All)
			GameManager.Instance.gameConfig.GameSelect = basePak;
		else
		{
			if (GameManager.Instance.gameConfig.GameSelect > basePak)
				GameManager.Instance.gameConfig.GameSelect = basePak;
			else
				basePak = GameManager.Instance.gameConfig.GameSelect;

			switch (basePak)
			{
				default:
				{
					LogError("NO CORRECT PAK0.PK3 FOUND IN " + filePath);
					GameManager.QuitGame();
					return;
				}
				break;
				case GameManager.BasePak.Demo:
					end = 1;
				break;
				case GameManager.BasePak.Quake3:
					start = 1;
					end = 10;
				break;
				case GameManager.BasePak.TeamArena:
					start = 1;
					end = 11;
				break;
				case GameManager.BasePak.QuakeLive:
					start = 11;
					end = 12;
				break;
			}
		}
		for (int i = 0; i < basePaks.Length; i++) 
		{
			if (QuakeFiles.TryGetValue(basePaks[i], out FileStream file))
			{
				if ((i >= start) && (i < end))
					AddPK3Files(file);
				QuakeFiles.Remove(basePaks[i]);
			}
		}

		foreach (FileStream file in QuakeFiles.Values)
			AddPK3Files(file);
		QuakeFiles.Clear();
	}

	public static void CheckPK3Files(string path, FileInfo[] files)
	{
		var fileList = files.OrderBy(file => Regex.Replace(file.Name, @"\d+", match => match.Value.PadLeft(4, '0')));
		foreach (FileInfo zipfile in files)
		{
			string FileName = path + zipfile.Name;

			FileStream file = File.Open(FileName, FileMode.Open, FileAccess.Read);
			AddPK3(file);
		}
	}

	public static void AddPK3Files(FileStream file)
	{
		ZipArchive reader = new ZipArchive(file, ZipArchiveMode.Read);
		GameManager.Print("Checking file " + file.Name);


		for (int index = 0; index < reader.Entries.Count; index++)
		{
			ZipArchiveEntry e = reader.Entries[index];
			//Only Files
			if (e.FullName.Contains("."))
			{
				string logName = e.FullName.ToUpper();
				//Process Shaders and continue
				if (logName.Contains(".SHADER"))
				{
					QShaderManager.ReadShaderData(e.Open());
					continue;
				}

				if (ZipFiles.ContainsKey(logName))
				{
//					GameManager.Print("Updating pak file with name " + logName);
					ZipFiles[logName] = file.Name;
					EntryByIndex[logName] = index;
				}
				else
				{
					ZipFiles.Add(logName, file.Name);
					EntryByIndex.Add(logName, index);
				}

				if (logName.Contains(".BSP"))
					AddMapToList(logName);
				else if (logName.Contains(".SKIN"))
					AddPlayerSkin(logName);
			}
		}
		reader.Dispose();
	}
	public static string Md5Sum(FileStream file)
	{
		// encrypt bytes
		MD5 md5 = MD5.Create();
		byte[] hashBytes = md5.ComputeHash(file);

		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";

		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
		}
		return hashString.PadLeft(32, '0');
	}

	public static void AddPK3(FileStream pak)
	{
		string md5 = Md5Sum(pak);
		
		switch (md5)
		{
			default:
			{
				if (!QuakeFiles.ContainsKey(md5))
				{
					QuakeFiles.Add(md5, pak);
					GameManager.Print("Adding File: " + pak.Name + " MD5: " + md5);
				}
			}
			break;
			case pak00Live:
			{
				if (!QuakeFiles.ContainsKey(md5))
				{
					QuakeFiles.Add(md5, pak);
					GameManager.Print("Adding File: " + pak.Name + " is QUAKE Live BasePak");
				}
				basePak = GameManager.BasePak.QuakeLive;
			}
			break;
			case pak0Retail:
			{
				if (!QuakeFiles.ContainsKey(md5))
				{
					QuakeFiles.Add(md5, pak);
					GameManager.Print("Adding File: " + pak.Name + " is QUAKE3 BasePak");
				}
				if (basePak < GameManager.BasePak.TeamArena)
					basePak = GameManager.BasePak.Quake3;
			}
			break;
			case pak1Patch:
			case pak2Patch:
			case pak3Patch:
			case pak4Patch:
			case pak5Patch:
			case pak6Patch:
			case pak7Patch:
			case pak8Patch:
			{
				if (!QuakeFiles.ContainsKey(md5))
				{
					QuakeFiles.Add(md5, pak);
					GameManager.Print("Adding File: " + pak.Name + " is QUAKE3 patch loading");
				}
			}
			break;
			case pak0TA:
			{
				if (!QuakeFiles.ContainsKey(md5))
				{
					QuakeFiles.Add(md5, pak);
					GameManager.Print("Adding File: " + pak.Name + " is QUAKE3 Team Arena");
				}
				if (basePak != GameManager.BasePak.QuakeLive)
					basePak = GameManager.BasePak.TeamArena;
			}
			break;
			case pak0Demo:
			{
				if (!QuakeFiles.ContainsKey(md5))
				{
					QuakeFiles.Add(md5, pak);
					GameManager.Print("Adding File: " + pak.Name + " is QUAKE3 Demo");
				}
				if (basePak == GameManager.BasePak.All)
					basePak = GameManager.BasePak.Demo;
			}
			break;
		}
	}

	public static void AddMapToList(string mapName)
	{
		string[] fullPath = mapName.Split('/');
		if (fullPath.Length > 1)
			mapName = fullPath[1];
		else
			mapName = fullPath[0];
		mapName = mapName.StripExtension();
		if (!mapList.Contains(mapName))
			mapList.Add(mapName);
	}

	public static void OrderLists()
	{
		mapList = mapList.OrderBy(mapName => mapName).ToList();
		playerModelList = playerModelList.OrderBy(modelName => modelName).ToList();
		playerSkinList = playerSkinList.OrderBy(skinName => skinName).ToList();
	}

	public static void KeepDemoList(List<string> demoList)
	{
		mapList = demoList.OrderBy(mapName => mapName).ToList();
	}

	public static void AddPlayerModels(string path)
	{
		string[] strword = path.Split('/');
		if (strword.Length > 3)
		{
			string model = strword[2];
			if (!playerModelList.Contains(model))
				playerModelList.Add(model);
		}
	}

	public static void AddPlayerSkin(string path)
	{
		if (!path.Contains("LOWER_"))
			return;

		string skin = path.StripExtension().Replace("LOWER_", "");
		if (!playerSkinList.Contains(skin))
			playerSkinList.Add(skin);

		AddPlayerModels(skin);
	}

	public static void LogError(string error)
	{
		string errorFileName = Directory.GetCurrentDirectory() + "/error.log";
		FileStream errorFile = File.Open(errorFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
		errorFile.Seek(0, SeekOrigin.End);
		byte[] writeError = System.Text.Encoding.UTF8.GetBytes(DateTime.Now.ToString() + " : " + error + "\n").ToArray();
		errorFile.Write(writeError);
		errorFile.Close();
	}

	public static List<string> LoadMapRotation()
	{
		string FileName = "/";
		switch(basePak)
		{
			default:
			case GameManager.BasePak.Demo:
				return new List<string>();
			break;
			case GameManager.BasePak.Quake3:
				FileName += "q3_";
			break;
			case GameManager.BasePak.TeamArena:
				FileName += "ta_";
			break;
			case GameManager.BasePak.QuakeLive:
				FileName += "ql_";
			break;
		}
		switch (GameManager.Instance.gameConfig.GameType)
		{
			default:
			case GameManager.GameType.FreeForAll:
			case GameManager.GameType.QuadHog:
				FileName += "ffa.txt";
			break;
			case GameManager.GameType.Tournament:
			case GameManager.GameType.SinglePlayer:
				FileName += "duel.txt";
			break;
			case GameManager.GameType.TeamDeathmatch:
				FileName += "tdm.txt";
			break;
			case GameManager.GameType.CaptureTheFlag:
			case GameManager.GameType.OneFlagCTF:
			case GameManager.GameType.Overload:
			case GameManager.GameType.Harvester:
				FileName += "ctf.txt";
			break;
		}
		string rotationFileName = Directory.GetCurrentDirectory() + FileName;

		if (!File.Exists(rotationFileName))
			return new List<string>();

		FileStream rotationFile = File.Open(rotationFileName, FileMode.Open, FileAccess.Read);
		StreamReader stream = new StreamReader(rotationFile);
		string mapName;

		List<string> mapRotation = new List<string>();
		stream.BaseStream.Seek(0, SeekOrigin.Begin);

		while (!stream.EndOfStream)
		{
			mapName = stream.ReadLine().Split('|')[0].ToUpper();
			if (mapList.Contains(mapName))
				mapRotation.Add(mapName);
		}
		stream.Close();
		rotationFile.Close();

		return mapRotation;
	}

	public static byte[] GetPK3FileData(string FileName, string PK3FileName)
	{
		ZipArchive reader;
		if (!OpenedZippedFiles.TryGetValue(PK3FileName, out reader))
		{
			FileStream file = File.Open(PK3FileName, FileMode.Open, FileAccess.Read);
			reader = new ZipArchive(file, ZipArchiveMode.Read);
			OpenedZippedFiles.Add(PK3FileName, reader);
			OpenedPK3Files.Add(file);
		}

		ZipArchiveEntry entry = reader.Entries[EntryByIndex[FileName]];

		using (MemoryStream ms = new MemoryStream())
		{
			entry.Open().CopyTo(ms);
			return ms.ToArray();
		}
	}

	public static void ClosePK3Files()
	{
		foreach (ZipArchive reader in OpenedZippedFiles.Values)
			reader.Dispose();
		OpenedZippedFiles.Clear();

		foreach (FileStream file in OpenedPK3Files)
			file.Dispose();
		OpenedPK3Files.Clear();
	}
}
