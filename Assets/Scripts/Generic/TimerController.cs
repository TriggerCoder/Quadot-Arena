using Godot;
using System;
public partial class TimerController : Node3D
{
	public float waitTime;
	public float randomTime;
	private float nextActivateTime;
	private TriggerController trigger;
	float time = 0f;
	public void Init(float wait, float random, TriggerController tc)
	{
		waitTime = wait;
		randomTime = random;
		trigger = tc;

		nextActivateTime = (float)GD.RandRange(waitTime - randomTime, waitTime + randomTime);
		if (trigger == null)
			SetProcess(false);
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		time += deltaTime;

		if (time >= nextActivateTime)
		{
			time = 0f;
			nextActivateTime = (float)GD.RandRange(waitTime - randomTime, waitTime + randomTime);
			trigger.Activate(null);
		}
	}
}
