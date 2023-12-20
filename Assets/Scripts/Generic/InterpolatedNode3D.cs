using Godot;
using System;
public partial class InterpolatedNode3D : Node3D
{
	public event Action SetTransformReset;

	public void InvoqueSetTransformReset()
	{
		SetTransformReset();
	}
}
