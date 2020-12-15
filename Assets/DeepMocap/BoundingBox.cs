using UnityEngine;

namespace DeepMocap
{
    public struct BoundingBox
    {
        public Vector3 min;
        public Vector3 max;

        public Vector3 Center => (min + max) * 0.5f;

        public float Width => max.x - min.x;

        public float Height => max.y - min.y;

        public Vector3[] Corners
        {
            get
            {
                return new Vector3[]{
                new Vector3(min.x, min.y, 0.0f),
                new Vector3(max.x, min.y, 0.0f),
                new Vector3(max.x, max.y, 0.0f),
                new Vector3(min.x, max.y, 0.0f)
            };
            }
        }

        public static BoundingBox Create(Vector3[] joints)
        {
            Vector3 min = joints[0];
            min.z = joints[(int)JointIndex.Hips].z;
            Vector3 max = min;

            for (int i = 1; i < joints.Length; ++i)
            {
                var point = joints[i];
                min.x = Mathf.Min(min.x, point.x);
                min.y = Mathf.Min(min.y, point.y);

                max.x = Mathf.Max(max.x, point.x);
                max.y = Mathf.Max(max.y, point.y);
            }

            return new BoundingBox()
            {
                min = min,
                max = max
            };
        }

        public void DebugDraw() => DebugDraw(AffineTransform.Identity, Color.black);

        public void DebugDraw(AffineTransform transform, Color color)
        {
            var corners = Corners;

            Debug.DrawLine(transform * corners[0], transform * corners[1], color);
            Debug.DrawLine(transform * corners[1], transform * corners[2], color);
            Debug.DrawLine(transform * corners[2], transform * corners[3], color);
            Debug.DrawLine(transform * corners[3], transform * corners[0], color);
        }
    }
}