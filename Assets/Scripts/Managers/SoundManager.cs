using Godot;
using System;
using System.IO;
using System.Collections.Generic;
public static class SoundManager
{
	public static Dictionary<string, AudioStreamWav> Sounds = new Dictionary<string, AudioStreamWav>();
	public static AudioStreamWav LoadSound(string soundName, bool loop = false)
	{
		if (Sounds.ContainsKey(soundName))
			return Sounds[soundName];

		byte[] WavSoudFile;
		string path = Directory.GetCurrentDirectory() + "/StreamingAssets/sound/" + soundName + ".wav";
		if (File.Exists(path))
			WavSoudFile = File.ReadAllBytes(path);
		else if (PakManager.ZipFiles.ContainsKey(path = ("sound/" + soundName + ".wav").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			var reader = new ZipReader();
			reader.Open(FileName);
			WavSoudFile = reader.ReadFile(path, false);
		}
		else
			return null;

		string[] soundFileName = path.Split('/');
		AudioStreamWav clip = ToAudioStream(WavSoudFile, 0, soundFileName[soundFileName.Length - 1], loop);

		if (clip == null)
			return null;

		Sounds.Add(soundName, clip);
		return clip;
	}
	private static AudioStreamWav ToAudioStream(byte[] fileBytes, int offsetSamples = 0, string name = "wav", bool loop = false)
	{
		int subchunk1 = BitConverter.ToInt32(fileBytes, 16);
		ushort audioFormat = BitConverter.ToUInt16(fileBytes, 20);

		string formatCode = FormatCode(audioFormat);
		if ((audioFormat != 1) && (audioFormat != 2) && (audioFormat != 65534))
		{
			GD.Print("Detected format code '" + audioFormat + "' " + formatCode + ", but only PCM and WaveFormatExtensable uncompressed formats are currently supported.");
			return null;
		}

		ushort channels = BitConverter.ToUInt16(fileBytes, 22);
		int sampleRate = BitConverter.ToInt32(fileBytes, 24);
		ushort bitDepth = BitConverter.ToUInt16(fileBytes, 34);

		int headerOffset = 16 + 4 + subchunk1 + 4;
		int totalSamples = BitConverter.ToInt32(fileBytes, headerOffset);

		AudioStreamWav audioStream= new AudioStreamWav();
		if (audioFormat == 2)
			audioStream.Format = AudioStreamWav.FormatEnum.ImaAdpcm;
		else
		{
			if (bitDepth == 8)
				audioStream.Format = AudioStreamWav.FormatEnum.Format8Bits;
			else
				audioStream.Format = AudioStreamWav.FormatEnum.Format16Bits;
		}
		audioStream.Data = fileBytes;
		audioStream.MixRate = sampleRate;
		if (channels == 2)
			audioStream.Stereo = true;
		if (loop)
		{
			audioStream.LoopBegin = 0;
			audioStream.LoopEnd = totalSamples;
			audioStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		}
		GD.Print("AudioStreamWav " + name + " created. Channels " + channels + " sampleRate " + sampleRate + " totalSamples "+ totalSamples + " format " + FormatCode(audioFormat));
		return audioStream;
	}
	public static MultiAudioStream Create2DSound(AudioStream audio, Node3D parent = null, bool destroyAfterSound = true)
	{
		return Create3DSound(Vector3.Zero, audio, parent, destroyAfterSound);
	}
	public static MultiAudioStream Create3DSound(Vector3 position, AudioStream audio, Node3D parent = null, bool destroyAfterSound = true)
	{
		MultiAudioStream sound = new MultiAudioStream();
		sound.Name = "3D Sound";
		if (parent == null)
			parent = GameManager.Instance.TemporaryObjectsHolder;
		
		parent.AddChild(sound);
		sound.GlobalPosition = position;
		sound.Stream = audio;
		sound.DestroyAfterSoundPlayed = destroyAfterSound;
		sound.Play();
		return sound;
	}
	private static string FormatCode(ushort code)
	{
		switch (code)
		{
			case 1:
				return "PCM";
			case 2:
				return "ADPCM";
			case 3:
				return "IEEE";
			case 7:
				return "μ-law";
			case 65534:
				return "WaveFormatExtensable";
			default:
				GD.Print("Unknown wav code format:" + code);
			return "";
		}
	}
}
