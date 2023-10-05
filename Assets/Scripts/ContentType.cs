using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class ContentType : Node
{
	public uint value;
	[Export]
	public bool Solid = false;
	[Export]
	public bool Lava = false;
	[Export]
	public bool Slime = false;
	[Export]
	public bool Water = false;
	[Export]
	public bool Fog = false;
	[Export]
	public bool AreaPortal = false;
	[Export]
	public bool PlayerClip = false;
	[Export]
	public bool MonsterClip = false;
	[Export]
	public bool Teleporter = false;
	[Export]
	public bool JumpPad = false;
	[Export]
	public bool ClusterPortal = false;
	[Export]
	public bool BotsNotEnter = false;
	[Export]
	public bool Origin = false;
	[Export]
	public bool Body = false;
	[Export]
	public bool Corpse = false;
	[Export]
	public bool Details = false;
	[Export]
	public bool Structural = false;
	[Export]
	public bool Translucent = false;
	[Export]
	public bool Trigger = false;
	[Export]
	public bool NoDrop = false;
	public void Init(uint contentType)
	{
		value = contentType;
		Name = "ContentType: "+value;
		if ((contentType & ContentFlags.Solid) != 0)
			Solid = true;
		if ((contentType & ContentFlags.Lava) != 0)
			Lava = true;
		if ((contentType & ContentFlags.Slime) != 0)
			Slime = true;
		if ((contentType & ContentFlags.Water) != 0)
			Water = true;
		if ((contentType & ContentFlags.Fog) != 0)
			Fog = true;
		if ((contentType & ContentFlags.AreaPortal) != 0)
			AreaPortal = true;
		if ((contentType & ContentFlags.PlayerClip) != 0)
			PlayerClip = true;
		if ((contentType & ContentFlags.MonsterClip) != 0)
			MonsterClip = true;
		if ((contentType & ContentFlags.Teleporter) != 0)
			Teleporter = true;
		if ((contentType & ContentFlags.JumpPad) != 0)
			JumpPad = true;
		if ((contentType & ContentFlags.ClusterPortal) != 0)
			ClusterPortal = true;
		if ((contentType & ContentFlags.BotsNotEnter) != 0)
			BotsNotEnter = true;
		if ((contentType & ContentFlags.Origin) != 0)
			Origin = true;
		if ((contentType & ContentFlags.Body) != 0)
			Body = true;
		if ((contentType & ContentFlags.Corpse) != 0)
			Corpse = true;
		if ((contentType & ContentFlags.Details) != 0)
			Details = true;
		if ((contentType & ContentFlags.Structural) != 0)
			Structural = true;
		if ((contentType & ContentFlags.Translucent) != 0)
			Translucent = true;
		if ((contentType & ContentFlags.Trigger) != 0)
			Trigger = true;
		if ((contentType & ContentFlags.NoDrop) != 0)
			NoDrop = true;
	}

}
