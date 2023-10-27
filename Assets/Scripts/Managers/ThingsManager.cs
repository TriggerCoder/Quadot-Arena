using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class ThingsManager : Node
{
	[Export]
	public PackedScene[] _ThingPrefabs;
	public static Dictionary<string, PackedScene> thingsPrefabs = new Dictionary<string, PackedScene>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (var thing in _ThingPrefabs)
		{
			SceneState sceneState = thing.GetState();
			GD.Print("Thing Name: "+ sceneState.GetNodeName(0));
			thingsPrefabs.Add(sceneState.GetNodeName(0), thing);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
