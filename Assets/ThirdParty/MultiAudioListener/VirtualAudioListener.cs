using Godot;
using System;

public partial class VirtualAudioListener : Node3D
{
	public float Volume = 1.0f;
	public int Num = 0;
	public override void _EnterTree()
	{
		MultiAudioListener.AddVirtualAudioListener(this);
	}

	public override void _ExitTree()
	{
		MultiAudioListener.RemoveVirtualAudioListener(this);
	}
}
