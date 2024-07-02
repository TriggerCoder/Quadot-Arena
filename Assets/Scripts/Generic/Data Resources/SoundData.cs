using Godot;
using System;

public partial class SoundData : Resource
{
	[Export]
	public string name;
	[Export]
	public AudioStream sound;
}
