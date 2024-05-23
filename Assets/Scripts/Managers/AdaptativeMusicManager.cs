using Godot;
using System.Collections.Generic;

public partial class AdaptativeMusicManager : Node
{
	public const int MaxTimesOnTop = 3;
	public const int TopIntensity = 3;
	public const int HighIntensity = 2;
	public const int LowIntensity = 1;
	public const int Ambient = 0;

	public static AdaptativeMusicManager Instance;

	[Export]
	public AdaptativeTrack[] MainTracks;
	[Export]
	public AdaptativeTrack[] BlendTracks;
	public AdaptativeTrack currentTrack;

	public static Dictionary<int, List<AdaptativeTrack>> mainTracks = new Dictionary<int, List<AdaptativeTrack>>();
	public static Dictionary<int, List<AdaptativeTrack>> blendTracks = new Dictionary<int, List<AdaptativeTrack>>();

	public AudioStreamPlayer track01, track02;

	[Export]
	public float baseVol = 4f;
	[Export]
	public int maxIntensity = 3;

	public int currentIntensity = 0;
	public float lastDeathRatio = 0;

	private bool isPlayingTack01 = true;
	private bool crossFade = false;
	private float targetVol;
	private bool StartedPlaying = false;
	private int onTop = 0;

	public override void _Ready()
	{
		int trackNum = 0;
		Instance = this;
		track01 = new AudioStreamPlayer();
		AddChild(track01);
		track01.VolumeDb = 7;
		track01.Name = "Track01";
		track01.Bus = "BKGBus";

		track02 = new AudioStreamPlayer();
		AddChild(track02);
		track02.VolumeDb = 7;
		track02.Name = "Track02";
		track02.Bus = "BKGBus";

		for (int i = 0; i < MainTracks.Length; i++)
		{
			AdaptativeTrack track = MainTracks[i];
			track.uniqueId = trackNum++;
			if (mainTracks.ContainsKey(track.intesityLevel))
				mainTracks[track.intesityLevel].Add(track);
			else
			{
				List<AdaptativeTrack> list = new List<AdaptativeTrack>
				{
					track
				};
				mainTracks.Add(track.intesityLevel, list);
				if (track.intesityLevel > maxIntensity)
					maxIntensity = track.intesityLevel;
			}
		}

		for (int i = 0; i < BlendTracks.Length; i++)
		{
			AdaptativeTrack track = BlendTracks[i];
			track.uniqueId = trackNum++;
			if (blendTracks.ContainsKey(track.intesityLevel))
				blendTracks[track.intesityLevel].Add(track);
			else
			{
				List<AdaptativeTrack> list = new List<AdaptativeTrack>
				{
					track
				};
				blendTracks.Add(track.intesityLevel, list);
			}
		}

		targetVol = GetCurrentVolume();
	}

	public override void _Process(double delta)
	{
		bool useOutro = true;

		if (GameManager.Paused)
			return;

		if (!StartedPlaying)
			return;

		float deltaTime = (float)delta;

		if (crossFade)
		{
			if (isPlayingTack01)
			{
				track01.VolumeDb = Mathf.Lerp(track01.VolumeDb, targetVol, deltaTime);
				track02.VolumeDb = Mathf.Lerp(track02.VolumeDb, 0, deltaTime);
				if (track02.VolumeDb < 0.001f)
				{
					track01.VolumeDb = targetVol;
					track02.VolumeDb = 0;
					crossFade = false;
				}
			}
			else
			{
				track02.VolumeDb = Mathf.Lerp(track02.VolumeDb, targetVol, deltaTime);
				track01.VolumeDb = Mathf.Lerp(track01.VolumeDb, 0, deltaTime);
				if (track01.VolumeDb < 0.001f)
				{
					track02.VolumeDb = targetVol;
					track01.VolumeDb = 0;
					crossFade = false;
				}
			}
		}
		else
		{
			if (isPlayingTack01)
			{
				if (track01.VolumeDb < targetVol)
				{
					track01.VolumeDb = Mathf.Lerp(track01.VolumeDb, targetVol, deltaTime);
					if ((targetVol - track01.VolumeDb) < 0.001f)
					{
						track01.VolumeDb = targetVol;
						track02.VolumeDb = targetVol;
					}
				}
			}
			else
			{
				if (track02.VolumeDb < targetVol)
				{
					track02.VolumeDb = Mathf.Lerp(track02.VolumeDb, targetVol, deltaTime);
					if ((targetVol - track02.VolumeDb) < 0.001f)
					{
						track01.VolumeDb = targetVol;
						track02.VolumeDb = targetVol;
					}
				}
			}
		}

		if ((!track01.Playing) && (!track02.Playing))
		{
			int newIntensity = currentIntensity + GD.RandRange(-1, currentIntensity == maxIntensity ? 0 : 1);

			if (currentIntensity > LowIntensity)
			{
				float deathRatio = GameManager.Instance.GetDeathRatioAndReset();
				float meanRatio = Mathf.Lerp(deathRatio, lastDeathRatio, .5f);
				GameManager.Print("meanRatio " + meanRatio);
				lastDeathRatio = meanRatio;
				switch (currentIntensity)
				{
					default:
					case HighIntensity:
						{
							if (meanRatio > 2.5)
								newIntensity = TopIntensity;
							else if (meanRatio > 2)
							{
								useOutro = false;
								if (newIntensity < HighIntensity)
									newIntensity = HighIntensity;
							}
							else if (meanRatio > 1)
								useOutro = false;
							else
							{
								if (newIntensity > HighIntensity)
									newIntensity = HighIntensity;
							}
						}
						break;
					case TopIntensity:
						{
							if (meanRatio > 2.5)
								newIntensity = TopIntensity;
							else if (meanRatio > 2)
							{
								useOutro = false;
								newIntensity = HighIntensity;
							}
							else
								newIntensity = HighIntensity;
						}
						break;
				}

				if (newIntensity == TopIntensity)
				{
					onTop++;
					if (onTop > MaxTimesOnTop)
					{
						lastDeathRatio *= .5f;
						newIntensity = HighIntensity;
						onTop = 0;
					}
				}
				else
					onTop = 0;
			}

			if (newIntensity < 0)
				newIntensity = 0;
			else if (newIntensity > maxIntensity)
				newIntensity = maxIntensity;

			if ((newIntensity < currentIntensity) || ((newIntensity == currentIntensity) && (currentIntensity < 2)))
			{
				currentIntensity = newIntensity;
				if ((currentTrack.hasOutro))
				{
					if (useOutro)
					{
						ChangeTrack(currentTrack.OutroFile);
						GetTrackOnCurrentIntensity(currentIntensity, true, true);
					}
					else
						GetTrackOnCurrentIntensity(currentIntensity);
				}
				else
				{
					newIntensity = GD.RandRange(0, currentIntensity + 1);
					if (newIntensity <= currentIntensity)
						GetTrackOnCurrentIntensity(currentIntensity);
					else
					{
						newIntensity = GD.RandRange(0, currentIntensity);
						GetTrackOnCurrentIntensity(newIntensity, true);
					}
				}
			}
			else
			{
				currentIntensity = newIntensity;
				GetTrackOnCurrentIntensity(currentIntensity);
			}
		}
	}
	public void StartMusic()
	{
		GetTrackOnCurrentIntensity(0);
		StartedPlaying = true;
	}

	public void StopMusic()
	{
		StartedPlaying = false;
		track01.Stop();
		track02.Stop();
	}

	public void GetTrackOnCurrentIntensity(int intensity, bool crossFade = false, bool secondary = false)
	{
		AdaptativeTrack track;

	searchagain:
		if (secondary)
			track = blendTracks[intensity][GD.RandRange(0, blendTracks[intensity].Count - 1)];
		else
			track = mainTracks[intensity][GD.RandRange(0, mainTracks[intensity].Count - 1)];

		if ((!track.isRepeatable) && (track.uniqueId == currentTrack.uniqueId))
			goto searchagain;

		ChangeTrack(track.TrackFile, crossFade);
		currentTrack = track;
	}
	public float GetCurrentVolume()
	{
		return (baseVol + currentIntensity);
	}

	public void ChangeTrack(AudioStream newClip, bool fade = false)
	{
		if (isPlayingTack01)
		{
			track02.Stream = newClip;
			if (fade)
			{
				crossFade = true;
				track02.VolumeDb = 0;
				track02.Play();
			}
			else
			{
				if (track02.VolumeDb == 0)
					track02.VolumeDb = track01.VolumeDb;
				track02.Play();
				track01.Stop();
			}
		}
		else
		{
			track01.Stream = newClip;
			if (fade)
			{
				crossFade = true;
				track01.VolumeDb = 0;
				track01.Play();
			}
			else
			{
				if (track01.VolumeDb == 0)
					track01.VolumeDb = track02.VolumeDb;
				track01.Play();
				track02.Stop();
			}
		}
		targetVol = GetCurrentVolume();
		isPlayingTack01 = !isPlayingTack01;
	}
}
