using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerHUD : MeshInstance3D
{
	[Export]
	public Texture2D painEffect;
	[Export]
	public Texture2D pickupEffect;
	public ShaderMaterial baseCamera;
	public ShaderMaterial currentMaterial;
	public ViewportTexture baseViewPortTexture;
	public ViewportTexture normalDepthViewPortTexture;

	public Camera3D NormalDepthCamera;
	public Dictionary<ShaderMaterial, ViewMaterial> ReplacementMaterial = new Dictionary<ShaderMaterial, ViewMaterial>();

	public void Init()
	{
		baseCamera = (ShaderMaterial)MaterialManager.Instance.baseCameraMaterial.Duplicate(true);
		Mesh.SurfaceSetMaterial(0, baseCamera);
		baseCamera.SetShaderParameter(MaterialManager.screenTexure, baseViewPortTexture);
		baseCamera.SetShaderParameter(MaterialManager.painTexure, painEffect);
		baseCamera.SetShaderParameter(MaterialManager.pickUpTexture, pickupEffect);
		currentMaterial = baseCamera;
		RenderingServer.FramePreDraw += () => OnPreRender();
	}

	public void SetCameraReplacementeMaterial(ShaderMaterial shaderMaterial)
	{
		if (shaderMaterial == null) 
		{
			NormalDepthCamera.Visible = false;
			SetSurfaceOverrideMaterial(0, null);
			currentMaterial = baseCamera;
			return;
		}

		ViewMaterial viewMaterial;
		if (!ReplacementMaterial.TryGetValue(shaderMaterial, out viewMaterial))
		{
			viewMaterial = new ViewMaterial();
			viewMaterial.material = (ShaderMaterial)shaderMaterial.Duplicate(true);
			viewMaterial.material.SetShaderParameter(MaterialManager.painTexure, painEffect);
			viewMaterial.material.SetShaderParameter(MaterialManager.pickUpTexture, pickupEffect);
			bool needNormalDepth = false;
			var Results = RenderingServer.GetShaderParameterList(shaderMaterial.Shader.GetRid());
			foreach (var result in Results)
			{
				if (!result.ContainsKey("name"))
					continue;

				string name = (string)result["name"];
				if (name.Contains(MaterialManager.normalDepthTexture))
				{
					needNormalDepth = true;
					break;
				}
			}
			viewMaterial.needNormalDepth = needNormalDepth;
			ReplacementMaterial.Add(shaderMaterial, viewMaterial);
		}

		SetSurfaceOverrideMaterial(0, viewMaterial.material);
		currentMaterial = viewMaterial.material;
		viewMaterial.material.SetShaderParameter(MaterialManager.screenTexure, baseViewPortTexture);
		if (viewMaterial.needNormalDepth)
		{
			NormalDepthCamera.Visible = true;
			viewMaterial.material.SetShaderParameter(MaterialManager.normalDepthTexture, normalDepthViewPortTexture);
		}
	}

	public void painFlashTime(float time)
	{
		currentMaterial.SetShaderParameter("pain_duration", time);
		currentMaterial.SetShaderParameter("pain_start_time", GameManager.CurrentTimeMsec);
	}

	public void pickupFlashTime(float time)
	{
		currentMaterial.SetShaderParameter("pick_up_duration", time);
		currentMaterial.SetShaderParameter("pick_up_start_time", GameManager.CurrentTimeMsec);
	}

	public void OnPreRender()
	{
		//		GameManager.Print("PreRender");
	}
	public class ViewMaterial
	{
		public ShaderMaterial material;
		public bool needNormalDepth; 
	}
}
