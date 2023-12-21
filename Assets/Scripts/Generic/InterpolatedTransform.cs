using Godot;
using ExtensionMethods;
using System;

public partial class InterpolatedTransform : Node3D
{
	[Export]
	public Node3D Source;
	[Export]
	public InterpolatedNode3D interpolationReset;

	private bool update = false;
	private Transform3D prev;
	private Transform3D current;

	public override void _Ready()
	{
		TopLevel = true;

		if (interpolationReset != null) 
			interpolationReset.SetTransformReset += ResetTransform;

		if (Source == null)
			Source = GetParentNode3D();

		if (Source == null)
			return;

		GlobalTransform = Source.GlobalTransform;
	}

	public void SetSource(Node3D source)
	{
		Source = source;
		GlobalTransform = Source.GlobalTransform;
	}

	public void SetInterpolationReset(InterpolatedNode3D interpolated) 
	{
		interpolationReset = interpolated;
		if (interpolationReset != null)
			interpolationReset.SetTransformReset += ResetTransform;
	}
	public void ResetTransform()
	{
		current = Source.GlobalTransform;
		prev = current;
		GlobalTransform = current;
	}
	public void UpdateTransform()
	{
		prev = current;
		current = Source.GlobalTransform;
		update = false;
	}

	public override void _Process(double delta)
	{
		if (update) 
			UpdateTransform();

		float deltaT = (float)Mathf.Clamp(Engine.GetPhysicsInterpolationFraction(), 0, 1);
//		GlobalTransform = prev.InterpolateWith(current, deltaT);
		GlobalTransform = prev.Lerp(current, deltaT);
	}

	public override void _PhysicsProcess(double delta)
	{
		update = true;
	}

	public override void _ExitTree()
	{
		if (interpolationReset != null)
			interpolationReset.SetTransformReset -= ResetTransform;
	}
}
