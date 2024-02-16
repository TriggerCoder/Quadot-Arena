using Godot;
using System.Collections;
using System.Collections.Generic;
using ExtensionMethods;
public partial class PortalSurface : Area3D
{
	public bool mirror = false;
	public string targetName;
	private Camera3D destCamera;
	private Portal destPortal;
	private float radius = 256 * GameManager.sizeDividor;
	private float radiusSquared;
	private List<PlayerThing> currentPlayers = new List<PlayerThing>();
	private Transform3D MirrorTransform;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExit;
		radiusSquared = radius * radius;
		RenderingServer.FramePreDraw += () => OnPreRender();
	}

	private Transform3D MirrorTransform3D(Vector3 n, Vector3 d)
	{
		Vector3 BasisX = new Vector3(1, 0, 0) - 2 * new Vector3(n.X * n.X, n.X * n.Y, n.X * n.Z);
		Vector3 BasisY = new Vector3(0, 1, 0) - 2 * new Vector3(n.Y * n.X, n.Y * n.Y, n.Y * n.Z);
		Vector3 BasisZ = new Vector3(0, 0, 1) - 2 * new Vector3(n.Z * n.X, n.Z * n.Y, n.Z * n.Z);

		Vector3 offset = 2 * n.Dot(d) * n;
		return new Transform3D(new Basis(BasisX, BasisY, BasisZ), offset);
	}

	public void OnPreRender()
	{
		for (int i = 0; i < currentPlayers.Count; i++) 
		{
			Camera3D playerCamera = currentPlayers[i].playerInfo.playerCamera.CurrentCamera;
			ClusterPVSManager.CheckPVS(currentPlayers[i].playerInfo.viewLayer, destCamera.GlobalPosition);
			Basis globalBasis = playerCamera.GlobalTransform.Basis;
			if (mirror)
			{
				destCamera.GlobalTransform = MirrorTransform * playerCamera.GlobalTransform;
				destCamera.GlobalTransform = destCamera.GlobalTransform.LookingAt(destCamera.GlobalPosition / 2 + playerCamera.GlobalPosition / 2, GlobalTransform.UpVector());
				Vector3 offSet = GlobalPosition - destCamera.GlobalPosition;
				float near = Mathf.Abs((offSet).Dot(destPortal.normal));
				near += 0.15f;

				Vector3 localCam = destCamera.GlobalTransform.Basis.Inverse() * offSet;
				Vector2 frustumOffset = new Vector2(localCam.X, localCam.Y);
				destCamera.SetFrustum(destPortal.size.X, frustumOffset, near, 4000);
			}
			else
			{
				float distanceSquared = (GlobalPosition - playerCamera.GlobalPosition).LengthSquared();
				float lenght = Mathf.Clamp(1.3f - (distanceSquared / radiusSquared), 0f, 1f);
				destPortal.material.SetShaderParameter("Transparency", lenght);
				destCamera.Basis = globalBasis;
			}
		}
	}

	public void SetUpPortal(Camera3D camera, Portal portal, bool isMirror = false)
	{
		destCamera = camera;
		destPortal = portal;
		Node3D parent = camera.GetParentNode3D();

		mirror = isMirror;
		if (mirror)
		{
			radius *= 2;
			GlobalPosition = portal.position;
			destPortal.material.SetShaderParameter("Mirror", 1f);
			destPortal.material.SetShaderParameter("Transparency", .65f);
			MirrorTransform = MirrorTransform3D(destPortal.normal, GlobalPosition);
			PlaneMesh plane = new PlaneMesh();
			plane.Size = new Vector2(portal.size.X, portal.size.Y);
			plane.Orientation = PlaneMesh.OrientationEnum.Z;
			plane.SurfaceSetMaterial(0,destPortal.material);
			destPortal.mesh.Position = GlobalPosition;
			destPortal.mesh.Mesh = plane;
		}
		else
			parent.Rotation += Transform3D.Identity.LookingAt(-portal.normal, Vector3.Up).Basis.GetEuler();

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

		if (mirror)
			viewport.Size = new Vector2I(Mathf.CeilToInt(320 * portal.size.X), Mathf.CeilToInt(320 * portal.size.Y));
		else
			viewport.Size = new Vector2I(1280,720);

		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.WhenParentVisible;
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
