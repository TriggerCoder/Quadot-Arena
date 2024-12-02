using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExtensionMethods;
public static class MapLoader
{
	public static string CurrentMap;

	private static BinaryReader BSPMap;

	public static BSPHeader header;

	public static List<QSurface> surfaces;
	public static List<ImageTexture> lightMaps;
	public static List<QVertex> verts;
	public static List<int> vertIndices;
	public static List<Plane> planes;
	public static List<QNode> nodes;
	public static List<QLeaf> leafs;
	public static List<int> leafsSurfaces;
	public static uint[] leafRenderFrameLayer;
	public static List<QModel> models;
	public static List<QBrush> brushes;
	public static List<int> leafsBrushes;
	public static List<QBrushSide> brushSides;
	public static List<QShader> mapTextures;
	public static List<QFog> mapFog;
	public static QVisData visData;

	public static LightMapSize currentLightMapSize = LightMapSize.Q3_QL;

	public static Node3D MapMesh;
	public static Node3D MapFlares;
	public static Node3D ColliderGroup;
	public static List<Node3D> Locations;
	public static List<WaterSurface> waterSurfaces;

	public static int MAX_MESH_SURFACES = 256;
	public enum LightMapSize
	{
		Q3_QL = 128,
		QAA = 512
	}
	public enum LightMapLenght
	{
		Q3_QL = 49152,      //128*128*3
		QAA = 786432        //512*512*3
	}

	//Don't add decals nor marks to these surfaces
	public static HashSet<CollisionObject3D> noMarks;
	public static Dictionary<CollisionObject3D, SurfaceType> mapSurfaceTypes;
	public static Dictionary<CollisionObject3D, ContentType> mapContentTypes;

	//Map Data Limits
	public static Vector3 mapMinCoord;
	public static Vector3 mapMaxCoord;
	public static Aabb mapBounds;

	//Light Vol Data
	public static Vector3 LightVolNormalize;
	public static Vector3 LightVolOffset;
	public static ImageTexture3D LightVolAmbient;
	public static ImageTexture3D LightVolDirectonal;

	public static bool UseCheats = false;
	public static bool Load(string mapName)
	{
		string FileName;
		string path = Directory.GetCurrentDirectory() + "/StreamingAssets/maps/" + mapName + ".bsp";
		if (File.Exists(path))
			BSPMap = new BinaryReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.TryGetValue(path = ("maps/" + mapName + ".bsp").ToUpper(), out FileName))
		{
			MemoryStream ms = new MemoryStream(PakManager.GetPK3FileData(path, FileName));
			BSPMap = new BinaryReader(ms);
		}
		else
			return false;

		//Clear noMarks
		noMarks = new HashSet<CollisionObject3D>();

		//clear waterSurfaces
		waterSurfaces = new List<WaterSurface>();

		//Clear SurfaceType
		mapSurfaceTypes = new Dictionary<CollisionObject3D, SurfaceType>();

		//Clear ContentType
		mapContentTypes = new Dictionary<CollisionObject3D, ContentType>();

		//Clean Locations
		Locations = new List<Node3D>();

		//header
		{
			header = new BSPHeader(BSPMap);
		}

		//entities
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Entities].Offset, SeekOrigin.Begin);
			ThingsManager.ReadEntities(BSPMap.ReadBytes(header.Directory[LumpType.Entities].Length));
		}

		//shaders (textures)
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Shaders].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Shaders].Length / 72;
			mapTextures = new List<QShader>(num);
			GameManager.Print("mapTextures " + num);
			for (int i = 0; i < num; i++)
			{
				mapTextures.Add(new QShader(mapName.GetStringFromBytes(BSPMap.ReadBytes(64)).ToUpper(), BSPMap.ReadUInt32(), BSPMap.ReadUInt32(), false));
			}
		}

		//planes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Planes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Planes].Length / 16;
			planes = new List<Plane>(num);
			GameManager.Print("planes " + num);
			for (int i = 0; i < num; i++)
			{
				planes.Add(new Plane(QuakeToGodot.Vect3(new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), false), BSPMap.ReadSingle() * GameManager.sizeDividor));
			}
		}

		//nodes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Nodes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Nodes].Length / 36;
			nodes = new List<QNode>(num);
			GameManager.Print("nodes " + num);
			for (int i = 0; i < num; i++)
			{
				nodes.Add(new QNode(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), new Vector3I(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), new Vector3I(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32())));
			}
		}

		//leafs
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Leafs].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Leafs].Length / 48;
			leafs = new List<QLeaf>(num);
			GameManager.Print("leafs " + num);
			for (int i = 0; i < num; i++)
			{
				leafs.Add(new QLeaf(BSPMap.ReadInt32(), BSPMap.ReadInt32(), new Vector3I(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), new Vector3I(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//leafs faces
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.LeafSurfaces].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.LeafSurfaces].Length / 4;
			leafsSurfaces = new List<int>(num);
			leafRenderFrameLayer = new uint[num];
			GameManager.Print("leafsSurfaces " + num);
			for (int i = 0; i < num; i++)
			{
				leafsSurfaces.Add(BSPMap.ReadInt32());
			}
		}

		//leafs brushes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.LeafBrushes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.LeafBrushes].Length / 4;
			leafsBrushes = new List<int>(num);
			GameManager.Print("leafsBrushes " + num);
			for (int i = 0; i < num; i++)
			{
				leafsBrushes.Add(BSPMap.ReadInt32());
			}
		}

		//models (map geometry)
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Models].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Models].Length / 40;
			models = new List<QModel>(num);
			GameManager.Print("map geometry " + num);
			for (int i = 0; i < num; i++)
			{
				models.Add(new QModel(new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()),
										new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()),
										BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//brushes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Brushes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Brushes].Length / 12;
			brushes = new List<QBrush>(num);
			GameManager.Print("brushes " + num);
			for (int i = 0; i < num; i++)
			{
				brushes.Add(new QBrush(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//brush sides
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.BrushSides].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.BrushSides].Length / 8;
			brushSides = new List<QBrushSide>(num);
			GameManager.Print("brushSides " + num);
			for (int i = 0; i < num; i++)
			{
				brushSides.Add(new QBrushSide(BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//vertices
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Vertexes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Vertexes].Length / 44;
			verts = new List<QVertex>(num);
			GameManager.Print("vertices " + num);
			for (int i = 0; i < num; i++)
			{
				verts.Add(new QVertex(i, new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()),
													BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle(),
													new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), BSPMap.ReadBytes(4)));
			}
		}

		//vertex indices
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.VertIndices].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.VertIndices].Length / 4;
			vertIndices = new List<int>(num);
			GameManager.Print("vertIndices " + num);
			for (int i = 0; i < num; i++)
			{
				vertIndices.Add(BSPMap.ReadInt32());
			}
		}

		//effects (Fog)
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Effects].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Effects].Length / 72;
			mapFog = new List<QFog>(num);
			GameManager.Print("mapFog " + num);
			for (int i = 0; i < num; i++)
			{
				mapFog.Add(new QFog(mapName.GetStringFromBytes(BSPMap.ReadBytes(64)).ToUpper(), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//We need to determine the max number in order to check lightmap type
		int maxlightMapNum = 0;
		//surfaces
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Surfaces].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Surfaces].Length / 104;
			surfaces = new List<QSurface>(num);
			GameManager.Print("surfaces " + num);
			for (int i = 0; i < num; i++)
			{
				surfaces.Add(new QSurface(i, BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(),
					BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), new[]
					{
						BSPMap.ReadInt32(),
						BSPMap.ReadInt32()
					}, new[]
					{
						BSPMap.ReadInt32(),
						BSPMap.ReadInt32()
					}, new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), new[]
					{
						new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()),
						new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle())
					}, new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), new[]
					{
						BSPMap.ReadInt32(),
						BSPMap.ReadInt32()
					}));

				if (surfaces[i].lightMapID > maxlightMapNum)
					maxlightMapNum = surfaces[i].lightMapID;
			}
			//Need to count lightmap 0
			maxlightMapNum++;
		}

		//Q3/QL lightmaps (128x128x3)
		//QAA lightmaps (512x512x3)
		{
			//Check lightmap type
			int lightMapLenght = (int)LightMapLenght.QAA;
			if ((maxlightMapNum * lightMapLenght) > header.Directory[LumpType.LightMaps].Length)
				lightMapLenght = (int)LightMapLenght.Q3_QL;
			else
				currentLightMapSize = LightMapSize.QAA;

			BSPMap.BaseStream.Seek(header.Directory[LumpType.LightMaps].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.LightMaps].Length / lightMapLenght;
			lightMaps = new List<ImageTexture>(num);
			GameManager.Print("lightMaps " + num);
			for (int i = 0; i < num; i++)
			{
				lightMaps.Add(TextureLoader.CreateLightmapTexture(BSPMap.ReadBytes(lightMapLenght)));
			}
		}

		//Light Vols
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.LightGrid].Offset, SeekOrigin.Begin);
			(LightVolAmbient, LightVolDirectonal) = TextureLoader.CreateLightVolTextures(BSPMap.ReadBytes(header.Directory[LumpType.LightGrid].Length), models[0].bb_Min, models[0].bb_Max, ref LightVolNormalize, ref LightVolOffset);
		}

		//vis data
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.VisData].Offset, SeekOrigin.Begin);
			if (header.Directory[LumpType.VisData].Length > 0)
			{
				visData = new QVisData(BSPMap.ReadInt32(), BSPMap.ReadInt32());
				visData.bitSets = BSPMap.ReadBytes(visData.numOfClusters * visData.bytesPerCluster);
			}
		}

		LerpColorOnRepeatedVertex();
		GetMapTextures();
		BSPMap.Close();
		
		return true;
	}

	public static void UnloadMap(bool useCheats)
	{
		UseCheats = useCheats;
		SpawnerManager.ClearLists();
		ModelsManager.ClearModels();
		ClusterPVSManager.Instance.ResetClusterList(1);
		Mesher.MultiMeshes = new Dictionary<MultiMesh, Dictionary<Node3D, int>>();
		Mesher.MultiMeshesInstances = new Dictionary<MultiMesh, MultiMeshInstance3D>();
		Mesher.MultiMeshSprites = new Dictionary<MultiMesh, List<SpriteData>>();
		Mesher.MultiMeshesChanged = new HashSet<MultiMesh>();
		ThingsManager.UnloadThings();
		MapMesh.QueueFree();
		MapFlares.QueueFree();
		ColliderGroup.QueueFree();
		GameManager.Instance.TemporaryObjectsHolder.QueueFree();
		GameManager.Console.ClearConsole();
		System.GC.Collect(2, System.GCCollectionMode.Forced);
	}

	public static void GenerateMapCollider()
	{
		Node3D MapColliders = new Node3D();
		MapColliders.Name = "MapColliders";
		ColliderGroup = MapColliders;

		mapBounds = new Aabb();
		List<QBrush> staticBrushes = new List<QBrush>();
		for (int i = 0; i < models[0].numBrushes; i++)
			staticBrushes.Add(brushes[models[0].firstBrush + i]);

		// Each brush group is its own object
		var groups = staticBrushes.GroupBy(x => new { mapTextures[x.shaderId].contentsFlags, mapTextures[x.shaderId].surfaceFlags });
		int groupId = 0;
		foreach (var group in groups)
		{
			QBrush[] groupBrushes = group.ToArray();
			if (groupBrushes.Length == 0)
				continue;
			
			groupId++;

			Mesher.GenerateGroupBrushCollider(groupId, ColliderGroup, groupBrushes);
		}
		mapMinCoord = mapBounds.Position;
		mapMaxCoord = mapBounds.Size;
	}
	public static void GenerateSurfaces()
	{
		MapMesh = new Node3D();
		MapMesh.Name = "MapMeshes";
		Node3D holder = MapMesh;
		GameManager.Instance.AddChild(MapMesh);
		MapFlares = new Node3D();
		MapFlares.Name = "MapFlares";
		holder.AddChild(MapFlares);

		List<QSurface> staticGeometry = new List<QSurface>();
		for (int i = 0; i < models[0].numSurfaces; i++)
			staticGeometry.Add(surfaces[models[0].firstSurface + i]);

		// Each surface group is its own object
		var groups = staticGeometry.GroupBy(x => new { x.type, x.shaderId, x.lightMapID });
		int groupId = 0;
		foreach (var bigGroup in groups)
		{
			bool billBoard = false;
			int ChunkSize = MAX_MESH_SURFACES;
			string shaderName = mapTextures[bigGroup.ElementAt(0).shaderId].name;
			if (MaterialManager.HasBillBoard.Contains(shaderName))
			{
				billBoard = true;
				if (bigGroup.Key.type != QSurfaceType.Billboard)
					ChunkSize = 1;
				GameManager.Print("AUTOSPRITE " + shaderName);
			}
			else if (MaterialManager.IsPortalMaterial(shaderName))
			{
				ChunkSize = 1;
				GameManager.Print("PORTAL " + shaderName);
			}
			var limitedGroup = bigGroup.Chunk(ChunkSize);
			foreach (var group in limitedGroup)
			{
				QSurface[] groupSurfaces = group.ToArray();
				if (groupSurfaces.Length == 0)
					continue;

				groupId++;

				switch (bigGroup.Key.type)
				{
					case QSurfaceType.Patch:
						Mesher.GenerateBezObject(groupSurfaces[0].shaderId, groupSurfaces[0].lightMapID, groupId, holder, groupSurfaces);
						break;
					case QSurfaceType.Polygon:
					case QSurfaceType.Mesh:
						if (billBoard)
							Mesher.GenerateBillBoardObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, holder, groupSurfaces[0]);
						else
							Mesher.GeneratePolygonObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, holder, groupSurfaces);
						break;
					case QSurfaceType.Billboard:
							Mesher.GenerateBillBoardSprites(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, MapFlares, groupSurfaces);
						break;
					default:
						GameManager.Print("Group " + groupId + "Skipped surface because it was not a polygon, mesh, or bez patch (" + bigGroup.Key.type + ").", GameManager.PrintType.Info);
						break;
				}
			}
		}
		//JOLT require Node (ColliderGroup) to be added after all the shapes have been adeed:
		//"Manipulating a body's shape(s) after it has entered a scene tree can be costly"
		GameManager.Instance.AddChild(ColliderGroup);
		System.GC.Collect(2, System.GCCollectionMode.Forced);
	}

	public static void GenerateMapFog()
	{
		for (int i = 0; i < mapFog.Count; i++)
		{
			QBrush brush = brushes[mapFog[i].brushNum];
			Mesher.GenerateVolumetricFog(i, brush, ColliderGroup, mapFog[i].name);
		}
	}

	public static void SetLightVolData()
	{
		RenderingServer.GlobalShaderParameterSet("LightVolNormalize", LightVolNormalize);
		RenderingServer.GlobalShaderParameterSet("LightVolOffset", LightVolOffset);
		RenderingServer.GlobalShaderParameterSet("LightVolAmbient", LightVolAmbient);
		RenderingServer.GlobalShaderParameterSet("LightVolDirectonal", LightVolDirectonal);
	}

public static void LerpColorOnRepeatedVertex()
	{
		// We are only looking for bezier type
		var groupsurfaces = surfaces.Where(s => s.type == QSurfaceType.Patch);

		// Initialize 2 lists (one for test) to hold the vertices of each surface in the group
		List<QVertex> surfVerts = new List<QVertex>();
		List<QVertex> testVerts = new List<QVertex>();

		// Now searh all the vertexes for all the bezier surface
		foreach (var groupsurface in groupsurfaces)
		{
			testVerts.Clear();

			int startVert = groupsurface.startVertIndex;
			for (int j = 0; j < groupsurface.numOfVerts; j++)
				testVerts.Add(verts[startVert + j]);

			//Get number of groups for all the vertexes by their position, as we want to get the uniques it need to match the number of vertex
			int numGroups = testVerts.GroupBy(v => new { v.position.X, v.position.Y, v.position.Z }).Count();

			// If the verts are unique, add the test vertices to the surface list
			if (numGroups == groupsurface.numOfVerts)
				surfVerts.AddRange(testVerts);
		}

		//Now we got unique vertexes for each bezier surface, search for common positions
		var vGroups = surfVerts.GroupBy(v => new { v.position.X, v.position.Y, v.position.Z });

		foreach (var vGroup in vGroups)
		{
			QVertex[] groupVerteces = vGroup.ToArray();

			if (groupVerteces.Length == 0)
				continue;

			// Set the initial color to the color of the first vertex in the group
			// The we will be interpolating the color of every common vertex
			Color color = groupVerteces[0].color;
			for (int i = 1; i < groupVerteces.Length; i++)
				color = color.Lerp(groupVerteces[i].color, 0.5f);

			// Finally set the final color to all the common vertexex
			for (int i = 0; i < groupVerteces.Length; i++)
			{
				int index = groupVerteces[i].vertId;
				verts[index].color = color;
			}
		}
	}

	public static void GenerateGeometricSurface(Node3D holder, int num)
	{
		GenerateGeometricSurface(holder, null, 0, num);
	}

	public static uint GenerateGeometricSurface(Node3D holder, CollisionObject3D collider, int num)
	{
		uint OwnerShapeId = collider.CreateShapeOwner(holder);
		GenerateGeometricSurface(holder, collider, OwnerShapeId, num);
		return OwnerShapeId;
	}


	public static void GenerateGeometricSurface(Node3D holder, CollisionObject3D collider, uint OwnerShapeId, int num)
	{
		List<QSurface> staticGeometry = new List<QSurface>();
		for (int i = 0; i < models[num].numSurfaces; i++)
			staticGeometry.Add(surfaces[models[num].firstSurface + i]);

		// Each surface group is its own object
		var groups = staticGeometry.GroupBy(x => new { x.type, x.shaderId, x.lightMapID });
		int groupId = 0;
		foreach (var bigGroup in groups)
		{
			bool billBoard = false;
			int ChunkSize = MAX_MESH_SURFACES;
			string shaderName = mapTextures[bigGroup.ElementAt(0).shaderId].name;
			if (MaterialManager.HasBillBoard.Contains(shaderName))
			{
				billBoard = true;
				if (bigGroup.Key.type != QSurfaceType.Billboard)
					ChunkSize = 1;
				GameManager.Print("AUTOSPRITE NUM" + num + " NAME: "+ shaderName);
			}
			else if (MaterialManager.IsPortalMaterial(shaderName))
			{
				ChunkSize = 1;
				GameManager.Print("PORTAL " + shaderName);
			}

			var limitedGroup = bigGroup.Chunk(ChunkSize);
			foreach (var group in limitedGroup)
			{
				QSurface[] groupSurfaces = group.ToArray();
				if (groupSurfaces.Length == 0)
					continue;

				groupId++;

				switch (bigGroup.Key.type)
				{
					case QSurfaceType.Patch:
						Mesher.GenerateBezObject(groupSurfaces[0].shaderId, groupSurfaces[0].lightMapID, groupId, holder, groupSurfaces, false, collider, OwnerShapeId);
						break;
					case QSurfaceType.Polygon:
					case QSurfaceType.Mesh:
						if (billBoard)
							Mesher.GenerateBillBoardObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, holder, groupSurfaces[0]);
						else
							Mesher.GeneratePolygonObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, holder, groupSurfaces, false);
						break;
					case QSurfaceType.Billboard:
//							Mesher.GenerateBillBoardObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, holder, modelObject, groupSurfaces);
						break;
					default:
						GameManager.Print("Group " + groupId + "Skipped surface because it was not a polygon, mesh, or bez patch (" + bigGroup.Key.type + ").", GameManager.PrintType.Info);
						break;
				}
			}
		}
	}
	public static (uint, CollisionObject3D) GenerateGeometricCollider(Node3D node, CollisionObject3D collider, int num, uint contentFlags = 0, bool isTrigger = true)
	{
		List<QBrush> listBrushes = new List<QBrush>();

		for (int i = 0; i < models[num].numBrushes; i++)
			listBrushes.Add(brushes[models[num].firstBrush + i]);

		if (listBrushes.Count == 0)
		{
			GameManager.Print("GenerateGeometricCollider brushes: " + num + " is empty", GameManager.PrintType.Info);
			return (0, null);
		}
		uint OwnerShapeId = 0;
		CollisionObject3D shapesOwner;
		(OwnerShapeId, shapesOwner) = Mesher.GenerateGroupBrushCollider(num, node, listBrushes.ToArray(), collider, contentFlags);
		return (OwnerShapeId, shapesOwner);
	}

	public static Vector3 GenerateJumpPadCollider(Area3D jumpPad, int num)
	{
		Vector3 center = Vector3.Zero;
		int numCenters = 0;
		for (int i = 0; i < models[num].numBrushes; i++)
		{
			if (!Mesher.GenerateBrushCollider(brushes[models[num].firstBrush + i], ColliderGroup, jumpPad, false, ContentFlags.JumpPad))
				continue;

			jumpPad.CollisionLayer = (1 << GameManager.WalkTriggerLayer);
			CollisionShape3D mc = jumpPad.GetChild<CollisionShape3D>(0);
			Shape3D boxShape = mc.Shape;
			Aabb box = boxShape.GetDebugMesh().GetAabb();
			center += box.GetCenter();
			numCenters++;
		}
		center /= numCenters;
		return center;
	}
	public static void GetMapTextures()
	{
		TextureLoader.LoadTextures(mapTextures, true, TextureLoader.ImageFormat.PNG);
		TextureLoader.LoadTextures(mapTextures, true, TextureLoader.ImageFormat.JPG);
		TextureLoader.LoadTextures(mapTextures, true, TextureLoader.ImageFormat.TGA);
	}
}
