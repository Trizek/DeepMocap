using UnityEngine;

namespace DeepMocap
{
    public struct AffineTransform
    {
        public Vector3 position;
        public Quaternion rotation;

        public AffineTransform(Transform transform)
        {
            position = transform.localPosition;
            rotation = transform.localRotation;
        }

        public static implicit operator AffineTransform(Transform transform) => new AffineTransform(transform);

        public static AffineTransform Identity = new AffineTransform()
        {
            position = Vector3.zero,
            rotation = Quaternion.identity
        };

        public AffineTransform Inverse
        {
            get
            {
                Quaternion invRotation = Quaternion.Inverse(rotation);
                return new AffineTransform()
                {
                    position = invRotation * (-position),
                    rotation = invRotation
                };
            }
        }

        public static AffineTransform operator *(AffineTransform a, AffineTransform b) => new AffineTransform()
        {
            position = a.position + a.rotation * b.position,
            rotation = a.rotation * b.rotation
        };

        public static Vector3 operator *(AffineTransform a, Vector3 position) => a.position + a.rotation * position;

        public void DebugDraw(float axisLength = 1.0f)
        {
            Debug.DrawLine(position, position + rotation * Vector3.right * axisLength, Color.red);
            Debug.DrawLine(position, position + rotation * Vector3.up * axisLength, Color.green);
            Debug.DrawLine(position, position + rotation * Vector3.forward * axisLength, Color.blue);
        }
    }
}