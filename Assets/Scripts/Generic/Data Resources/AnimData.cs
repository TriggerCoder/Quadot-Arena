using Godot;
using System;
public partial class AnimData : Resource
{
	[Export]
	public float fps;
	[Export]
	public float lerpTime;
	[Export]
	public float currentLerpTime;
}
