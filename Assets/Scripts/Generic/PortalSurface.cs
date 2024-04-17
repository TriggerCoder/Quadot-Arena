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
	private Vector3 UpVector;
	private Vector2 MirrorSize;
	private enum Axis
	{
		None,
		X,
		Y,
		Z
	}
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
				Vector3 lookVector = destCamera.GlobalPosition / 2 + playerCamera.GlobalPosition / 2;
				destCamera.GlobalTransform = destCamera.GlobalTransform.LookingAt(lookVector, UpVector);
				Vector3 offSet = GlobalPosition - destCamera.GlobalPosition;
				float near = Mathf.Abs((offSet).Dot(destPortal.normal));
				near += 0.15f;

				Vector3 localCam = destCamera.GlobalTransform.Basis.Inverse() * offSet;
				Vector2 frustumOffset = new Vector2(localCam.X, localCam.Y);
				destCamera.SetFrustum(MirrorSize.X, frustumOffset, near, 4000);
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
		Node3D parent = destCamera.GetParentNode3D();

		mirror = isMirror;
		if (mirror)
		{
			radius *= 2;
			GlobalPosition = portal.position;
			destPortal.material = MaterialManager.GetMirrorMaterial(destPortal.shaderName);
			destPortal.baseMat.NextPass = destPortal.material;
			MirrorTransform = MirrorTransform3D(destPortal.normal, GlobalPosition);
			if (Mathf.IsZeroApprox(portal.normal.Dot(Vector3.Forward)))
				UpVector = Vector3.Forward;
			else
			{
				Vector3 normal = portal.normal.Cross(Vector3.Up);
				if (normal.LengthSquared() > 0)
					UpVector = Vector3.Up;
				else
					UpVector = Vector3.Forward;
			}
			MirrorSize = FillMirrorData();
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
			viewport.Size = new Vector2I(Mathf.CeilToInt(320 * MirrorSize.X), Mathf.CeilToInt(320 * MirrorSize.Y));
		else
			viewport.Size = new Vector2I(1280,720);

		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		viewport.HandleInputLocally = false;

		var CamRID = destCamera.GetCameraRid();
		var viewPortRID = viewport.GetViewportRid();
		RenderingServer.ViewportAttachCamera(viewPortRID, CamRID);

		destPortal.material.SetShaderParameter("Tex_0", viewport.GetTexture());
	}

	private Vector2 FillMirrorData()
	{
		MeshDataTool meshDataTool = new MeshDataTool();
		meshDataTool.CreateFromSurface(destPortal.arrMesh, 0);
		Vector3 min = Vector3.One * float.MaxValue;
		Vector3 max = Vector3.One * float.MinValue;
		Vector2 size = Vector2.One;

		Axis axis;
		Quaternion changeRotation = Quaternion.Identity;

		if ((destPortal.normal.X == 1) || (destPortal.normal.X == -1))
			axis = Axis.X;
		else if ((destPortal.normal.Y == 1) || (destPortal.normal.Y == -1))
			axis = Axis.Y;
		else if ((destPortal.normal.Z == 1) || (destPortal.normal.Z == -1))
			axis = Axis.Z;
		else
		{
			GameManager.Print("Mirror is Rotated");
			float x = Mathf.Abs(destPortal.normal.X), y = Mathf.Abs(destPortal.normal.Y), z = Mathf.Abs(destPortal.normal.Z);
			Vector3 normalRef = Vector3.Zero;

			if ((x >= y) && (x >= z))
				axis = Axis.X;
			else if ((y >= x) && (y >= z))
				axis = Axis.Y;
			else
				axis = Axis.Z;

			switch (axis)
			{
				case Axis.X:
					if (destPortal.normal.X > 0)
						normalRef = Vector3.Right;
					else
						normalRef = Vector3.Left;
					break;
				case Axis.Y:
					if (destPortal.normal.Y > 0)
						normalRef = Vector3.Up;
					else
						normalRef = Vector3.Down;
					break;
				case Axis.Z:
					if (destPortal.normal.Z > 0)
						normalRef = Vector3.Back;
					else
						normalRef = Vector3.Forward;
					break;
			}
			changeRotation = CalculateRotation(destPortal.normal, normalRef);
		}

		float numVert = meshDataTool.GetVertexCount();
		for (int i = 0; i < numVert; i++)
		{
			Vector3 vertex = meshDataTool.GetVertex(i);
			if (vertex.X > max.X)
				max.X = vertex.X;
			if (vertex.Y > max.Y)
				max.Y = vertex.Y;
			if (vertex.Z > max.Z)
				max.Z = vertex.Z;

			if (vertex.X < min.X)
				min.X = vertex.X;
			if (vertex.Y < min.Y)
				min.Y = vertex.Y;
			if (vertex.Z < min.Z)
				min.Z = vertex.Z;
		}

		max = changeRotation * max;
		min = changeRotation * min;

		for (int i = 0; i < numVert; i++)
		{
			Vector3 vertex = changeRotation * meshDataTool.GetVertex(i);
			Vector2 uv2 = Vector2.Zero;
			switch (axis)
			{
				case Axis.X:
					uv2 = new Vector2(GetUV2RangeValue(vertex.Y, min.Y, max.Y), GetUV2RangeValue(vertex.Z, min.Z, max.Z));
					if (i == 0)
						size = new Vector2(max.Y - min.Y, max.Z - min.Z);
				break;
				case Axis.Y:
					uv2 = new Vector2(GetUV2RangeValue(vertex.X, min.X, max.X), GetUV2RangeValue(vertex.Z, min.Z, max.Z));
					if (i == 0)
					{
						if (destPortal.normal.Y < 0)
							destPortal.material.SetShaderParameter("InvertX", 0);
						destPortal.material.SetShaderParameter("InvertY", 0);
						size = new Vector2(max.X - min.X, max.Z - min.Z);
					}
				break;
				case Axis.Z:
					uv2 = new Vector2(GetUV2RangeValue(vertex.X, min.X, max.X), GetUV2RangeValue(vertex.Y, min.Y, max.Y));
					if (i == 0)
						size = new Vector2(max.X - min.X, max.Y - min.Y);;
				break;
			}
			meshDataTool.SetVertexUV2(i, uv2);
		}
		destPortal.arrMesh.ClearSurfaces();
		meshDataTool.CommitToSurface(destPortal.arrMesh);
		return size;
	}

	private float GetUV2RangeValue(float X, float Min, float Max)
	{
		return (X - Min) / (Max - Min);
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
//				GameManager.Print("Why does " + other.Name + " DARES to enter my dominion " + Name);
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
//				GameManager.Print("Finally " + other.Name + " got scared of my dominion " + Name);
			}
		}
	}
	private static Quaternion CalculateRotation(Vector3 normal1, Vector3 normal2)
	{
		float dotProduct = normal1.Dot(normal2);
		float angle = Mathf.RadToDeg(Mathf.Acos(dotProduct));

		Vector3 crossProduct = normal1.Cross(normal2);
		Vector3 axis = crossProduct.Normalized();

		return new Quaternion(axis, angle);
	}
}
