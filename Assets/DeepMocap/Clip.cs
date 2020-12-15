using UnityEngine;

namespace DeepMocap
{
    public struct Clip
    {
        public float frameRate;
        public Pose[] localPoses;

        public static Clip CreateFromPointClip(ref PointClip pointClip, float armsThreshold = 0.0f, float bodyThreshold = 0.0f)
        {
            // We interpolate valid joint rotations to estimate joint positions
            Pose[] localPoses = new Pose[pointClip.poses.Length];

            // 1. Find joints with low confidence and mark them as invalid
            for (int i = 0; i < localPoses.Length; ++i)
            {
                Pose globalPose = Pose.CreateGlobalPoseFromPointPose(pointClip.poses[i], JointValidity.Invalid);
                localPoses[i] = globalPose.ToLocalSpace();

                for (int j = 1; j < (int)JointIndex.Count; ++j)
                {
                    float threshold = 0.0f;

                    JointIndex jointIndex = (JointIndex)j;
                    if (jointIndex == JointIndex.LeftHand || jointIndex == JointIndex.LeftElbow || jointIndex == JointIndex.RightHand || jointIndex == JointIndex.RightElbow)
                    {
                        threshold = armsThreshold;
                    }
                    else
                    {
                        threshold = bodyThreshold;
                    }

                    if (pointClip.poses[i].confidences[j] < threshold)
                    {
                        JointIndex[] invalidIndices;

                        switch((JointIndex)j)
                        {
                            case JointIndex.LeftHand: invalidIndices = new JointIndex[] { JointIndex.LeftElbow, JointIndex.LeftShoulder }; break;
                            case JointIndex.RightHand: invalidIndices = new JointIndex[] { JointIndex.RightElbow, JointIndex.RightShoulder }; break;
                            case JointIndex.LeftFoot: invalidIndices = new JointIndex[] { JointIndex.LeftKnee, JointIndex.LeftHip }; break;
                            case JointIndex.RightFoot: invalidIndices = new JointIndex[] { JointIndex.RightKnee, JointIndex.RightHip }; break;
                            case JointIndex.Spine:
                            case JointIndex.Hips:
                                invalidIndices = new JointIndex[] { }; break;
                            default:
                                invalidIndices = new JointIndex[] { (JointIndex)PointPose.Parents[j] }; break;  
                        }

                        foreach(int invalidIndex in invalidIndices)
                        {
                            if (invalidIndex != (int)JointIndex.Hips)
                            {
                                localPoses[i].validity[invalidIndex] = JointValidity.LowConfidence;
                            }
                        }
                    }
                }
            }

            // 2. Replace invalid joints by interpolation between valid boundaries
            for (int j = 1; j < (int)JointIndex.Count; ++j)
            {
                for (int i = 0; i < localPoses.Length; ++i)
                {
                    var validity = localPoses[i].validity[j];
                    if (validity != JointValidity.Valid)
                    {
                        int lastValidIndex = i - 1;

                        do
                        {
                            ++i;
                        }
                        while (i < localPoses.Length && localPoses[i].validity[j] != JointValidity.Valid);

                        int firstValidIndex = i;

                        if (lastValidIndex < 0 && firstValidIndex >= localPoses.Length)
                        {
                            Debug.Log($"All keyframes for joint {(JointIndex)j} are invalid, taking the first keyframe as constant");

                            for (int k = lastValidIndex + 1; k < firstValidIndex; ++k)
                            {
                                localPoses[k].transforms[j].rotation = localPoses[0].transforms[j].rotation;
                            }

                        }
                        else if (lastValidIndex < 0)
                        {
                            Quaternion firstValidRot = localPoses[firstValidIndex].transforms[j].rotation;

                            for (int k = lastValidIndex + 1; k < firstValidIndex; ++k)
                            {
                                localPoses[k].transforms[j].rotation = firstValidRot;
                            }
                        }
                        else if (firstValidIndex >= localPoses.Length)
                        {
                            Quaternion lastValidRot = localPoses[lastValidIndex].transforms[j].rotation;

                            for (int k = lastValidIndex + 1; k < firstValidIndex; ++k)
                            {
                                localPoses[k].transforms[j].rotation = lastValidRot;
                            }
                        }
                        else
                        {
                            Quaternion lastValidRot = localPoses[lastValidIndex].transforms[j].rotation;
                            Quaternion firstValidRot = localPoses[firstValidIndex].transforms[j].rotation;
                            int range = firstValidIndex - lastValidIndex;

                            for (int k = lastValidIndex + 1; k < firstValidIndex; ++k)
                            {
                                float ratio = (k - lastValidIndex) / (float)range;

                                localPoses[k].transforms[j].rotation = Quaternion.Slerp(lastValidRot, firstValidRot, ratio);
                            }
                        }

                        --i;
                    }
                }
            }

            for (int j = 1; j < (int)JointIndex.Count; ++j)
            {
                for (int i = 0; i < localPoses.Length; ++i)
                {
                    if (localPoses[i].validity[j] == JointValidity.LowConfidence)
                    {
                        localPoses[i].validity[j] = JointValidity.Valid;
                    }
                }
            }


            // TODO: know we have a valid axis for straight angle joint, we can restore the straight angle

            for (int i = 0; i < localPoses.Length; ++i)
            {
                localPoses[i][JointIndex.HeadPart1].position = localPoses[0][JointIndex.HeadPart1].position;
                localPoses[i][JointIndex.HeadPart2].position = localPoses[0][JointIndex.HeadPart2].position;
            }

            return new Clip()
            {
                frameRate = pointClip.frameRate,
                localPoses = localPoses
            };
        }


        public Clip RetargetMocapClipToRig(RigComponent rig)
        {
            Clip clip = new Clip()
            {
                frameRate = frameRate,
                localPoses = new Pose[localPoses.Length]
            };

            for (int i = 0; i < localPoses.Length; ++i)
            {
                clip.localPoses[i] = rig.RetargetMocapSpacePose(localPoses[i]);
            }

            return clip;
        }

        public Pose SampleLocalPose(float time, float forceFrameRate = -1.0f)
        {
            float sampleRate = forceFrameRate < 0.0f ? frameRate : forceFrameRate;

            float decimalFrame = time * sampleRate;
            int frame = (int)decimalFrame;
            float frameRatio = decimalFrame - frame;

            frame = frame % (localPoses.Length - 1);

            return localPoses[frame].InterpolateToward(localPoses[frame + 1], frameRatio);
        }

        public void SmoothLimbs(float armMoveSmoothRatio = 0.2f, float armRollSmoothRatio = 0.05f, float legMoveSmoothRatio = 1.0f, float legRollSmoothRatio = 0.2f)
        {
            Pose[] globalPoses = new Pose[localPoses.Length];
            for (int i = 0; i < localPoses.Length; ++i)
            {
                globalPoses[i] = localPoses[i].ToGlobalSpace();
            }

            new LimbRotationsSmoother(JointIndex.RightShoulder, JointIndex.RightElbow, JointIndex.RightHand, globalPoses, localPoses).Smooth(armMoveSmoothRatio, armRollSmoothRatio);
            new LimbRotationsSmoother(JointIndex.LeftShoulder, JointIndex.LeftElbow, JointIndex.LeftHand, globalPoses, localPoses).Smooth(armMoveSmoothRatio, armRollSmoothRatio);

            new LimbRotationsSmoother(JointIndex.RightHip, JointIndex.RightKnee, JointIndex.RightFoot, globalPoses, localPoses).Smooth(legMoveSmoothRatio, legRollSmoothRatio);
            new LimbRotationsSmoother(JointIndex.LeftHip, JointIndex.LeftKnee, JointIndex.LeftFoot, globalPoses, localPoses).Smooth(legMoveSmoothRatio, legRollSmoothRatio);
        }
    }
}