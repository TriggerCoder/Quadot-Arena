using Godot;
using System;

public partial class AdaptativeTrack : Resource
{
	public int uniqueId;
	[Export]
	public int intesityLevel;
	[Export]
	public bool isRepeatable;
	[Export]
	public AudioStreamOggVorbis TrackFile;
	[Export]
	public bool hasOutro;
	[Export]
	public AudioStreamOggVorbis OutroFile;
}
