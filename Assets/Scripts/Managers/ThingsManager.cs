using Godot;
using System.IO;
using System.Collections;
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
	public static readonly string[] ignoreThings = { "misc_model", "light", "func_group", "info_null" };
	public static readonly string[] triggerThings = { "func_timer", "trigger_always", "trigger_multiple", "target_relay" , "target_delay" };
	public static readonly string[] targetThings = { "func_timer", "trigger_multiple", "target_relay", "target_delay", "target_position", "info_notnull", "misc_teleporter_dest" };
	public static List<Portal> portalsOnMap = new List<Portal>();
	public static readonly string ItemDrop = "ItemDrop";
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
							targetList.Add(new Target(origin, angle));
						else
						{
							targetList = new List<Target>
							{
								new Target(origin, angle)
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
	public static void AddThingsToMap()
	{
		AddTriggersOnMap();
		AddEntitiesToMap();
		AddTimersToMap();
		AddPortalsToMap();
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

			foreach (Dictionary<string, string> entityData in entityDataList)
			{
				switch (GameManager.Instance.gameType)
				{
					default:
						break;
					case GameManager.GameType.FreeForAll:
					case GameManager.GameType.Tournament:
					case GameManager.GameType.OneFlagCTF:
						{
							if (entityData.ContainsKey("notfree"))
								continue;
						}
						break;
					case GameManager.GameType.TeamDeathmatch:
					case GameManager.GameType.CaptureTheFlag:
					case GameManager.GameType.Overload:
					case GameManager.GameType.Harvester:
						{
							if (entityData.ContainsKey("notteam"))
								continue;
						}
						break;
					case GameManager.GameType.SinglePlayer:
						{
							if (entityData.ContainsKey("notsingle"))
								continue;
						}
						break;
				}

				float wait;
				if (entityData.TryGetNumValue("wait", out wait))
				{
					tc.AutoReturn = true;
					tc.AutoReturnTime = wait;
				}

				string strWord;
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
				case GameManager.GameType.Tournament:
				case GameManager.GameType.OneFlagCTF:
				{
					if (entity.entityData.ContainsKey("notfree"))
						continue;
				}
				break;
				case GameManager.GameType.TeamDeathmatch:
				case GameManager.GameType.CaptureTheFlag:
				case GameManager.GameType.Overload:
				case GameManager.GameType.Harvester:
				{
					if (entity.entityData.ContainsKey("notteam"))
						continue;
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
				
			thingObject.SpawnCheck();

			GameManager.Instance.TemporaryObjectsHolder.AddChild(thingObject);
			thingObject.Name = entity.name;

			switch (entity.name)
			{
				default:
				{
					float wait;
					if (entity.entityData.TryGetValue("spawnflags", out strWord))
					{
						int spawnflags = int.Parse(strWord);
						//Suspended
						if ((spawnflags & 1) != 0)
							thingObject.GlobalPosition = entity.origin;
						else
							thingObject.GlobalPosition = ItemLocationDropToFloor(entity.origin);
					}
					else
						thingObject.GlobalPosition = ItemLocationDropToFloor(entity.origin);

/*					if (entity.name.Contains("item_health_mega"))
						foreach (var data in entity.entityData)
							GameManager.Print("Key: " + data.Key + " Value: " + data.Value);
*/
					if (entity.entityData.TryGetNumValue("wait", out wait))
						thingObject.SetRespawnTime(wait);

					if (thingObject.thingType != ThingController.ThingType.Item)
						break;

					ItemPickup itemPickup = thingObject.itemPickup;
					if (itemPickup == null)
						break;

					if (entity.entityData.TryGetNumValue("count", out wait))
						itemPickup.amount =(int)wait;
				}
				break;

				//Teleporter
				case "info_player_deathmatch":
				{
					thingObject.GlobalPosition = entity.origin;

					if (entity.entityData.ContainsKey("nohumans"))
						continue;

					int angle = 0;
					if (entity.entityData.TryGetValue("angle", out strWord))
						angle = int.Parse(strWord);

					SpawnPosition spawnPosition = (SpawnPosition)thingObject;
					spawnPosition.Init(angle);
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

					MapLoader.GenerateGeometricSurface(door, model);
					uint OwnerShapeId = MapLoader.GenerateGeometricCollider(thingObject, door, model, 0, false);
					int shapes = door.ShapeOwnerGetShapeCount(OwnerShapeId);
					Aabb BigBox = new Aabb();

					DoorCollider doorCollider = new DoorCollider();
					door.AddChild(doorCollider);
					doorCollider.CollisionLayer = (1 << GameManager.WalkTriggerLayer);
					doorCollider.CollisionMask = GameManager.TakeDamageMask;
					doorCollider.door = door;
					uint bodyShapeId = doorCollider.CreateShapeOwner(door);
					for (int i = 0; i < shapes; i++)
					{
						Shape3D shape = door.ShapeOwnerGetShape(OwnerShapeId, i);
						doorCollider.ShapeOwnerAddShape(bodyShapeId, shape);
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
					teleporter.Init(dest);
				}
				break;
				//Location
				case "target_location":
				{
					thingObject.GlobalPosition = entity.origin;
					thingObject.EditorDescription = entity.entityData["message"];
					MapLoader.Locations.Add(thingObject);
				}
				break;
				//Speaker
				case "target_speaker":
				{
					strWord = entity.entityData["noise"];
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
										if (audioFile.Contains('*'))
											p.PlayModelSound(audioFile.Trim('*'));
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
				//Worldspawn
				case "worldspawn":
				{
					if (entity.entityData.TryGetValue("message", out strWord))
						GameManager.Print("Map Message: " + strWord);
					if (entity.entityData.TryGetValue("music", out strWord))
					{
						if (GameManager.Instance.musicType == GameManager.MusicType.Static)
						{
							string[] keyValue = strWord.Split(' ');
							if (keyValue.Length > 0)
							{
								strWord = keyValue[0].Split('.')[0].Replace('\\', '/');
								GameManager.Print("Music : " + strWord);
								MultiAudioStream audioStream = new MultiAudioStream();
								GameManager.Instance.TemporaryObjectsHolder.AddChild(audioStream);
								audioStream.Is2DAudio = true;
								audioStream.VolumeDb = 14;
								audioStream.Name = "Music";
								audioStream.Bus = "BKGBus";
								audioStream.Stream = SoundManager.LoadSound(strWord, true, true);
								audioStream.Play();
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
	}

	public static Vector3 ItemLocationDropToFloor(Vector3 Origin)
	{
		float maxRange = 400f;
		Vector3 collision = Origin;
		Vector3 End = Origin + Vector3.Down * maxRange;
		var RayCast = PhysicsRayQueryParameters3D.Create(Origin, End, ((1 << GameManager.ColliderLayer) | (1 << GameManager.InvisibleBlockerLayer)));
		var SpaceState = GameManager.Instance.Root.GetWorld3D().DirectSpaceState;
		var hit = SpaceState.IntersectRay(RayCast);
		if (hit.Count > 0)
			collision = (Vector3)hit["position"] + Vector3.Up * .6f;
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
	public Target(Vector3 destination, int angle)
	{
		this.destination = destination;
		this.angle = angle;
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
	public ArrayMesh arrMesh;

	public ShaderMaterial baseMat;
	public ShaderMaterial material;
	public Portal(string shaderName, ShaderMaterial baseMat)
	{
		this.shaderName = shaderName;
		this.baseMat = baseMat;
		material = (ShaderMaterial)baseMat.NextPass;
	}
}


