using Godot;
using System.Collections;
using System.Collections.Generic;

public class ContentType
{
	public uint value;
	public bool Solid = false;
	public bool Lava = false;
	public bool Slime = false;
	public bool Water = false;
	public bool Fog = false;
	public bool AreaPortal = false;
	public bool PlayerClip = false;
	public bool MonsterClip = false;
	public bool Teleporter = false;
	public bool JumpPad = false;
	public bool ClusterPortal = false;
	public bool BotsNotEnter = false;
	public bool Origin = false;
	public bool Body = false;
	public bool Corpse = false;
	public bool Details = false;
	public bool Structural = false;
	public bool Translucent = false;
	public bool Trigger = false;
	public bool NoDrop = false;
	public void Init(uint contentType)
	{
		value = contentType;
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
