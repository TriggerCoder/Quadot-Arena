using Godot;
using System.Collections;
using System.Collections.Generic;

public class SurfaceType
{
	public uint value;
	public bool NoFallDamage = false;
	public bool Slick = false;
	public bool Sky = false;
	public bool Ladder = false;
	public bool NoImpact = false;
	public bool NoMarks = false;
	public bool Flesh = false;
	public bool NoDraw = false;
	public bool Hint = false;
	public bool Skip = false;
	public bool NoLightMap = false;
	public bool PointLight = false;
	public bool MetalSteps = false;
	public bool NoSteps = false;
	public bool NonSolid = false;
	public bool LightFilter = false;
	public bool AlphaShadow = false;
	public bool NoDynLight = false;

	public void Init(uint surfaceType)
	{
		value = surfaceType;
		if ((surfaceType & SurfaceFlags.NoFallDamage) != 0)
			NoFallDamage = true;
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