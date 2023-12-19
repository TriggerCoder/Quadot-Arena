using Godot;
using ExtensionMethods;

public partial class InterpolatedTransform : Node3D
{
	[Export]
	public Node3D Source;

	private bool update = false;
	private Transform3D prev;
	private Transform3D current;

	public override void _Ready()
	{
		TopLevel = true;
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

}
