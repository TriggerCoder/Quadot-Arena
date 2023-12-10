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
}
