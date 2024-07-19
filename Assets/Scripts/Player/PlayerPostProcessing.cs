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

	public void InitPost(PlayerInfo p)
	{
		ViewPortCamera.CullMask = UIMask;
		NormalDepthCamera.CullMask = ViewMask | UIMask | (1 << GameManager.PlayerNormalDepthLayer);
		SetLocalViewPortToCamera(NormalDepthCamera, NormalDepthViewPort);
		playerHUD.Layers = UIMask;
		playerHUD.baseViewPortTexture = ViewPort.GetTexture();
		playerHUD.normalDepthViewPortTexture = NormalDepthViewPort.GetTexture();
		playerHUD.NormalDepthCamera = NormalDepthCamera;
		playerHUD.Init(p);
	}

	public void UpdateLayersPost()
	{
		ViewPortCamera.CullMask = UIMask;
		NormalDepthCamera.CullMask = ViewMask | UIMask | (1 << GameManager.PlayerNormalDepthLayer);
		playerHUD.Layers = UIMask;
		playerHUD.UpdateLayersHud(UIMask);
	}

	public void SetWaterEffect()
	{
		playerHUD.SetCameraReplacementeMaterial(MaterialManager.Instance.underWaterMaterial);
	}

	public void ResetEffects()
	{
		playerHUD.SetCameraReplacementeMaterial(null);
	}

	public void ChangeCurrentCamera(Camera3D camera, bool thirdPerson)
	{
		if (!camera.IsAncestorOf(NormalDepthCamera))
		{
			NormalDepthCamera.Reparent(camera);
			NormalDepthCamera.Transform = Transform3D.Identity;
		}
		NormalDepthCamera.CullMask = ViewMask | (1 << GameManager.PlayerNormalDepthLayer);
		if (!thirdPerson)
			NormalDepthCamera.CullMask |= UIMask;
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
