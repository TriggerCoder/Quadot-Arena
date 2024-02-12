using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class PortalSurface : Area3D
{
	public bool mirror = false;
	public string targetName;
	private Camera3D destCamera;
	private Portal destPortal;
	private float radius = 256 * GameManager.sizeDividor;
	private float radiusSquared;
	private List<PlayerThing> currentPlayers = new List<PlayerThing>();
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExit;
		radiusSquared = radius * radius;
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < currentPlayers.Count; i++) 
		{
			Camera3D playerCamera = currentPlayers[i].playerInfo.playerCamera.CurrentCamera;
			ClusterPVSManager.CheckPVS(currentPlayers[i].playerInfo.viewLayer, destCamera.GlobalPosition);
			Basis globalBasis = playerCamera.GlobalTransform.Basis;

			if (mirror)
			{
//				Vector3 forward = -globalBasis.Z;
//				Vector3 reflection = destPortal.normal * 2 * (forward.Dot(destPortal.normal)) - forward;
				if (!currentPlayers[i].playerInfo.playerCamera.currentThirdPerson)
				{
					Vector3 globalPosition = destCamera.GlobalPosition;
					globalPosition.Y = playerCamera.GlobalPosition.Y;
					destCamera.GlobalPosition = globalPosition;
				}
//				globalBasis = Transform3D.Identity.LookingAt(reflection, Vector3.Down).Basis;
			}
			else
			{
				float distanceSquared = (GlobalPosition - playerCamera.GlobalPosition).LengthSquared();
				float lenght = Mathf.Clamp(1.3f - (distanceSquared / radiusSquared), 0f, 1f);
				destPortal.material.SetShaderParameter("Transparency", lenght);
			}
			destCamera.Basis = globalBasis;
		}
	}

	public void SetUpPortal(Camera3D camera, Portal portal, bool isMirror = false)
	{
		int inverse = -1;
		destCamera = camera;
		destPortal = portal;
		Node3D parent = camera.GetParentNode3D();

		mirror = isMirror;
		if (mirror)
		{
			inverse = 1;
			radius *= 2;
			parent.GlobalPosition = portal.position;// - portal.normal * .75f;
//			parent.Scale = new Vector3(1.0f, -1.0f, 1.0f);
			parent.Scale = new Vector3(-1.0f, 1.0f, 1.0f);
			destPortal.material.SetShaderParameter("Transparency", .65f);
		}

		parent.Rotation += Transform3D.Identity.LookingAt(portal.normal * inverse, Vector3.Up).Basis.GetEuler();

		CollisionShape3D mc = new CollisionShape3D();
		mc.Name = "Portal Area";
		AddChild(mc);
		CollisionLayer = (1 << GameManager.WalkTriggerLayer);
		CollisionMask = GameManager.TakeDamageMask;

		SphereShape3D sphere = new SphereShape3D();
		sphere.Radius = radius;
		mc.Shape = sphere;

		SubViewport viewport = new SubViewport();
		AddChild(viewport);

		viewport.Size = new Vector2I(1280,720);
		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		viewport.HandleInputLocally = false;

		var CamRID = camera.GetCameraRid();
		var viewPortRID = viewport.GetViewportRid();
		RenderingServer.ViewportAttachCamera(viewPortRID, CamRID);

		destPortal.material.SetShaderParameter("Tex_0", viewport.GetTexture());
	}
	void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (other is PlayerThing playerThing)
		{
			if (!currentPlayers.Contains(playerThing))
			{
				currentPlayers.Add(playerThing);
				GameManager.Print("Why does " + other.Name + " DARES to enter my dominion " + Name);
			}
		}
	}
	void OnBodyExit(Node3D other)
	{
		if (GameManager.Paused)
			return;

		if (other is PlayerThing playerThing)
		{
			if (currentPlayers.Contains(playerThing))
			{
				currentPlayers.Remove(playerThing);
				GameManager.Print("Finally " + other.Name + " got scared of my dominion " + Name);
			}
		}
	}
}
