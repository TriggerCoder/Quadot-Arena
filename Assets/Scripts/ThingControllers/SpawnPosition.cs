using Godot;
using System;
public partial class SpawnPosition : Node3D
{
	public override void _Ready()
	{
		SpawnerManager.AddToList(GetParentNode3D());
	}

}
