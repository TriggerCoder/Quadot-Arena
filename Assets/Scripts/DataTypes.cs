using Godot;
using System.IO;
using System.Text.Json.Serialization;
public class BSPHeader
{
	private const int LumpCount = 17;

	private readonly BinaryReader BSP;

	public BSPHeader(BinaryReader BSP)
	{
		this.BSP = BSP;

		ReadMagic();
		ReadVersion();
		ReadLumps();
	}

	public BSPDirectoryEntry[] Directory { get; set; }

	public string Magic { get; private set; }

	public uint Version { get; private set; }

	public string PrintInfo()
	{
		string blob = "\r\n=== BSP Header =====\r\n";
		blob += "Magic Number: " + Magic + "\r\n";
		blob += "BSP Version: " + Version + "\r\n";
		blob += "Header Directory:\r\n";
		int count = 0;
		foreach (BSPDirectoryEntry entry in Directory)
		{
			blob += "Lump " + count + ": " + entry.Name + " Offset: " + entry.Offset + " Length: " + entry.Length +
					"\r\n";
			count++;
		}

		return blob;
	}

	private void ReadLumps()
	{
		Directory = new BSPDirectoryEntry[LumpCount];
		for (int i = 0; i < 17; i++)
			Directory[i] = new BSPDirectoryEntry(BSP.ReadInt32(), BSP.ReadInt32());

		Directory[LumpType.Entities].Name = "Entities";
		Directory[LumpType.Shaders].Name = "Shaders";
		Directory[LumpType.Planes].Name = "Planes";
		Directory[LumpType.Nodes].Name = "Nodes";
		Directory[LumpType.Leafs].Name = "Leafs";
		Directory[LumpType.LeafSurfaces].Name = "Leaf Surfaces";
		Directory[LumpType.LeafBrushes].Name = "Leaf Brushes";
		Directory[LumpType.Models].Name = "Models";
		Directory[LumpType.Brushes].Name = "Brushes";
		Directory[LumpType.BrushSides].Name = "Brush Sides";
		Directory[LumpType.Vertexes].Name = "Vertexes";
		Directory[LumpType.VertIndices].Name = "Vertexes Indices";
		Directory[LumpType.Effects].Name = "Effects";
		Directory[LumpType.Surfaces].Name = "Surfaces";
		Directory[LumpType.LightMaps].Name = "Light Maps";
		Directory[LumpType.LightGrid].Name = "Light Grid";
		Directory[LumpType.VisData].Name = "Vis data";
	}

	private void ReadMagic()
	{
		BSP.BaseStream.Seek(0, SeekOrigin.Begin);
		Magic = new string(BSP.ReadChars(4));
	}

	private void ReadVersion()
	{
		BSP.BaseStream.Seek(4, SeekOrigin.Begin);
		Version = BSP.ReadUInt32();
	}
}
public class BSPDirectoryEntry
{
	public BSPDirectoryEntry(int offset, int length)
	{
		Offset = offset;
		Length = length;
	}

	public int Offset { get; }

	public int Length { get; }

	public string Name { get; set; }

	public bool Validate()
	{
		if (Length % 4 == 0)
			return true;
		return false;
	}
}

[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true, NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString, ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip)]
[JsonSerializable(typeof(PlayerInfo.PlayerConfigData))]
[JsonSerializable(typeof(GameManager.GameConfigData))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}