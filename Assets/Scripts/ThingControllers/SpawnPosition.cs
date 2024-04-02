using Godot;
using System;
public partial class SpawnPosition : ThingController
{
	public Target spawnPosition;
	public void Init(int angle)
	{
		angle = angle + 90;
		if (angle < -180)
			angle += 360;
		if (angle > 180)
			angle -= 360;
		spawnPosition = new Target(GlobalPosition, angle);
		SpawnerManager.AddToList(spawnPosition);
	}
}
