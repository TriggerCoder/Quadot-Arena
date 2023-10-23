using Godot;
using System;

namespace ExtensionMethods
{
	public static class Transform3dExtension
	{
		//https://gamedev.stackexchange.com/questions/203073/how-to-convert-a-4x4-matrix-transformation-to-another-coordinate-system
		//We want to map +x to -x (-1 ,  0 ,  0)
		//We want to map +y to +z ( 0 ,  0 ,  1)
		//We want to map +z to +y ( 0 ,  1 ,  0)
		//We want the fourth, homogenous coordinate to survive unchanged (0, 0, 0, 1)
		//If we left-multiply this matrix by any homogeneous vector in our old coordinate system,
		//it converts it to the corresponding vector in the new coordinate system:
		//Vnew = T*Vold
		public static Transform3D QuakeToGodotConversion(this Transform3D transform3D)
		{
			Vector3 column0 = new Vector3(-1, 0, 0);
			Vector3 column1 = new Vector3(0, 0, 1);
			Vector3 column2 = new Vector3(0, 1, 0);
			Vector3 column3 = new Vector3(0, 0, 0);

			return new Transform3D(column0, column1, column2, column3);
		}

		//http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
		public static Quaternion ExtractRotation(this Transform3D transform3D)
		{
			Quaternion rotation = new Quaternion();
			rotation.W = Mathf.Sqrt(Mathf.Max(0, 1 + transform3D[0,0] + transform3D[1,1] + transform3D[2,2])) / 2;
			rotation.X = Mathf.Sqrt(Mathf.Max(0, 1 + transform3D[0,0] - transform3D[1,1] - transform3D[2,2])) / 2;
			rotation.Y = Mathf.Sqrt(Mathf.Max(0, 1 - transform3D[0,0] + transform3D[1,1] - transform3D[2,2])) / 2;
			rotation.Z = Mathf.Sqrt(Mathf.Max(0, 1 - transform3D[0,0] - transform3D[1,1] + transform3D[2,2])) / 2;
			rotation.X *= Mathf.Sign(rotation.X * (transform3D[1,2] - transform3D[2,1]));
			rotation.Y *= Mathf.Sign(rotation.Y * (transform3D[2,0] - transform3D[0,2]));
			rotation.Z *= Mathf.Sign(rotation.Z * (transform3D[0,1] - transform3D[1,0]));

			return rotation;
		}
		public static Vector3 ExtractPosition(this Transform3D transform3D)
		{
			Vector3 position;
			position.X = transform3D[3,0];
			position.Y = transform3D[3,1];
			position.Z = transform3D[3,2];
			return position;
		}

		public static Vector3 ExtractScale(this Transform3D transform3D)
		{
			Vector3 scale;
			scale.X = new Vector4(transform3D[0,0], transform3D[0,1], transform3D[0,2], transform3D[0,3]).Length();
			scale.Y = new Vector4(transform3D[1,0], transform3D[1,1], transform3D[1,2], transform3D[1,3]).Length();
			scale.Z = new Vector4(transform3D[2,0], transform3D[2,1], transform3D[2,2], transform3D[2,3]).Length();
			return scale;
		}
	}
}