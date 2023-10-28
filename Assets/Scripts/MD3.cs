using Godot;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using ExtensionMethods;

public class MD3GodotConverted
{
	public Node3D	node;
	public int numMeshes;
	public dataMeshes[] data;
	public class dataMeshes
	{
		public MeshDataTool meshDataTool = new MeshDataTool();
		public ArrayMesh arrMesh = new ArrayMesh();
	}
}
public class MD3
{
	public string name;																// The name of the model
	public int flags;																// The model flags
	public int version;																// The version of the model
	public int numFrames;															// The number of frames in the model
	public int numTags;																// The number of tags in the model
	public int numMeshes;															// The number of meshes in the model
	public int numSkins;															// The number of skins in the model
	public List<MD3Frame> frames;													// The list of frames in the model
	public Dictionary<string, int> tagsIdbyName = new Dictionary<string, int>();	// Get the index of tags list in the model by name
	public List<List<MD3Tag>> tagsbyId = new List<List<MD3Tag>>();					// The list of tags in the model by Id
	public List<MD3Mesh> meshes;													// The list of meshes in the model
	public List<MD3Skin> skins;														// The list of skins in the model
	public List<Godot.Collections.Array> readySurfaceArray = new List<Godot.Collections.Array>();			// This is the processed Godot Mesh
	public List<ShaderMaterial> readyMaterials = new List<ShaderMaterial>();		// This is the processed Material
	public static MD3 ImportModel(string modelName, bool forceSkinAlpha)
	{
		BinaryReader Md3ModelFile;
		string[] name;

		string path = Directory.GetCurrentDirectory() + "/StreamingAssets/models/" + modelName + ".md3";
		if (File.Exists(path))
			Md3ModelFile = new BinaryReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.ContainsKey(path = ("models/" + modelName + ".md3").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			var reader = new ZipReader();
			reader.Open(FileName);
			MemoryStream ms = new MemoryStream(reader.ReadFile(path, false));
			Md3ModelFile = new BinaryReader(ms);
		}
		else
			return null;

		Md3ModelFile.BaseStream.Seek(0, SeekOrigin.Begin);
		string header = new string(Md3ModelFile.ReadChars(4)); //4 IDP3
		if (header != "IDP3")
		{
			GD.PrintErr(modelName + " not a md3 model");
			return null;
		}

		MD3 md3Model = new MD3();

		md3Model.version = Md3ModelFile.ReadInt32();

		name = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
		md3Model.name = name[0].Replace("\0", string.Empty);

		md3Model.flags = Md3ModelFile.ReadInt32();
		md3Model.numFrames = Md3ModelFile.ReadInt32();
		md3Model.numTags = Md3ModelFile.ReadInt32();
		md3Model.numMeshes = Md3ModelFile.ReadInt32();
		md3Model.numSkins = Md3ModelFile.ReadInt32();

		int ofsFrames = Md3ModelFile.ReadInt32();
		int ofsTags = Md3ModelFile.ReadInt32();
		int ofsMeshes = Md3ModelFile.ReadInt32();
		int fileSize = Md3ModelFile.ReadInt32();


		md3Model.frames = new List<MD3Frame>();
		Md3ModelFile.BaseStream.Seek(ofsFrames, SeekOrigin.Begin);
		for (int i = 0, j = 0, numFrame = 1; i < md3Model.numFrames * md3Model.numTags; i++)
		{
			MD3Frame frame = new MD3Frame();

			float x = Md3ModelFile.ReadSingle();
			float y = Md3ModelFile.ReadSingle();
			float z = Md3ModelFile.ReadSingle();
			frame.bb_Min = new Vector3(x, y, z);

			x = Md3ModelFile.ReadSingle();
			y = Md3ModelFile.ReadSingle();
			z = Md3ModelFile.ReadSingle();
			frame.bb_Max = new Vector3(x, y, z);

			frame.bs_Radius = Md3ModelFile.ReadSingle();

			x = Md3ModelFile.ReadSingle();
			y = Md3ModelFile.ReadSingle();
			z = Md3ModelFile.ReadSingle();
			frame.locOrigin = new Vector3(x, y, z);

			Md3ModelFile.ReadBytes(16);
//			name = (new string(Md3ModelFile.ReadChars(16))).Split('\0');
//			frame.name = name[0].Replace("\0", string.Empty);
			frame.name = "Tag Frame " + numFrame++;
			if (((i + 1) % md3Model.numFrames) == 0)
			{
				j++;
				numFrame = 1;
			}
			frame.QuakeToGodotCoordSystem();
			md3Model.frames.Add(frame);
		}

		Md3ModelFile.BaseStream.Seek(ofsTags, SeekOrigin.Begin);

		for (int i = 0, tagId = 0; i < md3Model.numFrames * md3Model.numTags; i++)
		{
			MD3Tag tag = new MD3Tag();
			name = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
			tag.name = name[0].Replace("\0", string.Empty);
			float x = Md3ModelFile.ReadSingle();
			float y = Md3ModelFile.ReadSingle();
			float z = Md3ModelFile.ReadSingle();

			tag.origin = new Vector3(x, y, z);

			float m00 = Md3ModelFile.ReadSingle();
			float m01 = Md3ModelFile.ReadSingle();
			float m02 = Md3ModelFile.ReadSingle();

			float m10 = Md3ModelFile.ReadSingle();
			float m11 = Md3ModelFile.ReadSingle();
			float m12 = Md3ModelFile.ReadSingle();

			float m20 = Md3ModelFile.ReadSingle();
			float m21 = Md3ModelFile.ReadSingle();
			float m22 = Md3ModelFile.ReadSingle();

			Vector3 column0 = new Vector3(m00, m10, m20);
			Vector3 column1 = new Vector3(m01, m11, m21);
			Vector3 column2 = new Vector3(m02, m12, m22);
			Vector3 column3 = new Vector3(0, 0, 0);

			//https://math.stackexchange.com/questions/3882851/convert-rotation-matrix-between-coordinate-systems
			//We need to convert the rotation to the new coordinate system, the new coordinate system conversion is given by T (Quakt To Unity Conversion)
			//If the two coordinate system are in the same space and they are related by T, with the old rotation Ra then the new rotation Rb is given by 
			//Rb = TRaT^-1
			Transform3D R = new Transform3D(column0, column1, column2, column3);
			if (R.IsFinite())
			{
				tag.orientation = tag.orientation.QuakeToGodotConversion() * R * tag.orientation.QuakeToGodotConversion().Inverse();
//				tag.rotation = tag.orientation.Basis.GetRotationQuaternion();
				tag.rotation = tag.orientation.ExtractRotation();
//				GD.Print(" X " + tag.rotation.X + " Y " + tag.rotation.Y + " Z " + tag.rotation.Z + " W "+ tag.rotation.W);
			}
			tag.QuakeToGodotCoordSystem();
			if (!md3Model.tagsIdbyName.ContainsKey(tag.name))
			{
				List<MD3Tag> tagList = new List<MD3Tag>();
				md3Model.tagsIdbyName.Add(tag.name, tagId++);
				md3Model.tagsbyId.Add(tagList);
			}
			md3Model.tagsbyId[md3Model.tagsIdbyName[tag.name]].Add(tag);
		}

		int offset = ofsMeshes;
		md3Model.meshes = new List<MD3Mesh>(md3Model.numMeshes);
		for (int i = 0; i < md3Model.numMeshes; i++)
		{
			Md3ModelFile.BaseStream.Seek(offset, SeekOrigin.Begin);
			MD3Mesh md3Mesh = new MD3Mesh();

			md3Mesh.parseMesh(i, md3Model.name, Md3ModelFile, offset, forceSkinAlpha);
			offset += md3Mesh.meshSize;
			md3Model.meshes.Add(md3Mesh);
		}
		return md3Model;
	}
}
public class MD3Frame
{
	public string name;                 // The name of the frame
	public Vector3 bb_Min;              // The minimum bounds of the frame's bounding box
	public Vector3 bb_Max;              // The maximum bounds of the frame's bounding box
	public float bs_Radius;             // The radius of the frame's bounding sphere
	public Vector3 locOrigin;           // The local origin of the frame
	public void QuakeToGodotCoordSystem()
	{
		bb_Min = new Vector3(-bb_Min.X, bb_Min.Z, bb_Min.Y);
		bb_Max = new Vector3(-bb_Max.X, bb_Max.Z, bb_Max.Y);
		locOrigin = new Vector3(-locOrigin.X, locOrigin.Z, locOrigin.Y);

		bb_Min *= GameManager.sizeDividor;
		bb_Max *= GameManager.sizeDividor;
		locOrigin *= GameManager.sizeDividor;
		bs_Radius *= GameManager.sizeDividor;
	}
}
public class MD3Tag
{
	public string name;                 // The name of the tag
	public Vector3 origin;              // The origin of the tag in 3D space
	public Transform3D orientation;       // The orientation of the tag in 3D space
	public Quaternion rotation;         // The rotation of the tag in 3D space
	public void QuakeToGodotCoordSystem()
	{
		origin = new Vector3(-origin.X, origin.Z, origin.Y);
		origin *= GameManager.sizeDividor;
	}
}
public class MD3Skin
{
	public string name;                 // The name of the skin
	public int skinId;                  // The index of the skin in the list of skins
	public MD3Skin(int skinId, string name)
	{
		this.skinId = skinId;
		this.name = name;
	}
}

public class MD3Mesh
{
	public string name;                 // The name of the surface
	public int meshNum;                 // The index num of the mesh in the model
	public int meshId;                  // The index of the mesh in the list of meshes
	public int flags;                   // The flags associated with the surface
	public int numFrames;               // The number of frames in the surface
	public int numSkins;                // The number of skins in the surface
	public int numTriangles;            // The number of triangles in the surface
	public int numVertices;             // The number of vertexes in the surface
	public List<MD3Skin> skins;         // The list of shaders in the surface
	public List<MD3Triangle> triangles; // The list of triangles in the surface
	public List<Vector3>[] verts;       // The list of vertexes in the surface
	public List<Vector3>[] normals;       // The list of normals in the surface
	public List<Vector2> texCoords;     // The texture coordinates of the vertex
	public int meshSize;                // This stores the total mesh size
	public void parseMesh(int MeshNum, string modelName, BinaryReader Md3ModelFile, int MeshOffset, bool forceSkinAlpha)
	{
		string[] fullName;
		meshNum = MeshNum;
		meshId = Md3ModelFile.ReadInt32();
		fullName = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
		name = fullName[0].Replace("\0", string.Empty);

//		GD.Print("Loading Mesh: " + name + " , " + meshId);

		flags = Md3ModelFile.ReadInt32();
		numFrames = Md3ModelFile.ReadInt32();					// This stores the mesh aniamtion frame count
		numSkins = Md3ModelFile.ReadInt32();					// This stores the mesh skin count
		numVertices = Md3ModelFile.ReadInt32();					// This stores the mesh vertex count
		numTriangles = Md3ModelFile.ReadInt32();				// This stores the mesh face count
		int ofsTriangles = Md3ModelFile.ReadInt32();			// This stores the starting offset for the triangles
		int ofsSkins = Md3ModelFile.ReadInt32();				// This stores the header size for the mesh
		int ofsTexCoords = Md3ModelFile.ReadInt32();			// This stores the starting offset for the UV coordinates
		int ofsVerts = Md3ModelFile.ReadInt32();				// This stores the starting offset for the vertex indices
		meshSize = Md3ModelFile.ReadInt32();					// This stores the total mesh size

		skins = new List<MD3Skin>();
		List<string> skinList = new List<string>();

		Md3ModelFile.BaseStream.Seek(MeshOffset + ofsSkins, SeekOrigin.Begin);
		for (int i = 0; i < numSkins; i++)
		{
			fullName = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
			string skinName = fullName[0].Replace("\0", string.Empty);
			//Need to strip extension
			fullName = skinName.Split('.');

			int num = Md3ModelFile.ReadInt32();

			//Some skins are mentioned more than once
			if (skinList.Contains(fullName[0]))
				continue;

			if (!TextureLoader.HasTexture(fullName[0]))
				TextureLoader.AddNewTexture(fullName[0], forceSkinAlpha);

			skins.Add(new MD3Skin(num, fullName[0]));
			skinList.Add(fullName[0]);
		}
		//Update Number of skins as some are repeated
		numSkins = skins.Count;

		triangles = new List<MD3Triangle>();
		Md3ModelFile.BaseStream.Seek(MeshOffset + ofsTriangles, SeekOrigin.Begin);
		for (int i = 0; i < numTriangles; i++)
		{
			int f0 = Md3ModelFile.ReadInt32();
			int f1 = Md3ModelFile.ReadInt32();
			int f2 = Md3ModelFile.ReadInt32();
			triangles.Add(new MD3Triangle(i, f0, f1, f2));
		}

		texCoords = new List<Vector2>();
		Md3ModelFile.BaseStream.Seek(MeshOffset + ofsTexCoords, SeekOrigin.Begin);
		for (int i = 0; i < numVertices; i++)
		{
			float u = Md3ModelFile.ReadSingle();
			float v = Md3ModelFile.ReadSingle();
			texCoords.Add(new Vector2(u, v));
		}

		verts = new List<Vector3>[numFrames];
		normals = new List<Vector3>[numFrames];
		for (int i = 0; i < numFrames; i++)
		{
			verts[i] = new List<Vector3>();
			normals[i] = new List<Vector3>();
		}
		Md3ModelFile.BaseStream.Seek(MeshOffset + ofsVerts, SeekOrigin.Begin);
		for (int i = 0, j = 0; i < numVertices * numFrames; i++)
		{
			float x = Md3ModelFile.ReadInt16() * GameManager.modelScale;
			float y = Md3ModelFile.ReadInt16() * GameManager.modelScale;
			float z = Md3ModelFile.ReadInt16() * GameManager.modelScale;
			byte n1 = Md3ModelFile.ReadByte();
			byte n2 = Md3ModelFile.ReadByte();

			float lat = n1 * (6.28f) / 255.0f;
			float lng = n2 * (6.28f) / 255.0f;

			Vector3	normal = new Vector3(- Mathf.Cos(lat) * Mathf.Sin(lng), Mathf.Cos(lng), Mathf.Sin(lat) * Mathf.Sin(lng));
			Vector3 position = new Vector3(-x, z, y);
			position *= GameManager.sizeDividor;
			verts[j].Add(position);
			normals[j].Add(normal.Normalized());
			if (((i + 1) % numVertices) == 0)
				j++;
		}
	}

}

// The indexes of the vertexes that make up the triangle
public class MD3Triangle
{
	public int triId;
	public int vertex1;
	public int vertex2;
	public int vertex3;
	public MD3Triangle(int triId, int vertex1, int vertex2, int vertex3)
	{
		this.triId = triId;
		this.vertex1 = vertex1;
		this.vertex2 = vertex2;
		this.vertex3 = vertex3;
	}
}