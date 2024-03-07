using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerHUD : MeshInstance3D
{
	public ShaderMaterial baseCamera;
	public ViewportTexture baseViewPortTexture;
	public ViewportTexture normalDepthViewPortTexture;

	public Camera3D NormalDepthCamera;
	public Dictionary<ShaderMaterial, ViewMaterial> ReplacementMaterial = new Dictionary<ShaderMaterial, ViewMaterial>();
	public void Init()
	{
		baseCamera = (ShaderMaterial)MaterialManager.Instance.baseCameraMaterial.Duplicate(true);
		Mesh.SurfaceSetMaterial(0, baseCamera);
		baseCamera.SetShaderParameter(MaterialManager.screenTexure, baseViewPortTexture);
	}

	public void SetCameraReplacementeMaterial(ShaderMaterial shaderMaterial)
	{
		if (shaderMaterial == null) 
		{
			NormalDepthCamera.Visible = false;
			SetSurfaceOverrideMaterial(0, null);
			return;
		}

		ViewMaterial viewMaterial;
		if (!ReplacementMaterial.TryGetValue(shaderMaterial, out viewMaterial))
		{
			viewMaterial = new ViewMaterial();
			viewMaterial.material = (ShaderMaterial)shaderMaterial.Duplicate(true);
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
		viewMaterial.material.SetShaderParameter(MaterialManager.screenTexure, baseViewPortTexture);
		if (viewMaterial.needNormalDepth)
		{
			NormalDepthCamera.Visible = true;
			viewMaterial.material.SetShaderParameter(MaterialManager.normalDepthTexture, normalDepthViewPortTexture);
		}
	}

	public class ViewMaterial
	{
		public ShaderMaterial material;
		public bool needNormalDepth; 
	}
}
