using UnityEngine;

namespace DeepMocap
{
    public struct LimbRotationsSmoother
    {
        public LimbRotationsSmoother(JointIndex rootIndex, JointIndex midIndex, JointIndex tipIndex, Pose[] globalPoses, Pose[] localPoses)
        {
            m_RootIndex = rootIndex;
            m_MidIndex = midIndex;
            m_TipIndex = tipIndex;

            m_GlobalPoses = globalPoses;
            m_LocalPoses = localPoses;
        }

        public void Smooth(float ratio, float rollRatio)
        {
            Limb prevLimb = GetLimb(0);
            Quaternion firstRotation = m_GlobalPoses[0][m_RootIndex].rotation;
            Quaternion moveRotation = firstRotation;

            DecomposedRotation smoothRot = new DecomposedRotation()
            {
                movementRotation = Quaternion.identity,
                extensionRotation = prevLimb.localMid.rotation,
                rollRotation = Quaternion.identity
            };

            DecomposedRotation[] decomposedRots = new DecomposedRotation[m_LocalPoses.Length];
            for (int i = 1; i < m_LocalPoses.Length; ++i)
            {
                Limb limb = GetLimb(i);

                moveRotation = prevLimb.GetRotationToward(ref limb) * moveRotation;
                Quaternion rollRotation = Quaternion.Inverse(moveRotation) * limb.root.rotation;
                Quaternion extensionRotation = limb.localMid.rotation;

                smoothRot.SmoothToward(new DecomposedRotation()
                {
                    movementRotation = Quaternion.Inverse(limb.parent.rotation) * moveRotation,
                    rollRotation = rollRotation,
                    extensionRotation = extensionRotation
                }, ratio, rollRatio);

                decomposedRots[i] = smoothRot;

                prevLimb = limb;
            }

            for (int i = 1; i < m_LocalPoses.Length; ++i)
            {
                Limb limb = GetLimb(i);

                limb.SetLocalRootRotation(decomposedRots[i].movementRotation * decomposedRots[i].rollRotation);
                limb.SetLocalMidRotation(decomposedRots[i].extensionRotation);

                ApplyChanges(i, ref limb);
            }
        }

        void ApplyChanges(int poseIndex, ref Limb limb)
        {
            m_GlobalPoses[poseIndex][m_RootIndex] = limb.root;
            m_GlobalPoses[poseIndex][m_MidIndex] = limb.mid;
            m_GlobalPoses[poseIndex][m_TipIndex] = limb.tip;

            m_LocalPoses[poseIndex][m_RootIndex] = m_GlobalPoses[poseIndex][RootParentIndex].Inverse * limb.root;
            m_LocalPoses[poseIndex][m_MidIndex] = limb.localMid;
            m_LocalPoses[poseIndex][m_TipIndex] = limb.localTip;
        }

        struct Limb
        {
            public AffineTransform parent;
            public AffineTransform root;
            public AffineTransform mid;
            public AffineTransform tip;

            public AffineTransform localRoot;
            public AffineTransform localMid;
            public AffineTransform localTip;

            public Vector3 LimbDirection => tip.position - root.position;

            public Quaternion GetRotationToward(ref Limb targetLimb)
            {
                return Quaternion.FromToRotation(LimbDirection, targetLimb.LimbDirection);
            }

            public void UpdateGlobalTransforms()
            {
                root = parent * localRoot;
                mid = root * localMid;
                tip = mid * localTip;
            }

            public void SetGlobalRootRotation(Quaternion rotation)
            {
                SetLocalRootRotation(Quaternion.Inverse(parent.rotation) * rotation);
            }

            public void SetLocalRootRotation(Quaternion rotation)
            {
                localRoot.rotation = rotation;
                UpdateGlobalTransforms();
            }

            public void SetLocalMidRotation(Quaternion rotation)
            {
                localMid.rotation = rotation;
                UpdateGlobalTransforms();
            }
        }

        public struct DecomposedRotation
        {
            public void SmoothToward(DecomposedRotation targetRot, float ratio, float rollRatio)
            {
                movementRotation = Quaternion.Slerp(movementRotation, targetRot.movementRotation, ratio);
                extensionRotation = Quaternion.Slerp(extensionRotation, targetRot.extensionRotation, ratio);
                rollRotation = Quaternion.Slerp(rollRotation, targetRot.rollRotation, rollRatio);
            }

            public Quaternion movementRotation;
            public Quaternion extensionRotation;
            public Quaternion rollRotation;
        }


        int NumPoses => m_LocalPoses.Length;

        JointIndex RootParentIndex => (JointIndex)PointPose.Parents[(int)m_RootIndex];

        Limb GetLimb(int poseIndex) => new Limb()
        {
            parent = m_GlobalPoses[poseIndex][RootParentIndex],
            root = m_GlobalPoses[poseIndex][m_RootIndex],
            mid = m_GlobalPoses[poseIndex][m_MidIndex],
            tip = m_GlobalPoses[poseIndex][m_TipIndex],

            localRoot = m_LocalPoses[poseIndex][m_RootIndex],
            localMid = m_LocalPoses[poseIndex][m_MidIndex],
            localTip = m_LocalPoses[poseIndex][m_TipIndex]
        };

        JointIndex m_RootIndex;
        JointIndex m_MidIndex;
        JointIndex m_TipIndex;

        Pose[] m_GlobalPoses;
        Pose[] m_LocalPoses;
    }
}