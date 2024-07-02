using Godot;
using System.Collections;
using System.Collections.Generic;
using ExtensionMethods;

public partial class PortalSurface : Area3D
{
	public bool mirror = false;
	public string targetName;
	private List<Camera3D> destCamera = new List<Camera3D>();
	private List<SubViewport> viewPorts = new List<SubViewport>();
	private Portal destPortal;
	private float radius = 256 * GameManager.sizeDividor;
	private float radiusSquared;
	private List<PlayerThing> currentPlayers = new List<PlayerThing>();
	private Transform3D MirrorTransform;
	private Vector3 UpVector;
	private Vector2 MirrorSize;

	private bool InvertX = true;
	private bool InvertY = true;

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
	}

	private Transform3D MirrorTransform3D(Vector3 n, Vector3 d)
	{
		Vector3 BasisX = new Vector3(1, 0, 0) - 2 * new Vector3(n.X * n.X, n.X * n.Y, n.X * n.Z);
		Vector3 BasisY = new Vector3(0, 1, 0) - 2 * new Vector3(n.Y * n.X, n.Y * n.Y, n.Y * n.Z);
		Vector3 BasisZ = new Vector3(0, 0, 1) - 2 * new Vector3(n.Z * n.X, n.Z * n.Y, n.Z * n.Z);

		Vector3 offset = 2 * n.Dot(d) * n;
		return new Transform3D(new Basis(BasisX, BasisY, BasisZ), offset);
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < currentPlayers.Count; i++) 
		{
			int playerNum = currentPlayers[i].playerInfo.localPlayerNum;
			viewPorts[playerNum].RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
			Camera3D playerCamera = currentPlayers[i].playerInfo.playerCamera.CurrentCamera;
			Basis globalBasis = playerCamera.GlobalTransform.Basis;
			if (mirror)
			{
				destCamera[playerNum].GlobalTransform = MirrorTransform * playerCamera.GlobalTransform;
				Vector3 lookVector = destCamera[playerNum].GlobalPosition / 2 + playerCamera.GlobalPosition / 2;
				destCamera[playerNum].GlobalTransform = destCamera[playerNum].GlobalTransform.LookingAt(lookVector, UpVector);
				Vector3 offSet = GlobalPosition - destCamera[playerNum].GlobalPosition;
				float near = Mathf.Abs((offSet).Dot(destPortal.normal));
				near += 0.15f;

				Vector3 localCam = destCamera[playerNum].GlobalTransform.Basis.Inverse() * offSet;
				Vector2 frustumOffset = new Vector2(localCam.X, localCam.Y);
				destCamera[playerNum].SetFrustum(MirrorSize.X, frustumOffset, near, 4000);
			}
			else
			{
				float distanceSquared = (GlobalPosition - playerCamera.GlobalPosition).LengthSquared();
				float lenght = Mathf.Clamp(1.3f - (distanceSquared / radiusSquared), 0f, 1f);
				destPortal.surfaces[playerNum].material.SetShaderParameter("Transparency", lenght);
				destCamera[playerNum].Basis = globalBasis;
				ClusterPVSManager.CheckPVS(currentPlayers[i].playerInfo.viewLayer, destCamera[playerNum].GlobalPosition);
			}
		}
	}

	public void NewLocalPlayerAdded()
	{
		int index = destCamera.Count;
		Node3D parent = destCamera[0].GetParentNode3D();
		Camera3D camera = (Camera3D)destCamera[0].Duplicate();
		parent.AddChild(camera);
		destCamera.Add(camera);

		MeshInstance3D mesh = new MeshInstance3D();
		GameManager.Instance.TemporaryObjectsHolder.AddChild(mesh);
		mesh.Layers = (uint)(1 << (GameManager.Player1ViewLayer + index));
		mesh.Mesh = destPortal.commonMesh;
		ShaderMaterial material = (ShaderMaterial)destPortal.commonMat.Duplicate(true);
		mesh.SetSurfaceOverrideMaterial(0, material);
		destPortal.surfaces.Add(new Portal.Surface(mesh, material));

		if (mirror)
		{
			destPortal.surfaces[index].material = MaterialManager.GetMirrorMaterial(destPortal.shaderName);
			destPortal.surfaces[index].baseMat.NextPass = destPortal.surfaces[index].material;
			if (!InvertX)
				destPortal.surfaces[index].material.SetShaderParameter("InvertX", 0);
			if (!InvertY)
				destPortal.surfaces[index].material.SetShaderParameter("InvertY", 0);

		}

		SubViewport viewport = new SubViewport();
		AddChild(viewport);

		if (mirror)
			viewport.Size = new Vector2I(Mathf.CeilToInt(320 * MirrorSize.X), Mathf.CeilToInt(320 * MirrorSize.Y));
		else
			viewport.Size = GameManager.Instance.viewPortSize;

		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		viewport.HandleInputLocally = false;

		var CamRID = camera.GetCameraRid();
		var viewPortRID = viewport.GetViewportRid();
		RenderingServer.ViewportAttachCamera(viewPortRID, CamRID);

		destPortal.surfaces[index].material.SetShaderParameter("Tex_0", viewport.GetTexture());
		viewPorts.Add(viewport);
	}

	public void SetUpPortal(Camera3D camera, Portal portal, bool isMirror = false)
	{
		destCamera.Add(camera);
		destPortal = portal;
		Node3D parent = camera.GetParentNode3D();

		mirror = isMirror;
		if (mirror)
		{
			radius *= 2;
			GlobalPosition = portal.position;
			destPortal.surfaces[0].material = MaterialManager.GetMirrorMaterial(destPortal.shaderName);
			destPortal.surfaces[0].baseMat.NextPass = destPortal.surfaces[0].material;
			MirrorTransform = MirrorTransform3D(destPortal.normal, GlobalPosition);
			if (Mathf.IsZeroApprox(destPortal.normal.Dot(Vector3.Forward)))
				UpVector = Vector3.Forward;
			else
			{
				Vector3 normal = destPortal.normal.Cross(Vector3.Up);
				if (normal.LengthSquared() > 0)
					UpVector = Vector3.Up;
				else
					UpVector = Vector3.Forward;
			}
			MirrorSize = FillMirrorData();
			if (!InvertX)
				destPortal.surfaces[0].material.SetShaderParameter("InvertX", 0);
			if(!InvertY)
				destPortal.surfaces[0].material.SetShaderParameter("InvertY", 0);
		}
		else
			parent.Rotation += Transform3D.Identity.LookingAt(-destPortal.normal, Vector3.Up).Basis.GetEuler();

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
			viewport.Size = GameManager.Instance.viewPortSize;

		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		viewport.HandleInputLocally = false;

		var CamRID = camera.GetCameraRid();
		var viewPortRID = viewport.GetViewportRid();
		RenderingServer.ViewportAttachCamera(viewPortRID, CamRID);

		destPortal.surfaces[0].material.SetShaderParameter("Tex_0", viewport.GetTexture());
		viewPorts.Add(viewport);
		for (int i = 1; i < GameManager.NumLocalPlayers; i++)
			NewLocalPlayerAdded();
	}

	private Vector2 FillMirrorData()
	{
		MeshDataTool meshDataTool = new MeshDataTool();
		meshDataTool.CreateFromSurface(destPortal.commonMesh, 0);
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
			changeRotation.CalculateRotation(destPortal.normal, normalRef);
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
							InvertX = false;
						InvertY = false;
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
		destPortal.commonMesh.ClearSurfaces();
		meshDataTool.CommitToSurface(destPortal.commonMesh);
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
}
