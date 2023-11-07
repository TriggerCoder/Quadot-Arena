using Godot;
using System.Collections;
using System.Collections.Generic;
public static class SpawnerManager
{
	public static List<Node3D> deathMatchSpawner = new List<Node3D>();

	public static string respawnSound = "world/telein";

	public static void AddToList(Node3D node)
	{
		deathMatchSpawner.Add(node);
	}

	public static Vector3 FindSpawnLocation()
	{
		int index = GD.RandRange(0, deathMatchSpawner.Count - 1);
		Vector3 destination = deathMatchSpawner[index].GlobalPosition;
		SoundManager.Create3DSound(destination, SoundManager.LoadSound(respawnSound));
		return destination;
	}
}
