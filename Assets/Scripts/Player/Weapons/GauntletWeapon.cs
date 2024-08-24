using Godot;
using System;
using ExtensionMethods;
public partial class GauntletWeapon : PlayerWeapon
{
	public override float verticalDispersion { get { return .017f; } } // tan(2) / 2
	public override float horizontalDispersion { get { return .049f; } } // tan(5.6) / 2

	public float maxRange = 1f;

	[Export]
	public MultiAudioStream humStream;
	[Export]
	public string _humSound;
	[Export]
	public float HasteFireRate = 3;

	public float barrelSpeed = 10;
	public float hastebarrelSpeed = 11;
	private float currentRotSpeed = 0;

	private int lastHit = 0;
	protected override void OnUpdate(float deltaTime)
	{
		if (fireTime <= 0)
		{
			if (humStream.Playing)
				humStream.Stop();
		}
	}

	protected override void OnInit()
	{
		_hasteFireRate = HasteFireRate;
		if (Sounds.Length > 2)
		{
			audioStream.Stream = Sounds[2];
			audioStream.Play();
		}
		humStream.Stream = SoundManager.LoadSound(_humSound, true);
		playerInfo.playerPostProcessing.playerHUD.HideAmmo(true);
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (GameOptions.UseMuzzleLight)
		{
			if (muzzleLight != null)
			{
				muzzleLight.LightEnergy = 1.0f;
				if (muzzleObject != null)
					if (!muzzleObject.Visible)
					{
						muzzleObject.Visible = true;
						playerInfo.playerThing.avatar.MuzzleFlashSetActive(true);
					}
			}
		}

		if (fireTime <= 0)
		{
			if (hasQuad)
				SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(quadSound));

			humStream.Play();
		}

		//maximum fire rate 20/s, unless you use negative number (please don't)
		float currentFireRate = _fireRate;
		if (playerInfo.haste)
			currentFireRate = _hasteFireRate;

		fireTime = .15f;
		coolTimer = 0f;

		//Hitscan attack
		if (lastHit > 0)
			lastHit --;
		else if (lastHit <= 0)
		{
			Transform3D global = playerInfo.playerCamera.GlobalTransform;
			Vector3 d = global.ForwardVector();
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, (GameManager.TakeDamageMask & ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.RagdollLayer))));
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.IntersectRay(RayCast);
			if (hit.Count > 0)
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				if (collider is Damageable damageable)
				{
					lastHit = (int)currentFireRate;
					if (hasQuad)
						damageable.Damage(GD.RandRange(DamageMin * GameManager.Instance.QuadMul, DamageMax * GameManager.Instance.QuadMul), DamageType.Melee, playerInfo.playerThing);
					else
						damageable.Damage(GD.RandRange(DamageMin, DamageMax), DamageType.Melee, playerInfo.playerThing);
					if (Sounds.Length > 0)
					{
						audioStream.Stream = Sounds[0];
						audioStream.Play();
					}
					return true;
				}

			}
		}

		return false;
	}
	protected override Quaternion RotateBarrel(float deltaTime)
	{
		if (fireTime > 0f)
		{
			float currenBarrelSpeed = barrelSpeed;
			if (playerInfo.haste)
				currenBarrelSpeed = hastebarrelSpeed;
			currentRotSpeed += currenBarrelSpeed * deltaTime;
			if (currentRotSpeed >= 360)
				currentRotSpeed -= 360;
		}
		return new Quaternion(Vector3.Left, currentRotSpeed);
	}
}
