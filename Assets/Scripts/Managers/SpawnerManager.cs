using Godot;
using System.Collections.Generic;
public static class SpawnerManager
{
	public static List<Target> deathMatchSpawner = new List<Target>();
	public static List<Target> redTeamSpawner = new List<Target>();
	public static List<Target> blueTeamSpawner = new List<Target>();


	public static string respawnSound = "world/telein";
	public static int lastSpawn = 0;
	public static void AddToList(SpawnPosition spawnPosition)
	{
		switch(spawnPosition.spawnType)
		{
			default:
				deathMatchSpawner.Add(spawnPosition.spawnPosition);
			break;
			case SpawnPosition.SpawnType.Red:
				redTeamSpawner.Add(spawnPosition.spawnPosition);
			break;
			case SpawnPosition.SpawnType.Blue:
				blueTeamSpawner.Add(spawnPosition.spawnPosition);
			break;
		}
	}

	public static void SpawnToLocation(PlayerThing player)
	{
		int spawnIndex;
		do
			spawnIndex = GD.RandRange(0, deathMatchSpawner.Count - 1);
		while (spawnIndex == lastSpawn);
	
		lastSpawn = spawnIndex;
		Target target = deathMatchSpawner[spawnIndex];
		ClusterPVSManager.CheckPVS(player.playerInfo.viewLayer, target.destination);
		TeleporterThing.TelefragEverything(target.destination, player);
		player.Position = target.destination;
		player.playerControls.InvoqueSetTransformReset();

		SoundManager.Create3DSound(target.destination, SoundManager.LoadSound(respawnSound));
		player.playerControls.viewDirection.Y = target.angle;
		player.playerControls.impulseVector = Vector3.Zero;
		player.playerControls.playerVelocity = Vector3.Zero;
		player.playerControls.fallSpeed = 0;
		player.playerControls.jumpPadVel = Vector3.Zero;
		player.Impulse(Quaternion.FromEuler(new Vector3(0, Mathf.DegToRad(target.angle), 0)) * Vector3.Forward, 1500);

	}

	public static void CheckSpawnLocations()
	{
		switch (GameManager.Instance.gameConfig.GameType)
		{
			default:
			break;
			case GameManager.GameType.SinglePlayer:
			case GameManager.GameType.FreeForAll:
			case GameManager.GameType.QuadHog:
			case GameManager.GameType.Tournament:
				deathMatchSpawner.AddRange(redTeamSpawner);
				deathMatchSpawner.AddRange(blueTeamSpawner);
			break;
		}
	}
	public static void ClearLists()
	{
		deathMatchSpawner = new List<Target>();
		redTeamSpawner = new List<Target>();
		blueTeamSpawner = new List<Target>();
	}

}
