using Godot;
using System;

public partial class PlayerPostProcessing : Node3D
{
	[Export]
	public Camera3D ViewPortCamera;
	[Export]
	public Camera3D NormalDepthCamera;
	[Export]
	public SubViewport ViewPort;
	[Export]
	public SubViewport NormalDepthViewPort;
	[Export]
	public PlayerHUD playerHUD;
	public uint ViewMask;
	public uint UIMask;
	public override void _Ready()
	{
		ViewPortCamera.Current = true;
		GameManager.Instance.SetViewPortToCamera(ViewPortCamera);
	}

	public void InitPost()
	{
		ViewPortCamera.CullMask = UIMask;
		NormalDepthCamera.CullMask |= ViewMask;
		SetLocalViewPortToCamera(NormalDepthCamera, NormalDepthViewPort);
		playerHUD.Layers = UIMask;
		playerHUD.SetTexture("screen_texture",ViewPort);
		playerHUD.SetTexture("normal_depth_texture", NormalDepthViewPort);
		playerHUD.Init();
	}

	public void SetLocalViewPortToCamera(Camera3D camera, Viewport viewport = null)
	{
		var CamRID = camera.GetCameraRid();
		Rid viewPortRID;
		if (viewport == null)
			viewport = ViewPort;
		viewPortRID = viewport.GetViewportRid();
		RenderingServer.ViewportAttachCamera(viewPortRID, CamRID);
	}

}
