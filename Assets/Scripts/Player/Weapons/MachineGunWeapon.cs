using Godot;
using System;
public partial class MachineGunWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .017f; } } // tan(2) / 2
	public override float maxDispersion { get { return .049f; } } // tan(5.6) / 2

	public string caseName;

	public float maxRange = 400f;

	public float barrelSpeed = 400;

	private float currentRotSpeed = 0;

	protected override void OnUpdate()
	{
		if (playerInfo.Ammo[0] <= 0 && fireTime < .1f)
			putAway = true;
	}

	protected override void OnInit()
	{
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[0] <= 0)
			return false;

		playerInfo.Ammo[0]--;

		//maximum fire rate 20/s, unless you use negative number (please don't)
		fireTime = _fireRate + .05f;
		coolTimer = 0f;

		return true;
	}
	protected override Quaternion GetRotate(float deltaTime)
	{
		if (fireTime > 0f)
		{
			currentRotSpeed += barrelSpeed * deltaTime;
			if (currentRotSpeed < -180)
				currentRotSpeed += 360;
			if (currentRotSpeed > 180)
				currentRotSpeed -= 360;
		}
		return new Quaternion(Vector3.Right, currentRotSpeed);
	}
}
