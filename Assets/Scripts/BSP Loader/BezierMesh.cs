using Godot;
using System.Collections.Generic;
using ExtensionMethods;
public static class BezierMesh
{
	private static List<Vector3> vertsCache = new List<Vector3>();
	private static List<Vector2> uvCache = new List<Vector2>();
	private static List<Vector2> uv2Cache = new List<Vector2>();
	private static List<Vector3> normalsCache = new List<Vector3>();
	private static List<Color> vertsColor = new List<Color>();
	private static List<int> indiciesCache = new List<int>();

	private const int VertexInd = (int)Mesh.ArrayType.Vertex;
	private const int TexUVInd = (int)Mesh.ArrayType.TexUV;
	private const int TexUV2Ind = (int)Mesh.ArrayType.TexUV2;
	private const int ColorInd = (int)Mesh.ArrayType.Color;
	private const int TriIndex = (int)Mesh.ArrayType.Index;

	private static List<Vector3> vertsLocalCache = new List<Vector3>();
	private static List<Vector2> uvLocalCache = new List<Vector2>();
	private static List<Vector2> uv2LocalCache = new List<Vector2>();
	private static List<Color> vertsLocalColor = new List<Color>();
	private static List<int> indiciesLocalCache = new List<int>();

	private static List<Vector3> p0sCache = new List<Vector3>();
	private static List<Vector2> p0suvLocalCache = new List<Vector2>();
	private static List<Vector2> p0suv2LocalCache = new List<Vector2>();
	private static List<Color> p0svertsLocalColor = new List<Color>();

	private static List<Vector3> p1sCache = new List<Vector3>();
	private static List<Vector2> p1suvLocalCache = new List<Vector2>();
	private static List<Vector2> p1suv2LocalCache = new List<Vector2>();
	private static List<Color> p1svertsLocalColor = new List<Color>();

	private static List<Vector3> p2sCache = new List<Vector3>();
	private static List<Vector2> p2suvLocalCache = new List<Vector2>();
	private static List<Vector2> p2suv2LocalCache = new List<Vector2>();
	private static List<Color> p2svertsLocalColor = new List<Color>();
	public enum Axis
	{
		None,
		X,
		Y,
		Z
	}
	public static void ClearCaches()
	{
		vertsCache = new List<Vector3>();
		uvCache = new List<Vector2>();
		uv2Cache = new List<Vector2>();
		normalsCache = new List<Vector3>();
		vertsColor = new List<Color>();
		indiciesCache = new List<int>();
	}
	public static void GenerateBezierMesh(int level, List<Vector3> control, List<Vector2> controlUvs, List<Vector2> controlUv2s, List<Color> controlColor, ref int offset)
	{
		// We'll use these two to hold our verts, tris, and uvs
		int capacity = level * level + (2 * level);
		if (vertsLocalCache.Capacity < capacity)
		{
			vertsLocalCache.Capacity = capacity;
			uvLocalCache.Capacity = capacity;
			uv2LocalCache.Capacity = capacity;
			indiciesLocalCache.Capacity = capacity;
			vertsLocalColor.Capacity = capacity;
		}

		if (offset == 0)
		{
			vertsCache.Clear();
			uvCache.Clear();
			uv2Cache.Clear();
			normalsCache.Clear();
			vertsColor.Clear();
			indiciesCache.Clear();
		}

		vertsLocalCache.Clear();
		uvLocalCache.Clear();
		uv2LocalCache.Clear();
		indiciesLocalCache.Clear();
		vertsLocalColor.Clear();


		p0sCache.Clear();
		p0suvLocalCache.Clear();
		p0suv2LocalCache.Clear();
		p0svertsLocalColor.Clear();

		p1sCache.Clear();
		p1suvLocalCache.Clear();
		p1suv2LocalCache.Clear();
		p1svertsLocalColor.Clear();

		p2sCache.Clear();
		p2suvLocalCache.Clear();
		p2suv2LocalCache.Clear();
		p2svertsLocalColor.Clear();

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
		TessellateUV(level, controlUvs[0], controlUvs[3], controlUvs[6], p0suvLocalCache);
		TessellateUV(level, controlUv2s[0], controlUv2s[3], controlUv2s[6], p0suv2LocalCache);
		TessellateColor(level, controlColor[0], controlColor[3], controlColor[6], p0svertsLocalColor);

		Tessellate(level, control[1], control[4], control[7], p1sCache);
		TessellateUV(level, controlUvs[1], controlUvs[4], controlUvs[7], p1suvLocalCache);
		TessellateUV(level, controlUv2s[1], controlUv2s[4], controlUv2s[7], p1suv2LocalCache);
		TessellateColor(level, controlColor[1], controlColor[4], controlColor[7], p1svertsLocalColor);

		Tessellate(level, control[2], control[5], control[8], p2sCache);
		TessellateUV(level, controlUvs[2], controlUvs[5], controlUvs[8], p2suvLocalCache);
		TessellateUV(level, controlUv2s[2], controlUv2s[5], controlUv2s[8], p2suv2LocalCache);
		TessellateColor(level, controlColor[2], controlColor[5], controlColor[8], p2svertsLocalColor);

		// Tessellate all those new sets of control points and pack
		// all the results into our vertex array, which we'll return.
		for (int i = 0; i <= level; i++)
		{
			Tessellate(level, p0sCache[i], p1sCache[i], p2sCache[i], vertsLocalCache);
			TessellateUV(level, p0suvLocalCache[i], p1suvLocalCache[i], p2suvLocalCache[i], uvLocalCache);
			TessellateUV(level, p0suv2LocalCache[i], p1suv2LocalCache[i], p2suv2LocalCache[i], uv2LocalCache);
			TessellateColor(level, p0svertsLocalColor[i], p1svertsLocalColor[i], p2svertsLocalColor[i], vertsLocalColor);
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
				indiciesLocalCache.Add(i);
				indiciesLocalCache.Add(i + width);
				indiciesLocalCache.Add(i + 1);

				xStep++;
			}
			else if (xStep == width) //on right edge
			{
				indiciesLocalCache.Add(i);
				indiciesLocalCache.Add(i + (width - 1));
				indiciesLocalCache.Add(i + width);

				xStep = 1;
			}
			else // not on an edge, so add two
			{
				indiciesLocalCache.Add(i);
				indiciesLocalCache.Add(i + (width - 1));
				indiciesLocalCache.Add(i + width);


				indiciesLocalCache.Add(i);
				indiciesLocalCache.Add(i + width);
				indiciesLocalCache.Add(i + 1);

				xStep++;
			}
		}

		vertsCache.AddRange(vertsLocalCache);
		uvCache.AddRange(uvLocalCache);
		uv2Cache.AddRange(uv2LocalCache);
		vertsColor.AddRange(vertsLocalColor);

		int indicies = indiciesLocalCache.Count;
		for (int i = 0; i < indicies; i++)
			indiciesCache.Add(indiciesLocalCache[i] + offset);

		offset += vertsLocalCache.Count;
	}

	public static void BezierColliderMesh(uint ownerShapeId, CollisionObject3D collider, int surfaceId, int patchNumber, List<Vector3> control)
	{
		const int colliderTessellations = 4;  //Do not modify

		float step, s, f, m;
		int iterOne = colliderTessellations, interTwo = colliderTessellations;
		bool Collinear, allCollinear = false;

		// We'll use these two to hold our verts
		int capacity = control.Count;
		if (vertsLocalCache.Capacity < capacity)
			vertsLocalCache.Capacity = capacity;

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
				vertsLocalCache.Clear();
				for (int i = 0; i < 3; i++)
					vertsLocalCache.Add(control[(3 * i) + j]);
				allCollinear &= ArePointsCollinear(vertsLocalCache);
				if (!allCollinear)
					break;
			}
			if (allCollinear)
			{
				interTwo = 1;
				vertsLocalCache.Clear();
				for (int i = 0; i < capacity; i++)
					vertsLocalCache.Add(control[i]);
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
					vertsLocalCache.Clear();

					//Top row
					vertsLocalCache.Add(BezCurve(s, p0sCache[0], p1sCache[0], p2sCache[0]));
					vertsLocalCache.Add(BezCurve(m, p0sCache[0], p1sCache[0], p2sCache[0]));
					vertsLocalCache.Add(BezCurve(f, p0sCache[0], p1sCache[0], p2sCache[0]));

					//Middle row
					vertsLocalCache.Add(BezCurve(s, p0sCache[1], p1sCache[1], p2sCache[1]));
					vertsLocalCache.Add(BezCurve(m, p0sCache[1], p1sCache[1], p2sCache[1]));
					vertsLocalCache.Add(BezCurve(f, p0sCache[1], p1sCache[1], p2sCache[1]));

					//Bottom row
					vertsLocalCache.Add(BezCurve(s, p0sCache[2], p1sCache[2], p2sCache[2]));
					vertsLocalCache.Add(BezCurve(m, p0sCache[2], p1sCache[2], p2sCache[2]));
					vertsLocalCache.Add(BezCurve(f, p0sCache[2], p1sCache[2], p2sCache[2]));
				}
				Vector3 normal = Vector3.Zero;
				List<Vector3> vertsCleanLocalCache = Mesher.RemoveDuplicatedVectors(vertsLocalCache);
				if (!Mesher.CanForm3DConvexHull(vertsCleanLocalCache, ref normal, 0.00015f))
				{
					if (normal.LengthSquared() == 0)
					{
						GameManager.Print("BezierColliderMesh: Cannot Form 2D/3D ConvexHull " + surfaceId + "_" + patchNumber + " this was a waste of time", GameManager.PrintType.Warning);
						return;
					}
					Axis axis;
					Quaternion changeRotation = Quaternion.Identity;
					List<Vector2> vertex2d = new List<Vector2>();
					if ((normal.X == 1) || (normal.X == -1))
						axis = Axis.X;
					else if ((normal.Y == 1) || (normal.Y == -1))
						axis = Axis.Y;
					else if ((normal.Z == 1) || (normal.Z == -1))
						axis = Axis.Z;
					else
					{
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
						changeRotation.CalculateRotation(normal, normalRef);
					}
					//Check if it's a 2D Surface
					float Offset = 0;
					for (int k = 0; k < vertsCleanLocalCache.Count; k++)
					{
						Vector3 vertex = changeRotation * vertsCleanLocalCache[k];
						switch (axis)
						{
							case Axis.X:
								vertex2d.Add(new Vector2(vertex.Y, vertex.Z));
								Offset += vertex.X;
							break;
							case Axis.Y:
								vertex2d.Add(new Vector2(vertex.X, vertex.Z));
								Offset += vertex.Y;
							break;
							case Axis.Z:
								vertex2d.Add(new Vector2(vertex.X, vertex.Y));
								Offset += vertex.Z;
							break;
						}
					}
					Offset /= vertsCleanLocalCache.Count;
					vertex2d = ConvexHull2D.GenerateConvexHull(vertex2d);
					if (vertex2d.Count == 0)
					{
						GameManager.Print("BezierColliderMesh: Cannot Form 2D ConvexHull " + surfaceId + "_" + patchNumber + " this was a waste of time", GameManager.PrintType.Warning);
						return;
					}
					changeRotation = changeRotation.Inverse();
					vertsCleanLocalCache.Clear();
					for (int k = 0; k < vertex2d.Count; k++)
					{
						Vector3 vertex3d;
						switch (axis)
						{
							default:
							case Axis.X:
								vertex3d = new Vector3(Offset, vertex2d[k].X, vertex2d[k].Y);
							break;
							case Axis.Y:
								vertex3d = new Vector3(vertex2d[k].X, Offset, vertex2d[k].Y);
							break;
							case Axis.Z:
								vertex3d = new Vector3(vertex2d[k].X, vertex2d[k].Y, Offset);
							break;
						}
						vertex3d = changeRotation * vertex3d;
						vertsCleanLocalCache.Add(vertex3d);
					}
					vertsCleanLocalCache = Mesher.GetExtrudedVerticesFromPoints(Mesher.RemoveDuplicatedVectors(vertsCleanLocalCache), normal);
				}

				ConvexPolygonShape3D convexHull = new ConvexPolygonShape3D();
				convexHull.Points = vertsCleanLocalCache.ToArray();
				collider.ShapeOwnerAddShape(ownerShapeId, convexHull);
			}
		}

		return;
	}
	public static void FinalizeBezierMesh(ArrayMesh arrMesh)
	{
		// The mesh we're building
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		// Add the verts and tris
		surfaceArray[VertexInd] = vertsCache.ToArray();
		surfaceArray[TexUVInd] = uvCache.ToArray();
		surfaceArray[TexUV2Ind] = uv2Cache.ToArray();
		surfaceArray[ColorInd] = vertsColor.ToArray();
		surfaceArray[TriIndex] = indiciesCache.ToArray();

		// Create the Mesh.
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

		// Tool needed to recalculate normals .
		SurfaceTool st = new SurfaceTool();
		st.CreateFrom(arrMesh, 0);
		st.GenerateNormals();
		st.GenerateTangents();
		arrMesh.ClearSurfaces();
		surfaceArray.Clear();
		surfaceArray = st.CommitToArrays();
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
	}
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

	private static Vector2 BezCurveUV(float t, Vector2 p0, Vector2 p1, Vector2 p2)
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
	private static Color BezCurveColor(float t, Color p0, Color p1, Color p2)
	{
		float[] tPoints = new float[4];

		float a = 1f - t;
		float tt = t * t;

		for (int i = 0; i < 4; i++)
			tPoints[i] = a * a * p0[i] + 2 * a * (t * p1[i]) + tt * p2[i];

		Color bezPoint = new Color(tPoints[0], tPoints[1], tPoints[2], tPoints[3]);

		return bezPoint;
	}

	// Calculate a vector3 at point t on a quadratic Bezier curve between
	// Using the formula B(t) = (1-t)^2 * p0 + 2 * (1-t) * t * p1 + t^2 * p2
	// p0 and p2 via p1.  
	private static Vector3 BezCurve(float t, Vector3 p0, Vector3 p1, Vector3 p2)
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
	private static void Tessellate(int level, Vector3 p0, Vector3 p1, Vector3 p2, List<Vector3> appendList = null)
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
	private static void TessellateUV(int level, Vector2 p0, Vector2 p1, Vector2 p2, List<Vector2> appendList = null)
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
	private static void TessellateColor(int level, Color p0, Color p1, Color p2, List<Color> appendList = null)
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
