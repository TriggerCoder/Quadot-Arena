using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class PlayerThing : CharacterBody3D, Damageable
{
	public PlayerInfo playerInfo;
	[Export]
	public PlayerControls playerControls;

	public PlayerCamera playerCamera;

	public string modelName = "sarge";
	public string skinName = "default";

//	public PlayerModel avatar;

	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }
	public BloodType BloodColor { get { return BloodType.Red; } }

	public int hitpoints = 100;
	public int armor = 0;
	public float painTime = 0f;
	public float lookTime = .5f;
	public bool finished = false;
	public bool radsuit = false;
	public bool invul = false;
	public bool ready = false;
	private enum LookType
	{
		Left = 0,
		Center = 1,
		Right = 2
	}
	private LookType whereToLook = LookType.Center;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Impulse(Vector3 direction, float force)
	{
		float length = force / 80;

		//Gravity will be the only force down
		Vector3 impulse = direction * length;
		float magnitude = impulse.Length();
		impulse.Y = 0;
		impulse = impulse.Normalized() * magnitude;

		playerControls.impulseVector += impulse;
	}
	public void Damage(int amount, DamageType damageType = DamageType.Generic, Node3D attacker = null)
	{
		if (Dead)
			return;
	}
}
