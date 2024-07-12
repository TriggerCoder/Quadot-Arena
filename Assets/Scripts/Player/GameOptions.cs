using Godot;
public static class GameOptions
{
	public static float MainVolume = 1.0f;
	public static float BGMVolume = 1.0f;
	public static float SFXVolume = 1.0f;

	public static float hudBrightness = 0.9f;
	public static float ambientLight = 0.8f;
	public static bool aspectRatioCorrection = true;


//	public static Sprite crosshair = null;
	public static int crosshairIndex = 4;
	public static float crossHairAlpha = .4f;

	public static bool dynamicMusic = true;
	public static bool runToggle = true;
	public static bool UseMuzzleLight = true;
	public static bool HeadBob = true;
}