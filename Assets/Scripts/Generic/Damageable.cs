using Godot;
public enum DamageType
{
	Generic,
	Rocket,
	Grenade,
	Plasma,
	Lightning,
	BFGBall,
	BFGBlast,
	Explosion,
	Environment,
	Crusher,
	Telefrag,
	Electric,
	Drown,
	Bullet,
	Pellet,
	Melee,
	Rail,
	Land,
	Fall,
	Trigger
}

public enum BloodType
{
	Red,
	Blue,
	Green,
	None
}

public interface Damageable
{
	int Hitpoints { get; }
	bool Dead { get; }
	bool Bleed { get; }
	BloodType BloodColor { get; }
	void Damage(int amount, DamageType damageType = DamageType.Generic, Node3D attacker = null);
	void Impulse(Vector3 direction, float force);
}