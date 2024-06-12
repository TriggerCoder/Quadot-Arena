using Godot;
using System.Collections;
using System.Collections.Generic;
public static class ModelsManager
{
	public static Dictionary<string, MD3> Models = new Dictionary<string, MD3>();
	public static HashSet<ModelController> ActiveModels = new HashSet<ModelController>();

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

}

