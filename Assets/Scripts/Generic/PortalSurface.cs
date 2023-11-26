using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class PortalSurface : Area3D
{
	public string targetName;
	private Camera3D destCamera;
	private ThingsManager.Portal destPortal;
	private float radius = 256 * GameManager.sizeDividor;
	private List<PlayerThing> currentPlayers = new List<PlayerThing>();
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExit;
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < currentPlayers.Count; i++) 
		{
			Camera3D playerCamera = currentPlayers[i].playerInfo.playerCamera.CurrentCamera;
			Vector3 distance = (GlobalPosition - playerCamera.GlobalPosition);
//			destCamera.Position = distance;
			destCamera.Basis = playerCamera.GlobalTransform.Basis;
			int currentFrame = (Engine.GetFramesDrawn() << GameManager.Player1UIViewLayer);
			currentPlayers[i].playerInfo.CheckPVS(currentFrame, destCamera.GlobalPosition);
			float lenght = Mathf.Clamp(1.3f - (distance.Length() / radius), 0f, 1f);
			destPortal.material.SetShaderParameter("Transparency", lenght);
		}
	}

	public void SetUpPortal(Camera3D camera, ThingsManager.Portal portal)
	{
		destCamera = camera;
		destPortal = portal;
		Node3D parent = (Node3D)camera.GetParent();
		parent.Rotation += Transform3D.Identity.LookingAt(-portal.normal, Vector3.Up).Basis.GetEuler();

		CollisionShape3D mc = new CollisionShape3D();
		mc.Name = "Portal Area";
		AddChild(mc);
		CollisionLayer = (1 << GameManager.ColliderLayer);
		CollisionMask = GameManager.TakeDamageMask;

		SphereShape3D sphere = new SphereShape3D();
		sphere.Radius = radius;
		mc.Shape = sphere;

		SubViewport viewport = new SubViewport();
		AddChild(viewport);
		viewport.Size = new Vector2I(1280, 720);
		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		viewport.HandleInputLocally = false;

		var CamRID = camera.GetCameraRid();
		var viewPortRID = viewport.GetViewportRid();
		RenderingServer.ViewportAttachCamera(viewPortRID, CamRID);

		destPortal.material.SetShaderParameter("Tex_" + destPortal.texNum, viewport.GetTexture());
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
