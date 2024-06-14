using Godot;
using System;

public partial class ParticlesController : GpuParticles3D
{
	[Export]
	public float lifeTime = 0;
	[Export]
	public bool enableLifeTime = false;
	public override void _Ready()
	{

	}

	public override void _Process(double delta)
	{
		if (enableLifeTime)
		{
			float deltaTime = (float)delta;
			lifeTime -= deltaTime;
			if (lifeTime < 0)
			{
				OneShot = true;
				SetProcess(false);
			}
		}
	}
}
