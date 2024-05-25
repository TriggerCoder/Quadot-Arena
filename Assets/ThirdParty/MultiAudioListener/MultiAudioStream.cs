using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class MultiAudioStream : Node3D
{
	//Properties from the normal AudioStreamPlayer3D

	private AudioStream _stream = null;
	[Export]
	public AudioStream Stream
	{
		get { return _stream; }
		set
		{
			_stream = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.Stream = value;
			}
		}
	}

	private uint _areaMask = 1;
	[Export]
	public uint AreaMask
	{
		get { return _areaMask; }
		set
		{
			_areaMask = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.AreaMask = value;
			}
		}
	}

	private float _attenuationFilterCutoffHz = 5000.0f;
	[Export]
	public float AttenuationFilterCutoffHz
	{
		get { return _attenuationFilterCutoffHz; }
		set
		{
			_attenuationFilterCutoffHz = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.AttenuationFilterCutoffHz = value;
			}
		}
	}

	private float _attenuationFilterDb = -24.0f;
	[Export]
	public float AttenuationFilterDb
	{
		get { return _attenuationFilterDb; }
		set
		{
			_attenuationFilterDb = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.AttenuationFilterDb = value;
			}
		}
	}

	private AudioStreamPlayer3D.AttenuationModelEnum _attenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance;
	[Export]
	public AudioStreamPlayer3D.AttenuationModelEnum AttenuationModel
	{
		get { return _attenuationModel; }
		set
		{
			_attenuationModel = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.AttenuationModel = value;
			}
		}
	}

	private string _bus = "Master";
	[Export]
	public string Bus
	{
		get { return _bus; }
		set
		{
			_bus = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.Bus = value;
			}
		}
	}

	private float _emissionAngleDegrees = 45.0f;
	[Export]
	public float EmissionAngleDegrees
	{
		get { return _emissionAngleDegrees; }
		set
		{
			_emissionAngleDegrees = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.EmissionAngleDegrees = value;
			}
		}
	}

	private bool _emissionAngleEnabled = false;
	[Export]
	public bool EmissionAngleEnabled
	{
		get { return _emissionAngleEnabled; }
		set
		{
			_emissionAngleEnabled = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.EmissionAngleEnabled = value;
			}
		}
	}

	private float _emissionAngleFilterAttenuationDb = -12.0f;
	[Export]
	public float EmissionAngleFilterAttenuationDb
	{
		get { return _emissionAngleFilterAttenuationDb; }
		set
		{
			_emissionAngleFilterAttenuationDb = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.EmissionAngleFilterAttenuationDb = value;
			}
		}
	}

	private float _maxDb = 3.0f;
	[Export]
	public float MaxDb
	{
		get { return _maxDb; }
		set
		{
			_maxDb = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.MaxDb = value;
			}
		}
	}
	
	private float _maxDistance = 0.0f;
	[Export]
	public float MaxDistance
	{
		get { return _maxDistance; }
		set
		{
			_maxDistance = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.MaxDistance = value;
			}
		}
	}
	
	private int _maxPolyphony = 1;
	[Export]
	public int MaxPolyphony
	{
		get { return _maxPolyphony; }
		set
		{
			_maxPolyphony = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.MaxPolyphony = value;
			}
		}
	}
	
	private float _panningStrength = 1.0f;
	[Export]
	public float PanningStrength
	{
		get { return _panningStrength; }
		set
		{
			_panningStrength = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.PanningStrength = value;
			}
		}
	}
	
	private float _pitchScale = 1.0f;
	[Export]
	public float PitchScale
	{
		get { return _pitchScale; }
		set
		{
			_pitchScale = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.PitchScale = value;
			}
		}
	}
	
	private float _unitSize = 10.0f;
	[Export]
	public float UnitSize
	{
		get { return _unitSize; }
		set
		{
			_unitSize = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.UnitSize = value;
			}
		}
	}

	private float _volumeDb = 0.0f;
	[Export]
	public float VolumeDb
	{
		get { return _volumeDb; }
		set
		{
			_volumeDb = value;
			foreach (var subAudioStream in _subAudioStreams)
			{
				subAudioStream.Value.VolumeDb = value;
			}
		}
	}

	private bool _destroyAfterSoundPlayed = false;
	public bool DestroyAfterSoundPlayed
	{
		get { return _destroyAfterSoundPlayed; }
		set
		{
			_destroyAfterSoundPlayed = value;
		}
	}

	private bool _is2DAudio = false;
	public bool Is2DAudio
	{
		get { return _is2DAudio; }
		set
		{
			_is2DAudio = value;
		}
	}
	//Internal components

	private Dictionary<VirtualAudioListener, AudioStreamPlayer3D> _subAudioStreams = new Dictionary<VirtualAudioListener, AudioStreamPlayer3D>();

	private bool _Playing = false;
	public bool Playing
	{
		get { return _Playing; }

	}
	public override void _ExitTree()
	{
		if (_Playing)
		{
			Stop();
		}
	}

	public void Play(float FromPosition = 0.0f)
	{
		if (!_Playing)
		{
			_Playing = true;

			//We subscribe to these events so sub audio sources can be added or removed if needed
			MultiAudioListener.OnVirtualAudioListenerAdded += VirtualAudioListenerAdded;
			MultiAudioListener.OnVirtualAudioListenerRemoved += VirtualAudioListenerRemoved;

			bool hardwareChannelsLeft = true;

			//Create all sub audio stream
			var virtualAudioListeners = MultiAudioListener.ROVirtualAudioListeners;
			for (int i = 0; i < virtualAudioListeners.Count; i++)
			{
				CreateSubAudioStream(virtualAudioListeners[i], ref hardwareChannelsLeft);
			}
		}
		else
		{
			foreach (var audioStream in _subAudioStreams)
			{
				audioStream.Value.Play(FromPosition);
			}
		}
	}

	public void Stop()
	{
		if (!_Playing)
			return;

		_Playing = false;

		MultiAudioListener.OnVirtualAudioListenerAdded -= VirtualAudioListenerAdded;
		MultiAudioListener.OnVirtualAudioListenerRemoved -= VirtualAudioListenerRemoved;

		//Remove all old subAudio
		foreach (var subAudioStream in _subAudioStreams)
		{
			if (subAudioStream.Value != null)
			{
				MultiAudioListener.EnquequeAudioStreamInPool(subAudioStream.Value);
			}
		}
		_subAudioStreams.Clear();
		if (DestroyAfterSoundPlayed)
			QueueFree();
	}

	private void VirtualAudioListenerAdded(VirtualAudioListener virtualAudioListener)
	{
		bool hardwareChannelsLeft = true;
		if ((Is2DAudio) && (_subAudioStreams.Count > 0))
			return;

		CreateSubAudioStream(virtualAudioListener, ref hardwareChannelsLeft);
	}

	private void VirtualAudioListenerRemoved(VirtualAudioListener virtualAudioListener)
	{
		AudioStreamPlayer3D audioStream;

		if (!_subAudioStreams.TryGetValue(virtualAudioListener, out audioStream))
			return;

		_subAudioStreams.Remove(virtualAudioListener);
		if (audioStream != null)
			MultiAudioListener.EnquequeAudioStreamInPool(audioStream);
	}

	private void CreateSubAudioStream(VirtualAudioListener virtualAudioListener, ref bool hardWareChannelsLeft)
	{
		AudioStreamPlayer3D audioStream = CreateAudioStream("Sub Audio Stream " + virtualAudioListener.Num, ref hardWareChannelsLeft);
		_subAudioStreams.Add(virtualAudioListener, audioStream);
		audioStream.VolumeDb = VolumeDb * virtualAudioListener.Volume;

		//Do transform
		MoveSubAudioStreamToNeededLocation(virtualAudioListener, audioStream);
	}

	private void MoveSubAudioStreamToNeededLocation(VirtualAudioListener virtualListener, AudioStreamPlayer3D subAudioStream)
	{
		//There is no main listener so translation is not needed
		if (MultiAudioListener.Main == null) 
			return;

		if (Is2DAudio)
		{
			GlobalPosition = MultiAudioListener.Main.GlobalPosition;
			return;
		}

		// Get the position of the object relative to the virtual listener
		Vector3 localPos = GlobalPosition - virtualListener.GlobalPosition;
		subAudioStream.Position = virtualListener.Quaternion * localPos;
	}

	private AudioStreamPlayer3D CreateAudioStream(string nameSubAudioStream, ref bool hardwareChannelsLeft)
	{
		AudioStreamPlayer3D audioStream = MultiAudioListener.GetAudioStreamFromPool();
		//If no audiosource was given by pool, make a new one
		if (audioStream == null)
		{
			audioStream = new AudioStreamPlayer3D();
			audioStream.Name = nameSubAudioStream;
			MultiAudioListener.Main.AddChild(audioStream);
		}
		else
		{
			audioStream.Name = nameSubAudioStream;
		}

		SetAllValuesAudioStream(audioStream);

		if (_Playing && hardwareChannelsLeft)
		{
			float position = 0f;

			//Play from position of first audioStream
			if (_subAudioStreams.Count > 0)
				position = _subAudioStreams.First().Value.GetPlaybackPosition();

			audioStream.Play(position);
			//If this sound gets culled all following will be too
			if (!audioStream.Playing)
			{
				hardwareChannelsLeft = false;
			}
		}
		//All audio doppler effect should be  zero
		audioStream.DopplerTracking = AudioStreamPlayer3D.DopplerTrackingEnum.Disabled;
		return audioStream;
	}
	public void RefreshAllPropertiesAudioStreams()
	{
		foreach (var subAudioStream in _subAudioStreams)
		{
			SetAllValuesAudioStream(subAudioStream.Value);
		}
	}

	private void SetAllValuesAudioStream(AudioStreamPlayer3D audioStream)
	{
		audioStream.Stream = Stream;
		audioStream.AreaMask = AreaMask;
		audioStream.AttenuationFilterCutoffHz = AttenuationFilterCutoffHz;
		audioStream.AttenuationFilterDb = AttenuationFilterDb;
		audioStream.AttenuationModel = AttenuationModel;
		audioStream.Autoplay = false;
		audioStream.Bus = Bus;
		audioStream.EmissionAngleDegrees = EmissionAngleDegrees;
		audioStream.EmissionAngleEnabled = EmissionAngleEnabled;
		audioStream.EmissionAngleFilterAttenuationDb = EmissionAngleFilterAttenuationDb;
		audioStream.MaxDb = MaxDb;
		audioStream.MaxDistance = MaxDistance;
		audioStream.MaxPolyphony = MaxPolyphony;
		audioStream.PanningStrength = PanningStrength;
		audioStream.PitchScale = PitchScale;
		audioStream.UnitSize = UnitSize;

	}

	public override void _Ready()
	{
		ProcessPhysicsPriority = 10;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_Playing)
		{
			//Closest audioCulling
			AudioStreamPlayer3D closestAudio = null;
			float distanceClosestAudio = 0;
			bool isCurrentlyPlaying = false;

			foreach (var subAudioStream in _subAudioStreams)
			{
				//We set the the correct volume before we cull
				subAudioStream.Value.VolumeDb = VolumeDb;

				var distance = (subAudioStream.Key.GlobalPosition - GlobalPosition).LengthSquared();

				if ((closestAudio == null) || (distance < distanceClosestAudio))
				{
					if (closestAudio != null)
						closestAudio.VolumeDb = -100.0f;

					closestAudio = subAudioStream.Value;
					closestAudio.VolumeDb = VolumeDb * subAudioStream.Key.Volume;
					distanceClosestAudio = distance;
				}
				else
					subAudioStream.Value.VolumeDb = -100.0f;

				MoveSubAudioStreamToNeededLocation(subAudioStream.Key, subAudioStream.Value);
				isCurrentlyPlaying |= subAudioStream.Value.Playing;
			}

			//is there anyone listening?
			if (MultiAudioListener.ROVirtualAudioListeners.Count > 0)
			{
				if (!isCurrentlyPlaying)
					Stop();
			}
		}
	}
}
