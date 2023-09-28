using Godot;
using System.Collections.Generic;
using System.Diagnostics;

public partial class BezierMesh
{
	private static List<Vector3> vertexCache = new List<Vector3>();
	private static List<Vector2> uvCache = new List<Vector2>();
	private static List<Vector2> uv2Cache = new List<Vector2>();
	private static List<Color> colorCache = new List<Color>();
	private static List<int> indiciesCache = new List<int>();

	private static List<Vector3> p0sCache = new List<Vector3>();
	private static List<Vector2> p0suvCache = new List<Vector2>();
	private static List<Vector2> p0suv2Cache = new List<Vector2>();
	private static List<Color> p0scolorCache = new List<Color>();

	private static List<Vector3> p1sCache = new List<Vector3>();
	private static List<Vector2> p1suvCache = new List<Vector2>();
	private static List<Vector2> p1suv2Cache = new List<Vector2>();
	private static List<Color> p1scolorCache = new List<Color>();

	private static List<Vector3> p2sCache = new List<Vector3>();
	private static List<Vector2> p2suvCache = new List<Vector2>();
	private static List<Vector2> p2suv2Cache = new List<Vector2>();
	private static List<Color> p2scolorCache = new List<Color>();

	private static List<Vector2> vertex2d = new List<Vector2>();

	private const int VertexInd = (int)Mesh.ArrayType.Vertex;
	private const int NormalInd = (int)Mesh.ArrayType.Normal;
	private const int TexUVInd = (int)Mesh.ArrayType.TexUV;
	private const int TexUV2Ind = (int)Mesh.ArrayType.TexUV2;
	private const int ColorInd = (int)Mesh.ArrayType.Color;
	private const int TriIndex = (int)Mesh.ArrayType.Index;
	public enum Axis
	{
		None,
		X,
		Y,
		Z
	}
	public static void ClearCaches()
	{
		vertexCache = new List<Vector3>();
		uvCache = new List<Vector2>();
		uv2Cache = new List<Vector2>();
		indiciesCache = new List<int>();
		colorCache = new List<Color>();

		p0sCache = new List<Vector3>();
		p0suvCache = new List<Vector2>();
		p0suv2Cache = new List<Vector2>();
		p0scolorCache = new List<Color>();

		p1sCache = new List<Vector3>();
		p1suvCache = new List<Vector2>();
		p1suv2Cache = new List<Vector2>();
		p1scolorCache = new List<Color>();

		p2sCache = new List<Vector3>();
		p2suvCache = new List<Vector2>();
		p2suv2Cache = new List<Vector2>();
		p2scolorCache = new List<Color>();
	}

	public BezierMesh(int level, int patchNumber, List<Vector3> control, List<Vector2> controlUvs, List<Vector2> controlUv2s, List<Color> controlColor)
	{
		// The mesh we're building
		ArrayMesh patchMesh = new ArrayMesh();
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		mesh = new MeshInstance3D();
		mesh.Name = "Bezier_Patch_" + patchNumber;

		// We'll use these two to hold our verts, tris, and uvs
		int capacity = level * level + (2 * level);
		if (vertexCache.Capacity < capacity)
		{
			vertexCache.Capacity = capacity;
			uvCache.Capacity = capacity;
			uv2Cache.Capacity = capacity;
			indiciesCache.Capacity = capacity;
			colorCache.Capacity = capacity;
		}

		vertexCache.Clear();
		uvCache.Clear();
		uv2Cache.Clear();
		indiciesCache.Clear();
		colorCache.Clear();


		p0sCache.Clear();
		p0suvCache.Clear();
		p0suv2Cache.Clear();
		p0scolorCache.Clear();

		p1sCache.Clear();
		p1suvCache.Clear();
		p1suv2Cache.Clear();
		p1scolorCache.Clear();

		p2sCache.Clear();
		p2suvCache.Clear();
		p2suv2Cache.Clear();
		p2scolorCache.Clear();

		// The incoming list is 9 entires, 
		// referenced as p0 through p8 here.

		// Generate extra rows to tessellate
		// each row is three control points
		// start, curve, end
		// The "lines" go as such
		// p0s from p0 to p3 to p6 ''
		// p1s from p1 p4 p7
		// p2s from p2 p5 p8

		Tessellate(level, control[0], control[3], control[6], p0sCache);
		TessellateUV(level, controlUvs[0], controlUvs[3], controlUvs[6], p0suvCache);
		TessellateUV(level, controlUv2s[0], controlUv2s[3], controlUv2s[6], p0suv2Cache);
		TessellateColor(level, controlColor[0], controlColor[3], controlColor[6], p0scolorCache);

		Tessellate(level, control[1], control[4], control[7], p1sCache);
		TessellateUV(level, controlUvs[1], controlUvs[4], controlUvs[7], p1suvCache);
		TessellateUV(level, controlUv2s[1], controlUv2s[4], controlUv2s[7], p1suv2Cache);
		TessellateColor(level, controlColor[1], controlColor[4], controlColor[7], p1scolorCache);

		Tessellate(level, control[2], control[5], control[8], p2sCache);
		TessellateUV(level, controlUvs[2], controlUvs[5], controlUvs[8], p2suvCache);
		TessellateUV(level, controlUv2s[2], controlUv2s[5], controlUv2s[8], p2suv2Cache);
		TessellateColor(level, controlColor[2], controlColor[5], controlColor[8], p2scolorCache);

		// Tessellate all those new sets of control points and pack
		// all the results into our vertex array, which we'll return.
		for (int i = 0; i <= level; i++)
		{
			Tessellate(level, p0sCache[i], p1sCache[i], p2sCache[i], vertexCache);
			TessellateUV(level, p0suvCache[i], p1suvCache[i], p2suvCache[i], uvCache);
			TessellateUV(level, p0suv2Cache[i], p1suv2Cache[i], p2suv2Cache[i], uv2Cache);
			TessellateColor(level, p0scolorCache[i], p1scolorCache[i], p2scolorCache[i], colorCache);
		}

		// This will produce (tessellationLevel + 1)^2 verts
		int numVerts = (level + 1) * (level + 1);

		// Compute triangle indexes for forming a mesh.
		// The mesh will be tessellationlevel + 1 verts
		// wide and tall.
		int xStep = 1;
		int width = level + 1;
		for (int i = 0; i < numVerts - width; i++)
		{
			//on left edge
			if (xStep == 1)
			{
				indiciesCache.Add(i);
				indiciesCache.Add(i + width);
				indiciesCache.Add(i + 1);

				xStep++;
			}
			else if (xStep == width) //on right edge
			{
				indiciesCache.Add(i);
				indiciesCache.Add(i + (width - 1));
				indiciesCache.Add(i + width);

				xStep = 1;
			}
			else // not on an edge, so add two
			{
				indiciesCache.Add(i);
				indiciesCache.Add(i + (width - 1));
				indiciesCache.Add(i + width);


				indiciesCache.Add(i);
				indiciesCache.Add(i + width);
				indiciesCache.Add(i + 1);

				xStep++;
			}
		}

		// Add the verts and tris
		surfaceArray[VertexInd] = vertexCache.ToArray();
		surfaceArray[TexUVInd] = uvCache.ToArray();
		surfaceArray[TexUV2Ind] = uv2Cache.ToArray();
		surfaceArray[ColorInd] = colorCache.ToArray();
		surfaceArray[TriIndex] = indiciesCache.ToArray();

		// Create the Mesh.
		patchMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

		// Tool needed to recalculate normals .
		SurfaceTool st = new SurfaceTool();
		st.CreateFrom(patchMesh, 0);
		st.GenerateNormals();
		surfaceArray = st.CommitToArrays();
		patchMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		mesh.Mesh = patchMesh;
	}

	public void BezierColliderMesh(int surfaceId, int patchNumber, List<Vector3> control)
	{
		const int colliderTessellations = 4;  //Do not modify

		Node3D parent = null;
		float step, s, f, m;
		int iterOne = colliderTessellations, interTwo = colliderTessellations;
		bool Collinear, allCollinear = false;

		// We'll use these two to hold our verts
		int capacity = control.Count;
		if (vertexCache.Capacity < capacity)
			vertexCache.Capacity = capacity;

		//Check if control points rows are collinear
		{
			p0sCache.Clear();
			p1sCache.Clear();
			p2sCache.Clear();

			for (int i = 0; i < 3; i++)
			{
				p0sCache.Add(control[i]);
				p1sCache.Add(control[3 + i]);
				p2sCache.Add(control[6 + i]);
			}

			Collinear = ArePointsCollinear(p0sCache);
			Collinear &= ArePointsCollinear(p1sCache);
			Collinear &= ArePointsCollinear(p2sCache);
		}

		//Check if control points columns are collinear
		if (!Collinear)
		{
			p0sCache.Clear();
			p1sCache.Clear();
			p2sCache.Clear();

			for (int i = 0; i < 3; i++)
			{
				p0sCache.Add(control[3 * i]);
				p1sCache.Add(control[(3 * i) + 1]);
				p2sCache.Add(control[(3 * i) + 2]);
			}

			Collinear = ArePointsCollinear(p0sCache);
			Collinear &= ArePointsCollinear(p1sCache);
			Collinear &= ArePointsCollinear(p2sCache);
		}
		else //Check if all control points are collinear
		{
			allCollinear = true;
			for (int j = 0; j < 3; j++)
			{
				vertexCache.Clear();
				for (int i = 0; i < 3; i++)
					vertexCache.Add(control[(3 * i) + j]);
				allCollinear &= ArePointsCollinear(vertexCache);
				if (!allCollinear)
					break;
			}
			if (allCollinear)
			{
				interTwo = 1;
				vertexCache.Clear();
				for (int i = 0; i < capacity; i++)
					vertexCache.Add(control[i]);
			}
		}

		step = 1f / colliderTessellations;

		if (Collinear)
			iterOne = 1;

		for (int i = 0; i < iterOne; i++)
		{
			if (!Collinear)
			{
				s = i * step;
				f = (i + 1) * step;
				m = (s + f) / 2f;
				p0sCache.Clear();
				p1sCache.Clear();
				p2sCache.Clear();

				//Top row
				p0sCache.Add(BezCurve(s, control[0], control[1], control[2]));
				p0sCache.Add(BezCurve(m, control[0], control[1], control[2]));
				p0sCache.Add(BezCurve(f, control[0], control[1], control[2]));

				//Middle row
				p1sCache.Add(BezCurve(s, control[3], control[4], control[5]));
				p1sCache.Add(BezCurve(m, control[3], control[4], control[5]));
				p1sCache.Add(BezCurve(f, control[3], control[4], control[5]));

				//Bottom row
				p2sCache.Add(BezCurve(s, control[6], control[7], control[8]));
				p2sCache.Add(BezCurve(m, control[6], control[7], control[8]));
				p2sCache.Add(BezCurve(f, control[6], control[7], control[8]));
			}

			for (int j = 0; j < interTwo; j++)
			{
				if (!allCollinear)
				{
					s = j * step;
					f = (j + 1) * step;
					m = (s + f) / 2f;
					vertexCache.Clear();

					//Top row
					vertexCache.Add(BezCurve(s, p0sCache[0], p1sCache[0], p2sCache[0]));
					vertexCache.Add(BezCurve(m, p0sCache[0], p1sCache[0], p2sCache[0]));
					vertexCache.Add(BezCurve(f, p0sCache[0], p1sCache[0], p2sCache[0]));

					//Middle row
					vertexCache.Add(BezCurve(s, p0sCache[1], p1sCache[1], p2sCache[1]));
					vertexCache.Add(BezCurve(m, p0sCache[1], p1sCache[1], p2sCache[1]));
					vertexCache.Add(BezCurve(f, p0sCache[1], p1sCache[1], p2sCache[1]));

					//Bottom row
					vertexCache.Add(BezCurve(s, p0sCache[2], p1sCache[2], p2sCache[2]));
					vertexCache.Add(BezCurve(m, p0sCache[2], p1sCache[2], p2sCache[2]));
					vertexCache.Add(BezCurve(f, p0sCache[2], p1sCache[2], p2sCache[2]));
				}
				Vector3 normal = Vector3.Zero;
				Axis axis;
				bool is3D = true;
				Quaternion changeRotation = Quaternion.Identity;
				if (!Mesher.CanForm3DConvexHull(vertexCache, ref normal))
				{
					if ((normal.X == 1) || (normal.X == -1))
						axis = Axis.X;
					else if ((normal.Y == 1) || (normal.Y == -1))
						axis = Axis.Y;
					else if ((normal.Z == 1) || (normal.Z == -1))
						axis = Axis.Z;
					else
					{
						GD.Print("2D Plane is Rotated");
						float x = Mathf.Abs(normal.X), y = Mathf.Abs(normal.Y), z = Mathf.Abs(normal.Z);
						Vector3 normalRef = Vector3.Zero;

						if ((x >= y) && (x >= z))
							axis = Axis.X;
						else if ((y >= x) && (y >= z))
							axis = Axis.Y;
						else
							axis = Axis.Z;

						switch (axis)
						{
							case Axis.X:
								if (normal.X > 0)
									normalRef = Vector3.Right;
								else
									normalRef = Vector3.Left;
								break;
							case Axis.Y:
								if (normal.Y > 0)
									normalRef = Vector3.Up;
								else
									normalRef = Vector3.Down;
								break;
							case Axis.Z:
								if (normal.Z > 0)
									normalRef = Vector3.Back;
								else
									normalRef = Vector3.Forward;
								break;
						}
						changeRotation = CalculateRotation(normal, normalRef);
					}

					//Check if it's a 2D Surface
					vertex2d.Clear();
					for (int k = 0; k < vertexCache.Count; k++)
					{
						Vector3 vertex = changeRotation * vertexCache[k];
						switch (axis)
						{ 
							case Axis.X:
								vertex2d.Add(new Vector2(vertex.Y, vertex.Z));
								break;
							case Axis.Y:
								vertex2d.Add(new Vector2(vertex.X, vertex.Z));
								break;
							case Axis.Z:
								vertex2d.Add(new Vector2(vertex.X, vertex.Y));
								break;
						}
					}

					if (!Mesher.CanForm2DConvexHull(vertex2d))
					{
						ColliderNode = null;
						GD.Print("Cannot Form 2D ConvexHull " + surfaceId + "_" + patchNumber + " this was a waste of time");
						return;
					}
					else
						is3D = false;
				}

				if ((i == 0) && (j == 0))
				{
					string goName = "Bezier_Collider_";
					if (!is3D)
						goName += "2D_";
					ColliderNode = new Node3D();
					ColliderNode.Name = goName + surfaceId + "_" + patchNumber;
					parent = ColliderNode;
				}

				CollisionShape3D mc = new CollisionShape3D();
				mc.Name = "Collider_" + i + "_" + j + "_collider";
				parent.AddChild(mc);
				ConvexPolygonShape3D convexHull = new ConvexPolygonShape3D();
				convexHull.Points = Mesher.RemoveDuplicatedVectors(vertexCache).ToArray();
				mc.Shape = convexHull;
			}
		}
	}
	public MeshInstance3D mesh { get; }

	public Node3D ColliderNode { get; set; }
	//Check Collinear
	private static bool ArePointsCollinear(List<Vector3> points)
	{
		const float EPSILON = 0.00001f;

		if (points.Count < 3)
		{
			// Cannot have collinear points with less than 3 points
			return false;
		}

		Vector3 firstDirection = points[1] - points[0];
		for (int i = 2; i < points.Count; i++)
		{
			Vector3 currentDirection = points[i] - points[0];

			if (firstDirection.Cross(currentDirection).LengthSquared() > EPSILON)
			{
				// The cross product of the two vectors is non-zero, meaning they are not collinear
				return false;
			}
		}

		// All the points are collinear
		return true;
	}

	Quaternion CalculateRotation(Vector3 normal1, Vector3 normal2)
	{
		float dotProduct = normal1.Dot(normal2);
		float angle =  Mathf.RadToDeg(Mathf.Acos(dotProduct));

		Vector3 crossProduct = normal1.Cross(normal2);
		Vector3 axis = crossProduct.Normalized();

		return new Quaternion(axis, angle);
	}
	private Vector2 BezCurveUV(float t, Vector2 p0, Vector2 p1, Vector2 p2)
	{
		float[] tPoints = new float[2];

		float a = 1f - t;
		float tt = t * t;

		for (int i = 0; i < 2; i++)
			tPoints[i] = a * a * p0[i] + 2 * a * (t * p1[i]) + tt * p2[i];

		Vector2 bezPoint = new Vector2(tPoints[0], tPoints[1]);

		return bezPoint;
	}

	// This time for colors
	private Color BezCurveColor(float t, Color p0, Color p1, Color p2)
	{
		float[] tPoints = new float[4];

		float a = 1f - t;
		float tt = t * t;

		for (int i = 0; i < 3; i++)
			tPoints[i] = a * a * p0[i] + 2 * a * (t * p1[i]) + tt * p2[i];

		Color bezPoint = new Color(tPoints[0], tPoints[1], tPoints[2], tPoints[3]);

		return bezPoint;
	}

	// Calculate a vector3 at point t on a quadratic Bezier curve between
	// Using the formula B(t) = (1-t)^2 * p0 + 2 * (1-t) * t * p1 + t^2 * p2
	// p0 and p2 via p1.  
	private Vector3 BezCurve(float t, Vector3 p0, Vector3 p1, Vector3 p2)
	{
		float[] tPoints = new float[3];

		float a = 1f - t;
		float tt = t * t;

		for (int i = 0; i < 3; i++)
			tPoints[i] = a * a * p0[i] + 2 * a * (t * p1[i]) + tt * p2[i];

		Vector3 bezPoint = new Vector3(tPoints[0], tPoints[1], tPoints[2]);

		return bezPoint;
	}

	// This takes a tessellation level and three vector3
	// p0 is start, p1 is the midpoint, p2 is the endpoint
	// The returned list begins with p0, ends with p2, with
	// the tessellated verts in between.
	private void Tessellate(int level, Vector3 p0, Vector3 p1, Vector3 p2, List<Vector3> appendList = null)
	{
		if (appendList == null)
			appendList = new List<Vector3>(level + 1);

		float stepDelta = 1.0f / level;
		float step = stepDelta;

		appendList.Add(p0);
		for (int i = 0; i < level - 1; i++)
		{
			appendList.Add(BezCurve(step, p0, p1, p2));
			step += stepDelta;
		}
		appendList.Add(p2);
	}

	// Same as above, but for UVs
	private void TessellateUV(int level, Vector2 p0, Vector2 p1, Vector2 p2, List<Vector2> appendList = null)
	{
		if (appendList == null)
			appendList = new List<Vector2>(level + 2);

		float stepDelta = 1.0f / level;
		float step = stepDelta;

		appendList.Add(p0);
		for (int i = 0; i < level - 1; i++)
		{
			appendList.Add(BezCurveUV(step, p0, p1, p2));
			step += stepDelta;
		}
		appendList.Add(p2);
	}

	// Same, but this time for colors
	private void TessellateColor(int level, Color p0, Color p1, Color p2, List<Color> appendList = null)
	{
		if (appendList == null)
			appendList = new List<Color>(level + 1);

		float stepDelta = 1.0f / level;
		float step = stepDelta;

		appendList.Add(p0);
		for (int i = 0; i < level - 1; i++)
		{
			appendList.Add(BezCurveColor(step, p0, p1, p2));
			step += stepDelta;
		}
		appendList.Add(p2);
	}
}
