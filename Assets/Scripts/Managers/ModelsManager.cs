using Godot;
using System.Collections;
using System.Collections.Generic;

public static class ModelsManager
{
	public static Dictionary<string, MD3> Models = new Dictionary<string, MD3>();
	public static Dictionary<string, MeshProcessed.dataMeshes> Sprites = new Dictionary<string, MeshProcessed.dataMeshes>();
	public static HashSet<ModelController> ActiveModels = new HashSet<ModelController>();

	public static Dictionary<string, ModelAnimationData> AnimationData = new Dictionary<string, ModelAnimationData>();
	public static Dictionary<string, Dictionary<string, string>> SkinData = new Dictionary<string, Dictionary<string, string>>();

	public static Dictionary<string, string> GetSkinData(string skinName)
	{
		Dictionary<string, string> meshToSkin;
		if (SkinData.TryGetValue(skinName, out meshToSkin))
			return meshToSkin;
		return null;
	}

	public static void AddSkinData(string skinName, Dictionary<string, string> meshToSkin)
	{
		if (!SkinData.ContainsKey(skinName))
		{
			Dictionary<string, string> added = new Dictionary<string, string>();
			foreach (var value in meshToSkin)
			{
				if (!added.ContainsKey(value.Key))
					added.Add(value.Key, value.Value);
			}
			SkinData.Add(skinName, added);
		}
	}

	public static ModelAnimationData GetAnimationData(string animation)
	{
		if (AnimationData.TryGetValue(animation, out ModelAnimationData animationData))
			return animationData;
		return null;
	}

	public static void AddAnimationData(string animation, List<PlayerModel.ModelAnimation> Upper, List<PlayerModel.ModelAnimation> Lower, PlayerThing.FootStepType FootSteps)
	{
		if (AnimationData.ContainsKey(animation))
			return;

		ModelAnimationData animationData = new ModelAnimationData(Upper, Lower, FootSteps);
		AnimationData.Add(animation, animationData);
	}
	
	public static void ClearModels()
	{
		ActiveModels = new HashSet<ModelController>();
	}
	public static void AddModel(ModelController controller)
	{
		if (ActiveModels.Contains(controller))
			return;
		ActiveModels.Add(controller);
	}

	public static void RemoveModel(ModelController controller)
	{
		if (ActiveModels.Contains(controller))
			ActiveModels.Remove(controller);
	}


	public static void CacheModel(string modelName, bool forceSkinAlpha = false)
	{
		if (Models.ContainsKey(modelName))
			return;

		MD3 model = MD3.ImportModel(modelName, forceSkinAlpha);
		if (model == null)
			return;

		Models.Add(modelName, model);
		return;
	}
	public static MD3 GetModel(string modelName, bool forceSkinAlpha = false)
	{
		MD3 model;
		if (Models.TryGetValue(modelName, out model))
			return model;

		model = MD3.ImportModel(modelName, forceSkinAlpha);
		if (model == null)
			return null;

		Models.Add(modelName, model);
		return model;
	}

	public static void FrameProcessModels(float deltaTime)
	{
		foreach (var model in ActiveModels)
			model.Process(deltaTime);

	}

	public class ModelAnimationData
	{
		public List<PlayerModel.ModelAnimation> Upper;
		public List<PlayerModel.ModelAnimation> Lower;
		public PlayerThing.FootStepType FootSteps;

		public ModelAnimationData (List<PlayerModel.ModelAnimation> Upper, List<PlayerModel.ModelAnimation> Lower, PlayerThing.FootStepType FootSteps)
		{
			this.Upper = Upper;
			this.Lower = Lower;
			this.FootSteps = FootSteps;
		}	
	}
}

