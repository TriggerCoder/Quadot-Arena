using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class SurfaceType : Node
{
	public uint value;
	[Export]
	public bool NoDamage = false;
	[Export]
	public bool Slick = false;
	[Export]
	public bool Sky = false;
	[Export]
	public bool Ladder = false;
	[Export]
	public bool NoImpact = false;
	[Export]
	public bool NoMarks = false;
	[Export]
	public bool Flesh = false;
	[Export]
	public bool NoDraw = false;
	[Export]
	public bool Hint = false;
	[Export]
	public bool Skip = false;
	[Export]
	public bool NoLightMap = false;
	[Export]
	public bool PointLight = false;
	[Export]
	public bool MetalSteps = false;
	[Export]
	public bool NoSteps = false;
	[Export]
	public bool NonSolid = false;
	[Export]
	public bool LightFilter = false;
	[Export]
	public bool AlphaShadow = false;
	[Export]
	public bool NoDynLight = false;

	public void Init(uint surfaceType)
	{
		value = surfaceType;
		Name = "SurfaceType: " + value;
		if ((surfaceType & SurfaceFlags.NoDamage) != 0)
			NoDamage = true;
		if ((surfaceType & SurfaceFlags.Slick) != 0)
			Slick = true;
		if ((surfaceType & SurfaceFlags.Sky) != 0)
			Sky = true;
		if ((surfaceType & SurfaceFlags.Ladder) != 0)
			Ladder = true;
		if ((surfaceType & SurfaceFlags.NoImpact) != 0)
			NoImpact = true;
		if ((surfaceType & SurfaceFlags.NoMarks) != 0)
			NoMarks = true;
		if ((surfaceType & SurfaceFlags.Flesh) != 0)
			Flesh = true;
		if ((surfaceType & SurfaceFlags.NoDraw) != 0)
			NoDraw = true;
		if ((surfaceType & SurfaceFlags.Hint) != 0)
			Hint = true;
		if ((surfaceType & SurfaceFlags.Skip) != 0)
			Skip = true;
		if ((surfaceType & SurfaceFlags.NoLightMap) != 0)
			NoLightMap = true;
		if ((surfaceType & SurfaceFlags.PointLight) != 0)
			PointLight = true;
		if ((surfaceType & SurfaceFlags.MetalSteps) != 0)
			MetalSteps = true;
		if ((surfaceType & SurfaceFlags.NoSteps) != 0)
			NoSteps = true;
		if ((surfaceType & SurfaceFlags.NonSolid) != 0)
			NonSolid = true;
		if ((surfaceType & SurfaceFlags.LightFilter) != 0)
			LightFilter = true;
		if ((surfaceType & SurfaceFlags.AlphaShadow) != 0)
			AlphaShadow = true;
		if ((surfaceType & SurfaceFlags.NoDynLight) != 0)
			NoDynLight = true;
	}
}