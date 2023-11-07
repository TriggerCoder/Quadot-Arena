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
	public PackedScene[] _itemsPrefabs;
	[Export]
	public PackedScene[] _debrisPrefabs;
	[Export]
	public PackedScene[] _weaponsPrefabs;
	[Export]
	public PackedScene[] _healthsPrefabs;
	[Export]
	public PackedScene[] _armorPrefabs;
	[Export]
	public PackedScene[] _gameplayPrefabs;

	public static Dictionary<string, PackedScene> thingsPrefabs = new Dictionary<string, PackedScene>();
	public static List<Entity> entitiesOnMap = new List<Entity>();
	public static Dictionary<string, Target> targetsOnMap = new Dictionary<string, Target>();
	public static Dictionary<string, TriggerController> triggerToActivate = new Dictionary<string, TriggerController>();
	public static Dictionary<string, Dictionary<string, string>> timersOnMap = new Dictionary<string, Dictionary<string, string>>();
	public static Dictionary<string, Dictionary<string, string>> triggersOnMap = new Dictionary<string, Dictionary<string, string>>();
	public static readonly string[] ignoreThings = { "misc_model", "light", "func_group" };
	public static readonly string[] targetThings = { "func_timer", "trigger_multiple", "target_position", "info_notnull", "misc_teleporter_dest" };

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
	public override void _Ready()
	{
		foreach (var thing in _fxPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("FX Name: "+ prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _projectilesPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Projectile Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _decalsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Decal Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _itemsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Item Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _debrisPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Debris Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _weaponsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Weapon Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _healthsPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Health Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _armorPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Armor Name: " + prefabName);
			thingsPrefabs.Add(prefabName, thing);
		}
		foreach (var thing in _gameplayPrefabs)
		{
			SceneState sceneState = thing.GetState();
			string prefabName = sceneState.GetNodeName(0);
			GD.Print("Gamplay Item: " + prefabName);
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
					entityData.Add(keyValue[1].Trim('"'), keyValue[3].Trim('"'));
					strWord = stream.ReadLine();
				}

				if (!entityData.ContainsKey("classname"))
					continue;

				if (ignoreThings.Any(s => s == entityData["classname"]))
					continue;

				if (!thingsPrefabs.ContainsKey(entityData["classname"]))
				{
					GD.Print(entityData["classname"] + " not found");
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

				if (targetThings.Any(s => s == entityData["classname"]))
				{
					if (!entityData.TryGetValue("target", out strWord))
						strWord = entityData["targetname"];
					string target = strWord;

					switch (entityData["classname"])
					{
						default:
							targetsOnMap.Add(target, new Target(origin, angle));
							break;
						case "func_timer": //Timers
							timersOnMap.Add(target, entityData);
							break;
						case "trigger_multiple": //Triggers
							triggersOnMap.Add(target, entityData);
							break;
					}
				}
				else
					entitiesOnMap.Add(new Entity(entityData["classname"], origin, entityData));
			}
		}

		stream.Close();
		return;
	}
	public static void AddThingsToMap()
	{
//		AddTriggersOnMap();
		AddEntitiesToMap();
		AddTimersToMap();
	}
	/*
		public static void AddTriggersOnMap()
		{
			foreach (KeyValuePair<string, Dictionary<string, string>> trigger in triggersOnMap)
			{
				string target = trigger.Key;
				Dictionary<string, string> entityData = trigger.Value;
				GameObject thingObject = Instantiate(thingsPrefabs[entityData["classname"]]);
				if (thingObject == null)
					continue;
				thingObject.name = "Trigger " + target;

				TriggerController tc = thingObject.GetComponent<TriggerController>();
				if (tc == null)
					continue;

				string strWord = entityData["model"];
				int model = int.Parse(strWord.Trim('*'));
				float wait;

				if (entityData.TryGetNumValue("wait", out wait))
				{
					tc.AutoReturn = true;
					tc.AutoReturnTime = wait;
				}

				MapLoader.GenerateGeometricCollider(thingObject, model, ContentFlags.Trigger);
				triggerToActivate.Add(target, tc);
				thingObject.transform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
				thingObject.SetActive(true);
			}
		}
	*/

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
			ThingController thingObject = (ThingController)thingsPrefabs[entity.name].Instantiate();
			if (thingObject == null)
				continue;

			GameManager.Instance.TemporaryObjectsHolder.AddChild(thingObject);

			switch (entity.name)
			{
				default:
					thingObject.GlobalPosition = entity.origin;
				break;
/*				//Switch
				case "func_button":
				{
					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					int angle = 0, hitpoints = 0, speed = 40, lip = 4;
					float wait;

					SwitchController sw = thingObject.GetComponent<SwitchController>();
					if (sw == null)
						sw = thingObject.AddComponent<SwitchController>();
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

					MapLoader.GenerateGeometricSurface(thingObject, model);
					//As dynamic surface don't have bsp data, assign it to the always visible layer 
					GameManager.SetLayerAllChildren(currentTransform, GameManager.CombinesMapMeshesLayer);
					MapLoader.GenerateGeometricCollider(currentTransform, model);

					MeshFilter[] meshFilterChildren = thingObject.GetComponentsInChildren<MeshFilter>(includeInactive: true);
					CombineInstance[] combine = new CombineInstance[meshFilterChildren.Length];
					for (int i = 0; i < combine.Length; i++)
						combine[i].mesh = meshFilterChildren[i].mesh;

					var mesh = new Mesh();
					mesh.CombineMeshes(combine, true, false, false);
					Bounds bounds = mesh.bounds;
					sw.Init(angle, hitpoints, speed, wait, lip, bounds);

					//If it's not damagable, then create trigger collider
					if (hitpoints == 0)
					{
						float max = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
						SphereCollider sc = thingObject.AddComponent<SphereCollider>();
						sc.radius = max;
						sc.isTrigger = true;
					}
					//If it is, then we need to create a damage interface for the colliders
					else
					{
						Collider[] collidersChildren = thingObject.GetComponentsInChildren<Collider>(includeInactive: true);
						for (var i = 0; i < collidersChildren.Length; i++)
						{
							ParentIsDamageable parentIsDamageable = collidersChildren[i].gameObject.AddComponent<ParentIsDamageable>();
							parentIsDamageable.parent = sw;
						}
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
*/				//Door
/*				case "func_door":
				{
					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					int angle = 0, hitpoints = 0, speed = 200, lip = 8, dmg = 4;
					float wait;
					DoorController door = thingObject.GetComponent<DoorController>();
					if (door == null)
						door = thingObject.AddComponent<DoorController>();
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

					MapLoader.GenerateGeometricSurface(thingObject, model);
					//As dynamic surface don't have bsp data, assign it to the always visible layer 
					GameManager.SetLayerAllChildren(currentTransform, GameManager.CombinesMapMeshesLayer);
					MapLoader.GenerateGeometricCollider(currentTransform, model);

					MeshFilter[] meshFilterChildren = thingObject.GetComponentsInChildren<MeshFilter>(includeInactive: true);
					CombineInstance[] combine = new CombineInstance[meshFilterChildren.Length];
					for (var i = 0; i < combine.Length; i++)
						combine[i].mesh = meshFilterChildren[i].mesh;

					var mesh = new Mesh();
					mesh.CombineMeshes(combine, true, false, false);
					Bounds bounds = mesh.bounds;
					door.Init(angle, hitpoints, speed, wait, lip, bounds, dmg);

					//Need to change the rb to non kinematics in order for collision detection to work
					Rigidbody[] rigidbodiesChildren = thingObject.GetComponentsInChildren<Rigidbody>(includeInactive: true);
					for (int i = 0; i < rigidbodiesChildren.Length; i++)
					{
						rigidbodiesChildren[i].useGravity = false;
						rigidbodiesChildren[i].isKinematic = false;
						rigidbodiesChildren[i].constraints = RigidbodyConstraints.FreezeAll;
						DoorCollider doorCollider = rigidbodiesChildren[i].gameObject.AddComponent<DoorCollider>();
						doorCollider.door = door;
					}

					if (entity.entityData.TryGetValue("targetname", out strWord))
					{
						string target = strWord;

						TriggerController tc;
						if (!triggerToActivate.TryGetValue(target, out tc))
						{
							tc = thingObject.AddComponent<TriggerController>();
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
							float max = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
							SphereCollider sc = thingObject.AddComponent<SphereCollider>();
							sc.radius = max;
							sc.isTrigger = true;

							TriggerController tc = thingObject.AddComponent<TriggerController>();
							tc.Repeatable = true;
							tc.AutoReturn = true;
							tc.AutoReturnTime = wait;
							tc.SetController("", (p) =>
							{
								door.CurrentState = DoorController.State.Opening;
							});
						}
						else //If it is, then we need to create a damage interface for the colliders
						{
							for (int i = 0; i < rigidbodiesChildren.Length; i++)
							{
								ParentIsDamageable parentIsDamageable = rigidbodiesChildren[i].gameObject.AddComponent<ParentIsDamageable>();
								parentIsDamageable.parent = door;
							}
						}
					}
				}
				break;
*/				//Trigger Hurt
/*				case "trigger_hurt":
				{
					int dmg = 9999;
					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					if (entity.entityData.TryGetValue("dmg", out strWord))
						dmg = int.Parse(strWord);
					MapLoader.GenerateGeometricCollider(thingObject, model, ContentFlags.Trigger);
					TriggerController tc = thingObject.GetComponent<TriggerController>();
					if (tc == null)
						tc = thingObject.AddComponent<TriggerController>();
					tc.Repeatable = true;
					tc.SetController("", (p) =>
					{
						p.Damage(dmg, DamageType.Generic);
					});
				}
				break;
*/				//Remove PowerUps
/*				case "target_remove_powerups":
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
*/				//JumpPad
/*				case "trigger_push":
				{
					JumpPadThing thing = thingObject.GetComponent<JumpPadThing>();
					if (thing == null)
						continue;

					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					string target = entity.entityData["target"];
					Vector3 destination = targetsOnMap[target].destination;
					MapLoader.GenerateJumpPadCollider(thingObject, model);
					thing.Init(destination);
				}
				break;
*/				//Teleporter
/*				case "trigger_teleport":
				{
					TeleporterThing thing = thingObject.GetComponent<TeleporterThing>();
					if (thing == null)
						continue;

					strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					string target = entity.entityData["target"];
					Vector3 destination = targetsOnMap[target].destination;
					int angle = targetsOnMap[target].angle;
					MapLoader.GenerateGeometricCollider(thingObject, model, ContentFlags.Teleporter);
					thing.Init(destination, angle);
				}
				break;
*/				//Speaker
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


