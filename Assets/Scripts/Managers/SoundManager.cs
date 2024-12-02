using Godot;
using System;
using System.IO;
using System.Collections.Generic;
public static class SoundManager
{
	public static Dictionary<string, AudioStream> Sounds = new Dictionary<string, AudioStream>();

	public static void AddSounds(SoundData[] sounds)
	{
		if (sounds.Length == 0)
			return;

		foreach (var sound in sounds)
			Sounds.Add(sound.name, sound.sound);
	}

	public static AudioStream LoadSound(string soundName, bool loop = false, bool music = false)
	{
		AudioStream clip;
		if (Sounds.TryGetValue(soundName, out clip))
			return clip;

		string FileName;
		string dir = "sound/";
		if (music)
			dir = "";

		byte[] WavSoudFile;
		string path = Directory.GetCurrentDirectory() + "/StreamingAssets/"+ dir + soundName + ".wav";
		if (File.Exists(path))
		{
			WavSoudFile = File.ReadAllBytes(path);
			string[] soundFileName = path.Split('/');
			clip = ToAudioStream(WavSoudFile, 0, soundFileName[soundFileName.Length - 1], loop);
		}
		else if (PakManager.ZipFiles.TryGetValue(path = (dir + soundName + ".wav").ToUpper(), out FileName))
		{
			WavSoudFile = PakManager.GetPK3FileData(path, FileName);
			string[] soundFileName = path.Split('/');
			clip = ToAudioStream(WavSoudFile, 0, soundFileName[soundFileName.Length - 1], loop);
		}
		else if (PakManager.ZipFiles.TryGetValue(path = (dir + soundName + ".ogg").ToUpper(), out FileName))
		{
			WavSoudFile = PakManager.GetPK3FileData(path, FileName);
			AudioStreamOggVorbis audio = AudioStreamOggVorbis.LoadFromBuffer(WavSoudFile);
			audio.Loop = loop;
			clip = audio;
		}
		else
			GameManager.Print("LoadSound: " + path + " not found", GameManager.PrintType.Warning);

		//If clip is null we are also adding it so we don't have to check again
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
			GameManager.Print("Detected format code '" + audioFormat + "' " + formatCode + ", but only PCM and WaveFormatExtensable uncompressed formats are currently supported.", GameManager.PrintType.Warning);
			return null;
		}

		ushort channels = BitConverter.ToUInt16(fileBytes, 22);
		int sampleRate = BitConverter.ToInt32(fileBytes, 24);
		ushort bitDepth = BitConverter.ToUInt16(fileBytes, 34);

		int headerOffset = 16 + 4 + subchunk1 + 4;
		int totalSamples = BitConverter.ToInt32(fileBytes, headerOffset);

		var byteArray = new byte[totalSamples];
		Buffer.BlockCopy(fileBytes, 44, byteArray, 0, byteArray.Length);

		AudioStreamWav audioStream= new AudioStreamWav();
		if (audioFormat == 2)
			audioStream.Format = AudioStreamWav.FormatEnum.ImaAdpcm;
		else
		{
			if (bitDepth == 8)
			{
				audioStream.Format = AudioStreamWav.FormatEnum.Format8Bits;
				//Change data to Signed PCM8
				for (int i = 0; i < byteArray.Length; i++)
					byteArray[i] -= 128;
			}
			else
			{
				audioStream.Format = AudioStreamWav.FormatEnum.Format16Bits;
				totalSamples = (totalSamples >> 1); //block size = 2;
			}
		}
		audioStream.Data = byteArray;
		audioStream.MixRate = sampleRate;
		if (channels == 2)
		{
			audioStream.Stereo = true;
			totalSamples = (totalSamples >> 1);
		}
		if (loop)
		{
			audioStream.LoopBegin = 0;
			audioStream.LoopEnd = totalSamples;
			audioStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		}
		GameManager.Print("AudioStreamWav " + name + " created. Channels " + channels + " sampleRate " + sampleRate + " totalSamples "+ totalSamples + " format " + FormatCode(audioFormat) + (loop? " Loop" : " No Looping"));
		return audioStream;
	}
	public static MultiAudioStream Create2DSound(AudioStream audio, Node3D parent = null, bool destroyAfterSound = true)
	{
		return Create3DSound(Vector3.Zero, audio, parent, destroyAfterSound, true);
	}
	public static MultiAudioStream Create3DSound(Vector3 position, AudioStream audio, Node3D parent = null, bool destroyAfterSound = true, bool is2DAudio = false)
	{
		MultiAudioStream sound = new MultiAudioStream();
		sound.Name = "3D Sound";
		if (parent == null)
			parent = GameManager.Instance.TemporaryObjectsHolder;
		
		parent.AddChild(sound);
		sound.Is2DAudio = is2DAudio;
		sound.GlobalPosition = position;
		sound.Bus = "FXBus";
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
				GameManager.Print("Unknown wav code format:" + code, GameManager.PrintType.Warning);
			return "";
		}
	}
}
