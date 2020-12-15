
using UnityEngine;

namespace DeepMocap
{
    public struct PointPose
    {
        public Vector3[] points;
        public float[] confidences;

        public static int[] Parents = ComputeParents();

        public PointPose(Vector3[] points)
        {
            this.points = points;
            confidences = CreateDefaultConfidences();
        }

        public PointPose(Vector3[] points, float[] confidences)
        {
            this.points = points;
            this.confidences = confidences;
        }

        public PointPose(Transform[] transforms)
        {
            points = new Vector3[(int)JointIndex.Count];

            for (int i = 0; i < (int)JointIndex.HeadPart1; ++i)
            {
                points[i] = transforms[i].position;
            }

            Vector3 headForward = Vector3.Cross(points[(int)JointIndex.RightShoulder] - points[(int)JointIndex.LeftShoulder], Vector3.up);
            headForward.y = 0.0f;
            headForward.Normalize();

            const float headHalfSize = 0.15f;

            Vector3 headPos = transforms[(int)JointIndex.HeadPart1].position;
            points[(int)JointIndex.HeadPart1] = headPos + headHalfSize * (headForward + Vector3.up);
            points[(int)JointIndex.HeadPart2] = headPos + 2.0f * headHalfSize * Vector3.up;

            confidences = CreateDefaultConfidences();
        }

        public PointPose(Pose pose)
        {
            points = new Vector3[(int)JointIndex.Count];

            for (int i = 0; i < (int)JointIndex.Count; ++i)
            {
                points[i] = pose.transforms[i].position;
            }

            confidences = CreateDefaultConfidences();
        }

        public ref Vector3 this[JointIndex index]
        {
            get => ref points[(int)index];
        }

        public float PoseLength2D
        {
            get
            {
                return ComputeChain2DLength(new JointIndex[] { JointIndex.LeftShoulder, JointIndex.LeftElbow, JointIndex.LeftHand }) +
                    ComputeChain2DLength(new JointIndex[] { JointIndex.RightShoulder, JointIndex.RightElbow, JointIndex.RightHand }) +
                    ComputeChain2DLength(new JointIndex[] { JointIndex.LeftHip, JointIndex.LeftKnee, JointIndex.LeftFoot }) +
                    ComputeChain2DLength(new JointIndex[] { JointIndex.RightHip, JointIndex.RightKnee, JointIndex.RightFoot });
            }
        }

        public float[] BoneLengths2D
        {
            get
            {
                float[] lengths = new float[points.Length];

                for (int i = 0; i < points.Length; ++i)
                {
                    int parent = Parents[i];
                    lengths[i] = parent >= 0 ? Vector2.Distance(points[i], points[parent]) : 0.0f;
                }

                return lengths;
            }
        }

        public float[] BoneLengths
        {
            get
            {
                float[] lengths = new float[points.Length];

                for (int i = 0; i < points.Length; ++i)
                {
                    int parent = Parents[i];
                    lengths[i] = parent >= 0 ? Vector3.Distance(points[i], points[parent]) : 0.0f;
                }

                return lengths;
            }
        }

        public PointPose DeepCopy()
        {
            PointPose pose = new PointPose()
            {
                points = new Vector3[points.Length],
                confidences = new float[points.Length]
            };

            for (int i = 0; i < points.Length; ++i)
            {
                pose.points[i] = points[i];
                pose.confidences[i] = confidences[i];
            }

            return pose;
        }

        public void ScaleAndCenterToMatchReference(ref PointPose refPose, float length2d = -1.0f)
        {
            Vector3 translation = -this[JointIndex.Hips];

            if (length2d < 0.0f)
            {
                length2d = PoseLength2D;
            }

            float scale = refPose.PoseLength2D / length2d;

            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = (points[i] + translation) * scale;
            }
        }

        public void Translate(Vector3 translation)
        {
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = points[i] + translation;
            }
        }

        public void CorrectWith2DProjection(ref PointPose pose2d, float[] boneLengths)
        {
            int numIterations = 10;

            float ratioPerIteration = 0.2f;

            for (int iteration = 0; iteration < numIterations; ++iteration)
            {
                for (int i = 0; i < (int)JointIndex.HeadPart1; ++i)
                {
                    JointIndex jointIndex = (JointIndex)i;

                    if (jointIndex == JointIndex.Neck ||
                        jointIndex == JointIndex.RightHand ||
                        jointIndex == JointIndex.LeftHand ||
                        jointIndex == JointIndex.RightFoot ||
                        jointIndex == JointIndex.LeftFoot)
                    {
                        Vector3 joint2d = pose2d.points[i];
                        Vector3 targetJointPos = new Vector3(joint2d.x, joint2d.y, points[i].z);

                        points[i] = Vector3.Lerp(points[i], targetJointPos, ratioPerIteration);
                    }
                }

                for (int i = 0; i < points.Length; ++i)
                {
                    SetBoneLength(i, boneLengths[i], ratioPerIteration);
                }
            }

            for (int iteration = 0; iteration < numIterations; ++iteration)
            {
                for (int i = 0; i < points.Length; ++i)
                {
                    SetBoneLength(i, boneLengths[i], 0.5f);
                }
            }
        }

        public void SetBoneLength(int jointIndex, float targetLength, float ratio = 1.0f)
        {
            int parent = Parents[jointIndex];
            if (parent < 0)
            {
                return;
            }

            ref Vector3 parentPos = ref points[parent];
            ref Vector3 jointPos = ref points[jointIndex];

            Vector3 dir = jointPos - parentPos;
            float curLength = dir.magnitude;
            dir = dir / curLength;

            float length = Mathf.Lerp(curLength, targetLength, ratio);

            jointPos = parentPos + dir * length;
        }

        public void DebugDraw() => DebugDraw(AffineTransform.Identity);

        public void DebugDraw(AffineTransform transform)
        {
            DrawChain(transform,
                new JointIndex[] { JointIndex.HeadPart2, JointIndex.HeadPart1, JointIndex.Neck, JointIndex.Spine, JointIndex.Hips },
                Color.yellow);

            DrawChain(transform,
                new JointIndex[] { JointIndex.Neck, JointIndex.LeftShoulder, JointIndex.LeftElbow, JointIndex.LeftHand },
                Color.red);

            DrawChain(transform,
                new JointIndex[] { JointIndex.Neck, JointIndex.RightShoulder, JointIndex.RightElbow, JointIndex.RightHand },
                Color.blue);

            DrawChain(transform,
                new JointIndex[] { JointIndex.Hips, JointIndex.LeftHip, JointIndex.LeftKnee, JointIndex.LeftFoot },
                Color.red);

            DrawChain(transform,
                new JointIndex[] { JointIndex.Hips, JointIndex.RightHip, JointIndex.RightKnee, JointIndex.RightFoot },
                Color.blue);
        }

        static int[] ComputeParents()
        {
            int[] parents = new int[(int)JointIndex.Count];
            parents[(int)JointIndex.Hips] = -1;
            parents[(int)JointIndex.Spine] = (int)JointIndex.Hips;
            parents[(int)JointIndex.Neck] = (int)JointIndex.Spine;
            parents[(int)JointIndex.HeadPart1] = (int)JointIndex.Neck;
            parents[(int)JointIndex.HeadPart2] = (int)JointIndex.HeadPart1;
            parents[(int)JointIndex.LeftShoulder] = (int)JointIndex.Spine;
            parents[(int)JointIndex.LeftElbow] = (int)JointIndex.LeftShoulder;
            parents[(int)JointIndex.LeftHand] = (int)JointIndex.LeftElbow;
            parents[(int)JointIndex.RightShoulder] = (int)JointIndex.Spine;
            parents[(int)JointIndex.RightElbow] = (int)JointIndex.RightShoulder;
            parents[(int)JointIndex.RightHand] = (int)JointIndex.RightElbow;
            parents[(int)JointIndex.LeftHip] = (int)JointIndex.Hips;
            parents[(int)JointIndex.LeftKnee] = (int)JointIndex.LeftHip;
            parents[(int)JointIndex.LeftFoot] = (int)JointIndex.LeftKnee;
            parents[(int)JointIndex.RightHip] = (int)JointIndex.Hips;
            parents[(int)JointIndex.RightKnee] = (int)JointIndex.RightHip;
            parents[(int)JointIndex.RightFoot] = (int)JointIndex.RightKnee;

            return parents;
        }

        static float[] CreateDefaultConfidences()
        {
            float[] confidences = new float[(int)JointIndex.Count];
            for (int i = 0; i < (int)JointIndex.Count; ++i)
            {
                confidences[i] = 1.0f;
            }

            return confidences;
        }

        void DrawChain(AffineTransform transform, JointIndex[] jointIndices, Color color)
        {
            for (int i = 1; i < jointIndices.Length; ++i)
            {
                var from = jointIndices[i - 1];
                var to = jointIndices[i];

                Debug.DrawLine(transform * points[(int)from], transform * points[(int)to], color);
            }
        }

        float ComputeChain2DLength(JointIndex[] jointIndices)
        {
            float length = 0.0f;

            for (int i = 1; i < jointIndices.Length; ++i)
            {
                length += Vector2.Distance(this[jointIndices[i - 1]], this[jointIndices[i]]);
            }

            return length;
        }
    }
}