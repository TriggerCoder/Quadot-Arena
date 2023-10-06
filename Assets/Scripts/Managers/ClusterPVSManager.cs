using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class ClusterPVSManager : Node
{
	public static ClusterPVSManager Instance;
	private List<MeshInstance3D> AllClusters;
	private MeshInstance3D[] SurfaceToCluster;

	public void ResetClusterList()
	{
		AllClusters = new List<MeshInstance3D>();
		SurfaceToCluster = new MeshInstance3D[MapLoader.surfaces.Count];
	}
	public override void _Ready()
	{
		Instance = this;
		RenderingServer.FramePreDraw += () => OnPreRender();
		RenderingServer.FramePostDraw += () => OnPostRender();
	}
	public void OnPreRender()
	{
//		GD.Print("PreRender");
	}
	public void OnPostRender()
	{
		AllClusters.AsParallel().ForAll(mesh => { mesh.Layers = GameManager.InvisibleMask;});
//		GD.Print("PostRender");
	}
	public void RegisterClusterAndSurfaces(MeshInstance3D cluster, params QSurface[] surfaces)
	{
		for (int i = 0; i < surfaces.Length; i++)
			SurfaceToCluster[surfaces[i].surfaceId] = cluster;
		AllClusters.Add(cluster);
	}

	public void ActivateClusterBySurface(int surface, uint layer)
	{
		MeshInstance3D cluster = SurfaceToCluster[surface];
		if (cluster == null)
		{
			GD.Print("Cluster not found for surface: " + surface);
			return;
		}
		cluster.Layers |= layer;
	}
}
