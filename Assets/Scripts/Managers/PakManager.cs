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
				bool loaded = false;
				SteamPath = SteamPath.Replace("\\", "/");
				//Check Quake Live First
				if (File.Exists(SteamPath + "/steamapps/appmanifest_282440.acf"))
				{
					filePath = SteamPath + "/steamapps/common/Quake Live/baseq3/";
					GameManager.Print("Open Directory " + filePath);
					dir = new DirectoryInfo(filePath);
					if (dir.Exists)
					{
						files = dir.GetFiles("*.pk3");
						foreach (var file in files)
						{
							if (file.Name == "pak00.pk3")
								loaded = true;
						}
						AddPK3Files(filePath, files);
					}
				}
				//Check Quake 3
				if ((!loaded) && (File.Exists(SteamPath + "/steamapps/appmanifest_2200.acf")))
				{
					filePath = SteamPath + "/steamapps/common/Quake 3 Arena/baseq3/";
					GameManager.Print("Open Directory " + filePath);
					dir = new DirectoryInfo(filePath);
					if (dir.Exists)
					{
						files = dir.GetFiles("*.pk3");
						AddPK3Files(filePath, files);
					}
					//Check TeamArena
					filePath = SteamPath + "/steamapps/common/Quake 3 Arena/missionpack/";
					GameManager.Print("Open Directory " + filePath);
					dir = new DirectoryInfo(filePath);
					if (dir.Exists)
					{
						files = dir.GetFiles("*.pk3");
						AddPK3Files(filePath, files);
					}
				}
			}
		}

		filePath = Directory.GetCurrentDirectory() + "/StreamingAssets/";
		GameManager.Print("Open Directory " + filePath);
		dir = new DirectoryInfo(filePath);
		files = dir.GetFiles("*.pk3");
		AddPK3Files(filePath, files);
	}

	public static void AddPK3Files(string path, FileInfo[] files)
	{
		var fileList = files.OrderBy(file => Regex.Replace(file.Name, @"\d+", match => match.Value.PadLeft(4, '0')));
		foreach (FileInfo zipfile in files)
		{
			string FileName = path + zipfile.Name;

			FileStream file = new FileStream(FileName, FileMode.Open);

			if (!CheckPak(file))
				continue;

			ZipArchive reader = new ZipArchive(file, ZipArchiveMode.Read);
			foreach (ZipArchiveEntry e in reader.Entries)
			{
				//Only Files
				if (e.FullName.Contains("."))
				{
					string logName = e.FullName.ToUpper();
					if (ZipFiles.ContainsKey(logName))
					{
						GameManager.Print("Updating pak file with name " + logName);
						ZipFiles[logName] = FileName;
					}
					else
						ZipFiles.Add(logName, FileName);

					//Add Shaders Files for processing
					if (logName.Contains(".SHADER"))
						QShaderManager.AddShaderFiles(logName);
				}
			}
			reader.Dispose();
		}
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

	public static bool CheckPak(FileStream pak)
	{
		bool load = true;
		string md5 = Md5Sum(pak);
		
		switch (md5)
		{
			default:
				GameManager.Print("File: " + pak.Name + " MD5: " + md5);
			break;
			case "75aaae7c836b9ebdb1d4cfd53ba1c958": //pak00.pk3
				GameManager.Instance.basePak = GameManager.BasePak.QuakeLive;
				GameManager.Print("File: " + pak.Name + " is QUAKE Live BasePak loading: " + load);
				break;
			case "1197ca3df1e65f3c380f8abc10ca43bf": //pak0.pk3
				if (GameManager.Instance.basePak == GameManager.BasePak.QuakeLive)
					load = false;
				else
					GameManager.Instance.basePak = GameManager.BasePak.Quake3;
				GameManager.Print("File: " + pak.Name + " is QUAKE3 BasePak loading: " + load);
				break;
			case "48911719d91be25adb957f2d325db4a0": //pak1.pk3
			case "d550ce896130c47166ca44b53f8a670a": //pak2.pk3
			case "968dfd0f30dad67056115c8e92344ddc": //pak3.pk3
			case "24bb1f4fcabd95f6e320c0e2f62f19ca": //pak4.pk3
			case "734dcd06d2cbc7a16432ff6697f1c5ba": //pak5.pk3
			case "873888a73055c023f6c38b8ca3f2ce05": //pak6.pk3
			case "8fd38c53ed814b64f6ab03b5290965e4": //pak7.pk3
			case "d8b96d429ca4a9c289071cb7e77e14d2": //pak8.pk3
				if (GameManager.Instance.basePak == GameManager.BasePak.QuakeLive)
					load = false;
				GameManager.Print("File: " + pak.Name + " is QUAKE3 patch loading: " + load);
				break;
			case "e8ba9e3bf06210930bc0e7fdbcdd01c2": //pak0.pk3
				if (GameManager.Instance.basePak != GameManager.BasePak.Quake3)
					load = false;
				else
					GameManager.Instance.basePak = GameManager.BasePak.TeamArena;
				GameManager.Print("File: " + pak.Name + " is QUAKE3 Team Arena loading: " + load);
				break;
			case "0613b3d4ef05e613a2b470571498690f": //pak0.pk3
				if (GameManager.Instance.basePak != GameManager.BasePak.None)
					load = false;
				else
					GameManager.Instance.basePak = GameManager.BasePak.Demo;
				GameManager.Print("File: " + pak.Name + " is QUAKE3 Demo loading: "+ load);
				break;
		}
		return load;
	}

}
