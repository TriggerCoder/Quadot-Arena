using Godot;
using System;

public partial class ThingController : Node3D
{
	public Node3D parent = null;

	[Export]
	public string respawnSound = "items/respawn1";
	[Export]
	public float respawnTime;
	[Export]
	public float randomTime = 0;
	[Export]
	public bool initDisabled = false;

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

	[Export]
	public ItemPickup itemPickup = null;

	public string itemName;
	//Only 1 per Map according to GameType
	public bool uniqueItem = false;
	public void SpawnCheck(string name)
	{
		itemName = name;
		if (initDisabled)
		{
			DisableThing();
			//Set first spawn time to 30s
			remainingTime = 30;
		}
	}
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

	public void SetRandomTime(float waitTime)
	{
		randomTime = waitTime;
	}
	public void SetRespawnTime(float waitTime)
	{
		respawnTime = waitTime;
	}
	public void RespawnNow()
	{
		remainingTime = 0;
	}

	public void DisableThing()
	{
		//No longer respawn
		if (respawnTime < 0)
		{
			if (parent != null)
				parent.QueueFree();
			else
				QueueFree();
		}

		remainingTime = (float)GD.RandRange(respawnTime - randomTime, respawnTime + randomTime);
		Visible = false;
		_disabled = true;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationPredelete)
			CheckDestroy();
	}
	public void CheckDestroy()
	{
		if (disabled)
			return;

		//GameType
		if (uniqueItem)
		{
			if (ThingsManager.uniqueThingsOnMap.TryGetValue(itemName, out ThingController masterThing))
				masterThing.RespawnNow();
		}
	}
}
