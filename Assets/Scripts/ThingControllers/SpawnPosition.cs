using Godot;
using System.Collections.Generic;
public partial class SpawnPosition : ThingController
{
	public Target spawnPosition;
	public SpawnType spawnType;
	public enum SpawnType
	{
		DeathMatch,
		Red,
		Blue
	}
	public void Init(int angle, Dictionary<string, string> entityData, SpawnType type = SpawnType.DeathMatch)
	{
		angle = angle + 90;
		if (angle < -180)
			angle += 360;
		if (angle > 180)
			angle -= 360;
		spawnPosition = new Target(GlobalPosition, angle, entityData);
		spawnType = type;
		SpawnerManager.AddToList(this);
	}
}
