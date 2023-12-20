using Godot;

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
}
