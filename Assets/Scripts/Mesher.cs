using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public static class Mesher
{
	public static Node3D MapMeshes;

	private static List<Vector3> vertsCache = new List<Vector3>();
	private static List<Vector2> uvCache = new List<Vector2>();
	private static List<Vector2> uv2Cache = new List<Vector2>();
	private static List<Vector3> normalsCache = new List<Vector3>();
	private static List<Color> vertsColor = new List<Color>();
	private static List<int> indiciesCache = new List<int>();

	private const int VertexInd = (int)Mesh.ArrayType.Vertex;
	private const int NormalInd = (int)Mesh.ArrayType.Normal;
	private const int TexUVInd = (int)Mesh.ArrayType.TexUV;
	private const int TexUV2Ind = (int)Mesh.ArrayType.TexUV2;
	private const int ColorInd = (int)Mesh.ArrayType.Color;
	private const int TriIndex = (int)Mesh.ArrayType.Index;

	public const float APROX_ERROR = 0.001f;

	public const int LOW_USE_MULTIMESHES = 512;
	public const int HIGH_USE_MULTIMESHES = 16384;

	public const uint MaskSolid = ContentFlags.Solid;
	public const uint MaskPlayerSolid = ContentFlags.Solid | ContentFlags.PlayerClip | ContentFlags.Body;
	public const uint MaskDeadSolid = ContentFlags.Solid | ContentFlags.PlayerClip;
	public const uint MaskWater = ContentFlags.Water | ContentFlags.Lava | ContentFlags.Slime;
	public const uint MaskOpaque = ContentFlags.Solid | ContentFlags.Lava | ContentFlags.Slime;
	public const uint MaskShot = ContentFlags.Solid | ContentFlags.Body | ContentFlags.Corpse;

	public const uint MaskTransparent = SurfaceFlags.NonSolid | SurfaceFlags.Sky;
	public const uint NoMarks = SurfaceFlags.NoImpact | SurfaceFlags.NoMarks;

	public static Dictionary<MultiMesh, Dictionary<Node3D, int>> MultiMeshes = new Dictionary<MultiMesh, Dictionary<Node3D, int>>();
	public static HashSet<MultiMesh> MultiMeshesChanged = new HashSet<MultiMesh>();
	public static Dictionary<MultiMesh, List<SpriteData>> MultiMeshSprites = new Dictionary<MultiMesh, List<SpriteData>>();
	public static Dictionary<MultiMesh, MultiMeshInstance3D> MultiMeshesInstances = new Dictionary<MultiMesh, MultiMeshInstance3D>();
	
	public static void ClearMesherCache()
	{
		vertsCache = new List<Vector3>();
		uvCache = new List<Vector2>();
		uv2Cache = new List<Vector2>();
		normalsCache = new List<Vector3>();
		indiciesCache = new List<int>();
		vertsColor = new List<Color>();
		BezierMesh.ClearCaches();
	}

	public static void GenerateBezObject(int shaderId, int lmIndex, int indexId, Node3D holder, QSurface[] surfaces, bool addPVS = true, CollisionObject3D collider = null, uint OwnerShapeId = 0)
	{
		if (surfaces == null || surfaces.Length == 0)
		{
			GameManager.Print("GenerateBezObject: Failed to create bezier object " + indexId + " there are no surfaces", GameManager.PrintType.Warning);
			return;
		}

		bool addCollider = true;
		string textureName = MapLoader.mapTextures[shaderId].name;
		string Name = "Bezier_Surfaces";
		int[] numPatches = new int[surfaces.Length];
		int totalPatches = 0;
		for (int i = 0; i < surfaces.Length; i++)
		{
			int patches = (surfaces[i].size[0] - 1) / 2 * ((surfaces[i].size[1] - 1) / 2);
			numPatches[i] = patches;
			totalPatches += patches;
			Name += "_" + surfaces[i].surfaceId;
		}

		uint type = MapLoader.mapTextures[shaderId].contentsFlags;
		uint stype = MapLoader.mapTextures[shaderId].surfaceFlags;

		SurfaceType surfaceType = new SurfaceType();
		surfaceType.Init(stype);
		ContentType contentType = new ContentType();
		contentType.Init(type);

		if (((contentType.value & ContentFlags.Details) != 0) || ((contentType.value & ContentFlags.Structural) != 0))
			addCollider = false;
		if ((contentType.value & MaskPlayerSolid) == 0)
			addCollider = false;

		if (addCollider)
		{
			if (collider == null)
			{
				collider = new StaticBody3D();
				collider.InputRayPickable = false;
				MapLoader.ColliderGroup.AddChild(collider);
				collider.Name = "Bezier_" + indexId + "_collider";
				OwnerShapeId = collider.CreateShapeOwner(holder);
			}

			if (!MapLoader.mapContentTypes.ContainsKey(collider))
				MapLoader.mapContentTypes.Add(collider, contentType);
		}

		int offset = 0;
		for (int i = 0; i < surfaces.Length; i++)
		{
			for (int n = 0; n < numPatches[i]; n++)
				GenerateBezMesh(OwnerShapeId, collider, surfaces[i], n, (surfaceType.NoDraw == false), ref offset);
		}

		bool noDraw = surfaceType.NoDraw;
		if (addCollider)
		{
			if ((surfaceType.value & MaskTransparent) != 0)
				collider.CollisionLayer = (1 << GameManager.InvisibleBlockerLayer);
			else
				collider.CollisionLayer = (1 << GameManager.ColliderLayer);

			//If noMarks add it to the table
			if ((surfaceType.value & NoMarks) != 0)
				MapLoader.noMarks.Add(collider);

			collider.CollisionMask = GameManager.TakeDamageMask | (1 << GameManager.RagdollLayer);
			if (!MapLoader.mapSurfaceTypes.ContainsKey(collider))
				MapLoader.mapSurfaceTypes.Add(collider, surfaceType);
		}

		if (noDraw)
			return;

		bool hasPortal = false;
		bool forceSkinAlpha = false;

		ShaderMaterial material = MaterialManager.GetMaterials(textureName, lmIndex, ref forceSkinAlpha, ref hasPortal);
		MeshInstance3D mesh = new MeshInstance3D();
		ArrayMesh arrMesh = new ArrayMesh();

		BezierMesh.FinalizeBezierMesh(arrMesh);
		arrMesh.SurfaceSetMaterial(0, material);

		if (!forceSkinAlpha)
		{
			Texture mainText = (Texture2D)material.Get("shader_parameter/Tex_0");
			float luminance = .25f;
			if (mainText != null)
				if (mainText.HasMeta("luminance"))
					luminance = (float)mainText.GetMeta("luminance");
			mesh.SetInstanceShaderParameter(MaterialManager.shadowProperty, GameManager.Instance.shadowIntensity * luminance);
		}

		holder.AddChild(mesh);
		if (addPVS)
			mesh.Layers = GameManager.InvisibleMask;
		else //As dynamic surface don't have bsp data, assign it to the always visible layer 
			mesh.Layers = GameManager.AllPlayerViewMask;
		mesh.Name = Name;
		mesh.Mesh = arrMesh;

//		if (MaterialManager.IsSkyTexture(textureName))
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
//		else
//			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;

		//PVS only add on Static Geometry, as it has BSP Nodes
		if (addPVS)
			ClusterPVSManager.Instance.RegisterClusterAndSurfaces(mesh, surfaces);
	}
	public static void GenerateBezMesh(uint OwnerShapeId, CollisionObject3D collider, QSurface surface, int patchNumber, bool draw, ref int offset)
	{
		//Calculate how many patches there are using size[]
		//There are n_patchesX by n_patchesY patches in the grid, each of those
		//starts at a vert (i,j) in the overall grid
		//We don't actually need to know how many are on the Y length
		//but the forumla is here for historical/academic purposes
		int n_patchesX = (surface.size[0] - 1) / 2;
		//int n_patchesY = ((surface.size[1]) - 1) / 2;


		//Calculate what [n,m] patch we want by using an index
		//called patchNumber  Think of patchNumber as if you 
		//numbered the patches left to right, top to bottom on
		//the grid in a piece of paper.
		int pxStep = 0;
		int pyStep = 0;
		for (int i = 0; i < patchNumber; i++)
		{
			pxStep++;
			if (pxStep == n_patchesX)
			{
				pxStep = 0;
				pyStep++;
			}
		}

		//Create an array the size of the grid, which is given by
		//size[] on the surface object.
		QVertex[,] vertGrid = new QVertex[surface.size[0], surface.size[1]];

		//Read the verts for this surface into the grid, making sure
		//that the final shape of the grid matches the size[] of
		//the surface.
		int gridXstep = 0;
		int gridYstep = 0;
		int vertStep = surface.startVertIndex;
		for (int i = 0; i < surface.numOfVerts; i++)
		{
			vertGrid[gridXstep, gridYstep] = MapLoader.verts[vertStep];
			vertStep++;
			gridXstep++;
			if (gridXstep == surface.size[0])
			{
				gridXstep = 0;
				gridYstep++;
			}
		}

		//We now need to pluck out exactly nine vertexes to pass to our
		//teselate function, so lets calculate the starting vertex of the
		//3x3 grid of nine vertexes that will make up our patch.
		//we already know how many patches are in the grid, which we have
		//as n and m.  There are n by m patches.  Since this method will
		//create one object at a time, we only need to be able to grab
		//one.  The starting vertex will be called vi,vj think of vi,vj as x,y
		//coords into the grid.
		int vi = 2 * pxStep;
		int vj = 2 * pyStep;
		//Now that we have those, we need to get the vert at [vi,vj] and then
		//the two verts at [vi+1,vj] and [vi+2,vj], and then [vi,vj+1], etc.
		//the ending vert will at [vi+2,vj+2]

		int capacity = 3 * 3;
		List<Vector3> bverts = new List<Vector3>(capacity);

		//read texture/lightmap coords while we're at it
		//they will be tessellated as well.
		List<Vector2> uv = new List<Vector2>(capacity);
		List<Vector2> uv2 = new List<Vector2>(capacity);
		List<Color> color = new List<Color>(capacity);

		//Top row
		bverts.Add(vertGrid[vi, vj].position);
		bverts.Add(vertGrid[vi + 1, vj].position);
		bverts.Add(vertGrid[vi + 2, vj].position);

		uv.Add(vertGrid[vi, vj].textureCoord);
		uv.Add(vertGrid[vi + 1, vj].textureCoord);
		uv.Add(vertGrid[vi + 2, vj].textureCoord);

		uv2.Add(vertGrid[vi, vj].lightmapCoord);
		uv2.Add(vertGrid[vi + 1, vj].lightmapCoord);
		uv2.Add(vertGrid[vi + 2, vj].lightmapCoord);

		color.Add(vertGrid[vi, vj].color);
		color.Add(vertGrid[vi + 1, vj].color);
		color.Add(vertGrid[vi + 2, vj].color);

		//Middle row
		bverts.Add(vertGrid[vi, vj + 1].position);
		bverts.Add(vertGrid[vi + 1, vj + 1].position);
		bverts.Add(vertGrid[vi + 2, vj + 1].position);

		uv.Add(vertGrid[vi, vj + 1].textureCoord);
		uv.Add(vertGrid[vi + 1, vj + 1].textureCoord);
		uv.Add(vertGrid[vi + 2, vj + 1].textureCoord);

		uv2.Add(vertGrid[vi, vj + 1].lightmapCoord);
		uv2.Add(vertGrid[vi + 1, vj + 1].lightmapCoord);
		uv2.Add(vertGrid[vi + 2, vj + 1].lightmapCoord);

		color.Add(vertGrid[vi, vj + 1].color);
		color.Add(vertGrid[vi + 1, vj + 1].color);
		color.Add(vertGrid[vi + 2, vj + 1].color);

		//Bottom row
		bverts.Add(vertGrid[vi, vj + 2].position);
		bverts.Add(vertGrid[vi + 1, vj + 2].position);
		bverts.Add(vertGrid[vi + 2, vj + 2].position);

		uv.Add(vertGrid[vi, vj + 2].textureCoord);
		uv.Add(vertGrid[vi + 1, vj + 2].textureCoord);
		uv.Add(vertGrid[vi + 2, vj + 2].textureCoord);

		uv2.Add(vertGrid[vi, vj + 2].lightmapCoord);
		uv2.Add(vertGrid[vi + 1, vj + 2].lightmapCoord);
		uv2.Add(vertGrid[vi + 2, vj + 2].lightmapCoord);

		color.Add(vertGrid[vi, vj + 2].color);
		color.Add(vertGrid[vi + 1, vj + 2].color);
		color.Add(vertGrid[vi + 2, vj + 2].color);

		if (draw)
			BezierMesh.GenerateBezierMesh(GameManager.Instance.tessellations, bverts, uv, uv2, color, ref offset);
		if (collider != null)
			BezierMesh.BezierColliderMesh(OwnerShapeId, collider, surface.surfaceId, patchNumber, bverts);

		return;
	}
	public static void GeneratePolygonObject(string textureName, int lmIndex, Node3D holder, QSurface[] surfaces, bool addPVS = true)
	{
		if (surfaces == null || surfaces.Length == 0)
		{
			GameManager.Print("GeneratePolygonObject: Failed to create polygon there are no surfaces", GameManager.PrintType.Warning);
			return;
		}

		if (MaterialManager.IsFogMaterial(textureName))
		{
			GameManager.Print("GeneratePolygonObject: Object is FOG " + textureName + " so no Meshes are going to be made");
			return;
		}


		bool hasPortal = false;
		bool forceSkinAlpha = false;
		ShaderMaterial material = MaterialManager.GetMaterials(textureName, lmIndex, ref forceSkinAlpha, ref hasPortal);

//		Don't show illegal (sad faced) material, just remove mesh/shader
		if (material == MaterialManager.Instance.illegal)
			return;

		MeshInstance3D mesh = new MeshInstance3D();
		ArrayMesh arrMesh = new ArrayMesh();
		string Name = "Mesh_Surfaces";
		int offset = 0;
		for (var i = 0; i < surfaces.Length; i++)
		{
			GeneratePolygonMesh(surfaces[i], lmIndex, ref offset);
			Name += "_" + surfaces[i].surfaceId;
		}
		FinalizePolygonMesh(arrMesh);
		if (!hasPortal)
			arrMesh.SurfaceSetMaterial(0, material);
		holder.AddChild(mesh);
		if (addPVS)
			mesh.Layers = GameManager.InvisibleMask;
		else //As dynamic surface don't have bsp data, assign it to the always visible layer 
			mesh.Layers = GameManager.AllPlayerViewMask;
		mesh.Name = Name;
		mesh.Mesh = arrMesh;

//		if (MaterialManager.IsSkyTexture(textureName))
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
//		else
//			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;

		if (!forceSkinAlpha && !hasPortal)
		{
			Texture mainText = (Texture2D)material.Get("shader_parameter/Tex_0");
			float luminance = .25f;
			if (mainText != null)
				if (mainText.HasMeta("luminance"))
					luminance = (float)mainText.GetMeta("luminance");
			mesh.SetInstanceShaderParameter(MaterialManager.shadowProperty, GameManager.Instance.shadowIntensity * luminance);
		}

		if (hasPortal)
		{
			Portal portal = new Portal(textureName, material);
			Aabb box = arrMesh.GetAabb();
			portal.position = box.GetCenter();
			Vector3 normals = Vector3.Zero;
			for (var i = 0; i < normalsCache.Count; i++)
				normals += normalsCache[i];

			mesh.Layers = (1 << GameManager.Player1ViewLayer);
			mesh.SetSurfaceOverrideMaterial(0, material);
			portal.normal = normals.Normalized();
			portal.commonMesh = arrMesh;
			portal.surfaces.Add(new Portal.Surface(mesh, material));
			ThingsManager.AddPortalToMap(portal);
		}

		//PVS only add on Static Geometry, as it has BSP Nodes
		else if (addPVS)
			ClusterPVSManager.Instance.RegisterClusterAndSurfaces(mesh, surfaces);
	}
	public static void GeneratePolygonMesh(QSurface surface, int lm_index, ref int offset)
	{
		if (offset == 0)
		{
			vertsCache.Clear();
			uvCache.Clear();
			uv2Cache.Clear();
			normalsCache.Clear();
			indiciesCache.Clear();
			vertsColor.Clear();
		}

		int vstep = surface.startVertIndex;
		for (int n = 0; n < surface.numOfVerts; n++)
		{
			vertsCache.Add(MapLoader.verts[vstep].position);
			uvCache.Add(MapLoader.verts[vstep].textureCoord);
			uv2Cache.Add(MapLoader.verts[vstep].lightmapCoord);
			normalsCache.Add(MapLoader.verts[vstep].normal);

			//Need to compensate for Color lightning as lightmapped textures will change
			if (lm_index >= 0)
				vertsColor.Add(MapLoader.verts[vstep].color);
			else
				vertsColor.Add(TextureLoader.ChangeColorLighting(MapLoader.verts[vstep].color));
			vstep++;
		}

		// Rip meshverts / triangles
		int mstep = surface.startIndex;
		for (int n = 0; n < surface.numOfIndices; n++)
		{
			indiciesCache.Add(MapLoader.vertIndices[mstep] + offset);
			mstep++;
		}

		offset += surface.numOfVerts;
	}
	public static void FinalizePolygonMesh(ArrayMesh arrMesh)
	{
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		// add the verts, uvs, and normals we ripped to the surfaceArray
		surfaceArray[VertexInd] = vertsCache.ToArray();
		surfaceArray[NormalInd] = normalsCache.ToArray();

		// Add the texture co-ords (or UVs) to the surface/mesh
		surfaceArray[TexUVInd] = uvCache.ToArray();
		surfaceArray[TexUV2Ind] = uv2Cache.ToArray();

		// Add the vertex color
		surfaceArray[ColorInd] = vertsColor.ToArray();

		// add the meshverts to the object being built
		surfaceArray[TriIndex] = indiciesCache.ToArray();

		// Create the Mesh.
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
	}

	public static (Vector3 , Quaternion) GenerateBillBoardMesh(List<Vector3I> Quad, QSurface surface, int lm_index)
	{
		Vector3 center = Vector3.Zero;
		Vector3 normal = Vector3.Zero;
		Quaternion changeRotation;

		vertsCache.Clear();
		uvCache.Clear();
		uv2Cache.Clear();
		normalsCache.Clear();
		indiciesCache.Clear();
		vertsColor.Clear();

		List<int> localIndex = new List<int>();
		List<int> isRoot = new List<int>();
		int vstep = surface.startVertIndex;

		for (int i = 0; i < Quad.Count; i++)
		{
			int index = Quad[i].X;
			if (!localIndex.Contains(index))
			{
				localIndex.Add(index);
				isRoot.Add(index);
			}
			else
				isRoot.Remove(index);

			index = Quad[i].Y;
			if (!localIndex.Contains(index))
			{
				localIndex.Add(index);
				isRoot.Add(index);
			}
			else
				isRoot.Remove(index);

			index = Quad[i].Z;
			if (!localIndex.Contains(index))
			{
				localIndex.Add(index);
				isRoot.Add(index);
			}
			else
				isRoot.Remove(index);

		}

		localIndex.Sort();

		for (int n = 0; n < localIndex.Count; n++)
		{
			Vector3 pos = MapLoader.verts[vstep + localIndex[n]].position;
			vertsCache.Add(pos);
			center += pos;
		}

		center /= vertsCache.Count;
		CanForm3DConvexHull(vertsCache, ref normal, 0.00001f, false);

		if (Mathf.IsZeroApprox(normal.Dot(Vector3.Up)))
			changeRotation = Transform3D.Identity.LookingAt(normal, Vector3.Up).Basis.GetRotationQuaternion();
		else
			changeRotation = Transform3D.Identity.LookingAt(normal, Vector3.Forward).Basis.GetRotationQuaternion();

		float zRotationAngle = 0;
		float largestEdge = float.MinValue;
		//Need to make sure we don't take diagonals
		for (int i = 0; i < isRoot.Count; i++)
		{
			vertsCache.Clear();
			int indexA, indexB, indexC;
			indexA = isRoot[i];
			if (indexA == Quad[i].X)
			{
				indexB = Quad[i].Y;
				indexC = Quad[i].Z;
			}
			else if (indexA == Quad[i].Y)
			{
				indexB = Quad[i].X;
				indexC = Quad[i].Z;
			}
			else
			{
				indexB = Quad[i].X;
				indexC = Quad[i].Y;
			}
			Vector3 pos = changeRotation * (MapLoader.verts[vstep + indexA].position - center);
			pos.Z = 0;
			vertsCache.Add(pos);

			pos = changeRotation * (MapLoader.verts[vstep + indexB].position - center);
			pos.Z = 0;
			vertsCache.Add(pos);

			pos = changeRotation * (MapLoader.verts[vstep + indexC].position - center);
			pos.Z = 0;
			vertsCache.Add(pos);

			for (int j = 1; j < vertsCache.Count; j++)
			{
				Vector3 edge = vertsCache[j] - vertsCache[0];
				float edgeLenght = edge.LengthSquared();

				if (edgeLenght > largestEdge)
				{
					largestEdge = edgeLenght;
					zRotationAngle = (float)Math.Atan2(edge.X, edge.Y);
				}
			}
		}
		Quaternion quat = Quaternion.FromEuler(new Vector3(0, 0, zRotationAngle));
		changeRotation = quat * changeRotation;

		vertsCache.Clear();
		for (int n = 0; n < localIndex.Count; n++)
		{
			Vector3 pos = changeRotation * (MapLoader.verts[vstep + localIndex[n]].position - center);
			pos.Z = 0;
			vertsCache.Add(pos);
			uvCache.Add(MapLoader.verts[vstep + localIndex[n]].textureCoord);
			uv2Cache.Add(MapLoader.verts[vstep + localIndex[n]].lightmapCoord);
//			Because it's Z aligned, the normal will always be Back
//			normalsCache.Add(changeRotation * MapLoader.verts[vstep + localIndex[n]].normal);
			normalsCache.Add(Vector3.Back);

			//Need to compensate for Color lightning as lightmapped textures will change
			if (lm_index >= 0)
				vertsColor.Add(MapLoader.verts[vstep + localIndex[n]].color);
			else
				vertsColor.Add(TextureLoader.ChangeColorLighting(MapLoader.verts[vstep + localIndex[n]].color));
		}

		// Rip meshverts / triangles
		for (int i = 0; i < Quad.Count; i++)
		{
			indiciesCache.Add(localIndex.IndexOf(Quad[i].X));
			indiciesCache.Add(localIndex.IndexOf(Quad[i].Y));
			indiciesCache.Add(localIndex.IndexOf(Quad[i].Z));
		}

		return (center, changeRotation.Inverse());
	}
	public static void GenerateBillBoardObject(string textureName, int lmIndex, Node3D holder, QSurface surface, bool addPVS = true)
	{
		if (surface == null)
		{
			GameManager.Print("Failed to create polygon object because there are no surfaces", GameManager.PrintType.Warning);
			return;
		}

		ShaderMaterial material = MaterialManager.GetMaterials(textureName, lmIndex);

		//Get All Triangles in a List
		List<Vector3I> Tris = new List<Vector3I>();
		Vector3I triangle = Vector3I.Zero;
		for (int i = 0, vertex = 0; i < surface.numOfIndices; i++, vertex++)
		{
			int index = MapLoader.vertIndices[surface.startIndex + i];
			if (vertex > 2)
				vertex = 0;
			switch (vertex)
			{
				default:
				case 0:
					triangle = new Vector3I(index, 0, 0);
				break;
				case 1:
					triangle.Y = index;
				break;
				case 2:
					triangle.Z = index;
					Tris.Add(triangle);
				break;
			}
		}

		//Now Find the Quads
		List<List<Vector3I>> Quads = new List<List<Vector3I>>();
		for (int i = 1, j = 0; j < Tris.Count; i++)
		{
			if (i == Tris.Count)
			{
				j++;
				i = j + 1;
				if (j == (Tris.Count - 1))
					break;
			}

			if (i == j)
				continue;
			if ((Tris[j].X == Tris[i].X) || (Tris[j].X == Tris[i].Y) || (Tris[j].X == Tris[i].Z))
			{
				if (((Tris[j].Y == Tris[i].X) || (Tris[j].Y == Tris[i].Y) || (Tris[j].Y == Tris[i].Z))
					|| ((Tris[j].Z == Tris[i].X) || (Tris[j].Z == Tris[i].Y) || (Tris[j].Z == Tris[i].Z)))
				{
					List<Vector3I> Quad = new List<Vector3I>
					{
						Tris[j],
						Tris[i]
					};
					Quads.Add(Quad);
				}
			}
			else if ((Tris[j].Y == Tris[i].X) || (Tris[j].Y == Tris[i].Y) || (Tris[j].Y == Tris[i].Z))
			{
				if ((Tris[j].Z == Tris[i].X) || (Tris[j].Z == Tris[i].Y) || (Tris[j].Z == Tris[i].Z))
				{
					List<Vector3I> Quad = new List<Vector3I>
					{
						Tris[j],
						Tris[i]
					};
					Quads.Add(Quad);
				}
			}
		}

		addPVS = false;
		for (int i = 0; i < Quads.Count; i++)
		{
			MeshInstance3D mesh = new MeshInstance3D();
			ArrayMesh arrMesh = new ArrayMesh();
			string Name = "BillBoard_Surfaces_" + surface.surfaceId;
			Vector3 center;
			Quaternion rotation;
			(center, rotation) = GenerateBillBoardMesh(Quads[i], surface, lmIndex);

			Node3D billBoard = new Node3D();
			holder.AddChild(billBoard);
			billBoard.GlobalPosition = center;
			billBoard.Quaternion = rotation;
			FinalizePolygonMesh(arrMesh);
			arrMesh.SurfaceSetMaterial(0, material);
			billBoard.AddChild(mesh);
			if (addPVS)
				mesh.Layers = GameManager.InvisibleMask;
			else //As dynamic surface don't have bsp data, assign it to the always visible layer 
				mesh.Layers = GameManager.AllPlayerViewMask;
			mesh.Name = Name;
			mesh.Mesh = arrMesh;
//			if (MaterialManager.IsSkyTexture(textureName))
				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
//			else
//				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;
			//PVS only add on Static Geometry, as it has BSP Nodes
			if (addPVS)
				ClusterPVSManager.Instance.RegisterClusterAndSurface(mesh, surface);
		}
	}

	public static void GenerateBillBoardSprites(string textureName, int lmIndex, Node3D holder, QSurface[] surfaces, bool addPVS = true)
	{
		if (surfaces == null || surfaces.Length == 0)
		{
			GameManager.Print("Failed to create polygon object because there are no surfaces", GameManager.PrintType.Warning);
			return;
		}

		for (var i = 0; i < surfaces.Length; i++)
		{
			SpriteController sprite = new SpriteController();
			string Name = "BillBoard_Surfaces_" + surfaces[i].surfaceId;
			holder.AddChild(sprite);
			sprite.spriteName = textureName;
			sprite.billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			sprite.Name = Name;
			sprite.spriteRadius = 1;
			sprite.Position = surfaces[i].lm_Origin;
			sprite.spriteData = new SpriteData();
			sprite.spriteData.Modulate = new Color(surfaces[i].lm_vecs[0].X, surfaces[i].lm_vecs[0].Y, surfaces[i].lm_vecs[0].Z, 1f);
			sprite.Init();
		}
	}

	public static MeshProcessed GenerateModelFromMeshes(MD3 model, Dictionary<string, string> meshToSkin, uint layer = GameManager.AllPlayerViewMask, bool useCommon = false, int frame = 0)
	{
		return GenerateModelFromMeshes(model, layer, true, true, null, false, useCommon, meshToSkin, true, false, false, true, frame);
	}
	public static MeshProcessed GenerateModelFromMeshes(MD3 model, uint layer, bool receiveShadows, bool castShadows, Node3D ownerObject = null, bool forceSkinAlpha = false, bool useCommon = true, Dictionary<string, string> meshToSkin = null, bool useLowMultimeshes = true, bool useColorData = false, bool isViewModel = false, bool useLightVol = true, int frame = 0)
	{
		if (model == null || model.meshes.Count == 0)
		{
			GameManager.Print("Failed to create model object because there are no meshes", GameManager.PrintType.Warning);
			return null;
		}

		if (model.frameSurfaces.ContainsKey(frame))
			return FillModelFromProcessedData(model, layer, receiveShadows, castShadows, ownerObject, useCommon, meshToSkin, forceSkinAlpha, useLowMultimeshes, useColorData, isViewModel, useLightVol, frame);

		if (ownerObject == null)
		{
			GameManager.Print("No ownerObject");
			ownerObject = new Node3D();
			ownerObject.Name = "Model_" + model.name;
		}

		FrameSurfaces frameSurfaces = new FrameSurfaces();
		MeshProcessed md3Model = new MeshProcessed();
		md3Model.node = ownerObject;
		md3Model.numMeshes = model.meshes.Count;
		md3Model.data = new MeshProcessed.dataMeshes[md3Model.numMeshes];

		int n = 0;
		if ((model.numFrames > 1) || (meshToSkin != null))
		{
			for(n = 0; n < model.meshes.Count; n++)
			{
				MD3Mesh modelMesh = model.meshes[n];
				MeshProcessed.dataMeshes data = new MeshProcessed.dataMeshes();
				var surfaceArray = GenerateModelMesh(modelMesh, frame);
				data.arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

				Node3D modelObject;

				if (n == 0)
					modelObject = ownerObject;
				else
				{
					modelObject = new Node3D();
					modelObject.Name = "Mesh_" + n;
					ownerObject.AddChild(modelObject);
					modelObject.Position = Vector3.Zero;
				}

				string skinName;
				if (meshToSkin == null)
					skinName = modelMesh.skins[0].name;
				else
					skinName = meshToSkin[modelMesh.name];
				bool currentTransparent = forceSkinAlpha;
				ShaderMaterial material = MaterialManager.GetMaterials(skinName, -1, ref currentTransparent);
				data.isTransparent = currentTransparent;
				data.arrMesh.SurfaceSetMaterial(0, material);
				data.meshDataTool.CreateFromSurface(data.arrMesh, 0);
				md3Model.data[modelMesh.meshNum] = data;
				frameSurfaces.readySurfaceArray.Add(surfaceArray);
				MultiMesh multiMesh = new MultiMesh();
				data.multiMesh = multiMesh;
				multiMesh.Mesh = data.arrMesh;
				multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
				if (useColorData)
					multiMesh.UseColors = true;
				if (useLowMultimeshes)
					multiMesh.InstanceCount = LOW_USE_MULTIMESHES;
				else
					multiMesh.InstanceCount = HIGH_USE_MULTIMESHES;
				multiMesh.VisibleInstanceCount = 0;

				SurfaceData surfaceData = new SurfaceData();
				surfaceData.skinName = skinName;
				surfaceData.commonMesh = multiMesh;
				surfaceData.useTransparent = currentTransparent;
				surfaceData.readyMaterials = material;
				frameSurfaces.surfaceIdbySkinName.Add(skinName + "_" + n, frameSurfaces.readySurfaces.Count());
				frameSurfaces.readySurfaces.Add(surfaceData);

				if (!MultiMeshes.ContainsKey(multiMesh))
				{
					Dictionary<Node3D, int> Set = new Dictionary<Node3D, int>();
					MultiMeshes.Add(multiMesh, Set);
				}

				if (useCommon && !currentTransparent)
				{
					MultiMeshInstance3D mesh = new MultiMeshInstance3D();
					mesh.Name = "MultiMesh_" + model.name;
					mesh.Multimesh = multiMesh;
					mesh.Layers = layer;
					if (!castShadows)
						mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
					mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
					if (useLightVol)
						mesh.SetInstanceShaderParameter("UseLightVol", true);
					if (!currentTransparent && receiveShadows)
					{
						Texture mainText = (Texture2D)material.Get("shader_parameter/Tex_0");
						float luminance = .25f;
						if (mainText != null)
							if (mainText.HasMeta("luminance"))
								luminance = (float)mainText.GetMeta("luminance");
						mesh.SetInstanceShaderParameter(MaterialManager.shadowProperty, GameManager.Instance.shadowIntensity * luminance);
					}

					GameManager.Instance.TemporaryObjectsHolder.AddChild(mesh);
					MultiMeshesInstances.Add(multiMesh, mesh);
					GameManager.Print("Adding MultiMesh : " + mesh.Name);
				}
				else
				{
					MeshInstance3D mesh = new MeshInstance3D();
					mesh.Name = modelMesh.name;
					mesh.Mesh = data.arrMesh;
					mesh.Layers = layer;
					if (!castShadows)
						mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
					mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
					if (isViewModel)
						mesh.SetInstanceShaderParameter("ViewModel", true);
					if (useLightVol)
						mesh.SetInstanceShaderParameter("UseLightVol", true);
					if (!currentTransparent && receiveShadows)
					{
						Texture mainText = (Texture2D)material.Get("shader_parameter/Tex_0");
						float luminance = .25f;
						if (mainText != null)
							if (mainText.HasMeta("luminance"))
								luminance = (float)mainText.GetMeta("luminance");
						mesh.SetInstanceShaderParameter(MaterialManager.shadowProperty, GameManager.Instance.shadowIntensity * luminance);
					}

					modelObject.AddChild(mesh);
					GameManager.Print("Adding Child: " + mesh.Name + " to: " + modelObject.Name);
				}
			}
		}
		else
		{
			var baseGroups = model.meshes.GroupBy(x => new { x.numSkins });
			foreach (var baseGroup in baseGroups)
			{
				MD3Mesh[] baseGroupMeshes = baseGroup.ToArray();
				if (baseGroupMeshes.Length == 0)
					continue;

				var groupMeshes = baseGroupMeshes.GroupBy(x => new { x.skins[0].name });
				foreach (var groupMesh in groupMeshes)
				{
					MD3Mesh[] meshes = groupMesh.ToArray();
					if (meshes.Length == 0)
						continue;

					string Name = "Mesh_";
					int offset = 0;
					for (int i = 0; i < meshes.Length; i++)
					{
						GenerateModelMesh(meshes[i], frame, ref offset);
						if (i != 0)
							Name += "_";
						Name += meshes[i].name;
					}

					MeshProcessed.dataMeshes data = new MeshProcessed.dataMeshes();
					var surfaceArray = FinalizeModelMesh();
					data.arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

					Node3D modelObject;
					if (n == 0)
						modelObject = ownerObject;
					else
					{
						modelObject = new Node3D();
						modelObject.Name = "Mesh_" + n;
						ownerObject.AddChild(modelObject);
						modelObject.Position = Vector3.Zero;
					}

					string skinName = meshes[0].skins[0].name;
					bool currentTransparent = forceSkinAlpha;
					ShaderMaterial material = MaterialManager.GetMaterials(skinName, -1, ref currentTransparent);

					for (int i = 0; i < meshes.Length; i++)
						md3Model.data[meshes[i].meshNum] = data;

					data.isTransparent = currentTransparent;
					data.arrMesh.SurfaceSetMaterial(0, material);
					data.meshDataTool.CreateFromSurface(data.arrMesh, 0);
					frameSurfaces.readySurfaceArray.Add(surfaceArray);
					MultiMesh multiMesh = new MultiMesh();
					data.multiMesh = multiMesh;
					multiMesh.Mesh = data.arrMesh;
					multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
					if (useColorData)
						multiMesh.UseColors = true;
					if (useLowMultimeshes)
						multiMesh.InstanceCount = LOW_USE_MULTIMESHES;
					else
						multiMesh.InstanceCount = HIGH_USE_MULTIMESHES;
					multiMesh.VisibleInstanceCount = 0;

					SurfaceData surfaceData = new SurfaceData();
					surfaceData.skinName = skinName;
					surfaceData.commonMesh = multiMesh;
					surfaceData.useTransparent = currentTransparent;
					surfaceData.readyMaterials = material;
					frameSurfaces.surfaceIdbySkinName.Add(skinName + "_" + n, frameSurfaces.readySurfaces.Count());
					frameSurfaces.readySurfaces.Add(surfaceData);

					if (!MultiMeshes.ContainsKey(multiMesh))
					{
						Dictionary<Node3D, int> Set = new Dictionary<Node3D, int>();
						MultiMeshes.Add(multiMesh, Set);
					}

					if (useCommon && !currentTransparent)
					{
						MultiMeshInstance3D mesh = new MultiMeshInstance3D();
						mesh.Name = "MultiMesh_" + model.name;
						mesh.Multimesh = multiMesh;
						mesh.Layers = layer;
						if (!castShadows)
							mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
						mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
						if (useLightVol)
							mesh.SetInstanceShaderParameter("UseLightVol", true);
						if (receiveShadows)
						{
							Texture mainText = (Texture2D)material.Get("shader_parameter/Tex_0");
							float luminance = .25f;
							if (mainText != null)
								if (mainText.HasMeta("luminance"))
									luminance = (float)mainText.GetMeta("luminance");
							mesh.SetInstanceShaderParameter(MaterialManager.shadowProperty, GameManager.Instance.shadowIntensity * luminance);
						}

						GameManager.Instance.TemporaryObjectsHolder.AddChild(mesh);
						MultiMeshesInstances.Add(multiMesh, mesh);
						GameManager.Print("Adding MultiMesh : " + mesh.Name + " skin group name " + meshes[0].skins[0].name);
					}
					else
					{
						MeshInstance3D mesh = new MeshInstance3D();
						mesh.Name = Name;
						mesh.Mesh = data.arrMesh;
						mesh.Layers = layer;
						if (!castShadows)
							mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
						mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
						if (isViewModel)
							mesh.SetInstanceShaderParameter("ViewModel", true);
						if (useLightVol)
							mesh.SetInstanceShaderParameter("UseLightVol", true);
						if (receiveShadows)
						{
							Texture mainText = (Texture2D)material.Get("shader_parameter/Tex_0");
							float luminance = .25f;
							if (mainText != null)
								if (mainText.HasMeta("luminance"))
									luminance = (float)mainText.GetMeta("luminance");
							mesh.SetInstanceShaderParameter(MaterialManager.shadowProperty, GameManager.Instance.shadowIntensity * luminance);
						}

						modelObject.AddChild(mesh);
						GameManager.Print("Adding Child: " + mesh.Name + " to: " + modelObject.Name + " skin group name " + meshes[0].skins[0].name);
					}
					n++;
				}
			}
		}
		model.frameSurfaces.Add(frame, frameSurfaces);
		return md3Model;
	}
	
	public static MeshProcessed FillModelFromProcessedData(MD3 model, uint layer, bool receiveShadows, bool castShadows, Node3D ownerObject = null, bool useCommon = true, Dictionary<string, string> meshToSkin = null, bool forceSkinAlpha = false, bool useLowMultimeshes = true, bool useColorData = false, bool isViewModel = false, bool useLightVol = true, int frame = 0)
	{
		if (ownerObject == null)
		{
			ownerObject = new Node3D();
			ownerObject.Name = "Model_" + model.name;
		}

		MeshProcessed meshProcessed = new MeshProcessed();
		meshProcessed.node = ownerObject;
		meshProcessed.numMeshes = model.meshes.Count;
		meshProcessed.data = new MeshProcessed.dataMeshes[meshProcessed.numMeshes];

		FrameSurfaces frameSurfaces = model.frameSurfaces[frame];
		for (int i = 0; i < frameSurfaces.readySurfaceArray.Count; i++)
		{
			Node3D modelObject;
			if (i == 0)
				modelObject = ownerObject;
			else
			{
				modelObject = new Node3D();
				modelObject.Name = "Mesh_" + i;
				ownerObject.AddChild(modelObject);
				modelObject.Position = Vector3.Zero;
			}

			meshProcessed.data[model.meshes[i].meshNum] = new MeshProcessed.dataMeshes();
			SurfaceData surfaceData = null;
			int skinIndex = -1;
			string skinName;
			if (meshToSkin == null)
			{
				surfaceData = frameSurfaces.readySurfaces[i];
				skinName = surfaceData.skinName;
			}
			else
				skinName = meshToSkin[model.meshes[i].name];

			
			if (frameSurfaces.surfaceIdbySkinName.TryGetValue(skinName + "_" + i, out skinIndex))
			{
				if (surfaceData == null)
					surfaceData = frameSurfaces.readySurfaces[skinIndex];

				bool useTransparent = surfaceData.useTransparent;
				meshProcessed.data[model.meshes[i].meshNum].isTransparent = useTransparent;
				if (useCommon && !useTransparent)
				{
					meshProcessed.data[model.meshes[i].meshNum].multiMesh = surfaceData.commonMesh;
					if (!MultiMeshes.ContainsKey(surfaceData.commonMesh))
					{
						Dictionary<Node3D, int> Set = new Dictionary<Node3D, int>();
						MultiMeshes.Add(surfaceData.commonMesh, Set);
					}

					if (!MultiMeshesInstances.ContainsKey(surfaceData.commonMesh))
					{
						MultiMeshInstance3D mesh = new MultiMeshInstance3D();
						mesh.Name = "MultiMesh_" + model.name;
						mesh.Multimesh = surfaceData.commonMesh;
						mesh.Layers = layer;
						if (!castShadows)
							mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
						GameManager.Instance.TemporaryObjectsHolder.AddChild(mesh);
						MultiMeshesInstances.Add(surfaceData.commonMesh, mesh);
						mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
						if (useLightVol)
							mesh.SetInstanceShaderParameter("UseLightVol", true);
						if (receiveShadows)
						{
							Texture mainText = (Texture2D)surfaceData.readyMaterials.Get("shader_parameter/Tex_0");
							float luminance = .25f;
							if (mainText != null)
								if (mainText.HasMeta("luminance"))
									luminance = (float)mainText.GetMeta("luminance");
							mesh.SetInstanceShaderParameter(MaterialManager.shadowProperty, GameManager.Instance.shadowIntensity * luminance);
						}
					}
				}
				else
				{
					MeshInstance3D mesh = new MeshInstance3D();
					var surfaceArray = frameSurfaces.readySurfaceArray[i];
					meshProcessed.data[model.meshes[i].meshNum].arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
					meshProcessed.data[model.meshes[i].meshNum].arrMesh.SurfaceSetMaterial(0, surfaceData.readyMaterials);
					meshProcessed.data[model.meshes[i].meshNum].meshDataTool.CreateFromSurface(meshProcessed.data[model.meshes[i].meshNum].arrMesh, 0);
					mesh.Name = "Mesh_" + model.name;
					mesh.Mesh = meshProcessed.data[model.meshes[i].meshNum].arrMesh;
					mesh.Layers = layer;
					if (!castShadows)
						mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
					modelObject.AddChild(mesh);
					mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
					if (isViewModel)
						mesh.SetInstanceShaderParameter("ViewModel", true);
					if (useLightVol)
						mesh.SetInstanceShaderParameter("UseLightVol", true);
					if (receiveShadows)
					{
						Texture mainText = (Texture2D)surfaceData.readyMaterials.Get("shader_parameter/Tex_0");
						float luminance = .25f;
						if (mainText != null)
							if (mainText.HasMeta("luminance"))
								luminance = (float)mainText.GetMeta("luminance");
						mesh.SetInstanceShaderParameter(MaterialManager.shadowProperty, GameManager.Instance.shadowIntensity * luminance);
					}
				}
			}
			else
			{
				GameManager.Print("NO SKIN FOUND" + skinName);
				surfaceData = new SurfaceData();
				surfaceData.skinName = skinName;

				bool useTransparent = forceSkinAlpha;
				var surfaceArray = frameSurfaces.readySurfaceArray[i];

				ShaderMaterial material = MaterialManager.GetMaterials(skinName, -1, ref useTransparent);
				ArrayMesh arrMesh = new ArrayMesh();
				arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
				arrMesh.SurfaceSetMaterial(0, material);
				MultiMesh multiMesh = new MultiMesh();
				multiMesh.Mesh = arrMesh;
				multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
				if (useColorData)
					multiMesh.UseColors = true;
				if (useLowMultimeshes)
					multiMesh.InstanceCount = LOW_USE_MULTIMESHES;
				else
					multiMesh.InstanceCount = HIGH_USE_MULTIMESHES;
				multiMesh.VisibleInstanceCount = 0;

				surfaceData.commonMesh = multiMesh;
				surfaceData.useTransparent = useTransparent;
				surfaceData.readyMaterials = material;
				frameSurfaces.surfaceIdbySkinName.Add(skinName + "_" + i, frameSurfaces.readySurfaces.Count());
				frameSurfaces.readySurfaces.Add(surfaceData);
				if (!MultiMeshes.ContainsKey(multiMesh))
				{
					Dictionary<Node3D, int> Set = new Dictionary<Node3D, int>();
					MultiMeshes.Add(multiMesh, Set);
				}

				meshProcessed.data[model.meshes[i].meshNum].isTransparent = useTransparent;
				if (useCommon && !useTransparent)
				{
					meshProcessed.data[model.meshes[i].meshNum].multiMesh = surfaceData.commonMesh;
					if (!MultiMeshesInstances.ContainsKey(surfaceData.commonMesh))
					{
						MultiMeshInstance3D mesh = new MultiMeshInstance3D();
						mesh.Name = "MultiMesh_" + model.name;
						mesh.Multimesh = surfaceData.commonMesh;
						mesh.Layers = layer;
						if (!castShadows)
							mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
						GameManager.Instance.TemporaryObjectsHolder.AddChild(mesh);
						MultiMeshesInstances.Add(surfaceData.commonMesh, mesh);
						mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
						if (useLightVol)
							mesh.SetInstanceShaderParameter("UseLightVol", true);
					}
				}
				else
				{
					MeshInstance3D mesh = new MeshInstance3D();
					meshProcessed.data[model.meshes[i].meshNum].arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
					meshProcessed.data[model.meshes[i].meshNum].arrMesh.SurfaceSetMaterial(0, surfaceData.readyMaterials);
					meshProcessed.data[model.meshes[i].meshNum].meshDataTool.CreateFromSurface(meshProcessed.data[model.meshes[i].meshNum].arrMesh, 0);
					mesh.Mesh = meshProcessed.data[model.meshes[i].meshNum].arrMesh;
					mesh.Layers = layer;
					if (!castShadows)
						mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
					modelObject.AddChild(mesh);
					mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
					if (isViewModel)
						mesh.SetInstanceShaderParameter("ViewModel", true);
					if (useLightVol)
						mesh.SetInstanceShaderParameter("UseLightVol", true);
				}
			}
		}
		return meshProcessed;
	}

	public static MeshProcessed GenerateSprite(string spriteName, string textureName, float width, float height, uint layer, bool castShadows, float destroyTimer, Node3D ownerObject = null, bool forceSkinAlpha = false, bool useCommon = true, bool useLowMultimeshes = true)
	{
		if (ModelsManager.Sprites.ContainsKey(spriteName))
			return FillSpriteFromProcessedData(spriteName, layer, castShadows, destroyTimer, ownerObject, forceSkinAlpha, useCommon);

		if (ownerObject == null)
		{
			GameManager.Print("No ownerObject");
			ownerObject = new Node3D();
			ownerObject.Name = "Sprite_" + spriteName;
		}

		MeshProcessed meshProcessed = new MeshProcessed();
		meshProcessed.node = ownerObject;
		meshProcessed.numMeshes = 1;
		meshProcessed.data = new MeshProcessed.dataMeshes[meshProcessed.numMeshes];

		MeshProcessed.dataMeshes data = new MeshProcessed.dataMeshes();
		data.arrMesh =  GenerateQuadMesh(width, height, 0.5f, 0.5f);

		bool currentTransparent = forceSkinAlpha;
		ShaderMaterial material = MaterialManager.GetMaterials(textureName, -1, ref currentTransparent);

		meshProcessed.data[0] = data;
		data.isTransparent = currentTransparent;
		data.arrMesh.SurfaceSetMaterial(0, material);
		data.meshDataTool.CreateFromSurface(data.arrMesh, 0);
		MultiMesh multiMesh = new MultiMesh();
		data.multiMesh = multiMesh;
		multiMesh.Mesh = data.arrMesh;
		multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		multiMesh.UseColors = true;
		if (useLowMultimeshes)
			multiMesh.InstanceCount = LOW_USE_MULTIMESHES;
		else
			multiMesh.InstanceCount = HIGH_USE_MULTIMESHES;
		multiMesh.VisibleInstanceCount = 0;

		if (!MultiMeshSprites.ContainsKey(multiMesh))
		{
			List<SpriteData> Set = new List<SpriteData>(multiMesh.InstanceCount);
			MultiMeshSprites.Add(multiMesh, Set);
		}

		if (useCommon && !currentTransparent)
		{
			MultiMeshInstance3D mesh = new MultiMeshInstance3D();
			mesh.Name = "MultiMeshSprite_" + spriteName;
			mesh.Multimesh = multiMesh;
			mesh.Layers = layer;
			if (!castShadows)
				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
			if (destroyTimer > 0)
				mesh.SetInstanceShaderParameter("LifeTime", destroyTimer);
			GameManager.Instance.TemporaryObjectsHolder.AddChild(mesh);
			MultiMeshesInstances.Add(multiMesh, mesh);
			GameManager.Print("Adding MultiMesh : " + mesh.Name);
		}
		else
		{
			MeshInstance3D mesh = new MeshInstance3D();
			mesh.Name = spriteName;
			mesh.Mesh = data.arrMesh;
			mesh.Layers = layer;
			if (!castShadows)
				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
			if (destroyTimer > 0)
				mesh.SetInstanceShaderParameter("LifeTime", destroyTimer);
			ownerObject.AddChild(mesh);
		}
		ModelsManager.Sprites.Add(spriteName, data);
		return meshProcessed;
	}
	public static MeshProcessed FillSpriteFromProcessedData(string spriteName, uint layer, bool castShadows, float destroyTimer, Node3D ownerObject = null, bool forceSkinAlpha = false, bool useCommon = true)
	{
		if (ownerObject == null)
		{
			GameManager.Print("No ownerObject");
			ownerObject = new Node3D();
			ownerObject.Name = "Sprite_" + spriteName;
		}

		MeshProcessed meshProcessed = new MeshProcessed();
		meshProcessed.node = ownerObject;
		meshProcessed.numMeshes = 1;
		meshProcessed.data = new MeshProcessed.dataMeshes[meshProcessed.numMeshes];

		MeshProcessed.dataMeshes data = ModelsManager.Sprites[spriteName];
		MultiMesh multiMesh = data.multiMesh;
		bool currentTransparent = forceSkinAlpha;
		meshProcessed.data[0] = data;

		if (useCommon && !currentTransparent)
		{
			if (!MultiMeshesInstances.ContainsKey(multiMesh))
			{
				MultiMeshInstance3D mesh = new MultiMeshInstance3D();
				mesh.Name = "MultiMeshSprite_" + spriteName;
				mesh.Multimesh = multiMesh;
				mesh.Layers = layer;
				if (!castShadows)
					mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
				GameManager.Instance.TemporaryObjectsHolder.AddChild(mesh);
				MultiMeshesInstances.Add(multiMesh, mesh);
				GameManager.Print("Adding MultiMesh : " + mesh.Name);
				mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
				if (destroyTimer > 0)
					mesh.SetInstanceShaderParameter("LifeTime", destroyTimer);
			}

			if (!MultiMeshSprites.ContainsKey(multiMesh))
			{
				List<SpriteData> Set = new List<SpriteData>(multiMesh.InstanceCount);
				MultiMeshSprites.Add(multiMesh, Set);
			}
		}
		else
		{
			MeshInstance3D mesh = new MeshInstance3D();
			mesh.Name = spriteName;
			mesh.Mesh = data.arrMesh;
			mesh.Layers = layer;
			if (!castShadows)
				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			mesh.SetInstanceShaderParameter("OffSetTime", GameManager.CurrentTimeMsec);
			if (destroyTimer > 0)
				mesh.SetInstanceShaderParameter("LifeTime", destroyTimer);
			ownerObject.AddChild(mesh);
		}
		return meshProcessed;
	}

	public static Godot.Collections.Array GenerateModelMesh(MD3Mesh md3Mesh, int frame)
	{
		if (md3Mesh == null)
		{
			GameManager.Print("Failed to generate polygon mesh because there are no meshe info", GameManager.PrintType.Warning);
			return null;
		}

		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		List<int> Triangles = new List<int>();

		for (int i = 0; i < md3Mesh.triangles.Count; i++)
		{
			Triangles.Add(md3Mesh.triangles[i].vertex1);
			Triangles.Add(md3Mesh.triangles[i].vertex2);
			Triangles.Add(md3Mesh.triangles[i].vertex3);
		}

		// add the verts
		surfaceArray[VertexInd] = md3Mesh.verts[frame].ToArray();

		// add normals
		surfaceArray[NormalInd] = md3Mesh.normals[frame].ToArray();

		// Add the texture co-ords (or UVs) to the surface/mesh
		surfaceArray[TexUVInd] = md3Mesh.texCoords.ToArray();

		// add the meshverts to the object being built
		surfaceArray[TriIndex] = Triangles.ToArray();

		return surfaceArray;
	}

	public static void GenerateModelMesh(MD3Mesh md3Mesh, int frame, ref int offset)
	{
		if (offset == 0)
		{
			vertsCache.Clear();
			uvCache.Clear();
			uv2Cache.Clear();
			normalsCache.Clear();
			indiciesCache.Clear();
			vertsColor.Clear();
		}

		vertsCache.AddRange(md3Mesh.verts[frame].ToArray());
		normalsCache.AddRange(md3Mesh.normals[frame].ToArray());
		uvCache.AddRange(md3Mesh.texCoords.ToArray());

		// Rip meshverts / triangles
		for (int i = 0; i < md3Mesh.triangles.Count; i++)
		{
			indiciesCache.Add(md3Mesh.triangles[i].vertex1 + offset);
			indiciesCache.Add(md3Mesh.triangles[i].vertex2 + offset);
			indiciesCache.Add(md3Mesh.triangles[i].vertex3 + offset);
		}
		offset += md3Mesh.verts[frame].Count;
	}
	public static Godot.Collections.Array FinalizeModelMesh()
	{
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		// add the verts, uvs, and normals we ripped to the surfaceArray
		surfaceArray[VertexInd] = vertsCache.ToArray();
		surfaceArray[NormalInd] = normalsCache.ToArray();

		// Add the texture co-ords (or UVs) to the surface/mesh
		surfaceArray[TexUVInd] = uvCache.ToArray();

		// add the meshverts to the object being built
		surfaceArray[TriIndex] = indiciesCache.ToArray();

		return surfaceArray;
	}

	public static ArrayMesh GenerateRagdollFromMesh(ArrayMesh arrMesh)
	{
		SurfaceTool st = new SurfaceTool();
		ArrayMesh ragdoll = new ArrayMesh();
		st.CreateFrom(arrMesh, 0);
		st.GenerateNormals();
		st.GenerateTangents();
		var surfaceArray = st.CommitToArrays();
		ragdoll.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

		return ragdoll;
	}

	public static (uint, CollisionObject3D) GenerateGroupBrushCollider(int indexId, Node3D holder, QBrush[] brushes, CollisionObject3D objCollider = null, uint extraContentFlag = 0)
	{
		WaterSurface waterSurface = null;
		bool isWater = false;
		uint type = MapLoader.mapTextures[brushes[0].shaderId].contentsFlags;
		uint stype = MapLoader.mapTextures[brushes[0].shaderId].surfaceFlags;
		if (((type & ContentFlags.Details) != 0) || ((type & ContentFlags.Structural) != 0))
		{
			GameManager.Print("brushes: " + indexId + " Not used for collisions, Content Type is: " + type, GameManager.PrintType.Info);
			return (0 , null);
		}

		type |= extraContentFlag;

		if (((type & ContentFlags.Water) != 0) || ((type & ContentFlags.Lava) != 0) || ((type & ContentFlags.Slime) != 0))
		{
			isWater = true;
			if ((type & ContentFlags.Translucent) == 0)
			{
				GameManager.Print("brushes: " + indexId + " state it's liquid however it is not Translucent, Content Type is: " + type);
				return (0, null);
			}
		}
		else if ((type & MaskPlayerSolid) == 0)
		{
			GameManager.Print("brushes: " + indexId + " Is not solid, Content Type is: " + type);
			return (0, null);
		}

		ContentType contentType = new ContentType();
		contentType.Init(type);

		if (objCollider == null)
		{
			if (isWater)
			{
				waterSurface = new WaterSurface();
				if ((type & ContentFlags.Lava) != 0)
					waterSurface.damageable = WaterSurface.DamageableType.Lava;
				else if ((type & ContentFlags.Slime) != 0)
					waterSurface.damageable = WaterSurface.DamageableType.Slime;
				objCollider = waterSurface;
			}
			else
				objCollider = new StaticBody3D();

			if (isWater)
				objCollider.Name = "Water_" + indexId + "_collider";
			else
				objCollider.Name = "Polygon_" + indexId + "_collider";

			holder.AddChild(objCollider);
		}
		else if (isWater)
		{
			if (objCollider is WaterSurface)
				waterSurface = (WaterSurface)objCollider;
			else
			{
				waterSurface = new WaterSurface();
				if ((type & ContentFlags.Lava) != 0)
					waterSurface.damageable = WaterSurface.DamageableType.Lava;
				else if ((type & ContentFlags.Slime) != 0)
					waterSurface.damageable = WaterSurface.DamageableType.Slime;
				objCollider.AddChild(waterSurface);
				objCollider = waterSurface;
			}
		}

		if (!MapLoader.mapContentTypes.ContainsKey(objCollider))
			MapLoader.mapContentTypes.Add(objCollider, contentType);

		uint OwnerShapeId = objCollider.CreateShapeOwner(holder);
		bool gotValidShapes = false;
		for (int i = 0; i < brushes.Length; i++)
		{
			ConvexPolygonShape3D convexHull = GenerateBrushCollider(brushes[i]);
			if (convexHull == null)
				continue;

			gotValidShapes = true;
			//Fill Map BB
			Aabb box = convexHull.GetDebugMesh().GetAabb();
			MapLoader.mapBounds = MapLoader.mapBounds.Merge(box);
			objCollider.ShapeOwnerAddShape(OwnerShapeId, convexHull);
			if (isWater)
			{
				waterSurface.Boxes.Add(box);
				GenerateWaterFog(indexId + "_" + i, holder, box, waterSurface.damageable);
			}
		}

		//No a single shape was valid
		if (!gotValidShapes)
			return (0, null);

		SurfaceType surfaceType = new SurfaceType();
		surfaceType.Init(stype);

		if ((surfaceType.value & MaskTransparent) != 0)
			objCollider.CollisionLayer = (1 << GameManager.InvisibleBlockerLayer);
		else
			objCollider.CollisionLayer = (1 << GameManager.ColliderLayer);
		objCollider.InputRayPickable = false;

		if (isWater)
		{
			objCollider.CollisionLayer |= ((1 << GameManager.FogLayer) | (1 << GameManager.WaterLayer));
			MapLoader.waterSurfaces.Add(waterSurface);
			MapLoader.noMarks.Add(objCollider);
		}
		//If noMarks add it to the table
		else if ((surfaceType.value & NoMarks) != 0)
			MapLoader.noMarks.Add(objCollider);

		objCollider.CollisionMask = GameManager.TakeDamageMask | (1 << GameManager.RagdollLayer);
		if (isWater)
			objCollider.CollisionMask |= (1 << GameManager.PhysicCollisionLayer);

		if (!MapLoader.mapSurfaceTypes.ContainsKey(objCollider))
			MapLoader.mapSurfaceTypes.Add(objCollider, surfaceType);

		return (OwnerShapeId, objCollider);
	}

	public static ConvexPolygonShape3D GenerateBrushCollider(QBrush brush)
	{
		var brushPlanes = new Godot.Collections.Array<Plane>();
		List<Vector3> intersectPoint = new List<Vector3>();

		for (int i = 0; i < brush.numOfBrushSides; i++)
		{
			int planeIndex = MapLoader.brushSides[brush.brushSide + i].plane;
			brushPlanes.Add(MapLoader.planes[planeIndex]);
		}
		intersectPoint.AddRange(Geometry3D.ComputeConvexMeshPoints(brushPlanes));
		intersectPoint = RemoveDuplicatedVectors(intersectPoint);
		Vector3 normal = Vector3.Zero;
		if (!CanForm3DConvexHull(intersectPoint, ref normal))
		{
			GameManager.Print("GenerateGroupBrushCollider: Cannot Form 3DConvexHull " + brush.brushSide + " this was a waste of time", GameManager.PrintType.Warning);
			return null;
		}

		ConvexPolygonShape3D convexHull = new ConvexPolygonShape3D();
		convexHull.Points = intersectPoint.ToArray();

		return convexHull;
	}

	public static bool GenerateBrushCollider(QBrush brush, Node3D holder, CollisionObject3D objCollider = null, bool addRigidBody = false, uint extraContentFlag = 0)
	{
		bool isTrigger = false;
		//Remove brushed used for BSP Generations and for Details
		uint type = MapLoader.mapTextures[brush.shaderId].contentsFlags;

		if (((type & ContentFlags.Details) != 0) || ((type & ContentFlags.Structural) != 0))
		{
			GameManager.Print("GenerateBrushCollider: brushSide: " + brush.brushSide + " Not used for collisions, Content Type is: " + type);
			return false;
		}

		var brushPlanes = new Godot.Collections.Array<Plane>();
		List<Vector3> intersectPoint = new List<Vector3>();

		for (int i = 0; i < brush.numOfBrushSides; i++)
		{
			int planeIndex = MapLoader.brushSides[brush.brushSide + i].plane;
			brushPlanes.Add(MapLoader.planes[planeIndex]);
		}
		intersectPoint.AddRange(Geometry3D.ComputeConvexMeshPoints(brushPlanes));
		intersectPoint = RemoveDuplicatedVectors(intersectPoint);
		Vector3 normal = Vector3.Zero;
		if (!CanForm3DConvexHull(intersectPoint, ref normal))
		{
			GameManager.Print("GenerateBrushCollider: Cannot Form 3DConvexHull " + brush.brushSide + " this was a waste of time", GameManager.PrintType.Warning);
			return false;
		}

		ContentType contentType = new ContentType();
		contentType.Init(type | extraContentFlag);

		if ((contentType.value & MaskPlayerSolid) == 0)
			isTrigger = true;

		if (objCollider == null)
		{
			if (isTrigger)
				objCollider = new Area3D();
			else if (addRigidBody)
				objCollider = new AnimatableBody3D();
			else
				objCollider = new StaticBody3D();
			objCollider.Name = "Polygon_" + brush.brushSide + "_collider";
			holder.AddChild(objCollider);
		}
		objCollider.CollisionLayer = (1 << GameManager.ColliderLayer);
		objCollider.CollisionMask = GameManager.TakeDamageMask | (1 << GameManager.RagdollLayer);
		objCollider.InputRayPickable = false;

		CollisionShape3D mc = new CollisionShape3D();
		mc.Name = "brushSide: " + brush.brushSide;
		objCollider.AddChild(mc);

		if (!MapLoader.mapContentTypes.ContainsKey(objCollider))
			MapLoader.mapContentTypes.Add(objCollider, contentType);

		ConvexPolygonShape3D convexHull = new ConvexPolygonShape3D();
		convexHull.Points = intersectPoint.ToArray();
		mc.Shape = convexHull;

//		if ((contentType.value & ContentFlags.PlayerClip) == 0)
//			objCollider.layer = GameManager.InvisibleBlockerLayer;

		type = MapLoader.mapTextures[brush.shaderId].surfaceFlags;
		SurfaceType surfaceType = new SurfaceType();
		surfaceType.Init(type);

		if (!MapLoader.mapSurfaceTypes.ContainsKey(objCollider))
			MapLoader.mapSurfaceTypes.Add(objCollider, surfaceType);

//		if ((surfaceType.value & NoMarks) != 0)
//			MapLoader.noMarks.Add(mc);

		if ((surfaceType.value & MaskTransparent) != 0)
			objCollider.CollisionLayer = (1 << GameManager.InvisibleBlockerLayer);

//		if ((type & SurfaceFlags.NonSolid) != 0)
//			GameManager.Print("brushSide: " + brush.brushSide + " Surface Type is: " + type);

		return true;
	}

	public static ArrayMesh GenerateQuadMesh(float width, float height, float pivotX, float pivotY)
	{
		Vector3[] vertices = new Vector3[4];
		Vector2[] uvs = new Vector2[4];
		int[] indices = new int[6];
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		float x0 = -width * pivotX;
		float x1 = width * (1 - pivotX);
		float y0 = -height * pivotY;
		float y1 = height * (1 - pivotY);

		vertices[0] = new Vector3(x0, y0, 0);
		vertices[1] = new Vector3(x1, y0, 0);
		vertices[2] = new Vector3(x0, y1, 0);
		vertices[3] = new Vector3(x1, y1, 0);

		indices[0] = 2;
		indices[1] = 1;
		indices[2] = 0;
		indices[3] = 3;
		indices[4] = 1;
		indices[5] = 2;

		uvs[0] = new Vector2(0, 0);
		uvs[1] = new Vector2(1, 0);
		uvs[2] = new Vector2(0, 1);
		uvs[3] = new Vector2(1, 1);

		surfaceArray[VertexInd] = vertices.ToArray();
		surfaceArray[TexUVInd] = uvs.ToArray();
		surfaceArray[TriIndex] = indices.ToArray();

		ArrayMesh arrMesh = new ArrayMesh();
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		return arrMesh;
	}
	public static void GenerateWaterFog(string Name, Node3D holder, Aabb box, WaterSurface.DamageableType type)
	{
		FogVolume Fog = new FogVolume();
		Fog.Name = "FogVolume_" + Name;
		holder.AddChild(Fog);

		Fog.Layers = GameManager.AllPlayerViewMask;
		Fog.Position = box.GetCenter();
		Fog.Shape = RenderingServer.FogVolumeShape.Box;
		Fog.Size = box.Size;
		switch (type)
		{
			default:
			case WaterSurface.DamageableType.None:
				Fog.Material = MaterialManager.waterFogMaterial;
				break;
			case WaterSurface.DamageableType.Lava:
				Fog.Material = MaterialManager.lavaFogMaterial;
			break;
			case WaterSurface.DamageableType.Slime:
				Fog.Material = MaterialManager.slimeFogMaterial;
			break;
		}
	}
	public static void GenerateVolumetricFog(int index, QBrush brush, Node3D holder, string textureName)
	{
		ConvexPolygonShape3D convexHull = GenerateBrushCollider(brush);
		if (convexHull == null)
			return;

		FogVolume Fog = new FogVolume();
		Fog.Name = "FogVolume_" + index + "_"+ textureName;
		holder.AddChild(Fog);

		Area3D fogArea = new Area3D();
		fogArea.Name = "FogArea_" + index;
		fogArea.CollisionLayer = (1 << GameManager.FogLayer);
		fogArea.CollisionMask = (1 << GameManager.PhysicCollisionLayer);
		fogArea.InputRayPickable = false;

		holder.AddChild(fogArea);

		CollisionShape3D mc = new CollisionShape3D();
		mc.Shape = convexHull;
		fogArea.AddChild(mc);

		Aabb box = convexHull.GetDebugMesh().GetAabb();
		Fog.Layers = GameManager.AllPlayerViewMask;
		Fog.Position = box.GetCenter();
		Fog.Shape = RenderingServer.FogVolumeShape.Box;
		Fog.Size = box.Size;
		Fog.Material = QShaderManager.GetFog(textureName, Fog.Size.Y);
		GameManager.Print("FOG: " + textureName + " Height " + Fog.Size.Y);
	}

	public static bool CanForm3DConvexHull(List<Vector3> points, ref Vector3 normal, float DISCARD_LIMIT = 0.00001f, bool retried = true)
	{
		int i;

		// Calculate a normal vector
		tryagain:
		if (points.Count < 4)
			return false;

		for (i = 0; i < points.Count; i++)
		{
			Vector3 v1 = points[1] - points[i];
			Vector3 v2 = points[2] - points[i];
			normal = v1.Cross(v2);

			// check that v1 and v2 were NOT collinear
			if (normal.LengthSquared() > 0)
				break;
			if (i == 0)
				i = 2;
		}

		//Check if we got a normal
		if (i == points.Count)
		{
			if (retried)
				return false;
			retried = true;
			points = RemoveDuplicatedVectors(points);
			goto tryagain;
		}

		// Check if all points lie on the plane
		for (i = 0; i < points.Count; i++)
		{
			Vector3 px = points[i] - points[0];
			float dotProduct = px.Dot(normal);

			if (Mathf.Abs(dotProduct) > DISCARD_LIMIT)
				return true;
		}
		
		normal = normal.Normalized();

		return false;
	}

	public static List<Vector3> GetExtrudedVerticesFromPoints(List<Vector3> points, Vector3 normal)
	{
		List<Vector3> vertices = new List<Vector3>(points.Count * 2);
		float depth = 0.002f;
		vertices.AddRange(points);
		for (int i = 0; i < points.Count; i++)
		{
			Vector3 vertice = new Vector3(points[i].X - depth * normal.X, points[i].Y - depth * normal.Y, points[i].Z + depth * normal.Z);
			vertices.Add(vertice);
		}

		return vertices;
	}

	public static List<Vector3> RemoveDuplicatedVectors(List<Vector3> test)
	{
		List<Vector3> uniqueVector = new List<Vector3>();
		Vector3 previousPoint = Vector3.Zero;
		for (int i = 0; i < test.Count; i++)
		{
			bool isUnique = true;
			for (int j = i + 1; j < test.Count; j++)
			{ 
				if (FloatAprox(test[i].X, test[j].X) &&
					FloatAprox(test[i].Y, test[j].Y) &&
					FloatAprox(test[i].Z, test[j].Z))
						isUnique = false;
			}
			if (isUnique)
			{
				if (uniqueVector.Count > 0)
					uniqueVector = uniqueVector.OrderBy(n => (n - previousPoint).LengthSquared()).ToList();
				uniqueVector.Add(new Vector3(RoundUp4Decimals(test[i].X), RoundUp4Decimals(test[i].Y), RoundUp4Decimals(test[i].Z)));
				previousPoint = uniqueVector[uniqueVector.Count - 1];
			}
		}
		return uniqueVector;
	}
	public static bool FloatAprox(float f1, float f2)
	{
		float d = f1 - f2;

		if (d < -APROX_ERROR || d > APROX_ERROR)
			return false;
		return true;
	}
	public static float RoundUp4Decimals(float f)
	{
		float d = Mathf.CeilToInt(f * 10000) / 10000.0f;
		return d;
	}

	public static void AddSpriteToMultiMeshes(MultiMesh multiMesh, SpriteData sprite, Color color)
	{
		List<SpriteData> spriteDataList;
		int instanceNum;
		if (MultiMeshSprites.TryGetValue(multiMesh, out spriteDataList))
		{
			instanceNum = spriteDataList.Count;
			int threshold = (multiMesh.InstanceCount >> 1);
			if (instanceNum > threshold)
			{
				foreach (SpriteData spriteToDestroy in spriteDataList)
				{
					if (spriteToDestroy.readyToDestroy)
						continue;

					spriteToDestroy.readyToDestroy = true;
					spriteToDestroy.GlobalPosition = MapLoader.mapMinCoord * 2f;
					break;
				}
			}

			spriteDataList.Add(sprite);
			multiMesh.VisibleInstanceCount = instanceNum + 1;
			if (multiMesh.UseColors)
				multiMesh.SetInstanceColor(instanceNum, color);
			multiMesh.SetInstanceTransform(instanceNum, sprite.GlobalTransform);
		}
	}
	public static void AddNodeToMultiMeshes(MultiMesh multiMesh, Node3D owner, Color color)
	{
		Dictionary<Node3D, int> multiMeshSet;
		int instanceNum;
		if (MultiMeshes.TryGetValue(multiMesh, out multiMeshSet))
		{
			if (multiMeshSet.ContainsKey(owner))
				return;

			instanceNum = multiMeshSet.Count;
			int threshold = (multiMesh.InstanceCount >> 1);
			if (instanceNum > threshold)
			{
				foreach (Node3D node3D in multiMeshSet.Keys)
				{
					if (node3D.HasMeta("destroying"))
						continue;

					Node parent = node3D.GetParent();
					{
						Node child = node3D;
						while (parent != GameManager.Instance.TemporaryObjectsHolder)
						{
							child = parent;
							parent = child.GetParent();
						}
						parent = child;
					}
		
					DestroyAfterTime destroy = new DestroyAfterTime();
					parent.AddChild(destroy);
					destroy.Start();
					node3D.SetMeta("destroying", true);
					break;
				}
			}

			multiMeshSet.Add(owner, instanceNum);
			if (multiMesh.UseColors)
				multiMesh.SetInstanceColor(instanceNum, color);
			multiMesh.SetInstanceTransform(instanceNum, owner.GlobalTransform);
			multiMesh.VisibleInstanceCount = instanceNum + 1;
		}
	}
	public static void UpdateInstanceMultiMesh(MultiMesh multiMesh, Node3D owner)
	{
		Dictionary<Node3D, int> multiMeshSet;
		if (MultiMeshes.TryGetValue(multiMesh, out multiMeshSet))
		{
			int index;
			if (!multiMeshSet.TryGetValue(owner, out index))
				return;

			if (owner.IsVisibleInTree())
				multiMesh.SetInstanceTransform(index, owner.GlobalTransform);
			else //Move it out of the map
			{
				Transform3D min = new Transform3D(owner.Basis, MapLoader.mapMinCoord * 2f);
				multiMesh.SetInstanceTransform(index, min);
			}
		}
	}
	public static void UpdateInstanceMultiMesh(MultiMesh multiMesh, Node3D owner, Color color)
	{
		Dictionary<Node3D, int> multiMeshSet;
		if (MultiMeshes.TryGetValue(multiMesh, out multiMeshSet))
		{
			int index; 
			if (!multiMeshSet.TryGetValue(owner, out index))
				return;

			multiMesh.SetInstanceColor(index, color);
			if (owner.IsVisibleInTree())
				multiMesh.SetInstanceTransform(index, owner.GlobalTransform);
			else //Move it out of the map
			{
				Transform3D min = new Transform3D(owner.Basis, MapLoader.mapMinCoord * 2f);
				multiMesh.SetInstanceTransform(index, min);
			}
		}
	}
	public static void MultiMeshUpdateInstances(MultiMesh multiMesh)
	{
		if (MultiMeshesChanged.Contains(multiMesh))
			return;
		MultiMeshesChanged.Add(multiMesh);
	}

	public static void UpdateChangedMultiMeshes()
	{
		foreach(MultiMesh multiMesh in MultiMeshesChanged)
		{
			Dictionary<Node3D, int> multiMeshSet;
			if (MultiMeshes.TryGetValue(multiMesh, out multiMeshSet))
			{
				multiMesh.VisibleInstanceCount = multiMeshSet.Count;
				int i = 0;
				foreach (var instance in multiMeshSet)
				{
					Node3D node = instance.Key;
					multiMeshSet[node] = i;
					if (node.IsVisibleInTree())
						multiMesh.SetInstanceTransform(i, node.GlobalTransform);
					else
					{
						Transform3D min = new Transform3D(node.Basis, MapLoader.mapMinCoord * 2f);
						multiMesh.SetInstanceTransform(i, min);
					}
					i++;
				}
			}
		}
		MultiMeshesChanged.Clear();
	}


	public static void ProcessSprites(float deltaTime)
	{
		bool destroyTime = ((Engine.GetFramesDrawn() % 300) == 0);
		List<SpriteData> detroyDataList = new List<SpriteData>();
		int i = 0;
		foreach (var keyValuePair in MultiMeshSprites)
		{
			MultiMesh multiMesh = keyValuePair.Key;
			List<SpriteData> spriteDataList = keyValuePair.Value;
			detroyDataList.Clear();
			int oldCount = spriteDataList.Count;
			for(i = 0; i < oldCount; i++)
			{
				spriteDataList[i].Process(deltaTime);
				if ((destroyTime) && (spriteDataList[i].readyToDestroy))
					detroyDataList.Add(spriteDataList[i]);
			}
			bool forceUpdate = false;
			if (destroyTime)
			{
				for (i = 0; i < detroyDataList.Count; i++)
					detroyDataList[i].Destroy();

				multiMesh.VisibleInstanceCount = spriteDataList.Count;
				forceUpdate = (oldCount != multiMesh.VisibleInstanceCount);
			}

			for (i = 0; i < spriteDataList.Count; i++)
			{
				if ((forceUpdate) || (spriteDataList[i].update))
				{
					if (spriteDataList[i].update)
						spriteDataList[i].update = false;
					multiMesh.SetInstanceColor(i, spriteDataList[i].Modulate);
					multiMesh.SetInstanceTransform(i, spriteDataList[i].GlobalTransform);
				}
			}
		}
	}

}

