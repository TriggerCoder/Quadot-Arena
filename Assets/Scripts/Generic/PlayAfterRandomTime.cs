using Godot;
using System;

public partial class PlayAfterRandomTime : Node
{
	public float waitTime;
	public float randomTime;
	private float nextPlayTime;
	private AudioStreamPlayer sAudioStream;
	private MultiAudioStream mAudioStream;
	AudioType audioType = AudioType.None;
	enum AudioType
	{
		None,
		Single,
		Multi
	}

	float time = 0f;
	public void Init(float wait, float random)
	{
		waitTime = wait;
		randomTime = random;
		nextPlayTime = (float)GD.RandRange(waitTime - randomTime, waitTime + randomTime);
	}

	public void AddMultiAudioStream(MultiAudioStream audioStream)
	{
		if (audioStream == null)
		{
			SetProcess(false);
			return;
		}
		mAudioStream = audioStream;
		audioType = AudioType.Multi;
	}
	public void AddAudioStream(AudioStreamPlayer audioStream)
	{
		if (audioStream == null)
		{
			SetProcess(false);
			return;
		}
		sAudioStream = audioStream;
		audioType = AudioType.Single;
	}

	void ResetTimer()
	{
		time = 0f;
		nextPlayTime = (float)GD.RandRange(waitTime - randomTime, waitTime + randomTime + 1);
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		time += deltaTime;

		if (time >= nextPlayTime)
		{
			switch (audioType)
			{
				default:
				break;
				case AudioType.Single:
					if (!sAudioStream.Playing)
					{
						ResetTimer();
						sAudioStream.Play();
					}
				break;
				case AudioType.Multi:
					if (!mAudioStream.Playing)
					{
						ResetTimer();
						mAudioStream.Play();
					}
				break;
			}
		}
	}
}
