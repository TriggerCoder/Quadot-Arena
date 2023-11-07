using Godot;
using System;

public partial class ThingController : Node3D
{
	public Vector3 location;
	public Quaternion angularrotation;

	[Export]
	public string respawnSound = "items/respawn1";
	[Export]
	public float respawnTime;
	private float remainingTime = 0;
	private bool _disabled = false;
	public bool disabled { get { return _disabled; } }
	public enum ThingType
	{
		Decor, //non-blocking, non-interactive
		Blocking, //blocking or interactive
		Item,
		Teleport,
		JumpPad,
		TargetDestination,
		Trigger,
		Door,
		Player
	}
	[Export]
	public ThingType thingType = ThingType.Decor;

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (disabled)
		{
			float deltaTime = (float)delta;
			remainingTime -= deltaTime;
			if (remainingTime < 0)
			{
				remainingTime = 0;
				_disabled = false;
				Visible = true;
				if (!string.IsNullOrEmpty(respawnSound))
					SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(respawnSound));
			}
		}
	}
	public void DisableThing()
	{
		remainingTime = respawnTime;
		Visible = false;
		_disabled = true;
	}
}
