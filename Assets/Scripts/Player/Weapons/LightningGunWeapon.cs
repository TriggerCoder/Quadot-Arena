using Godot;
using System;
using ExtensionMethods;

public partial class LightningGunWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .01f; } }
	public override float maxDispersion { get { return .02f; } }

	public string onDeathSpawn = "LightningExplosion";
	public string decalMark = "LightningMark";
	public float maxRange = 24f;
	public Vector3 spawnPos;
	[Export]
	public Node3D lightningBolt;
	[Export]
	public MeshInstance3D[] Arcs;
	[Export]
	public ShaderMaterial boltMaterial;
	[Export]
	public MultiAudioStream humStream;
	[Export]
	public string[] _humSounds;
	public AudioStreamWav[] humSounds;

	private float BoltLenght = 24f;
	private float BoltWidht = 2f;
	private Quaternion baseRotation;
	private ArrayMesh mesh;
	private MeshDataTool meshDataTool = new MeshDataTool();
	public enum CurrentHum
	{
		None,
		Idle,
		Fire
	}

	private CurrentHum currentHum = CurrentHum.None;
	protected override void OnUpdate(float deltaTime)
	{
		if (playerInfo.Ammo[PlayerInfo.lightningAmmo] <= 0 && fireTime < .1f)
		{
			if ((!putAway) && (Sounds.Length > 1))
			{
				audioStream.Stream = Sounds[1];
				audioStream.Play();
			}
			putAway = true;
		}

		if (fireTime <= 0)
		{
			if (currentHum != CurrentHum.Idle)
			{
				humStream.Stream = humSounds[0];
				humStream.Play();
				currentHum = CurrentHum.Idle;
			}
			lightningBolt.Hide();
			BoltLenght = 24f;
			lightningBolt.Quaternion = baseRotation;
		}
	}
	protected override void OnInit()
	{
		if (Sounds.Length > 2)
		{
			audioStream.Stream = Sounds[2];
			audioStream.Play();
		}

		humSounds = new AudioStreamWav[_humSounds.Length];
		for (int i = 0; i < _humSounds.Length; i++)
			humSounds[i] = SoundManager.LoadSound(_humSounds[i], true);

		playerInfo.playerPostProcessing.playerHUD.UpdateAmmoType(PlayerInfo.lightningAmmo);
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.lightningAmmo]);
		mesh = Mesher.GenerateQuadMesh(BoltWidht, BoltLenght, 0.5f, 0f);
		mesh.SurfaceSetMaterial(0, boltMaterial);
		meshDataTool.CreateFromSurface(mesh, 0);

		for (int i = 0; i < Arcs.Length; i++)
		{
			Arcs[i].Mesh = mesh;
			Arcs[i].Layers = GameManager.AllPlayerViewMask;
		}

		humStream.Stream = humSounds[0];
		humStream.Play();
		currentHum = CurrentHum.Idle;
		lightningBolt.Position = MuzzleOffset;
//		lightningBolt.Reparent(muzzleObject, false);
		baseRotation = lightningBolt.Quaternion;
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[PlayerInfo.lightningAmmo] <= 0)
			return false;

		playerInfo.Ammo[PlayerInfo.lightningAmmo]--;
		playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(playerInfo.Ammo[PlayerInfo.lightningAmmo]);

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
			if (Sounds.Length > 0)
			{
				audioStream.Stream = Sounds[0];
				audioStream.Play();
			}

			if (hasQuad)
				SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(quadSound));

			if (currentHum != CurrentHum.Fire)
			{
				humStream.Stream = humSounds[1];
				humStream.Play();
				currentHum = CurrentHum.Fire;
			}

			lightningBolt.Show();
		}
		//maximum fire rate 20/s, unless you use negative number (please don't)
		fireTime = _fireRate + .05f;
		coolTimer = 0f;

		//Hitscan attack
		{
			Transform3D global = playerInfo.playerCamera.GlobalTransform;
			Vector3 d = global.ForwardVector();
			Vector2 r = GetDispersion();
			d += global.RightVector() * r.X + global.UpVector() * r.Y;
			d = d.Normalized();
			Vector3 Origin = playerInfo.playerCamera.GlobalPosition;
			Vector3 End = Origin - d * maxRange;
			var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | ~((playerInfo.playerLayer) | (1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.RagdollLayer))));
			var SpaceState = GetWorld3D().DirectSpaceState;
			var hit = SpaceState.IntersectRay(RayCast);
			if (hit.Count > 0)
			{
				CollisionObject3D collider = (CollisionObject3D)hit["collider"];
				Vector3 collision = (Vector3)hit["position"];
				Vector3 normal = (Vector3)hit["normal"];

				lightningBolt.LookAt(collision);

				Node3D LightningExplosion = (Node3D)ThingsManager.thingsPrefabs[onDeathSpawn].Instantiate();
				GameManager.Instance.TemporaryObjectsHolder.AddChild(LightningExplosion);
				LightningExplosion.Position = collision + (normal * .2f);
				LightningExplosion.SetForward(normal);
				LightningExplosion.Rotate(LightningExplosion.UpVector(), -Mathf.Pi * .5f);
				LightningExplosion.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				if (Sounds.Length > 3)
					SoundManager.Create3DSound(collision, Sounds[GD.RandRange(3, Sounds.Length - 1)]);

				Vector3 direction = (lightningBolt.GlobalPosition - collision);
				BoltLenght = direction.Length();
				meshDataTool.SetVertex(2, new Vector3(-.5f * BoltWidht, BoltLenght, 0));
				meshDataTool.SetVertex(3, new Vector3(.5f * BoltWidht, BoltLenght, 0));
				meshDataTool.SetVertexUV(2, new Vector2(0, BoltLenght / 2f));
				meshDataTool.SetVertexUV(3, new Vector2(1, BoltLenght / 2f));
				mesh.ClearSurfaces();
				meshDataTool.CommitToSurface(mesh);

				if (CheckIfCanMark(SpaceState, collider, collision))
				{
					Node3D BulletMark = (Node3D)ThingsManager.thingsPrefabs[decalMark].Instantiate();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(BulletMark);
					BulletMark.Position = collision + (normal * .05f);
					BulletMark.SetForward(-normal);
					BulletMark.Rotate((BulletMark.UpVector()).Normalized(), -Mathf.Pi * .5f);
					BulletMark.Rotate(normal, (float)GD.RandRange(0, Mathf.Pi * 2.0f));
				}
			}
			else
				lightningBolt.Quaternion = baseRotation;
		}
		return true;
	}
}