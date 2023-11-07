using Godot;
using System;

public partial class ThingController : Node3D
{
	public Vector3 location;
	public Quaternion angularrotation;

	[Export]
	public string respawnSound = "items/respawn1";
	[Export]
	public float respawnTime;
	public enum ThingType
	{
		Decor, //non-blocking, non-interactive
		Blocking, //blocking or interactive
		Item,
		Teleport,
		JumpPad,
		TargetDestination,
		Trigger,
		Door,
		Player
	}
	[Export]
	public ThingType thingType = ThingType.Decor;
}
