using Godot;
using System;

public partial class LightFade : OmniLight3D
{
	[Export]
	public float destroyTimer = 0;

	private float baseTime = 1;
	private float BaseLight = 0;
	public override void _Ready()
	{
		if (destroyTimer == 0)
			SetProcess(false);
		else
			baseTime = destroyTimer;
		BaseLight = LightEnergy;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		destroyTimer -= deltaTime;
		if (destroyTimer < 0)
		{
			QueueFree();
			return;
		}
		LightEnergy = Mathf.Lerp(0.0f, BaseLight, destroyTimer / baseTime);
	}
}
