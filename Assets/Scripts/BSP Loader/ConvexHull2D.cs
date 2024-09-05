/* MIT License

Copyright (c) 2020 Erik Nordeus

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


erik.nordeus@gmail.com
https://github.com/Habrador/Computational-geometry
*/

using Godot;
using System.Collections.Generic;
using System.Linq;
public static class ConvexHull2D
{
	public static float GetPointInRelationToVectorValue(Vector2 a, Vector2 b, Vector2 p)
	{
		float x1 = a.X - p.X;
		float x2 = a.Y - p.Y;
		float y1 = b.X - p.X;
		float y2 = b.Y - p.Y;

		float determinant = x1 * y2 - y1 * x2;

		return determinant;
	}

	public static int IsPoint_Left_On_Right_OfVector(Vector2 a, Vector2 b, Vector2 p)
	{
		float relationValue = GetPointInRelationToVectorValue(a, b, p);
		float epsilon = Mathf.Epsilon;

		if (relationValue < -epsilon)
			return 1;
		else if (relationValue > epsilon)
			return -1;
		else
			return 0;
	}
	public static List<Vector2> GenerateConvexHull(List<Vector2> points)
	{
		List<Vector2> pointsOnConvexHull = new List<Vector2>();
		Vector2 startPos = points[0];

		for (int i = 1; i < points.Count; i++)
		{
			Vector2 testPos = points[i];
			if (testPos.X < startPos.X || ((Mathf.Abs(testPos.X - startPos.X) < Mathf.Epsilon && testPos.Y < startPos.Y)))
				startPos = points[i];
		}

		pointsOnConvexHull.Add(startPos);
		Vector2 previousPoint = pointsOnConvexHull[0];
		int counter = 0;
		for (;  counter < 100000; counter++)
		{
			List<Vector2> pointsToAddToTheHull = new List<Vector2>();
			Vector2 nextPoint = points[GD.RandRange(0, points.Count - 1)];
			if (previousPoint.Equals(pointsOnConvexHull[0]) && nextPoint.Equals(pointsOnConvexHull[0]))
				continue;
			

			pointsToAddToTheHull.Add(nextPoint);
			for (int i = 0; i < points.Count; i++)
			{
				Vector2 testPoint = points[i];

				if (testPoint.Equals(nextPoint) || testPoint.Equals(previousPoint))
					continue;

				int pointRelation = IsPoint_Left_On_Right_OfVector(previousPoint, nextPoint, testPoint);

				if (pointRelation == 0)
					pointsToAddToTheHull.Add(testPoint);
				else if (pointRelation == 1)
				{
					nextPoint = testPoint;
					pointsToAddToTheHull.Clear();
					pointsToAddToTheHull.Add(nextPoint);
				}
			}
			pointsToAddToTheHull = pointsToAddToTheHull.OrderBy(n => (n - previousPoint).LengthSquared()).ToList();
			pointsOnConvexHull.AddRange(pointsToAddToTheHull);
			previousPoint = pointsOnConvexHull[pointsOnConvexHull.Count - 1];
			if (previousPoint.Equals(pointsOnConvexHull[0]))
			{
				pointsOnConvexHull.RemoveAt(pointsOnConvexHull.Count - 1);
				break;
			}
		}
		if (counter == 100000)
		{
			GameManager.Print("Stuck in Endless Loop when generating 2d Convex Hull with Jarvis March", GameManager.PrintType.Warning);
			return new List<Vector2>();
		}

		return pointsOnConvexHull;
	}
}
