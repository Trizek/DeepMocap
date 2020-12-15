using UnityEngine;

namespace DeepMocap
{
    public enum JointValidity
    {
        Invalid = 0,
        StraightAngle,
        LowConfidence,
        Valid
    }

    public struct Pose
    {
        public AffineTransform[] transforms;
        public JointValidity[] validity;

        public Pose(int jointCount)
        {
            transforms = new AffineTransform[jointCount];
            validity = new JointValidity[jointCount];

            for (int i = 0; i < jointCount; ++i)
            {
                transforms[i] = AffineTransform.Identity;
                validity[i] = JointValidity.Invalid;
            }
        }

        public static Pose CreateGlobalPoseFromTransforms(Transform[] transforms)
        {
            Pose globalPose = new Pose((int)JointIndex.Count);
            for (int i = 0; i < (int)JointIndex.Count; ++i)
            {
                globalPose.transforms[i] = new AffineTransform() { position = transforms[i].position, rotation = transforms[i].rotation };
                globalPose.validity[i] = JointValidity.Valid;
            }

            return globalPose;
        }

        // Create a global pose in mocap space, i.e. with rotation conventions of DeepMocap
        public static Pose CreateGlobalPoseFromPointPose(PointPose pointPose, JointValidity defaultJointValidity = JointValidity.Valid)
        {
            Pose pose = new Pose((int)JointIndex.Count);

            for (int i = 0; i < (int)JointIndex.Count; ++i)
            {
                pose.transforms[i].position = pointPose.points[i];
                pose.validity[i] = defaultJointValidity;
            }

            void SetLookForwardUp(ref Pose p, JointIndex jointIndex, Vector3 forward, Vector3 up)
            {
                if (forward.magnitude < Mathf.Epsilon || up.magnitude < Mathf.Epsilon)
                {
                    p.validity[(int)jointIndex] = JointValidity.StraightAngle;
                    return;
                }

                if (Vector3.Cross(forward, up).magnitude < Mathf.Epsilon)
                {
                    p.validity[(int)jointIndex] = JointValidity.StraightAngle;
                    return;
                }

                p.SetRotation(jointIndex, Quaternion.LookRotation(forward, up));
            }

            void SetLookRightUp(ref Pose p, JointIndex jointIndex, Vector3 right, Vector3 up)
            {
                Vector3 forward = Vector3.Cross(right, up);
                SetLookForwardUp(ref p, jointIndex, forward, up);
            }

            void SetLookForwardRight(ref Pose p, JointIndex jointIndex, Vector3 forward, Vector3 right)
            {
                Vector3 up = Vector3.Cross(forward, right);
                SetLookForwardUp(ref p, jointIndex, forward, up);
            }

            void ComputeLimbRotations(ref PointPose ptPose, ref Pose trPose, JointIndex rootIndex, JointIndex midIndex, JointIndex tipIndex)
            {
                Vector3 root = ptPose[rootIndex];
                Vector3 mid = ptPose[midIndex];
                Vector3 tip = ptPose[tipIndex];

                Vector3 upperLimb = mid - root;
                Vector3 lowerLimb = tip - mid;

                Vector3 right = Vector3.Cross(upperLimb, lowerLimb);

                SetLookForwardRight(ref trPose, rootIndex, upperLimb, right);
                SetLookForwardRight(ref trPose, midIndex, lowerLimb, right);
            }

            Vector3 hipsRight = pointPose[JointIndex.RightHip] - pointPose[JointIndex.LeftHip];
            Vector3 hipsUp = pointPose[JointIndex.Spine] - pointPose[JointIndex.Hips];
            SetLookRightUp(ref pose, JointIndex.Hips, hipsRight, hipsUp);

            Vector3 neckRight = pointPose[JointIndex.RightShoulder] - pointPose[JointIndex.LeftShoulder];
            Vector3 neckUp = pointPose[JointIndex.HeadPart2] - pointPose[JointIndex.Neck];
            SetLookRightUp(ref pose, JointIndex.Neck, neckRight, neckUp);

            Vector3 spineUp = pointPose[JointIndex.Neck] - pointPose[JointIndex.Spine];
            spineUp.Normalize();
            Vector3 spineRight = Vector3.Lerp(hipsRight, neckRight, 0.5f);
            spineRight = spineRight - Vector3.Dot(spineRight, spineUp) * spineUp;
            SetLookRightUp(ref pose, JointIndex.Spine, spineRight, spineUp);

            ComputeLimbRotations(ref pointPose, ref pose, JointIndex.RightHip, JointIndex.RightKnee, JointIndex.RightFoot);
            ComputeLimbRotations(ref pointPose, ref pose, JointIndex.LeftHip, JointIndex.LeftKnee, JointIndex.LeftFoot);

            ComputeLimbRotations(ref pointPose, ref pose, JointIndex.RightShoulder, JointIndex.RightElbow, JointIndex.RightHand);
            ComputeLimbRotations(ref pointPose, ref pose, JointIndex.LeftShoulder, JointIndex.LeftElbow, JointIndex.LeftHand);

            Vector3 headUp = pointPose[JointIndex.HeadPart2] - pointPose[JointIndex.Neck];
            Vector3 headRight = Vector3.Cross(pointPose[JointIndex.HeadPart2] - pointPose[JointIndex.HeadPart1], pointPose[JointIndex.HeadPart1] - pointPose[JointIndex.Neck]);
            SetLookRightUp(ref pose, JointIndex.HeadPart1, headRight, headUp);
            SetLookRightUp(ref pose, JointIndex.HeadPart2, headRight, headUp);

            return pose;
        }

        public ref AffineTransform this[JointIndex index]
        {
            get => ref transforms[(int)index];
        }

        void SetRotation(JointIndex jointIndex, Quaternion rotation)
        {
            transforms[(int)jointIndex].rotation = rotation;
            validity[(int)jointIndex] = JointValidity.Valid;
        }

        public Pose ToLocalSpace()
        {
            Pose pose = DeepCopy();

            for (int i = 0; i < (int)JointIndex.Count; ++i)
            {
                int parentIndex = PointPose.Parents[i];
                if (parentIndex < 0)
                {
                    continue;
                }

                AffineTransform globalParent = transforms[parentIndex];
                pose.transforms[i] = globalParent.Inverse * transforms[i];
            }

            return pose;
        }

        public Pose ToGlobalSpace()
        {
            Pose pose = DeepCopy();

            for (int i = 0; i < (int)JointIndex.Count; ++i)
            {
                int parentIndex = PointPose.Parents[i];
                if (parentIndex < 0)
                {
                    continue;
                }

                AffineTransform globalParent = pose.transforms[parentIndex];
                pose.transforms[i] = globalParent * transforms[i];
            }

            return pose;
        }

        public static Pose operator *(Pose a, Pose b)
        {
            Pose pose = a.DeepCopy();

            for (int i = 0; i <= (int)JointIndex.HeadPart1; ++i)
            {
                if (b.validity[i] == JointValidity.Valid)
                {
                    pose.transforms[i] = a.transforms[i] * b.transforms[i];
                }
                else if (a.validity[i] == JointValidity.Valid)
                {
                    pose.validity[i] = b.validity[i];
                }
            }

            return pose;
        }

        public Pose GetInverse()
        {
            Pose pose = DeepCopy();

            for (int i = 0; i <= (int)JointIndex.HeadPart1; ++i)
            {
                pose.transforms[i] = transforms[i].Inverse;
            }

            return pose;
        }

        public Pose RotationOnly()
        {
            Pose pose = DeepCopy();

            for (int i = 0; i <= (int)JointIndex.HeadPart1; ++i)
            {
                pose.transforms[i].position = Vector3.zero;
            }

            return pose;
        }

        public Pose DeepCopy()
        {
            Pose pose = new Pose(transforms.Length);

            for (int i = 0; i < transforms.Length; ++i)
            {
                pose.transforms[i] = transforms[i];
                pose.validity[i] = validity[i];
            }

            return pose;
        }

        public void DebugDraw(AffineTransform transform, float transformsAxisLength = 1.0f)
        {
            PointPose pointPose = new PointPose(this);
            pointPose.DebugDraw(transform);

            for (int i = 0; i < (int)JointIndex.Count; ++i)
            {
                transforms[i].DebugDraw(transformsAxisLength);
            }
        }

        public Pose InterpolateToward(Pose toPose, float ratio)
        {
            Pose pose = DeepCopy();

            for (int i = 0; i < (int)JointIndex.Count; ++i)
            {
                pose.transforms[i].position = Vector3.Lerp(transforms[i].position, transforms[i].position, ratio);
                pose.transforms[i].rotation = Quaternion.Slerp(transforms[i].rotation, transforms[i].rotation, ratio);
            }

            return pose;
        }
    }
}