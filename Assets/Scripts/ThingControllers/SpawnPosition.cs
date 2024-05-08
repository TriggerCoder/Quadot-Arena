using Godot;
using System.Collections.Generic;
public partial class SpawnPosition : ThingController
{
	public Target spawnPosition;
	public void Init(int angle, Dictionary<string, string> entityData)
	{
		angle = angle + 90;
		if (angle < -180)
			angle += 360;
		if (angle > 180)
			angle -= 360;
		spawnPosition = new Target(GlobalPosition, angle, entityData);
		SpawnerManager.AddToList(spawnPosition);
	}
}
