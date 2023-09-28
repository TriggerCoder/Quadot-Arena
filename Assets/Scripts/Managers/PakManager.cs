using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
public static class PakManager
{
	public static Dictionary<string, string> ZipFiles = new Dictionary<string, string>();

	public static void LoadPK3Files()
	{
		string path = Directory.GetCurrentDirectory()+"/StreamingAssets/";
		GD.Print("Open Directory " + path);
		DirectoryInfo dir = new DirectoryInfo(path);
		var info = dir.GetFiles("*.PK3").OrderBy(file => Regex.Replace(file.Name, @"\d+", match => match.Value.PadLeft(4, '0')));
		foreach (FileInfo zipfile in info)
		{
			string FileName = path + zipfile.Name;
			GD.Print("Open File " + FileName);
			var reader = new ZipReader();
			var err = reader.Open(FileName);
			string[] zip = reader.GetFiles();
			foreach (string e in zip)
			{
				//Only Files
				if (e.Contains("."))
				{
					string logName = e.ToUpper();
					if (ZipFiles.ContainsKey(logName))
					{
						GD.Print("Updating pak file with name " + logName);
						ZipFiles[logName] = FileName;
					}
					else
						ZipFiles.Add(logName, FileName);
				}
			}
			reader.Close();
		}
	}
}
