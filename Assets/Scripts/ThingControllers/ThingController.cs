using Godot;
using System.Collections.Generic;

public partial class ThingController : Node3D
{
	public Node3D parent = null;

	[Export]
	public string respawnSound = "items/respawn1";
	[Export]
	public float respawnTime;
	[Export]
	public float randomTime = 0;
	[Export]
	public bool initDisabled = false;

	private float remainingTime = 0;
	private bool _disabled = false;
	public bool disabled { get { return _disabled; } }
	public enum ThingType
	{
		Decor, //non-blocking, non-interactive
		Blocking, //blocking or interactive
		Item,
		Teleport,
		WorldSpawn,
		Target,
		Trigger,
		Door,
		Spawn
	}
	[Export]
	public ThingType thingType = ThingType.Decor;

	[Export]
	public ItemPickup itemPickup = null;

	public string itemName;
	//Only 1 per Map according to GameType
	public bool uniqueItem = false;

	private List<ThingController> collidedItems = new List<ThingController>();
	public void SpawnCheck(string name)
	{
		itemName = name;
		if (initDisabled)
		{
			DisableThing();
			//Set first spawn time to 30s
			remainingTime = 30;
		}
	}

	public override void _Ready()
	{
		//Only Keep Items Active
		if (thingType != ThingType.Item)
			SetProcess(false);
	}

	public void CheckItemsNearBy()
	{
		SetPhysicsProcess(false);
		if (itemPickup == null)
			return;

		//Unique Items don't do checks
		if (uniqueItem)
			return;

		List<CollisionShape3D> Childrens = GameManager.GetAllChildrensByType<CollisionShape3D>(this);
		if (Childrens.Count == 0)
			return;
		
		var Sphere = Childrens[0].Shape.GetRid();
		var SphereCast = new PhysicsShapeQueryParameters3D();
		SphereCast.ShapeRid = Sphere;
		SphereCast.CollideWithAreas = true;
		SphereCast.CollideWithBodies = false;
		SphereCast.CollisionMask = (1 << GameManager.ThingsLayer);
		SphereCast.Motion = Vector3.Zero;
		SphereCast.Transform = GlobalTransform;
		var SpaceState = GetWorld3D().DirectSpaceState;
		var hits = SpaceState.IntersectShape(SphereCast);
		var max = hits.Count;

		for (int i = 0; i < max; i++)
		{
			var hit = hits[i];

			CollisionObject3D collider = (CollisionObject3D)hit["collider"];
			if (collider is ItemPickup item)
			{
				//Don't Add Self
				if (item == itemPickup)
					continue;

				collidedItems.Add(item.thingController);
			}
		}
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		if (disabled)
		{
			float deltaTime = (float)delta;
			remainingTime -= deltaTime;
			if (remainingTime < 0)
			{
				if (collidedItems.Count > 0)
				{
					bool blocked = false;
					for (int i = 0; i < collidedItems.Count; i++)
					{
						if (!collidedItems[i].disabled)
							blocked = true;
					}
					if (blocked)
					{
						remainingTime += 5;
						return;
					}
				}
				remainingTime = 0;
				_disabled = false;
				Visible = true;
				if (!string.IsNullOrEmpty(respawnSound))
					SoundManager.Create3DSound(GlobalPosition, SoundManager.LoadSound(respawnSound));
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		CheckItemsNearBy();
	}

	public void SetRandomTime(float waitTime)
	{
		randomTime = waitTime;
	}
	public void SetRespawnTime(float waitTime)
	{
		respawnTime = waitTime;
	}
	public void RespawnNow()
	{
		remainingTime = 0;
	}

	public void DisableThing()
	{
		//No longer respawn
		if (respawnTime < 0)
		{
			if (parent != null)
				parent.QueueFree();
			else
				QueueFree();
		}

		remainingTime = (float)GD.RandRange(respawnTime - randomTime, respawnTime + randomTime);
		Visible = false;
		_disabled = true;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationPredelete)
			CheckDestroy();
	}
	public void CheckDestroy()
	{
		if (disabled)
			return;

		//GameType
		if (uniqueItem)
		{
			if (ThingsManager.uniqueThingsOnMap.TryGetValue(itemName, out ThingController masterThing))
				masterThing.RespawnNow();
		}
	}
}
