using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Win32;

public static class PakManager
{
	public static Dictionary<string, string> ZipFiles = new Dictionary<string, string>();
	public static Dictionary<string, FileStream> QuakeFiles = new Dictionary<string, FileStream>();

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
		if (GameManager.Instance.gameSelect != GameManager.BasePak.All)
		{
			if (GameManager.Instance.gameSelect > basePak)
				GameManager.Instance.gameSelect = basePak;
			else
				basePak = GameManager.Instance.gameSelect;

			switch (basePak)
			{
				default:
					GameManager.Print("NO CORRECT PAK0.PK3 FOUND");
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

			FileStream file = new FileStream(FileName, FileMode.Open);
			AddPK3(file);
		}
	}

	public static void AddPK3Files(FileStream file)
	{
		ZipArchive reader = new ZipArchive(file, ZipArchiveMode.Read);
		GameManager.Print("Checking file " + file.Name);
		foreach (ZipArchiveEntry e in reader.Entries)
		{
			//Only Files
			if (e.FullName.Contains("."))
			{
				string logName = e.FullName.ToUpper();
				if (ZipFiles.ContainsKey(logName))
				{
					GameManager.Print("Updating pak file with name " + logName);
					ZipFiles[logName] = file.Name;
				}
				else
					ZipFiles.Add(logName, file.Name);

				//Add Shaders Files for processing
				if (logName.Contains(".SHADER"))
					QShaderManager.AddShaderFiles(logName);
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
}
