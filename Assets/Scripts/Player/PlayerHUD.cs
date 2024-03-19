using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerHUD : MeshInstance3D
{
	[Export]
	public Texture2D painEffect;
	[Export]
	public Texture2D pickupEffect;
	[Export]
	public Node3D viewHeadContainer;
	[Export]
	public Node3D viewHead;
	[Export]
	public AnimationTree headAnimation;
	[Export]
	public Vector3 headOffset = new Vector3(0, -1f, -1.5f);
	public PlayerInfo playerInfo;
	public ShaderMaterial baseCamera;
	public ShaderMaterial currentMaterial;
	public ViewportTexture baseViewPortTexture;
	public ViewportTexture normalDepthViewPortTexture;

	public Camera3D NormalDepthCamera;
	public Dictionary<ShaderMaterial, ViewMaterial> ReplacementMaterial = new Dictionary<ShaderMaterial, ViewMaterial>();

	public bool hasQuad = false;
	private float lookTime = 0;
	private bool faceAttack = false;
	private List<MeshInstance3D> fxMeshes;
	private MD3GodotConverted head;
	public enum HeadDir
	{
		Left,
		Center,
		Right
	}

	public HeadDir headState = HeadDir.Center;
	private float currentDir = 0;
	public void Init(PlayerInfo p)
	{
		baseCamera = (ShaderMaterial)MaterialManager.Instance.baseCameraMaterial.Duplicate(true);
		Mesh.SurfaceSetMaterial(0, baseCamera);
		baseCamera.SetShaderParameter(MaterialManager.screenTexure, baseViewPortTexture);
		baseCamera.SetShaderParameter(MaterialManager.painTexure, painEffect);
		baseCamera.SetShaderParameter(MaterialManager.pickUpTexture, pickupEffect);
		currentMaterial = baseCamera;
		playerInfo = p;
		viewHeadContainer.Position = headOffset;
	}

	public void InitHUD(MD3 headModel, Dictionary<string, string> meshToSkin)
	{
		if (headModel != null)
		{
			if (headModel.readySurfaceArray.Count == 0)
				head = Mesher.GenerateModelFromMeshes(headModel, Layers, false, false, viewHead, false, false, meshToSkin, true, false, true, false);
			else
				head = Mesher.FillModelFromProcessedData(headModel, Layers, false, false, viewHead, false, meshToSkin, false, true, false, true, false);
		}
		fxMeshes = GameManager.CreateFXMeshInstance3D(viewHeadContainer);
//		headAnimation.Play("idle");
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

	public void SetAttackFace()
	{
		if (faceAttack)
			return;

		if (currentDir > 0)
		{
			if (headState != HeadDir.Left)
			{
				headState = HeadDir.Left;
				lookTime = .5f;
			}
		}
		else if (currentDir < 0)
		{
			if (headState != HeadDir.Right)
			{
				headState = HeadDir.Right;
				lookTime = .5f;
			}
		}
		else
		{
			if (headState == HeadDir.Center)
			{
				lookTime = .5f;
				return;
			}

			if (headState == HeadDir.Right)
			{
				currentDir = 1;
				headState = HeadDir.Left;
			}
			if (headState == HeadDir.Left)
			{
				currentDir = -1;
				headState = HeadDir.Right;
			}
			lookTime = .5f - lookTime;
		}
		faceAttack = true;
	}

	public void CheckNextHeadAnimation(float deltaTime)
	{
		if (lookTime > 0)
		{
			lookTime -= deltaTime;
			if (lookTime < 0)
				lookTime = 0;
			float value = Mathf.Clamp(2 * (0.5f - lookTime), 0, 1);
			switch (headState)
			{
				default:
				break;
				case HeadDir.Left:
					headAnimation.Set("parameters/Look/left_right/blend_position", -value + currentDir);
				break;
				case HeadDir.Right:
					headAnimation.Set("parameters/Look/left_right/blend_position", value + currentDir);
				break;
			}
		}
		else
		{
			int index;
			float oldDir = currentDir;
			currentDir = (float)headAnimation.Get("parameters/Look/left_right/blend_position");

			if (oldDir != currentDir)
			{
				lookTime = .5f;
				headState = HeadDir.Center;
				return;
			}

			if (currentDir > 0)
				index = GD.RandRange(-1, 0);
			else if (currentDir < 0)
				index = GD.RandRange(0, 1);
			else
				index = GD.RandRange(-1, 1);

			switch (index)
			{
				default:
					headState = HeadDir.Center;
				break;
				case -1:
					headState = HeadDir.Left;
				break;
				case 1:
					headState = HeadDir.Right;
				break;
			}
			lookTime = .5f;
			faceAttack = false;
		}
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		if (hasQuad != playerInfo.quadDamage)
		{
			hasQuad = playerInfo.quadDamage;
			GameManager. ChangeQuadFx(fxMeshes,hasQuad);
		}

		CheckNextHeadAnimation(deltaTime);
	}
	public class ViewMaterial
	{
		public ShaderMaterial material;
		public bool needNormalDepth; 
	}
}
