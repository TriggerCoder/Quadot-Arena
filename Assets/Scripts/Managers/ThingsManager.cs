using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class ThingsManager : Node
{
	[Export]
	public PackedScene[] _fxPrefabs;
	[Export]
	public PackedScene[] _projectilesPrefabs;
	[Export]
	public PackedScene[] _decalsPrefabs;
	[Export]
	public PackedScene[] _itemsPrefabs;

	public static Dictionary<string, PackedScene> thingsPrefabs = new Dictionary<string, PackedScene>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (var thing in _fxPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("FX Name: "+ prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _projectilesPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Projectile Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _decalsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Decal Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _itemsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Item Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
