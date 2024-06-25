using Godot;
using System.Collections.Generic;

public partial class ClusterPVSManager : Node
{
	public static ClusterPVSManager Instance;
	private List<GeometryInstance3D> AllClusters;
	private GeometryInstance3D[] SurfaceToCluster;
	private Dictionary<GeometryInstance3D, uint> ActivatedSurfaces = new Dictionary<GeometryInstance3D, uint>();
	public const int RenderFrameMask = (0xFFFF << GameManager.MaxLocalPlayers);
	public void ResetClusterList(int count)
	{
		AllClusters = new List<GeometryInstance3D>();
		SurfaceToCluster = new GeometryInstance3D[count];
	}
	public override void _Ready()
	{
		Instance = this;
		RenderingServer.FramePreDraw += () => OnPreRender();
		RenderingServer.FramePostDraw += () => OnPostRender();
	}

	public void OnPreRender()
	{
		foreach (var surfaceToActivate in ActivatedSurfaces)
			surfaceToActivate.Key.Layers = surfaceToActivate.Value;
	}

	public void OnPostRender()
	{
		for (int i = 0; i < AllClusters.Count; i++) 
			AllClusters[i].Layers = (1 << GameManager.NotVisibleLayer);
		ActivatedSurfaces.Clear();
	}
	public void RegisterClusterAndSurface(GeometryInstance3D cluster, QSurface surface)
	{
		SurfaceToCluster[surface.surfaceId] = cluster;
		AllClusters.Add(cluster);
	}
	public void RegisterClusterAndSurfaces(GeometryInstance3D cluster, QSurface[] surfaces)
	{
		for (int i = 0; i < surfaces.Length; i++)
			SurfaceToCluster[surfaces[i].surfaceId] = cluster;
		AllClusters.Add(cluster);
	}

	public void ActivateClusterBySurface(int surface, uint layer)
	{
		uint currentLayer;
		GeometryInstance3D cluster = SurfaceToCluster[surface];
		if (cluster == null)
			return;

		if (ActivatedSurfaces.TryGetValue(cluster, out currentLayer))
			ActivatedSurfaces[cluster] = currentLayer | layer;
		else
			ActivatedSurfaces.Add(cluster, layer);
	}
	private static int FindCurrentLeaf(Vector3 currentPos)
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

	private static bool IsClusterVisible(int current, int test)
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

	public static void CheckPVS(int viewLayer, Vector3 currentPos)
	{
		int currentFrame = (Engine.GetFramesDrawn() << GameManager.MaxLocalPlayers);
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
				Instance.ActivateClusterBySurface(surfaceId, (uint)viewLayer);
			}
		}
	}
}
