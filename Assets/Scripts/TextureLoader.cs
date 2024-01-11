using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using static Godot.Image;

public static class TextureLoader
{
	public static ImageTexture illegal;
	public static readonly string FlareTexture = "GFX/MISC/FLARE";
	public enum ImageFormat
	{
		JPG,
		TGA
	}

	public static Dictionary<string, ImageTexture> Textures = new Dictionary<string, ImageTexture>();
	public static Dictionary<string, ImageTexture> TransparentTextures = new Dictionary<string, ImageTexture>();
	public static Dictionary<string, ImageTexture> ColorizeTextures = new Dictionary<string, ImageTexture>();
	public static void AddNewTexture(string textureName, bool forceSkinAlpha)
	{
		List<QShader> list = new List<QShader>
		{
			new QShader(textureName, 0, 0, forceSkinAlpha)
		};
		LoadTextures(list, false);
		LoadTextures(list, false , ImageFormat.TGA);
	}

	public static ImageTexture GetTextureOrAddTexture(string textureName, bool forceAlpha)
	{
		string upperName = textureName.ToUpper();
		if (forceAlpha)
		{
			if (TransparentTextures.ContainsKey(upperName))
				return TransparentTextures[upperName];
		}
		else if (Textures.ContainsKey(upperName))
			return Textures[upperName];

		GameManager.Print("GetTextureOrAddTexture: No texture \"" + upperName + "\"");
		AddNewTexture(upperName, forceAlpha);
		return GetTexture(upperName, forceAlpha);
	}
	public static bool HasTexture(string textureName)
	{
		string upperName = textureName.ToUpper();

		if (QShaderManager.HasShader(upperName))
			return true;
		if (Textures.ContainsKey(upperName))
			return true;
		return false;
	}
	public static ImageTexture GetTexture(string textureName, bool forceAlpha = false)
	{
		string upperName = textureName.ToUpper();
		if (forceAlpha)
		{
			if (TransparentTextures.ContainsKey(upperName))
				return TransparentTextures[upperName];
		}
		else if (Textures.ContainsKey(upperName))
			return Textures[upperName];

//		GameManager.Print("TextureLoader: No texture \"" + upperName + "\"");
		return illegal;
	}
	public static void LoadTextures(List<QShader> mapTextures, bool ignoreShaders, ImageFormat imageFormat = ImageFormat.JPG)
	{
		foreach (QShader tex in mapTextures)
		{
			string upperName = tex.name.ToUpper();
			string path = upperName;

			if (ignoreShaders)
				if (QShaderManager.QShaders.ContainsKey(upperName))
					continue;

			if (imageFormat == ImageFormat.TGA)
				path += ".TGA";
			else
				path += ".JPG";

			if (PakManager.ZipFiles.ContainsKey(path))
			{
				string FileName = PakManager.ZipFiles[path];
				var reader = new ZipReader();
				reader.Open(FileName);
				byte[] imageBytes = reader.ReadFile(path, false);

				Image baseTex = new Image();
				if (imageFormat == ImageFormat.TGA)
//					baseTex.LoadTgaFromBuffer(imageBytes);
					LoadTGA(baseTex, imageBytes);
				else
					baseTex.LoadJpgFromBuffer(imageBytes);

				int width = baseTex.GetWidth();
				int height = baseTex.GetHeight();
				float luminance = 0;

				if ((tex.addAlpha) && (baseTex.DetectAlpha() == AlphaMode.None))
				{
					baseTex.Convert(Format.Rgba8);

					Color black = Colors.Black;
					for (int i = 0; i < width; i++)
					{
						for (int j = 0; j < height; j++)
						{
							Color pulledColors = baseTex.GetPixel(i, j);
							luminance += .21f * pulledColors.R + .72f * pulledColors.G + .07f * pulledColors.G;
							float alpha = computeAlphaFromColorFilter(pulledColors, black);
							pulledColors.A = alpha;
							baseTex.SetPixel(i, j, pulledColors);
						}
					}
				}
				else
				{
					for (int i = 0; i < width; i++)
					{
						for (int j = 0; j < height; j++)
						{
							Color pulledColors = baseTex.GetPixel(i, j);
							luminance += .21f * pulledColors.R + .72f * pulledColors.G + .07f * pulledColors.G;
						}
					}
				}
				luminance /= (width * height);
				luminance = Mathf.Clamp(luminance, 0f, .35f);
				baseTex.ResizeToPo2(false, Interpolation.Lanczos);
				ImageTexture readyTex = ImageTexture.CreateFromImage(baseTex);
				readyTex.ResourceName = Convert.ToBase64String(BitConverter.GetBytes(luminance));

				if (Textures.ContainsKey(upperName))
				{
					if ((tex.addAlpha) || (baseTex.DetectAlpha() != AlphaMode.None))
					{
						GameManager.Print("Adding transparent texture with name " + upperName + "." + imageFormat);
						TransparentTextures.Add(upperName, readyTex);
					}
					else
					{
						GameManager.Print("Updating texture with name " + upperName + "." + imageFormat);
						Textures[upperName] = readyTex;
					}
				}
				else
				{
					if ((tex.addAlpha) || (baseTex.DetectAlpha() != AlphaMode.None))
					{
						GameManager.Print("Adding transparent texture with name " + upperName + "."+ imageFormat);
						TransparentTextures.Add(upperName, readyTex);
					}
					else
						GameManager.Print("Adding texture with name " + upperName + "." + imageFormat);
					Textures.Add(upperName, readyTex);
				}
			}
			else
				GameManager.Print("Image not found " + upperName + "." + imageFormat);
		}
	}

	public static void LoadTGA(Image TgaImage, byte[] TGABytes)
	{
		MemoryStream ms = new MemoryStream(TGABytes);
		BinaryReader TGAData = new BinaryReader(ms);
		Format format;
		int byteSize;
		// Read the TGA or TARGA header data
		byte idLength = TGAData.ReadByte();
		byte colorMapType = TGAData.ReadByte();
		byte imageType = TGAData.ReadByte();
		ushort colorMapFirstEntryIndex = TGAData.ReadUInt16();
		ushort colorMapLength = TGAData.ReadUInt16();
		byte colorMapEntrySize = TGAData.ReadByte();
		ushort xOrigin = TGAData.ReadUInt16();
		ushort yOrigin = TGAData.ReadUInt16();
		ushort width = TGAData.ReadUInt16();
		ushort height = TGAData.ReadUInt16();
		byte pixelDepth = TGAData.ReadByte();
		byte imageDescriptor = TGAData.ReadByte();

		bool topToBottom = ((imageDescriptor & 32) > 0);

		// Skip the TGA or TARGA ID field
		TGAData.BaseStream.Seek(idLength, SeekOrigin.Current);

		if (pixelDepth == 32)
		{
			format = Format.Rgba8;
			byteSize = 4;
		}
		else
		{
			format = Format.Rgb8;
			byteSize = 3;
		}

		byte[] colors = new byte[width * height * byteSize];
		int currentPixel = 0;

		if (imageType == 10) // RLE compressed
		{
			while (currentPixel < colors.Length)
			{
				// Get the RLE packet header byte
				byte header = TGAData.ReadByte();

				int packetLength = header & 0x7F;

				// Check if the RLE packet header is a RLE packet
				if ((header & 0x80) != 0)
				{
					// Read the repeated color data
					byte r = TGAData.ReadByte();
					byte g = TGAData.ReadByte();
					byte b = TGAData.ReadByte();
					byte a = (pixelDepth == 32) ? TGAData.ReadByte() : (byte)0xFF;

					// Copy the repeated color into the Color array
					for (int i = 0; i <= packetLength; i++)
					{
						colors[currentPixel] = b;
						colors[currentPixel + 1] = g;
						colors[currentPixel + 2] = r;
						if (byteSize == 4)
							colors[currentPixel + 3] = a;
						currentPixel += byteSize;
					}
				}
				else
				{
					for (int i = 0; i <= packetLength; i++, currentPixel += byteSize)
					{
						// Read the raw color data
						byte r = TGAData.ReadByte();
						byte g = TGAData.ReadByte();
						byte b = TGAData.ReadByte();
						byte a = (pixelDepth == 32) ? TGAData.ReadByte() : (byte)0xFF;
						colors[currentPixel] = b;
						colors[currentPixel + 1] = g;
						colors[currentPixel + 2] = r;
						if (byteSize == 4)
							colors[currentPixel + 3] = a;
					}
				}
			}
		}
		else if (imageType == 2) //Uncompressed
		{
			for (currentPixel = 0; currentPixel < colors.Length; currentPixel += byteSize)
			{
				// Read the color data
				byte r = TGAData.ReadByte();
				byte g = TGAData.ReadByte();
				byte b = TGAData.ReadByte();
				byte a = (pixelDepth == 32) ? TGAData.ReadByte() : (byte)0xFF;

				colors[currentPixel] = b;
				colors[currentPixel + 1] = g;
				colors[currentPixel + 2] = r;
				if (byteSize == 4)
					colors[currentPixel + 3] = a;
			}
		}
		else
			GameManager.Print("TGA texture: unknown type.");

		TgaImage.SetData(width, height, false, format, colors);
		if (!topToBottom)
			TgaImage.FlipY();
	}
	public static float computeAlphaFromColorFilter(Color color, Color filter)
	{ 
		return Mathf.Max(Mathf.Max(Mathf.Abs(color.R - filter.R), Mathf.Abs(color.G - filter.G)), Mathf.Abs(color.B - filter.B));
	}
	public static ImageTexture CreateLightmapTexture(byte[] rgb)
	{
		Color colors;
		int pixelSize = (int)MapLoader.currentLightMapSize;
		Image baseTex = Image.Create(pixelSize, pixelSize, false, Image.Format.Rgba8);
		int k = 0;
		for (int j = 0; j < pixelSize; j++)
		{
			for (int i = 0; i < pixelSize; i++)
			{
				colors = ChangeColorLighting(rgb[k++], rgb[k++], rgb[k++]);
				baseTex.SetPixel(i, j, colors);
			}
		}
		ImageTexture tex = ImageTexture.CreateFromImage(baseTex);
		return tex;
	}

	public static Color ChangeColorLighting(byte r, byte g, byte b)
	{
		float scale = 1.0f, temp;
		float R, G, B;

		R = r * GameManager.Instance.colorLightning / 255.0f;
		G = g * GameManager.Instance.colorLightning / 255.0f;
		B = b * GameManager.Instance.colorLightning / 255.0f;

		if (R > 1.0f && (temp = (1.0f / R)) < scale)
			scale = temp;
		if (G > 1.0f && (temp = (1.0f / G)) < scale)
			scale = temp;
		if (B > 1.0f && (temp = (1.0f / B)) < scale)
			scale = temp;

		R *= scale;
		G *= scale;
		B *= scale;
		return new Color(R, G, B, 1f);
	}
	public static Color ChangeColorLighting(Color icolor)
	{
		float scale = 1.0f, temp;
		float R, G, B;

		R = icolor.R * GameManager.Instance.colorLightning;
		G = icolor.G * GameManager.Instance.colorLightning;
		B = icolor.B * GameManager.Instance.colorLightning;

		if (R > 1.0f && (temp = (1.0f / R)) < scale)
			scale = temp;
		if (G > 1.0f && (temp = (1.0f / G)) < scale)
			scale = temp;
		if (B > 1.0f && (temp = (1.0f / B)) < scale)
			scale = temp;

		R *= scale;
		G *= scale;
		B *= scale;

		return new Color(R, G, B, 1f);
	}
}
