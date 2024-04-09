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
	public Node3D ArmorContainer;
	[Export]
	public Node3D AmmoContainer;
	[Export]
	public Node3D WeaponContainer;
	[Export]
	public Node3D viewHead;
	[Export]
	public AnimationTree headAnimation;
	[Export]
	public Label3D healthLabel;
	[Export]
	public Label3D armorLabel;
	[Export]
	public Label3D weaponLabel;
	[Export]
	public Label3D ammoLabel;
	[Export]
	public Sprite3D[] weaponIcon;
	[Export]
	public Sprite3D crossHair;
	[Export]
	public Sprite3D pickUpIcon;
	[Export]
	public Label3D pickUpText;
	[Export]
	public Sprite3D[] powerUpIcon;
	[Export]
	public Label3D[] powerUpText;
	[Export]
	public Label3D fpsText;
	[Export]
	public Sprite3D holdableItemIcon;

	public PlayerInfo playerInfo;
	public ShaderMaterial baseCamera;
	public ShaderMaterial currentMaterial;
	public ViewportTexture baseViewPortTexture;
	public ViewportTexture normalDepthViewPortTexture;

	public Camera3D NormalDepthCamera;
	public Dictionary<ShaderMaterial, ViewMaterial> ReplacementMaterial = new Dictionary<ShaderMaterial, ViewMaterial>();
	public List<Node> NodeList = new List<Node>();

	private bool hasQuad = false;
	private bool swapColors = false;
	private bool faceAttack = false;

	private float spawnColorTime = 0;
	private float lookTime = 0;
	private float pickUpTime = 0;
	private float weaponTime = 0;

	private List<MeshInstance3D> fxMeshes;
	private List<Node3D> ammoContainers = new List<Node3D>();
	private int currentAmmoType = -1;

	private static string ammoModelPath = "powerups/ammo/";
	private static string armorModel = "powerups/armor/shard";
	private static string selectSprite = "GFX/2D/SELECT";
	private static string noAmmoSprite = "ICONS/NOAMMO";

	private Sprite3D[] noAmmoIcon;
	private Sprite3D selectIcon;
	private static readonly string[] weaponNames = { "Gauntlet", "Machinegun", "Shotgun", "Grenade Launcher", "Rocket Launcher", "Lightning Gun", "Railgun", "Plasma Gun", "BFG 10K", "Grapple Hook" };
	private static readonly string[] weaponSprites = { "ICONS/ICONW_GAUNTLET", "ICONS/ICONW_MACHINEGUN", "ICONS/ICONW_SHOTGUN", "ICONS/ICONW_GRENADE", "ICONS/ICONW_ROCKET", "ICONS/ICONW_LIGHTNING", "ICONS/ICONW_RAILGUN", "ICONS/ICONW_PLASMA", "ICONS/ICONW_BFG", "ICONS/ICONW_GRAPPLE" };
	private static readonly string[] ammoModels = { "machinegunam", "shotgunam", "grenadeam", "rocketam", "lightningam", "railgunam", "plasmaam", "bfgam" };
	private static readonly string[] powerUpsSprites = { "ICONS/QUAD", "ICONS/HASTE", "ICONS/INVIS", "ICONS/REGEN", "ICONS/ENVIROSUIT", "ICONS/FLIGHT" };
	private static readonly string[] holdableItemsSprites = { "ICONS/TELEPORTER", "ICONS/MEDKIT" };

	public enum NumColor
	{
		Yellow,
		Red,
		White
	}

	private NumColor curretHealthColor = NumColor.Yellow;
	private NumColor curretAmmoColor = NumColor.Yellow;
	public enum HeadDir
	{
		Left,
		Center,
		Right
	}
	public HeadDir headState = HeadDir.Center;
	private float currentDir = 0;

	private Color YellowColor = new Color(0xEAA000FF);
	private Color RedColor = new Color(0xE92F2FFF);
	private Color WhiteColor = new Color(0xD5D5D5FF);
	public enum PowerUpType : int
	{
		Quad = 0,
		Haste = 1,
		Invis = 2,
		Regen = 3,
		EnviroSuit = 4,
		Flight = 5
	}

	public enum HoldableItemType : int
	{
		Teleporter = 0,
		Medkit = 1
	}

	private class PowerUpInfo
	{
		public PowerUpType type;
		public int displayTime;
	}

	private List<PowerUpInfo> currentPowerUps = new List<PowerUpInfo>();
	private List<int> currentWeapons = new List<int>();
	public void Init(PlayerInfo p)
	{
		baseCamera = (ShaderMaterial)MaterialManager.Instance.baseCameraMaterial.Duplicate(true);
		Mesh.SurfaceSetMaterial(0, baseCamera);
		baseCamera.SetShaderParameter(MaterialManager.screenTexure, baseViewPortTexture);
		baseCamera.SetShaderParameter(MaterialManager.painTexure, painEffect);
		baseCamera.SetShaderParameter(MaterialManager.pickUpTexture, pickupEffect);
		currentMaterial = baseCamera;
		playerInfo = p;
		headAnimation.Active = true;
		//Load HUD Models
		MD3 model = ModelsManager.GetModel(armorModel, false);
		if (model != null)
		{
			if (model.readySurfaceArray.Count == 0)
				Mesher.GenerateModelFromMeshes(model, Layers, false, false, ArmorContainer, false, false, null, true, false, true, false);
			else
				Mesher.FillModelFromProcessedData(model, Layers, false, false, ArmorContainer, false, null, false, true, false, true, false);
		}

		for (int i = 0; i < ammoModels.Length; i++)
		{
			Node3D container = new Node3D();
			container.Name = ammoModels[i];
			AmmoContainer.AddChild(container);
			model = ModelsManager.GetModel(ammoModelPath + ammoModels[i], false);
			if (model != null)
			{
				if (model.readySurfaceArray.Count == 0)
					Mesher.GenerateModelFromMeshes(model, Layers, false, false, container, false, false, null, true, false, true, false);
				else
					Mesher.FillModelFromProcessedData(model, Layers, false, false, container, false, null, false, true, false, true, false);
			}
			container.Hide();
			ammoContainers.Add(container);
		}

		//Set Layers
		healthLabel.Layers = Layers;
		armorLabel.Layers = Layers;
		ammoLabel.Layers = Layers;
		crossHair.Layers = Layers;
		pickUpIcon.Layers = Layers;
		pickUpText.Layers = Layers;
		weaponLabel.Layers = Layers;
		holdableItemIcon.Layers = Layers;
		fpsText.Layers = Layers;

		for (int i = 0; i < powerUpIcon.Length; i++)
			powerUpIcon[i].Layers = Layers;
		for (int i = 0; i < powerUpText.Length; i++)
			powerUpText[i].Layers = Layers;

		selectIcon = new Sprite3D();
		WeaponContainer.AddChild(selectIcon);
		selectIcon.DoubleSided = false;
		selectIcon.NoDepthTest = true;
		selectIcon.Layers = Layers;
		selectIcon.Texture = TextureLoader.GetTextureOrAddTexture(selectSprite, false);
		selectIcon.Hide();

		noAmmoIcon = new Sprite3D[weaponIcon.Length];
		for (int i = 0; i < weaponIcon.Length; i++)
		{
			weaponIcon[i].Layers = Layers;
			noAmmoIcon[i] = new Sprite3D();
			weaponIcon[i].AddChild(noAmmoIcon[i]);
			noAmmoIcon[i].Position += Vector3.Back * .001f;
			noAmmoIcon[i].DoubleSided = false;
			noAmmoIcon[i].NoDepthTest = true;
			noAmmoIcon[i].Layers = Layers;
			noAmmoIcon[i].Texture = TextureLoader.GetTextureOrAddTexture(noAmmoSprite, false);
			noAmmoIcon[i].Hide();
		}
	}

	public void InitHUD(MD3 headModel, Dictionary<string, string> meshToSkin)
	{
		if (NodeList.Count > 0)
		{
			for (int i = 0; i < NodeList.Count; i++)
				NodeList[i].QueueFree();
		}
		NodeList.Clear();

		if (headModel != null)
		{
			if (headModel.readySurfaceArray.Count == 0)
				Mesher.GenerateModelFromMeshes(headModel, Layers, false, false, viewHead, false, false, meshToSkin, true, false, true, false);
			else
				Mesher.FillModelFromProcessedData(headModel, Layers, false, false, viewHead, false, meshToSkin, false, true, false, true, false);
		}
		fxMeshes = GameManager.CreateFXMeshInstance3D(viewHeadContainer);
		NodeList = GameManager.GetAllChildrens(viewHead);
		headAnimation.Set("parameters/Look/pain_shot/active", true);
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
				Variant nameVar;
				if (result.TryGetValue("name", out nameVar))
				{
					string name = (string)nameVar;
					if (name.Contains(MaterialManager.normalDepthTexture))
					{
						needNormalDepth = true;
						break;
					}
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
		headAnimation.Set("parameters/Look/pain_shot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
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

	public void RemoveAllItems()
	{
		currentPowerUps = new List<PowerUpInfo>();
		currentWeapons = new List<int>();
		for (int i = 0; i < powerUpIcon.Length; i++)
			powerUpIcon[i].Hide();
		for (int i = 0; i < powerUpText.Length; i++)
			powerUpText[i].Hide();
		for (int i = 0; i < weaponIcon.Length; i++)
			weaponIcon[i].Hide();
		WeaponContainer.Hide();
		weaponLabel.Hide();
		holdableItemIcon.Hide();
	}


	public void RemovePowerUp(PowerUpType type)
	{
		int i;
		bool found = false;
		for (i = 0; i < currentPowerUps.Count; i++)
		{
			if (currentPowerUps[i].type == type)
			{
				currentPowerUps.RemoveAt(i);
				found = true;
				break;
			}
		}

		if (!found)
			return;

		for (i = 0; i < currentPowerUps.Count; i++)
		{
			powerUpIcon[i].Texture = TextureLoader.GetTextureOrAddTexture(powerUpsSprites[(int)currentPowerUps[i].type], false);
			powerUpText[i].Text = "" + currentPowerUps[i].displayTime;
		}

		for (; i < powerUpIcon.Length; i++)
		{
			powerUpIcon[i].Hide();
			powerUpText[i].Hide();
		}

	}
	public void UpdatePowerUpTime(PowerUpType type, int time)
	{
		int i;
		bool found = false;
		for (i = 0; i < currentPowerUps.Count; i++)
		{
			if (currentPowerUps[i].type == type)
			{
				found = true;
				break;
			}				
		}
		if (found)
			currentPowerUps[i].displayTime = time;
		else
		{
			PowerUpInfo powerUpInfo = new PowerUpInfo();
			powerUpInfo.type = type;
			powerUpInfo.displayTime = time;
			currentPowerUps.Add(powerUpInfo);
		}

		if (!found)
			currentPowerUps.Sort((a,b) => { if (a.displayTime > b.displayTime) return 1; else if (a.displayTime == b.displayTime) return 0; else return -1; });

		for (i = 0; i < currentPowerUps.Count; i++)
		{
			if (powerUpIcon[i].Visible == false)
				powerUpIcon[i].Show();
			if (powerUpText[i].Visible == false)
				powerUpText[i].Show();
			if (!found)
				powerUpIcon[i].Texture = TextureLoader.GetTextureOrAddTexture(powerUpsSprites[(int)currentPowerUps[i].type], false);
			powerUpText[i].Text = "" + currentPowerUps[i].displayTime;
		}
	}

	public void AddHoldableItem(PlayerThing.HoldableItem item)
	{

		switch (item)
		{
			default:
				return;
			break;
			case PlayerThing.HoldableItem.Teleporter:
				holdableItemIcon.Texture = TextureLoader.GetTextureOrAddTexture(holdableItemsSprites[(int)HoldableItemType.Teleporter], false);
			break;
		}
		holdableItemIcon.Show();
	}

	public void RemoveHoldableItem()
	{
		holdableItemIcon.Hide();
	}

	public void AddWeapon(int weapon)
	{
		if (currentWeapons.Contains(weapon))
			return;

		if (WeaponContainer.Visible == false)
			WeaponContainer.Show();

		if (weaponLabel.Visible == false)
			weaponLabel.Show();

		WeaponContainer.Position = new Vector3(currentWeapons.Count * -0.175f, WeaponContainer.Position.Y, 0); 
		currentWeapons.Add(weapon);
		currentWeapons.Sort((a, b) => a.CompareTo(b));
		for (int i = 0; i < currentWeapons.Count; i++)
		{
			if (weaponIcon[i].Visible == false)
				weaponIcon[i].Show();

			noAmmoIcon[i].Visible = !HasAmmo(currentWeapons[i]);

			if (currentWeapons[i] == weapon)
			{
				selectIcon.Position = weaponIcon[i].Position;
				weaponLabel.Text = weaponNames[weapon];
			}
			weaponIcon[i].Texture = TextureLoader.GetTextureOrAddTexture(weaponSprites[currentWeapons[i]], false);
		}
		//In order to get near the screen
		selectIcon.Position += Vector3.Back * .001f;
		if (selectIcon.Visible == false)
			selectIcon.Show();
		weaponTime = 3f;
	}

	public void ChangeWeapon(int weapon)
	{
		if (WeaponContainer.Visible == false)
			WeaponContainer.Show();

		if (weaponLabel.Visible == false)
			weaponLabel.Show();

		for (int i = 0; i < currentWeapons.Count; i++)
		{
			noAmmoIcon[i].Visible = !HasAmmo(currentWeapons[i]);
			if (currentWeapons[i] == weapon)
			{
				weaponLabel.Text = weaponNames[weapon];
				selectIcon.Position = weaponIcon[i].Position;
			}
		}
		//In order to get near the screen
		selectIcon.Position += Vector3.Back * .001f;
		if (selectIcon.Visible == false)
			selectIcon.Show();
		weaponTime = 3f;
	}

	public bool HasAmmo(int weapon)
	{
		switch (weapon)
		{
			default:
				return false;

			case PlayerInfo.Gauntlet:
				break;

			case PlayerInfo.MachineGun:
				if (playerInfo.Ammo[PlayerInfo.bulletsAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.Shotgun:
				if (playerInfo.Ammo[PlayerInfo.shellsAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.GrenadeLauncher:
				if (playerInfo.Ammo[PlayerInfo.grenadesAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.RocketLauncher:
				if (playerInfo.Ammo[PlayerInfo.rocketsAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.LightningGun:
				if (playerInfo.Ammo[PlayerInfo.lightningAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.Railgun:
				if (playerInfo.Ammo[PlayerInfo.slugAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.PlasmaGun:
				if (playerInfo.Ammo[PlayerInfo.cellsAmmo] <= 0)
					return false;
				break;

			case PlayerInfo.BFG10K:
				if (playerInfo.Ammo[PlayerInfo.bfgAmmo] < 40)
					return false;
				break;
		}
		return true;
	}

	public void CheckWeapon(float deltaTime)
	{
		if (weaponTime > 0)
			weaponTime -= deltaTime;

		if (weaponTime < 0)
		{
			weaponTime = 0;
			WeaponContainer.Hide();
			weaponLabel.Hide();
		}
	}

	public void CheckPickUp(float deltaTime)
	{
		if (pickUpTime > 0)
			pickUpTime -= deltaTime;

		if (pickUpTime < 0)
		{
			pickUpTime = 0;
			pickUpIcon.Hide();
			pickUpText.Hide();
		}

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

	public void SetAmmoCoolDown(bool cooldown)
	{
		if (cooldown)
		{
			if (curretAmmoColor != NumColor.White)
			{
				curretAmmoColor = NumColor.White;
				ammoLabel.Modulate = WhiteColor;
			}
			return;
		}

		if (curretAmmoColor != NumColor.Yellow)
		{
			curretAmmoColor = NumColor.Yellow;
			ammoLabel.Modulate = YellowColor;
		}
	}

	public void ItemPickUp(string icon, string text)
	{
		pickUpIcon.Texture = TextureLoader.GetTextureOrAddTexture(icon, false);
		pickUpText.Text = text;
		pickUpTime = 1.5f;
		pickUpIcon.Show();
		pickUpText.Show();


		//Small Check in case we picked Up Ammo
		if (weaponTime > 0)
		{
			for (int i = 0; i < currentWeapons.Count; i++)
				noAmmoIcon[i].Visible = !HasAmmo(currentWeapons[i]);
		}
	}

	public void HideAmmo()
	{
		for (int i = 0; i < ammoContainers.Count; i++)
			ammoContainers[i].Hide();

		if (playerInfo.playerThing.Dead)
			ammoLabel.Hide();

		currentAmmoType = -1;
	}
	public void UpdateAmmoType(int type)
	{
		if (ammoContainers.Count > type)
		{
			ammoContainers[type].Show();
			currentAmmoType = type;
			if (ammoLabel.Visible == false)
				ammoLabel.Show();
		}
		SetAmmoCoolDown(false);
	}

	public void UpdateAmmo(int ammo, int type = -1)
	{
		if (type >= 0)
		{
			if (currentAmmoType !=  type)
				return;
		}

		string ammoText = "";
		if (ammo >= 0)
			ammoText = "" + ammo;
		ammoLabel.Text = ammoText;
	}
	public void UpdateArmor(int armor)
	{
		armorLabel.Text = "" + armor;
	}

	public void UpdateHealth(int hitpoint)
	{
		
		float currentPain = Mathf.Clamp(hitpoint / 100f, 0, 1);
		healthLabel.Text = "" + hitpoint;
		headAnimation.Set("parameters/Look/TimeScale/scale", currentPain);
		headAnimation.Set("parameters/Look/side_limit/add_amount", currentPain);
		headAnimation.Set("parameters/Look/up_limit/add_amount", 1 - currentPain);
		if (hitpoint < 0)
		{
			swapColors = false;
			if (curretHealthColor != NumColor.Red)
			{
				curretHealthColor = NumColor.Red;
				healthLabel.Modulate = RedColor;
			}
		}
		else if (hitpoint < 30)
			swapColors = true;
		else
		{
			swapColors = false;
			if (curretHealthColor == NumColor.Red)
			{
				curretHealthColor = NumColor.Yellow;
				healthLabel.Modulate = YellowColor;
			}
		}
	}

	public void UpdateFPS()
	{
		fpsText.Text = "FPS: " + Engine.GetFramesPerSecond();
	}

	public override void _Process(double delta)
	{
		UpdateFPS();

		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		if (swapColors)
		{
			spawnColorTime -= deltaTime;
			if (spawnColorTime <= 0)
			{
				spawnColorTime = .5f;
				if (curretHealthColor != NumColor.Red)
				{
					curretHealthColor = NumColor.Red;
					healthLabel.Modulate = RedColor;
				}
				else
				{
					curretHealthColor = NumColor.Yellow;
					healthLabel.Modulate = YellowColor;
				}
			}
		}
		if (hasQuad != playerInfo.quadDamage)
		{
			hasQuad = playerInfo.quadDamage;
			GameManager.ChangeQuadFx(fxMeshes,hasQuad, true);
		}

		CheckNextHeadAnimation(deltaTime);
		CheckPickUp(deltaTime);
		CheckWeapon(deltaTime);
	}
	public class ViewMaterial
	{
		public ShaderMaterial material;
		public bool needNormalDepth; 
	}
}
