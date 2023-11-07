using Godot;
using System;
public partial class PlayerInfo : Node3D
{
	[Export]
	public PlayerControls playerControls;
	[Export]
	public PlayerCamera playerCamera;
	[Export]
	public MultiAudioStream audioStream;
	[Export]
	public PlayerThing playerThing;
//	public PlayerHUD playerHUD;
//	public Canvas UICanvas;
	[Export]
	public Node3D WeaponHand;

	public int[] Ammo = new int[8] { 1000, 500, 0, 100, 0, 0, 1000, 0 }; //bullets, shells, grenades, rockets, lightning, slugs, cells, bfgammo
	public bool[] Weapon = new bool[9] { false, true, true, false, true, false, false, true, false }; //gauntlet, machinegun, shotgun, grenade launcher, rocket launcher, lightning gun, railgun, plasma gun, bfg10k
	public int[] MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

	public bool godMode = false;
	public int MaxHealth = 100;
	public int MaxBonusHealth = 200;
	public int MaxArmor = 100;
	public int MaxBonusArmor = 200;
	[Export]
	public PackedScene[] WeaponPrefabs = new PackedScene[9];

	public int viewLayer = (1 << GameManager.Player1ViewLayer);
	public uint playerLayer = (1 << GameManager.Player1Layer);
	public uint uiLayer = (1 << GameManager.Player1UIViewLayer);
	public const int RenderFrameMask = 0xFFF0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public void Reset()
	{
		Ammo = new int[8] { 100, 0, 0, 0, 0, 0, 0, 0 };
		Weapon = new bool[9] { false, true, false, false, false, false, false, false, false };
		MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

		godMode = false;

//		playerHUD.pickupFlashTime = 0f;
//		playerHUD.painFlashTime = 0f;

		playerThing.hitpoints = 100;
		playerThing.armor = 0;

		playerControls.impulseVector = Vector3.Zero;
		playerControls.CurrentWeapon = -1;
		playerControls.SwapWeapon = -1;
//		playerControls.SwapToBestWeapon();

//		playerHUD.HUDUpdateAmmoNum();
//		playerHUD.HUDUpdateHealthNum();
//		playerHUD.HUDUpdateArmorNum();
	}
	public override void _Process(double delta)
	{
		int currentFrame = (Engine.GetFramesDrawn() << GameManager.Player1UIViewLayer);
		CheckPVS(currentFrame,GlobalPosition);
	}

	private int FindCurrentLeaf(Vector3 currentPos)
	{
		// Search trought the BSP tree until the index is negative, and indicate it's a leaf.
		int i = 0;
		while (i >= 0)
		{
			// Retrieve the node and slit Plane
			QNode node = MapLoader.nodes[i];
			QPlane slitPlane = MapLoader.planes[node.plane];

			// Determine whether the current position is on the front or back side of this plane.
			if (slitPlane.GetSide(currentPos))
			{
				// If the current position is on the front side this is the index our new tree node
				i = node.front;
			}
			else
			{
				// Otherwise, the back is the index our new tree node
				i = node.back;
			}
		}
		//  abs(index value + 1) is our leaf
		return ~i;
	}

	private bool IsClusterVisible(int current, int test)
	{
		// If the bitSets array is empty, make all the clusters as visible
		if (MapLoader.visData.bitSets.Length == 0)
			return true;

		// If the player is no-clipping then don't draw
		if (current < 0)
			return false;

		// Calculate the index of the test cluster in the bitSets array
		int testIndex = test / 8;

		// Retrieve the appropriate byte from the bitSets array
		byte visTest = MapLoader.visData.bitSets[(current * MapLoader.visData.bytesPerCluster) + (testIndex)];

		// Check if the test cluster is marked as visible in the retrieved byte
		bool visible = ((visTest & (1 << (test & 7))) != 0);

		// Return whether or not the cluster is visible
		return visible;
	}
	public void CheckPVS(int currentFrame, Vector3 currentPos)
	{
		// Find the index of the current leaf
		int leafIndex = FindCurrentLeaf(currentPos);

		// Get the cluster the current leaf belongs to
		int cluster = MapLoader.leafs[leafIndex].cluster;

		// Loop through all leafs in the map
		int i = MapLoader.leafs.Count;
		while (i-- != 0)
		{
			QLeaf leaf = MapLoader.leafs[i];

			//If negative, then leaf is back leaf and contains no visibility data
			if (leaf.cluster < 0)
				continue;

			// Check if the leaf's cluster is visible from the current leaf's cluster
			if (!IsClusterVisible(cluster, leaf.cluster))
				continue;

			// Loop through all the surfaces in the leaf
			int surfaceCount = leaf.numOfLeafFaces;
			while (surfaceCount-- != 0)
			{
				int surfaceId = MapLoader.leafsSurfaces[leaf.leafSurface + surfaceCount];

				// Check if the surface has already been added to be render in the current frame
				// and that the layer mask is not visible to the player
				if ((MapLoader.leafRenderFrameLayer[surfaceId] & RenderFrameMask) == currentFrame)
				{
					if ((MapLoader.leafRenderFrameLayer[surfaceId] & viewLayer) != 0)
						continue;
					// Add the player layer mask to the surface in the current frame
					MapLoader.leafRenderFrameLayer[surfaceId] |= viewLayer;
				}
				else // Add the player layer mask and the current frame to the surface
					MapLoader.leafRenderFrameLayer[surfaceId] = currentFrame | viewLayer;

				// Activate the cluster associated with the surface
				ClusterPVSManager.Instance.ActivateClusterBySurface(surfaceId, (uint)viewLayer);
			}
		}
	}
}
