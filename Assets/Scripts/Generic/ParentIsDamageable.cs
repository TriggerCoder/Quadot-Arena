using Godot;
using System;

public partial class ParentIsDamageable : StaticBody3D, Damageable
{
	public Damageable parent = null;
	public int Hitpoints { get { return parent.Hitpoints; } }
	public bool Dead { get { return parent.Dead; } }
	public bool Bleed { get { return parent.Bleed; } }
	public BloodType BloodColor { get { return parent.BloodColor; } }

	public void Damage(int amount, DamageType damageType = DamageType.Generic, Node3D attacker = null)
	{
		if (parent == null)
			return;

		parent.Damage(amount, damageType, attacker);
	}
	public void Impulse(Vector3 direction, float force)
	{
		if (parent == null)
			return;

		parent.Impulse(direction, force);
	}
}