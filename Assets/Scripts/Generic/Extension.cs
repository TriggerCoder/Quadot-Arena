using Godot;
using System;

namespace ExtensionMethods
{
	public static class NodeExtensions
	{
		public static void SetForward(this Node3D node3D, Vector3 forward)
		{
			if (Mathf.IsZeroApprox(forward.Dot(Vector3.Forward)))
				node3D.Rotation = Transform3D.Identity.LookingAt(forward, Vector3.Forward).Basis.GetEuler();
			else
				node3D.Rotation = Transform3D.Identity.LookingAt(forward, Vector3.Up).Basis.GetEuler();
		}

		public static Vector3 ForwardVector(this Node3D node3D)
		{
			Vector3 Forward = node3D.Basis.Z;
			if (Forward.IsNormalized())
				return Forward;
			return Forward.Normalized();
		}
		public static Vector3 UpVector(this Node3D node3D)
		{
			Vector3 Up = node3D.Basis.Y;
			if (Up.IsNormalized())
				return Up;
			return Up.Normalized();
		}
		public static Vector3 RightVector(this Node3D node3D)
		{
			Vector3 Right = node3D.Basis.X;
			if (Right.IsNormalized())
				return Right;
			return Right.Normalized();
		}
	}

	public static class QuaternionExtensions
	{
		public static Quaternion FastNormal(this Quaternion quaternion)
		{
			float qmagsq = quaternion.LengthSquared();
			if (Mathf.Abs(1.0 - qmagsq) < 2.107342e-08)
				quaternion *= (2.0f / (1.0f + qmagsq));
			else
				quaternion = quaternion.Normalized();
			return quaternion;
		}

		public static Quaternion CalculateRotation(this Quaternion quaternion, Vector3 normal1, Vector3 normal2)
		{
			float dotProduct = normal1.Dot(normal2);
			float angle = Mathf.RadToDeg(Mathf.Acos(dotProduct));

			Vector3 crossProduct = normal1.Cross(normal2);
			Vector3 axis = crossProduct.Normalized();
			quaternion = new Quaternion(axis, angle);

			return quaternion;
		}
	}

	public static class TransformExtensions
	{
		public static Transform3D Lerp(this Transform3D transform, Transform3D to, float weight)
		{
			Vector3 scale = transform.Basis.Scale;
			Quaternion rotationQuaternion = transform.Basis.GetRotationQuaternion().FastNormal();
			Vector3 origin = transform.Origin;
			Vector3 scale2 = to.Basis.Scale;
			Quaternion rotationQuaternion2 = to.Basis.GetRotationQuaternion().FastNormal();
			Vector3 origin2 = to.Origin;
			Transform3D result = Transform3D.Identity;
			Quaternion quaternion = rotationQuaternion.Slerp(rotationQuaternion2, weight).FastNormal();
			result.Basis = new Basis(quaternion);
			Vector3 scale3 = scale.Lerp(scale2, weight);
			result.Basis.Scaled(scale3);
			result.Origin = origin.Lerp(origin2, weight);
			return result;
		}
		public static Vector3 ForwardVector(this Transform3D transform)
		{
			return transform.Basis.Z;
		}
		public static Vector3 UpVector(this Transform3D transform)
		{
			return transform.Basis.Y;
		}
		public static Vector3 RightVector(this Transform3D transform)
		{
			return transform.Basis.X;
		}
	}

	public static class BasisExtensions
	{
		public static Basis Lerp(this Basis basis, Basis to, float weight)
		{
			Vector3 scale = basis.Scale;
			Quaternion rotationQuaternion = basis.GetRotationQuaternion().FastNormal();
			Vector3 scale2 = to.Scale;
			Quaternion rotationQuaternion2 = to.GetRotationQuaternion().FastNormal();
			Quaternion quaternion = rotationQuaternion.Slerp(rotationQuaternion2, weight).FastNormal();
			Basis result = new Basis(quaternion);
			Vector3 scale3 = scale.Lerp(scale2, weight);
			result.Scaled(scale3);
			return result;
		}
	}

	public static class StringExtensions
	{
		public static string StripExtension(this string String)
		{
			int fileExtPos = String.LastIndexOf(".");
			if (fileExtPos >= 0)
				return String.Substring(0, fileExtPos);
			return String;
		}
		public static string GetStringFromBytes(this string String, byte[] bytes)
		{
			string name;
			char[] nameData = new char[bytes.Length];
			bool fill = true;
			for (int i = 0; i < bytes.Length; i++)
			{
				if (fill)
					nameData[i] = Convert.ToChar(bytes[i]);
				else
				{
					nameData[i] = '\0';
					continue;
				}
				if (nameData[i] == '\0')
					fill = false;
			}
			if (nameData[0] == '\0')
				name = string.Empty;
			else
				name = new string(nameData).Replace("\0", string.Empty);

			return name;
		}
		public static float GetNumValue(this string String)
		{
			int inum = 0;
			float num = 0;
			if (int.TryParse(String, out inum))
				num = inum;
			else
				num = float.Parse(String);
			return num;
		}
	}

	public static class Vector3Extension
	{
		public static Vector3 GetLenghtAndNormalize(this Vector3 vector3, out float num)
		{
			num = vector3.LengthSquared();
			if (num == 0f)
			{
				vector3.X = (vector3.Y = (vector3.Z = 0f));
				return vector3;
			}

			num = Mathf.Sqrt(num);
			vector3.X /= num;
			vector3.Y /= num;
			vector3.Z /= num;

			return vector3;
		}
	}
}
