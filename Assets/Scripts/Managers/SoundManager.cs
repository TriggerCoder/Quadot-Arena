using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class SoundManager
{
	public static Dictionary<string, AudioStreamWav> Sounds = new Dictionary<string, AudioStreamWav>();
	public static AudioStreamWav LoadSound(string soundName)
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
		AudioStreamWav clip = ToAudioClip(WavSoudFile, 0, soundFileName[soundFileName.Length - 1]);

		if (clip == null)
			return null;

		Sounds.Add(soundName, clip);
		return clip;
	}
	private static AudioStreamWav ToAudioClip(byte[] fileBytes, int offsetSamples = 0, string name = "wav")
	{
		int subchunk1 = BitConverter.ToInt32(fileBytes, 16);
		ushort audioFormat = BitConverter.ToUInt16(fileBytes, 20);

		string formatCode = FormatCode(audioFormat);
		if ((audioFormat != 1) && (audioFormat != 65534))
		{
			GD.Print("Detected format code '" + audioFormat + "' " + formatCode + ", but only PCM and WaveFormatExtensable uncompressed formats are currently supported.");
			return null;
		}

		ushort channels = BitConverter.ToUInt16(fileBytes, 22);
		int sampleRate = BitConverter.ToInt32(fileBytes, 24);
		ushort bitDepth = BitConverter.ToUInt16(fileBytes, 34);

		int headerOffset = 16 + 4 + subchunk1 + 4;
		int subchunk2 = BitConverter.ToInt32(fileBytes, headerOffset);

/*/		float[] data;
		switch (bitDepth)
		{
			case 8:
				data = Convert8BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
				break;
			case 16:
				data = Convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
				break;
			case 24:
				data = Convert24BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
				break;
			case 32:
				data = Convert32BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
				break;
			default:
				GD.Print(bitDepth + " bit depth is not supported.");
				return null;
		}

		if (data == null)
			return null;
*/
		AudioStreamWav audioClip = new AudioStreamWav();
		if (audioFormat == 2) 
			audioClip.Format = AudioStreamWav.FormatEnum.ImaAdpcm;
		else
		{
			if (bitDepth == 8)
				audioClip.Format = AudioStreamWav.FormatEnum.Format8Bits;
			else
				audioClip.Format = AudioStreamWav.FormatEnum.Format16Bits;
		}
		audioClip.Data = fileBytes.ToArray();
		audioClip.MixRate = sampleRate;
		if (channels == 2)
			audioClip.Stereo = true;
		GD.Print("AudioStreamWav " + name + " created. Channels " + channels + " sampleRate " + sampleRate +" format " + FormatCode(audioFormat));
		return audioClip;
	}

	private static float[] Convert8BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
	{
		int wavSize = BitConverter.ToInt32(source, headerOffset);
		headerOffset += sizeof(int);

		if ((wavSize <= 0) || (wavSize != dataSize))
		{
			GD.Print("Failed to get valid 8-bit wav size: " + wavSize + " from data bytes: " + dataSize + " at offset: " + headerOffset);
			return null;
		}

		float[] data = new float[wavSize];
		sbyte maxValue = sbyte.MaxValue;
		sbyte minValue = sbyte.MinValue;

		int i = 0;
		while (i < wavSize)
		{
			data[i] = (source[i + headerOffset] + minValue) / (float)maxValue;
			++i;
		}

		return data;
	}

	private static float[] Convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
	{
		int wavSize = BitConverter.ToInt32(source, headerOffset);
		headerOffset += sizeof(int);

		if ((wavSize <= 0) || (wavSize != dataSize))
		{
			GD.Print("Failed to get valid 16-bit wav size: " + wavSize + " from data bytes: " + dataSize + " at offset: " + headerOffset);
			return null;
		}

		int x = sizeof(ushort); // block size = 2
		int convertedSize = wavSize / x;

		float[] data = new float[convertedSize];

		ushort maxValue = ushort.MaxValue;

		int offset = 0;
		int i = 0;
		while (i < convertedSize)
		{
			offset = i * x + headerOffset;
			data[i] = (float)BitConverter.ToInt16(source, offset) / maxValue;
			++i;
		}

		return data;
	}
	private static float[] Convert24BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
	{
		int wavSize = BitConverter.ToInt32(source, headerOffset);
		headerOffset += sizeof(int);

		if ((wavSize <= 0) || (wavSize != dataSize))
		{
			GD.Print("Failed to get valid 24-bit wav size: " + wavSize + " from data bytes: " + dataSize + " at offset: " + headerOffset);
			return null;
		}

		int x = 3; // block size = 3
		int convertedSize = wavSize / x;

		int maxValue = Int32.MaxValue;

		float[] data = new float[convertedSize];

		byte[] block = new byte[sizeof(int)]; // using a 4 byte block for copying 3 bytes, then copy bytes with 1 offset

		int offset = 0;
		int i = 0;
		while (i < convertedSize)
		{
			offset = i * x + headerOffset;
			Buffer.BlockCopy(source, offset, block, 1, x);
			data[i] = (float)BitConverter.ToInt32(block, 0) / maxValue;
			++i;
		}

		return data;
	}
	private static float[] Convert32BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
	{
		int wavSize = BitConverter.ToInt32(source, headerOffset);
		headerOffset += sizeof(int);

		if ((wavSize <= 0) || (wavSize != dataSize))
		{
			GD.Print("Failed to get valid 32-bit wav size: " + wavSize + " from data bytes: " + dataSize + " at offset: " + headerOffset);
			return null;
		}

		int x = sizeof(float); //  block size = 4
		int convertedSize = wavSize / x;

		uint maxValue = uint.MaxValue;

		float[] data = new float[convertedSize];

		int offset = 0;
		int i = 0;
		while (i < convertedSize)
		{
			offset = i * x + headerOffset;
			data[i] = (float)BitConverter.ToInt32(source, offset) / maxValue;
			++i;
		}

		return data;
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
