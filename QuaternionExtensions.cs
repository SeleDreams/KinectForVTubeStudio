using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LumiosNoctis
{
    public static class QuaternionMethods
    {
        public static void ToYawPitchRoll(this Vector4 quaternion, out float yaw, out float pitch, out float roll)
        {
            // Roll (rotation around z-axis)
            float sinr_cosp = 2 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            float cosr_cosp = 1 - 2 * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
            roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // Pitch (rotation around y-axis)
            float sinp = 2 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
            if (Math.Abs(sinp) >= 1)
                pitch = (float)Math.Sign(sinp) * (float)Math.PI / 2; // use 90 degrees if out of range
            else
                pitch = (float)Math.Asin(sinp);

            // Yaw (rotation around x-axis)
            float siny_cosp = 2 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            float cosy_cosp = 1 - 2 * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);
            yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

            // Convert angles from radians to degrees if needed
            yaw = ToDegrees(yaw);
            pitch = ToDegrees(pitch);
            roll = ToDegrees(roll);
        }

        // Helper function to convert angles from radians to degrees
        private static float ToDegrees(float radians)
        {
            return radians * (180.0f / (float)Math.PI);
        }
    }
}
