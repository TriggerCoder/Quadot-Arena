using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public partial class MultiAudioListener : Node3D
{
	private static List<VirtualAudioListener> _virtualAudioListeners = new List<VirtualAudioListener>();
	public static event Action<VirtualAudioListener> OnVirtualAudioListenerAdded;
	public static event Action<VirtualAudioListener> OnVirtualAudioListenerRemoved;

	public static MultiAudioListener Main;

	//AudioStraem pool
	//We limit the amount of items in the audio source pool. This number can be changed
	private const int MaximumItemsAudioStreamPool = 512;
	//We limit the amount of audio listener to 8 SplitScreen
	private const int MaximumVirtualAudioListener = 8;
	private static Queue<AudioStreamPlayer3D> _audioStreamPool = new Queue<AudioStreamPlayer3D>();

	//We add the AudioStream in the pool, so that it can be reused.
	public static void EnquequeAudioStreamInPool(AudioStreamPlayer3D audioStream)
	{
		if (audioStream == null)
			return;
		if (_audioStreamPool.Count >= MaximumItemsAudioStreamPool)
		{
			audioStream.QueueFree();
			return;
		}
		audioStream.Stop();
		audioStream.VolumeDb = -100.0f;
		_audioStreamPool.Enqueue(audioStream);
	}

	//Will be null if no valid AudioStream is in pool
	public static AudioStreamPlayer3D GetAudioStreamFromPool()
	{
		while (_audioStreamPool.Count > 0)
		{
			var audioStream = _audioStreamPool.Dequeue();
			if (audioStream != null)
			{
				audioStream.VolumeDb = 0;
				return audioStream;
			}
		}
		//No valid audio stream in queque anymore
		return null;
	}

	public static void DeleteContentsAudioStreamPool()
	{
		while (_audioStreamPool.Count > 0)
		{
			var audioStream = _audioStreamPool.Dequeue();
			if (audioStream != null)
			{
				audioStream.QueueFree();
			}
		}
	}

	public static ReadOnlyCollection<VirtualAudioListener> ROVirtualAudioListeners
	{
		get { return _virtualAudioListeners.AsReadOnly(); }
	}

	public static void AddVirtualAudioListener(VirtualAudioListener virtualAudioListener)
	{
		if (_virtualAudioListeners.Count >= MaximumVirtualAudioListener)
			return;
		if (_virtualAudioListeners.Contains(virtualAudioListener))
			return;
		_virtualAudioListeners.Add(virtualAudioListener);
		virtualAudioListener.Num = _virtualAudioListeners.Count;
		if (OnVirtualAudioListenerAdded != null)
			OnVirtualAudioListenerAdded(virtualAudioListener);
	}
	public static void RemoveVirtualAudioListener(VirtualAudioListener virtualAudioListener)
	{
		if (_virtualAudioListeners.Contains(virtualAudioListener))
		{
			_virtualAudioListeners.Remove(virtualAudioListener);
			if (OnVirtualAudioListenerRemoved != null)
				OnVirtualAudioListenerRemoved(virtualAudioListener);
		}
	}
	public override void _Ready()
	{
		Main = this;
	}
}
