using Godot;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public partial class ThingsManager : Node
{
	[Export]
	public PackedScene[] _fxPrefabs;
	[Export]
	public PackedScene[] _projectilesPrefabs;
	[Export]
	public PackedScene[] _decalsPrefabs;
	[Export]
	public PackedScene[] _ammoPrefabs;
	[Export]
	public PackedScene[] _debrisPrefabs;
	[Export]
	public PackedScene[] _gibsPrefabs;
	[Export]
	public PackedScene[] _weaponsPrefabs;
	[Export]
	public PackedScene[] _healthsPrefabs;
	[Export]
	public PackedScene[] _armorPrefabs;
	[Export]
	public PackedScene[] _powerUpsPrefabs;
	[Export]
	public PackedScene[] _funcPrefabs;
	[Export]
	public PackedScene[] _infoPrefabs;
	[Export]
	public PackedScene[] _miscPrefabs;
	[Export]
	public PackedScene[] _targetPrefabs;
	[Export]
	public PackedScene[] _triggerPrefabs;

	public static Dictionary<string, PackedScene> thingsPrefabs = new Dictionary<string, PackedScene>();
	public static List<Entity> entitiesOnMap = new List<Entity>();
	public static List<PortalSurface> portalSurfaces = new List<PortalSurface>();
	public static Dictionary<string, List<Target>> targetsOnMap = new Dictionary<string, List<Target>>();
	public static Dictionary<string, Camera3D> portalCameras = new Dictionary<string, Camera3D>();
	public static Dictionary<string, TriggerController> triggerToActivate = new Dictionary<string, TriggerController>();
	public static Dictionary<string, Dictionary<string, string>> timersOnMap = new Dictionary<string, Dictionary<string, string>>();
	public static Dictionary<string, List<Dictionary<string, string>>> triggersOnMap = new Dictionary<string, List<Dictionary<string, string>>>();
	public static Dictionary<string, ItemPickup> giveItemPickup = new Dictionary<string, ItemPickup>();
	public static Dictionary<string, ConvexPolygonShape3D> gibsShapes = new Dictionary<string, ConvexPolygonShape3D>();
	public static Dictionary<string, ThingController> uniqueThingsOnMap = new Dictionary<string, ThingController>();
	public static Dictionary<string, ThingController> potentialuniqueThingsOnMap = new Dictionary<string, ThingController>();
	public static List<string> uniqueGamePlayThings = new List<string>();

	public static readonly string[] quadHogReplacement = { "item_haste", "item_regen", "item_flight", "item_invis", "item_enviro", "item_health_mega", "item_armor_body", "item_armor_combat" };

	public static readonly string[] gibsParts = { "GibSkull", "GibBrain", "GibAbdomen", "GibArm", "GibChest", "GibFist", "GibFoot", "GibForearm", "GibIntestine", "GibLeg", "GibLeg" };
	public static readonly string[] ignoreThings = { "misc_model", "light", "func_group", "info_null", "info_spectator_start", "info_firstplace", "info_secondplace", "info_thirdplace" };
	public static readonly string[] triggerThings = { "func_timer", "trigger_always", "trigger_multiple", "target_relay" , "target_delay", "target_give" };
	public static readonly string[] targetThings = { "func_timer", "trigger_multiple", "target_relay", "target_delay", "target_give", "target_position", "target_location", "info_notnull", "misc_teleporter_dest" };
	public static List<Portal> portalsOnMap = new List<Portal>();
	public static readonly string ItemDrop = "ItemDrop";
	public static readonly string Blood = "Blood";
	public static readonly string BloodTrail = "BloodTrail";
	public override void _Ready()
	{
		foreach (var thing in _fxPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("FX Name: "+ prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _projectilesPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Projectile Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _decalsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Decal Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _ammoPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Item Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _debrisPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Debris Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _gibsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Gib Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _weaponsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Weapon Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _healthsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Health Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _armorPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Armor Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _powerUpsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("PowerUp Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _funcPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Func: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _infoPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Info : " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _miscPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Misc : " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _targetPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Target : " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _triggerPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GameManager.Print("Trigger : " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		BFGTracers.SetTracers();
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;

		ModelsManager.FrameProcessModels(deltaTime);
		Mesher.ProcessSprites(deltaTime);
		if ((Engine.GetFramesDrawn() % 360) == 0)
			Mesher.UpdateChangedMultiMeshes();
	}
	public static void AddGibsShapes(string name, ConvexPolygonShape3D shape3D)
	{
		gibsShapes[name] = shape3D;
	}

	public static void ReadEntities(byte[] entities)
	{
		MemoryStream ms = new MemoryStream(entities);
		StreamReader stream = new StreamReader(ms);
		string strWord;

		stream.BaseStream.Seek(0, SeekOrigin.Begin);

		while (!stream.EndOfStream)
		{
			strWord = stream.ReadLine();

			if (strWord.Length == 0)
				continue;

			if (strWord[0] != '{')
				continue;

			{
				strWord = stream.ReadLine();
				Dictionary<string, string> entityData = new Dictionary<string, string>();
				while (strWord[0] != '}')
				{
					string[] keyValue = strWord.Split('"');
					entityData[keyValue[1].Trim('"')] = keyValue[3].Trim('"');
					strWord = stream.ReadLine();
				}
				string ClassName;
				if (!entityData.TryGetValue("classname",out ClassName))
					continue;

				if (ignoreThings.Any(s => s == ClassName))
					continue;

				if (!thingsPrefabs.ContainsKey(ClassName))
				{
					GameManager.Print(ClassName + " not found", GameManager.PrintType.Warning);
					continue;
				}

				int angle = 0;
				float fangle = 0;
				if (entityData.TryGetNumValue("angle", out fangle))
					angle = (int)fangle;

				Vector3 origin = Vector3.Zero;
				if (entityData.TryGetValue("origin", out strWord))
				{
					string[] values = new string[3] { "", "", "", };
					bool lastDigit = true;
					for (int i = 0, j = 0; i < strWord.Length; i++)
					{
						if ((char.IsDigit(strWord[i])) || (strWord[i] == '-'))
						{
							if (lastDigit)
								values[j] += strWord[i];
							else
							{
								j++;
								values[j] += strWord[i];
								lastDigit = true;
							}
						}
						else
							lastDigit = false;
						if ((j == 2) && (!lastDigit))
							break;
					}
					int x = int.Parse(values[0]);
					int y = int.Parse(values[1]);
					int z = int.Parse(values[2]);
					origin = new Vector3(-x, z, y);
					origin *= GameManager.sizeDividor;
				}

				bool found = false;
				if (targetThings.Any(s => s == ClassName))
				{
					found = true;
					string target;
					if (entityData.TryGetValue("targetname", out target))
					{
						List<Target> targetList = null;
						if (targetsOnMap.TryGetValue(target, out targetList))
							targetList.Add(new Target(origin, angle, entityData));
						else
						{
							targetList = new List<Target>
							{
								new Target(origin, angle, entityData)
							};
							targetsOnMap.Add(target, targetList);
						}
					}
				}

				if (triggerThings.Any(s => s == ClassName))
				{
					found = true;
					string target;
					if (entityData.TryGetValue("target", out target))
					{
						switch (ClassName)
						{
							default:
								if (ClassName == "trigger_always")
									entityData.Add("activate", "true");
								else if ((ClassName == "target_delay") || (ClassName == "target_relay") || (ClassName == "target_give")) //Need to add the delay/relay and give after
									found = false;
								List<Dictionary<string, string>> triggersList = null;
								if (triggersOnMap.TryGetValue(target, out triggersList))
									triggersList.Add(entityData);
								else
								{
									triggersList = new List<Dictionary<string, string>>
								{
									entityData
								};
									triggersOnMap.Add(target, triggersList);
								}
								break;
							case "func_timer": //Timers
								timersOnMap.Add(target, entityData);
								break;
						}
					}
				}

				if (!found)
					entitiesOnMap.Add(new Entity(ClassName, origin, entityData));
			}
		}

		stream.Close();
		return;
	}

	public static void UnloadThings()
	{
		entitiesOnMap = new List<Entity>();
		portalSurfaces = new List<PortalSurface>();
		targetsOnMap = new Dictionary<string, List<Target>>();
		portalCameras = new Dictionary<string, Camera3D>();
		triggerToActivate = new Dictionary<string, TriggerController>();
		timersOnMap = new Dictionary<string, Dictionary<string, string>>();
		triggersOnMap = new Dictionary<string, List<Dictionary<string, string>>>();
		giveItemPickup = new Dictionary<string, ItemPickup>();
		portalsOnMap = new List<Portal>();
		uniqueThingsOnMap = new Dictionary<string, ThingController>();
		potentialuniqueThingsOnMap = new Dictionary<string, ThingController>();
	}

	public static void AddThingsToMap()
	{
		AddTriggersOnMap();
		AddEntitiesToMap();
		AddTimersToMap();
		AddPortalsToMap();
		//Map Creator didn't put an intermission point
		if (GameManager.Instance.interMissionCamera == null)
			CreateInterMission();

	}
	public static void CreateInterMission()
	{
		Target target = SpawnerManager.deathMatchSpawner[GD.RandRange(0, SpawnerManager.deathMatchSpawner.Count - 1)];
		Camera3D camera = new Camera3D();
		camera.Position = target.destination;
		GameManager.Instance.TemporaryObjectsHolder.AddChild(camera);
		camera.CullMask = GameManager.AllPlayerViewMask | (1 << GameManager.NotVisibleLayer);
		ScoreBoard scoreBoard = (ScoreBoard)GameManager.Instance.scoreBoard.Instantiate();
		camera.AddChild(scoreBoard);
		camera.Quaternion = Quaternion.FromEuler(new Vector3(0, Mathf.DegToRad(target.angle), 0));
		GameManager.Instance.interMissionCamera = camera;
		GameManager.Instance.SetViewPortToCamera(camera, GameManager.Instance.IntermissionViewPort);
	}

	public static void AddPortalToMap(Portal portal)
	{
		if (portalsOnMap.Contains(portal))
			return;

		portalsOnMap.Add(portal);
		GameManager.Print("Got Portal at X: " + portal.position.X + " Y: " + portal.position.Y + " Z: " + portal.position.Z);
	}

	public static void AddTriggersOnMap()
	{
		foreach (KeyValuePair<string, List<Dictionary<string, string>>> trigger in triggersOnMap)
		{
			string target = trigger.Key;
			List <Dictionary<string, string>> entityDataList = trigger.Value;

			Node3D thingObject = new Node3D();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(thingObject);
			thingObject.Name = "Trigger " + target;
			TriggerController tc = new TriggerController();
			thingObject.AddChild(tc);
			string gameType;

			foreach (Dictionary<string, string> entityData in entityDataList)
			{
				switch (GameManager.Instance.gameType)
				{
					default:
						break;
					case GameManager.GameType.FreeForAll:
					case GameManager.GameType.QuadHog:
					{
						if (entityData.ContainsKey("notfree"))
							continue;
						if (entityData.TryGetValue("gametype", out gameType))
						{
							if (!gameType.Contains("ffa"))
								continue;
						}
						else if (entityData.TryGetValue("not_gametype", out gameType))
						{
							if (gameType.Contains("ffa"))
								continue;
						}
					}
					break;
					case GameManager.GameType.Tournament:
					{
						if (entityData.ContainsKey("notfree"))
							continue;
						if (entityData.TryGetValue("gametype", out gameType))
						{
							if (!gameType.Contains("duel"))
								continue;
						}
						else if (entityData.TryGetValue("not_gametype", out gameType))
						{
							if (gameType.Contains("duel"))
								continue;
						}
					}
					break;
					case GameManager.GameType.TeamDeathmatch:
					{
						if (entityData.ContainsKey("notteam"))
							continue;
						if (entityData.TryGetValue("gametype", out gameType))
						{
							if (!gameType.Contains("tdm"))
								continue;
						}
						else if (entityData.TryGetValue("not_gametype", out gameType))
						{
							if (gameType.Contains("tdm"))
								continue;
						}
					}
					break;
					case GameManager.GameType.OneFlagCTF:
					{
						if (entityData.ContainsKey("notfree"))
							continue;
						if (entityData.TryGetValue("gametype", out gameType))
						{
							if (!gameType.Contains("1f"))
								continue;
						}
						else if (entityData.TryGetValue("not_gametype", out gameType))
						{
							if (gameType.Contains("1f"))
								continue;
						}
					}
					break;
					case GameManager.GameType.CaptureTheFlag:
					{
						if (entityData.ContainsKey("notteam"))
							continue;
						if (entityData.TryGetValue("gametype", out gameType))
						{
							if (!gameType.Contains("ctf"))
								continue;
						}
						else if (entityData.TryGetValue("not_gametype", out gameType))
						{
							if (gameType.Contains("ctf"))
								continue;
						}
					}
					break;
					case GameManager.GameType.Overload:
					{
						if (entityData.ContainsKey("notteam"))
							continue;
						if (entityData.TryGetValue("gametype", out gameType))
						{
							if (!gameType.Contains("ob"))
								continue;
						}
						else if (entityData.TryGetValue("not_gametype", out gameType))
						{
							if (gameType.Contains("ob"))
								continue;
						}
					}
					break;
					case GameManager.GameType.Harvester:
					{
						if (entityData.ContainsKey("notteam"))
							continue;
						if (entityData.TryGetValue("gametype", out gameType))
						{
							if (!gameType.Contains("har"))
								continue;
						}
						else if (entityData.TryGetValue("not_gametype", out gameType))
						{
							if (gameType.Contains("har"))
								continue;
						}
					}
					break;
					case GameManager.GameType.SinglePlayer:
					{
						if (entityData.ContainsKey("notsingle"))
							continue;
					}
					break;
				}


				if (entityData.ContainsKey("activate"))
					tc.activateOnInit = true;

				//Delays/Relays
				if (entityData["classname"].Contains("elay"))
				{
					tc.Repeatable = true;
					tc.AutoReturn = true;
					tc.AutoReturnTime = .5f;
				}

				float wait;
				if (entityData.TryGetNumValue("wait", out wait))
				{
					tc.Repeatable = true;
					tc.AutoReturn = true;
					tc.AutoReturnTime = wait;
				}

				//If this trigger is a target, then is useless to setup a dummy Area3D
				string strWord;
				if (entityData.TryGetValue("targetname", out strWord))
				{
					GameManager.Print("Skipping Area3D for " + entityData["classname"] + " as it's already targetted, targetName: " + strWord);
					continue;
				}


				if (entityData.TryGetValue("model", out strWord))
				{
					int model = int.Parse(strWord.Trim('*'));
					Area3D objCollider = new Area3D();
					thingObject.AddChild(objCollider);
					MapLoader.GenerateGeometricCollider(thingObject, objCollider, model, ContentFlags.Trigger);
					objCollider.BodyEntered += tc.OnBodyEntered;
					tc.Areas.Add(objCollider);
				}
			}
			triggerToActivate.Add(target, tc);
		}
	}

	public static void AddTimersToMap()
	{
		foreach (KeyValuePair<string, Dictionary<string, string>> timer in timersOnMap)
		{
			string target = timer.Key;
			TriggerController tc;
			if (!triggerToActivate.TryGetValue(target, out tc))
				continue;

			Dictionary<string, string> entityData = timer.Value;
			Node3D node = new Node3D();
			GameManager.Instance.TemporaryObjectsHolder.AddChild(node);
			node.Name = "Timer " + target;
			float random, wait;

			entityData.TryGetNumValue("random", out random);
			entityData.TryGetNumValue("wait", out wait);

			TimerController timerController = new TimerController();
			node.AddChild(timerController);
			timerController.Init(wait, random, tc);
		}
	}

	public static void AddEntitiesToMap()
	{
		string strWord;

		foreach (Entity entity in entitiesOnMap)
		{
			switch (GameManager.Instance.gameType)
			{
				default:
				break;
				case GameManager.GameType.FreeForAll:
				case GameManager.GameType.QuadHog:
				{
					if (entity.entityData.ContainsKey("notfree"))
						continue;
					if (entity.entityData.TryGetValue("gametype", out strWord))
					{
						if (!strWord.Contains("ffa"))
							continue;
					}
					else if (entity.entityData.TryGetValue("not_gametype", out strWord))
					{
						if (strWord.Contains("ffa"))
							continue;
					}
				}
				break;
				case GameManager.GameType.Tournament:
				{
					if (entity.entityData.ContainsKey("notfree"))
						continue;
					if (entity.entityData.TryGetValue("gametype", out strWord))
					{
						if (!strWord.Contains("duel"))
							continue;
					}
					else if (entity.entityData.TryGetValue("not_gametype", out strWord))
					{
						if (strWord.Contains("duel"))
							continue;
					}
				}
				break;
				case GameManager.GameType.TeamDeathmatch:
				{
					if (entity.entityData.ContainsKey("notteam"))
						continue;
					if (entity.entityData.TryGetValue("gametype", out strWord))
					{
						if (!strWord.Contains("tdm"))
							continue;
					}
					else if (entity.entityData.TryGetValue("not_gametype", out strWord))
					{
						if (strWord.Contains("tdm"))
							continue;
					}
				}
				break;
				case GameManager.GameType.OneFlagCTF:
				{
					if (entity.entityData.ContainsKey("notfree"))
						continue;
					if (entity.entityData.TryGetValue("gametype", out strWord))
					{
						if (!strWord.Contains("1f"))
							continue;
					}
					else if (entity.entityData.TryGetValue("not_gametype", out strWord))
					{
						if (strWord.Contains("1f"))
							continue;
					}
				}
				break;
				case GameManager.GameType.CaptureTheFlag:
				{
					if (entity.entityData.ContainsKey("notteam"))
						continue;
					if (entity.entityData.TryGetValue("gametype", out strWord))
					{
						if (!strWord.Contains("ctf"))
							continue;
					}
					else if (entity.entityData.TryGetValue("not_gametype", out strWord))
					{
						if (strWord.Contains("ctf"))
							continue;
					}
				}
				break;
				case GameManager.GameType.Overload:
				{
					if (entity.entityData.ContainsKey("notteam"))
						continue;
					if (entity.entityData.TryGetValue("gametype", out strWord))
					{
						if (!strWord.Contains("ob"))
							continue;
					}
					else if (entity.entityData.TryGetValue("not_gametype", out strWord))
					{
						if (strWord.Contains("ob"))
							continue;
					}
				}
				break;
				case GameManager.GameType.Harvester:
				{
					if (entity.entityData.ContainsKey("notteam"))
						continue;
					if (entity.entityData.TryGetValue("gametype", out strWord))
					{
						if (!strWord.Contains("har"))
							continue;
					}
					else if (entity.entityData.TryGetValue("not_gametype", out strWord))
					{
						if (strWord.Contains("har"))
							continue;
					}
				}
				break;
				case GameManager.GameType.SinglePlayer:
				{
					if (entity.entityData.ContainsKey("notsingle"))
						continue;
				}
				break;
			}

			ThingController thingObject = (ThingController)thingsPrefabs[entity.name].Instantiate();
			if (thingObject == null)
				continue;

			//Check for Unique Things according to GamePlay rules
			bool skip = false;
			for (int i = 0; i < uniqueGamePlayThings.Count; i++)
			{
				if (entity.name == uniqueGamePlayThings[i])
				{
					thingObject.initDisabled = false;
					thingObject.uniqueItem = true;
					if (uniqueThingsOnMap.ContainsKey(entity.name))
						skip = true;
					else
						uniqueThingsOnMap.Add(entity.name, thingObject);
				}
				if (uniqueThingsOnMap.Count != uniqueGamePlayThings.Count)
				{
					switch (GameManager.Instance.gameType)
					{
						default:
						break;
						case GameManager.GameType.QuadHog:
						{
							if (quadHogReplacement.Any(s => s == entity.name))
								potentialuniqueThingsOnMap[entity.name] = thingObject;
						}
						break;
					}
				}
			}
			if (skip)
			{
				thingObject.QueueFree();
				continue;
			}


			thingObject.SpawnCheck(entity.name);

			GameManager.Instance.TemporaryObjectsHolder.AddChild(thingObject);
			thingObject.Name = entity.name;

			switch (entity.name)
			{
				default:
				{
					float num;
					CollisionObject3D collider = null;
					if (entity.entityData.TryGetValue("spawnflags", out strWord))
					{
						int spawnflags = int.Parse(strWord);
						//Suspended
						if ((spawnflags & 1) != 0)
							thingObject.GlobalPosition = entity.origin;
						else
							thingObject.GlobalPosition = ItemLocationDropToFloor(entity.origin, ref collider);
					}
					else
						thingObject.GlobalPosition = ItemLocationDropToFloor(entity.origin, ref collider);

					if (collider != null)
						thingObject.Reparent(collider);

					if (entity.name.Contains("func_bobbing"))
					{
						GameManager.Print("func_bobbing");
						foreach (var data in entity.entityData)
							GameManager.Print("Key: " + data.Key + " Value: " + data.Value);
					}

					if (entity.entityData.TryGetNumValue("wait", out num))
						thingObject.SetRespawnTime(num);

					if (entity.entityData.TryGetNumValue("random", out num))
						thingObject.SetRandomTime(num);

					if (thingObject.thingType != ThingController.ThingType.Item)
						break;

					ItemPickup itemPickup = thingObject.itemPickup;
					if (itemPickup == null)
						break;

					//GamePlay Rules:
					if ((GameManager.Instance.gameType == GameManager.GameType.QuadHog) && (thingObject.uniqueItem))
					{
						itemPickup.amount = 60;
						thingObject.SetRespawnTime(float.MaxValue);
					}
					else if (entity.entityData.TryGetNumValue("count", out num))
						itemPickup.amount = (int)num;

					string target;
					if (entity.entityData.TryGetValue("targetname", out target))
					{
						List<Target> targetList = null;
						if (targetsOnMap.TryGetValue(target, out targetList))
							targetList.Add(new Target(entity.origin, 0, entity.entityData));
						else
						{
							targetList = new List<Target>
							{
								new Target(entity.origin, 0, entity.entityData)
							};
							targetsOnMap.Add(target, targetList);
						}
						giveItemPickup[entity.entityData["classname"]] = itemPickup;
					}
				}
				break;
				//Deathmatch spawning  location
				case "info_player_deathmatch":
				{
					thingObject.GlobalPosition = entity.origin;

					if (entity.entityData.ContainsKey("nohumans"))
						continue;

					float angle = 0;
					entity.entityData.TryGetNumValue("angle", out angle);

					SpawnPosition spawnPosition = (SpawnPosition)thingObject;
					spawnPosition.Init((int)angle, entity.entityData);
				}
				break;
				//Red Team spawning  location
				case "team_CTF_redspawn":
				{
					thingObject.GlobalPosition = entity.origin;

					if (entity.entityData.ContainsKey("nohumans"))
						continue;

					float angle = 0;
					entity.entityData.TryGetNumValue("angle", out angle);

					SpawnPosition spawnPosition = (SpawnPosition)thingObject;
					spawnPosition.Init((int)angle, entity.entityData, SpawnPosition.SpawnType.Red);
				}
				break;
				//Blue Team spawning  location
				case "team_CTF_bluespawn":
				{
					thingObject.GlobalPosition = entity.origin;

					if (entity.entityData.ContainsKey("nohumans"))
						continue;

					float angle = 0;
					entity.entityData.TryGetNumValue("angle", out angle);

					SpawnPosition spawnPosition = (SpawnPosition)thingObject;
					spawnPosition.Init((int)angle, entity.entityData, SpawnPosition.SpawnType.Blue);
				}
				break;
				// Solid Model
				case "func_static":
				{
					thingObject.GlobalPosition = entity.origin;
					if (entity.entityData.TryGetValue("model", out strWord))
					{
						int model = int.Parse(strWord.Trim('*'));
						MapLoader.GenerateGeometricSurface(thingObject, model);
						MapLoader.GenerateGeometricCollider(thingObject, null, model, 0, false);
					}
					else if (entity.entityData.TryGetValue("model2", out strWord))
					{
						ModelController modelController = new ModelController();
						thingObject.AddChild(modelController);
						modelController.modelName = strWord.Split('.')[0].Split("models/")[1];
						modelController.Init();
					}
				}
				break;
				//Switch
				case "func_button":
				{
					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					int angle = 0, hitpoints = 0, speed = 40, lip = 4;
					float wait;

					SwitchController sw = new SwitchController();
					thingObject.AddChild(sw);

					sw.startSound = "movers/switches/butn2";
					sw.endSound = "";

					if (entity.entityData.TryGetValue("angle", out strWord))
						angle = int.Parse(strWord);
					if (entity.entityData.TryGetValue("health", out strWord))
						hitpoints = int.Parse(strWord);
					if (entity.entityData.TryGetValue("speed", out strWord))
						speed = int.Parse(strWord);
					if (!entity.entityData.TryGetNumValue("wait", out wait))
						wait = 1;
					if (entity.entityData.TryGetValue("lip", out strWord))
						lip = int.Parse(strWord);

					MapLoader.GenerateGeometricSurface(sw, model);
					uint OwnerShapeId = MapLoader.GenerateGeometricCollider(thingObject, sw, model, 0, false);
					int shapes = sw.ShapeOwnerGetShapeCount(OwnerShapeId);
					Aabb BigBox = new Aabb();
					for (int i = 0; i < shapes; i++)
					{
						Shape3D boxShape = sw.ShapeOwnerGetShape(OwnerShapeId, i);
						Aabb box = boxShape.GetDebugMesh().GetAabb();
						if (i == 0)
							BigBox = new Aabb(box.Position, box.Size);
						else
							BigBox = BigBox.Merge(box);
					}
					sw.Init(angle, hitpoints, speed, wait, lip, BigBox);

					//If it's not damagable, then create trigger collider
					if (hitpoints == 0)
					{
						float max = BigBox.GetLongestAxisSize();
						Area3D triggerCollider = new Area3D();
						sw.AddChild(triggerCollider);
						CollisionShape3D mc = new CollisionShape3D();
						mc.Name = "Switch Trigger";
						triggerCollider.AddChild(mc);
						triggerCollider.CollisionLayer = (1 << GameManager.WalkTriggerLayer);
						triggerCollider.CollisionMask = GameManager.TakeDamageMask;
						triggerCollider.InputRayPickable = false;

						SphereShape3D sphere = new SphereShape3D();
						sphere.Radius = max;
						mc.Shape = sphere;
						triggerCollider.GlobalPosition = BigBox.GetCenter();
						triggerCollider.BodyEntered += sw.internalSwitch.OnBodyEntered;
						sw.internalSwitch.Areas.Add(triggerCollider);
					}

					if (entity.entityData.TryGetValue("target", out strWord))
					{
						string target = strWord;
						TriggerController tc;
						if (!triggerToActivate.TryGetValue(target, out tc))
							tc = null;
						sw.tc = tc;

						sw.internalSwitch.SetController(target, (p) =>
						{
							if (sw.tc == null)
							{
								TriggerController swTrigger;
								if (!triggerToActivate.TryGetValue(sw.internalSwitch.triggerName, out swTrigger))
									return;
								sw.tc = swTrigger;
							}
							sw.CurrentState = DoorController.State.Opening;
							sw.tc.Activate(null);
						});
					}
				}
				break;
				//Door
				case "func_door":
				{
					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					int angle = 0, hitpoints = 0, speed = 200, lip = 8, dmg = 4;
					float wait;

					DoorController door = new DoorController();
					thingObject.AddChild(door);

					Node3D SourceTransform = new Node3D();
					door.AddChild(SourceTransform);

					InterpolatedTransform interpolatedTransform = new InterpolatedTransform();
					interpolatedTransform.SetSource(SourceTransform);
					thingObject.AddChild(interpolatedTransform);

					if (entity.entityData.TryGetValue("angle", out strWord))
						angle = int.Parse(strWord);
					if (entity.entityData.TryGetValue("health", out strWord))
						hitpoints = int.Parse(strWord);
					if (entity.entityData.TryGetValue("speed", out strWord))
						speed = int.Parse(strWord);
					if (!entity.entityData.TryGetNumValue("wait", out wait))
						wait = 2;
					if (entity.entityData.TryGetValue("lip", out strWord))
						lip = int.Parse(strWord);
					if (entity.entityData.TryGetValue("dmg", out strWord))
						dmg = int.Parse(strWord);

					MapLoader.GenerateGeometricSurface(interpolatedTransform, model);
					uint OwnerShapeId = MapLoader.GenerateGeometricCollider(thingObject, door, model, 0, false);
					int shapes = door.ShapeOwnerGetShapeCount(OwnerShapeId);
					Aabb BigBox = new Aabb();

					for (int i = 0; i < shapes; i++)
					{
						Shape3D shape = door.ShapeOwnerGetShape(OwnerShapeId, i);
						Aabb box = shape.GetDebugMesh().GetAabb();
						if (i == 0)
							BigBox = new Aabb(box.Position, box.Size);
						else
							BigBox = BigBox.Merge(box);
					}
					door.Init(angle, hitpoints, speed, wait, lip, BigBox, dmg);

					if (entity.entityData.TryGetValue("targetname", out strWord))
					{
						string target = strWord;

						TriggerController tc;
						if (!triggerToActivate.TryGetValue(target, out tc))
						{
							tc = new TriggerController();
							thingObject.AddChild(tc);
							triggerToActivate.Add(target, tc);
						}
						tc.Repeatable = true;
						tc.AutoReturn = true;
						tc.AutoReturnTime = wait;
						tc.SetController(target, (p) =>
						{
							door.CurrentState = DoorController.State.Opening;
						});
					}
					else //If it's not external trigger
					{
						if (hitpoints == 0)//If  not damagable, then create a trigger and collider
						{
							float max = BigBox.GetLongestAxisSize();
							Area3D triggerCollider = new Area3D();
							door.AddChild(triggerCollider);
							CollisionShape3D mc = new CollisionShape3D();
							mc.Name = "Door Trigger";
							triggerCollider.AddChild(mc);
							triggerCollider.CollisionLayer = (1 << GameManager.WalkTriggerLayer);
							triggerCollider.CollisionMask = GameManager.TakeDamageMask;
							triggerCollider.InputRayPickable = false;

							SphereShape3D sphere = new SphereShape3D();
							sphere.Radius = max * .5f;
							mc.Shape = sphere;

							TriggerController tc = new TriggerController();
							thingObject.AddChild(tc);
							tc.Repeatable = true;
							tc.AutoReturn = true;
							tc.AutoReturnTime = wait;
							tc.SetController("", (p) =>
							{
								door.CurrentState = DoorController.State.Opening;
							});

							triggerCollider.GlobalPosition = BigBox.GetCenter();
							triggerCollider.BodyEntered += tc.OnBodyEntered;
							tc.Areas.Add(triggerCollider);
						}
					}
				}
				break;
				//Platform
				case "func_bobbing":
				{
					Vector3 center = Vector3.Zero;
					int model = -1;
					int angle = 0, spawnflags = 0, height = 32;
					float speed = 4, phase = 0;
					string noise;

					ModelController modelController = null;
					if (entity.entityData.TryGetValue("model", out strWord))
						model = int.Parse(strWord.Trim('*'));
					if (entity.entityData.TryGetValue("model2", out strWord))
					{
						model = -1;
						modelController = new ModelController();
						modelController.modelName = strWord.Split('.')[0].Split("models/")[1];
						modelController.Init();
						thingObject.GlobalPosition = entity.origin;
					}

					entity.entityData.TryGetNumValue("speed", out speed);
					entity.entityData.TryGetNumValue("phase", out phase);

					if (speed == 0)
					{
						if (model >= 0)
						{
							MapLoader.GenerateGeometricSurface(thingObject, model);
							MapLoader.GenerateGeometricCollider(thingObject, null, model, 0, false);
						}
						else if (modelController != null)
							thingObject.AddChild(modelController);
						break;
					}

					PlatformController platform = new PlatformController();
					thingObject.AddChild(platform);

					if (modelController != null)
						platform.AddChild(modelController);

					Node3D SourceTransform = new Node3D();
					platform.AddChild(SourceTransform);

					InterpolatedTransform interpolatedTransform = new InterpolatedTransform();
					interpolatedTransform.SetSource(SourceTransform);
					thingObject.AddChild(interpolatedTransform);

					if (entity.entityData.TryGetValue("angle", out strWord))
						angle = int.Parse(strWord);
					if (entity.entityData.TryGetValue("height", out strWord))
						height = int.Parse(strWord);
					if (entity.entityData.TryGetValue("spawnflags", out strWord))
						spawnflags = int.Parse(strWord);
					if (entity.entityData.TryGetValue("noise", out noise))
						noise = GetSoundName(noise);

					Vector3 direction = Vector3.Up;
					if ((spawnflags & 1) != 0)
						direction = Vector3.Right;
					else if ((spawnflags & 2) != 0)
						direction = Vector3.Forward;

					bool isCrusher = false;
					if (model >= 0)
					{
						MapLoader.GenerateGeometricSurface(interpolatedTransform, model);
						uint OwnerShapeId = MapLoader.GenerateGeometricCollider(thingObject, platform, model, 0, false);
						if (OwnerShapeId != 0)
						{
							int shapes = platform.ShapeOwnerGetShapeCount(OwnerShapeId);
							Aabb BigBox = new Aabb();

							for (int i = 0; i < shapes; i++)
							{
								Shape3D shape = platform.ShapeOwnerGetShape(OwnerShapeId, i);
								Aabb box = shape.GetDebugMesh().GetAabb();
								if (i == 0)
									BigBox = new Aabb(box.Position, box.Size);
								else
									BigBox = BigBox.Merge(box);
							}
							center = BigBox.GetCenter();
							isCrusher = true;
						}
					}
					platform.Init(direction, speed, phase, height, isCrusher, center, noise);
				}
				break;
				//Rotating Object
				case "func_rotating":
				{
					thingObject.GlobalPosition = entity.origin;

					int speed = 100;
					NodeAnimation nodeAnim = new NodeAnimation();
					thingObject.AddChild(nodeAnim);
					if (entity.entityData.TryGetValue("speed", out strWord))
						speed = int.Parse(strWord);
					nodeAnim.rotFPS = speed;
					nodeAnim.rotEnable = true;
					if (entity.entityData.TryGetValue("model2", out strWord))
					{
						ModelController modelController = new ModelController();
						thingObject.AddChild(modelController);
						modelController.modelName = strWord.Split('.')[0].Split("models/")[1];
						modelController.Init();
					}
					nodeAnim.Init();
				}
				break;
				//Trigger Hurt
				case "trigger_hurt":
				{
					int dmg = 9999;
					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					if (entity.entityData.TryGetValue("dmg", out strWord))
						dmg = int.Parse(strWord);

					TriggerController tc = new TriggerController();
					thingObject.AddChild(tc);
					Area3D objCollider = new Area3D();
					thingObject.AddChild(objCollider);
					MapLoader.GenerateGeometricCollider(thingObject, objCollider, model, ContentFlags.Trigger);
					objCollider.BodyEntered += tc.OnBodyEntered;
					tc.Areas.Add(objCollider);

					tc.Repeatable = true;
					tc.SetController("trigger_hurt", (p) =>
					{
						p.Damage(dmg, DamageType.Generic);
					});
				}
				break;
				//Intermission Camera
				case "info_player_intermission":
				{
					thingObject.GlobalPosition = entity.origin;

					Camera3D camera = new Camera3D();
					thingObject.AddChild(camera);
					camera.CullMask = GameManager.AllPlayerViewMask | (1 << GameManager.NotVisibleLayer);
					ScoreBoard scoreBoard =	(ScoreBoard)GameManager.Instance.scoreBoard.Instantiate();
					camera.AddChild(scoreBoard);

					int angle = 0;
					Vector3 lookAt = Vector3.Forward;

					if (entity.entityData.TryGetValue("target", out strWord))
						if (targetsOnMap.ContainsKey(strWord))
						{
							lookAt = targetsOnMap[strWord][0].destination;
							thingObject.LookAt(lookAt, Vector3.Up);
							angle = targetsOnMap[strWord][0].angle;
						}
					GameManager.Instance.interMissionCamera = camera;
					GameManager.Instance.SetViewPortToCamera(camera, GameManager.Instance.IntermissionViewPort);
				}
				break;
				//Portal Camera
				case "misc_portal_camera":
				{
					thingObject.GlobalPosition = entity.origin;

					int angle = 0;
					Vector3 lookAt = Vector3.Forward;
//					if (entity.entityData.TryGetValue("angle", out strWord))
//						GameManager.Print("Angle " + strWord);

					if (entity.entityData.TryGetValue("target", out strWord))
						if (targetsOnMap.ContainsKey(strWord))
						{
							lookAt = targetsOnMap[strWord][0].destination;
							thingObject.LookAt(lookAt, Vector3.Up);
							angle = targetsOnMap[strWord][0].angle;
						}

					if (entity.entityData.TryGetValue("targetname", out strWord))
					{
						Camera3D camera = new Camera3D();
						thingObject.AddChild(camera);
						camera.CullMask = GameManager.AllPlayerViewMask;
						portalCameras.Add(strWord, camera);
					}
//					if (entity.entityData.TryGetValue("roll", out strWord))
//						GameManager.Print("Roll " + strWord);
				}
				break;
				//Portal Surface
				case "misc_portal_surface":
				{
					PortalSurface portalSurface = new PortalSurface();
					GameManager.Instance.TemporaryObjectsHolder.AddChild(portalSurface);
					portalSurface.GlobalPosition = entity.origin;
					if (entity.entityData.TryGetValue("target", out strWord))
					{
						portalSurface.targetName = strWord;
						GameManager.Print("TargetName " + strWord);
					}
					portalSurfaces.Add(portalSurface);
				}
				break;
				//Remove PowerUps
/*
				case "target_remove_powerups":
					{
						if (entity.entityData.TryGetValue("targetname", out strWord))
						{
							string target = strWord;

							TriggerController tc;
							if (!triggerToActivate.TryGetValue(target, out tc))
							{
								tc = thingObject.AddComponent<TriggerController>();
								triggerToActivate.Add(target, tc);
							}
							else
								Destroy(thingObject);
							tc.Repeatable = true;
							tc.SetController(target, (p) =>
							{
								p.RemovePowerUps();
							});
						}
					}
					break;
*/
				//Delay
				case "target_delay":
				{
					string target;
					if (!entity.entityData.TryGetValue("target", out target))
						continue;
					if (entity.entityData.TryGetValue("targetname", out strWord))
					{
						float wait = 0;
						entity.entityData.TryGetNumValue("wait", out wait);

						string targetName = strWord;
						TriggerController source, relay;
						if (!triggerToActivate.TryGetValue(targetName, out source))
						{
							source = new TriggerController();
							thingObject.AddChild(source);
							triggerToActivate.Add(targetName, source);
						}

						if (!triggerToActivate.TryGetValue(target, out relay))
							relay = null;

						source.SetController(targetName, (p) =>
						{
							if (relay == null)
							{
								if (!triggerToActivate.TryGetValue(target, out relay))
									return;
							}

							relay.ActivateAfterTime(wait, p);
						});
						}
					}
					break;
				//Relay
				case "target_relay":
				{
					string target;
					if (!entity.entityData.TryGetValue("target", out target))
						continue;
					if (entity.entityData.TryGetValue("targetname", out strWord))
					{
						string targetName = strWord;
						TriggerController source, relay;
						if (!triggerToActivate.TryGetValue(targetName, out source))
						{
							source = new TriggerController();
							thingObject.AddChild(source);
							triggerToActivate.Add(targetName, source);
						}

						if (!triggerToActivate.TryGetValue(target, out relay))
							relay = null;

						source.SetController(targetName, (p) =>
						{
							if (relay == null)
							{
								if (!triggerToActivate.TryGetValue(target, out relay))
									return;
							}
							relay.Activate(p);
						});
					}
				}
				break;
				//Give
				case "target_give":
				{
					string target;
					if (!entity.entityData.TryGetValue("target", out target))
						continue;
					if (entity.entityData.TryGetValue("targetname", out strWord))
					{
						string targetName = strWord;
						TriggerController tc;
						if (!triggerToActivate.TryGetValue(targetName, out tc))
						{
							tc = new TriggerController();
							thingObject.AddChild(tc);
							triggerToActivate.Add(targetName, tc);
						}

						tc.SetController(targetName, (p) =>
						{
							if (p == null)
								return;

							List<Target> targetList = null;
							if (!targetsOnMap.TryGetValue(target, out targetList))
								return;
							foreach (Target target in targetList)
							{
								ItemPickup itemPickup;
								if (giveItemPickup.TryGetValue(target.entityData["classname"], out itemPickup))
									itemPickup.PickUp(p, false);
							}
						});
					}
				}
				break;
				//JumpPad
				case "trigger_push":
				{
					string target;
					List<Target> dest;
					if (!entity.entityData.TryGetValue("target", out target))
						continue;
					if (!targetsOnMap.TryGetValue(target, out dest))
						continue;

					thingObject.GlobalPosition = entity.origin;
					JumpPadThing jumpPad = new JumpPadThing();
					thingObject.AddChild(jumpPad);
					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					Vector3 destination = dest[0].destination;
					Vector3 center = MapLoader.GenerateJumpPadCollider(jumpPad, model);
					jumpPad.Init(destination, center);
				}
				break;
				//Teleporter
				case "trigger_teleport":
				{
					string target;
					List<Target> dest;
					if (!entity.entityData.TryGetValue("target", out target))
						continue;
					if (!targetsOnMap.TryGetValue(target, out dest))
						continue;
					if (!entity.entityData.TryGetValue("model", out strWord))
						continue;

					TeleporterThing teleporter = new  TeleporterThing();
					thingObject.AddChild(teleporter);
					int model = int.Parse(strWord.Trim('*'));
					MapLoader.GenerateGeometricCollider(thingObject, teleporter, model, ContentFlags.Teleporter);
					teleporter.Init(dest, entity.entityData);
				}
				break;
				//Location
				case "target_location":
				{
					string message;
					thingObject.GlobalPosition = entity.origin;
					if (entity.entityData.TryGetValue("message", out message))
						thingObject.EditorDescription = message;
					MapLoader.Locations.Add(thingObject);
				}
				break;
				//Speaker
				case "target_speaker":
				{

					strWord = GetSoundName(entity.entityData["noise"]);
					bool isAudio3d = true;
					AudioStreamPlayer audioStream2D = null;
					MultiAudioStream audioStream = null;

					thingObject.GlobalPosition = entity.origin;
					if (entity.entityData.ContainsKey("spawnflags"))
					{

						string audioFile = strWord;
						audioStream = new MultiAudioStream();
						thingObject.AddChild(audioStream);
						audioStream.Bus = "BKGBus";

						strWord = entity.entityData["spawnflags"];

						int spawnflags = int.Parse(strWord);
						if ((spawnflags & 3) != 0)
						{
							audioStream.Stream = SoundManager.LoadSound(audioFile,true);
							if ((spawnflags & 1) != 0)
								audioStream.Play();
						}
						else
						{
							if ((spawnflags & 8) != 0) //Activator Sound
							{
								if (entity.entityData.TryGetValue("targetname", out strWord))
								{
									string target = strWord;
									bool playerSound = false;

									if (audioFile.Contains('*'))
									{
										playerSound = true;
										audioFile = audioFile.Trim('*');
									}
									else
										audioStream.Stream = SoundManager.LoadSound(audioFile);
									TriggerController tc;
									if (!triggerToActivate.TryGetValue(target, out tc))
									{
										tc = new TriggerController();
										thingObject.AddChild(tc);
										triggerToActivate.Add(target, tc);
									}
									tc.Repeatable = true;
									tc.SetController(target, (p) =>
									{
										if (playerSound)
											p.PlayModelSound(audioFile);
										else if (audioStream != null)
											audioStream.Play();
									});
								}
							}
							else
							{
								if ((spawnflags & 4) != 0) //Global 2D Sound
								{
									isAudio3d = false;
									audioStream.QueueFree();
									audioStream2D = new AudioStreamPlayer();
									thingObject.AddChild(audioStream2D);
									audioStream2D.Stream = SoundManager.LoadSound(audioFile);
									audioStream2D.Bus = "BKGBus";
								}
								if (entity.entityData.TryGetValue("targetname", out strWord))
								{
									string target = strWord;

									TriggerController tc;
									if (!triggerToActivate.TryGetValue(target, out tc))
									{
										tc = new TriggerController();
										thingObject.AddChild(tc);
										triggerToActivate.Add(target, tc);
									}
									tc.Repeatable = true;
									tc.SetController(target, (p) =>
									{
										if (isAudio3d)
											audioStream.Play();
										else
											audioStream2D.Play();
									});
								}
							}
						}
					}
					else if (entity.entityData.ContainsKey("random"))
						AddRandomTimeToSound(thingObject, entity.entityData, audioStream2D, audioStream, isAudio3d);
				}
				break;
				//Advertisement
				case "advertisement":
				{
					if (entity.entityData.TryGetValue("model", out strWord))
					{
						int model = int.Parse(strWord.Trim('*'));
						MapLoader.GenerateGeometricSurface(thingObject, model);
						MapLoader.GenerateGeometricCollider(thingObject, null, model, 0, false);
					}
				}
				break;
				//Worldspawn
				case "worldspawn":
				{
					if (entity.entityData.TryGetValue("message", out strWord))
						GameManager.Print("Map Message: " + strWord);
					if (entity.entityData.TryGetValue("music", out strWord))
					{
						if ((GameManager.Instance.musicType == GameManager.MusicType.Static) || (GameManager.Instance.musicType == GameManager.MusicType.Random))
						{
							string[] keyValue = strWord.Split(' ');
							if (keyValue.Length > 0)
							{
								strWord = keyValue[0].Split('.')[0].Replace('\\', '/');
								GameManager.Print("Music : " + strWord);
								GameManager.Instance.StaticMusicPlayer.Stream = SoundManager.LoadSound(strWord, true, true);
							}
						}
					}
					if (entity.entityData.TryGetValue("gravity", out strWord))
					{
						GameManager.Print("Gravity : " + strWord);
						int gravity = int.Parse(strWord);
						GameManager.Instance.gravity = gravity * GameManager.sizeDividor;
					}
				}
				break;
			}
		}
		//Unique GameType stuff
		//Check if all uniqueThings are on the map
		if (uniqueThingsOnMap.Count != uniqueGamePlayThings.Count)
		{
			switch (GameManager.Instance.gameType)
			{
				default:
				break;
				case GameManager.GameType.QuadHog:
				{
					for (int i = 0; i < uniqueGamePlayThings.Count; i++)
					{
						string uniqueItem = uniqueGamePlayThings[i];
						for (int j = 0; j < quadHogReplacement.Length; j++)
						{
							string searchItem = quadHogReplacement[j];
							if (potentialuniqueThingsOnMap.TryGetValue(searchItem, out ThingController thing))
							{
								ThingController uniqueObject = (ThingController)thingsPrefabs[uniqueItem].Instantiate();
								GameManager.Instance.TemporaryObjectsHolder.AddChild(uniqueObject);
								uniqueObject.GlobalPosition = thing.GlobalPosition;
								uniqueObject.initDisabled = false;
								uniqueObject.uniqueItem = true;
								uniqueObject.SpawnCheck(uniqueItem);
								uniqueObject.Name = uniqueItem;
								ItemPickup itemPickup = uniqueObject.itemPickup;
								itemPickup.amount = 60;
								uniqueObject.SetRespawnTime(float.MaxValue);
								thing.SetRespawnTime(float.MaxValue);
								thing.DisableThing();
								uniqueThingsOnMap.Add(uniqueItem, uniqueObject);
								break;
							}
						}
					}
				}
				break;
			}
		}
	}

	public static Vector3 ItemLocationDropToFloor(Vector3 Origin)
	{
		CollisionObject3D collider = null;
		return ItemLocationDropToFloor(Origin, ref collider);
	}

	public static Vector3 ItemLocationDropToFloor(Vector3 Origin, ref CollisionObject3D collider)
	{
		float maxRange = 400f;
		Vector3 collision = Origin;
		Vector3 End = Origin + Vector3.Down * maxRange;
		var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | (1 << GameManager.InvisibleBlockerLayer)));
		var SpaceState = GameManager.Instance.Root.GetWorld3D().DirectSpaceState;
		var hit = SpaceState.IntersectRay(RayCast);
		if (hit.Count > 0)
		{
			collision = (Vector3)hit["position"] + Vector3.Up * .6f;
			collider = (CollisionObject3D)hit["collider"];
		}
		return collision;
	}
	public static void AddPortalsToMap()
	{
		for (int n = 0; n < portalSurfaces.Count; n++)
		{
			PortalSurface portalSurface = portalSurfaces[n];
			Portal portal = null;
			float closestPortal = 0;
			Camera3D camera;
			if (string.IsNullOrEmpty(portalSurface.targetName))
			{
				GameManager.Print("Found Mirror");
				Node3D mirror = new Node3D();
				mirror.Name = "Mirror";
				GameManager.Instance.TemporaryObjectsHolder.AddChild(mirror);
				camera = new Camera3D();
				mirror.AddChild(camera);
				camera.Projection = Camera3D.ProjectionType.Frustum;
				camera.CullMask = GameManager.AllPlayerViewMask;
				for (int i = 0; i < portalsOnMap.Count; i++)
				{
					float distance = (portalsOnMap[i].position - portalSurface.GlobalPosition).LengthSquared();
					if ((portal == null) || (distance < closestPortal))
					{
						portal = portalsOnMap[i];
						closestPortal = distance;
					}
				}
				if (portal != null)
					portalSurface.SetUpPortal(camera, portal, true);
				else
				{
					portalSurface.QueueFree();
					portalSurfaces.Remove(portalSurface);
				}
			}
			else if (portalCameras.TryGetValue(portalSurface.targetName, out camera))
			{
				GameManager.Print("Found Portal Camera " + camera.Name);
				for (int i = 0; i < portalsOnMap.Count; i++)
				{
					float distance = (portalsOnMap[i].position - portalSurface.GlobalPosition).LengthSquared();
					if ((portal == null) || (distance < closestPortal))
					{
						portal = portalsOnMap[i];
						closestPortal = distance;
					}
				}
				if (portal != null)
					portalSurface.SetUpPortal(camera, portal);
				else
				{
					portalSurface.QueueFree();
					portalSurfaces.Remove(portalSurface);
				}
			}
			else
			{
				portalSurface.QueueFree();
				portalSurfaces.Remove(portalSurface);
			}
		}
	}

	public static void NewLocalPlayerAdded()
	{
		for (int n = 0; n < portalSurfaces.Count; n++)
		{
			PortalSurface portalSurface = portalSurfaces[n];
			portalSurface.NewLocalPlayerAdded();
		}
	}
	public static string GetSoundName(string strWord)
	{
		string[] keyValue = strWord.Split('.');
		strWord = keyValue[0];
		if (!strWord.Contains('*'))
		{
			keyValue = strWord.Split('/');
			strWord = "";
			for (int i = 1; i < keyValue.Length; i++)
			{
				if (i > 1)
					strWord += "/";
				strWord += keyValue[i];
			}
		}
		return strWord;
	}

	public static void AddRandomTimeToSound(Node3D node, Dictionary<string, string> entityData, AudioStreamPlayer audioStream2D, MultiAudioStream audioStream, bool isAudio3d)
	{
		float random, wait;
		PlayAfterRandomTime playRandom =new PlayAfterRandomTime();
		node.AddChild(playRandom);
		entityData.TryGetNumValue("random", out random);
		entityData.TryGetNumValue("wait", out wait);
		if (isAudio3d)
			playRandom.AddMultiAudioStream(audioStream);
		else
			playRandom.AddAudioStream(audioStream2D);
		playRandom.Init(wait, random);
	}
}
public static class DictionaryExtensions
{
	public static bool TryGetNumValue(this Dictionary<string, string> entityData, string key, out float num)
	{
		int inum = 0;
		num = 0;
		string strWord;
		if (entityData.TryGetValue(key, out strWord))
		{
			if (int.TryParse(strWord, out inum))
				num = inum;
			else
				num = float.Parse(strWord);
			return true;
		}
		return false;
	}
}
public class Target
{
	public Vector3 destination;
	public int angle;
	public Dictionary<string, string> entityData;
	public Target(Vector3 destination, int angle, Dictionary<string, string> entityData)
	{
		this.destination = destination;
		this.angle = angle;
		this.entityData = entityData;
	}
}
public class Entity
{
	public string name;
	public Vector3 origin;
	public Dictionary<string, string> entityData;
	public Entity(string name, Vector3 origin, Dictionary<string, string> entityData)
	{
		this.name = name;
		this.origin = origin;
		this.entityData = entityData;
	}
}
public class RespawnItem
{
	public Node3D node;
	public string respawnSound;
	public float time;

	public RespawnItem(Node3D node, string respawnSound, float time)
	{
		this.node = node;
		this.respawnSound = respawnSound;
		this.time = time;
	}
}

public class Portal
{
	public string shaderName;
	public Vector3 position;
	public Vector3 normal;
	public ArrayMesh commonMesh;
	public List<Surface> surfaces = new List<Surface>();
	public ShaderMaterial commonMat;

	public class Surface
	{
		public MeshInstance3D mesh;
		public ShaderMaterial baseMat;
		public ShaderMaterial material;

		public Surface(MeshInstance3D mesh, ShaderMaterial material)
		{
			this.mesh = mesh;
			this.baseMat = material;
			this.material = (ShaderMaterial)material.NextPass;
		}

	}
	public Portal(string shaderName, ShaderMaterial baseMat)
	{
		this.shaderName = shaderName;
		this.commonMat = baseMat;
	}
}

public static class BFGTracers
{
	public static float[] hx;
	public static float[] hy;
	public static int pixels;
	public static int samples = 500;
	private static float HaltonSequence(int index, int b)
	{
		float r = 0.0f;
		float f = 1.0f / b;
		int i = index;

		while (i > 0)
		{
			r = r + f * (i % b);
			i = Mathf.FloorToInt(i / b);
			f = f / b;
		}

		return r;
	}

	public static void SetTracers()
	{
		pixels = Mathf.FloorToInt(640 * 360); // 1/4th 720p
		hx = new float[pixels];
		hy = new float[pixels];

		for (int i = 0; i < pixels; i++)
		{
			hx[i] = HaltonSequence(i, 2);
			hy[i] = HaltonSequence(i, 3);
		}
	}
}


