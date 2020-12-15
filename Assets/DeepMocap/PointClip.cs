using System.IO;
using UnityEngine;

namespace DeepMocap
{
    public struct PointClip
    {
        public float frameRate;
        public PointPose[] poses;

        public static (PointClip clip2d, PointClip clip3d) ReadClips(string filePath, float frameRate = 60.0f)
        {
            PointClip clip2d = Read2dClip(filePath + "_poses2d.json");
            PointClip clip3d = Read3dClip(filePath + "_poses3d.json");

            for(int i = 0; i < clip3d.poses.Length; ++i)
            {
                for(int j = 0; j < (int)JointIndex.Count; ++j)
                {
                    clip3d.poses[i].confidences[j] = clip2d.poses[i].confidences[j];
                }
            }

            return (clip2d, clip3d);
        }

        public static PointClip Read3dClip(string filePath, float frameRate = 60.0f)
        {
            string path = Application.dataPath + "/" + filePath;

            var jsonString = File.ReadAllText(path);
            SimpleJSON.JSONArray poses = SimpleJSON.JSON.Parse(jsonString).AsArray;

            PointClip clip = new PointClip()
            {
                frameRate = frameRate,
                poses = new PointPose[poses.Count]
            };

            for (int i = 0; i < poses.Count; ++i)
            {
                SimpleJSON.JSONArray pose = poses[i].AsArray;

                Vector3[] unorderedPoints = new Vector3[pose.Count];

                for (int j = 0; j < pose.Count; ++j)
                {
                    var pos = pose[j].ReadVector3();
                    pos.y = -pos.y;
                    unorderedPoints[j] = pos;
                }

                Vector3[] points = new Vector3[(int)JointIndex.Count];

                points[(int)JointIndex.LeftHip] = unorderedPoints[4];
                points[(int)JointIndex.LeftKnee] = unorderedPoints[5];
                points[(int)JointIndex.LeftFoot] = unorderedPoints[6];

                points[(int)JointIndex.RightHip] = unorderedPoints[1];
                points[(int)JointIndex.RightKnee] = unorderedPoints[2];
                points[(int)JointIndex.RightFoot] = unorderedPoints[3];

                points[(int)JointIndex.Hips] = unorderedPoints[0];
                points[(int)JointIndex.Spine] = unorderedPoints[7];
                points[(int)JointIndex.Neck] = unorderedPoints[8];

                points[(int)JointIndex.HeadPart1] = unorderedPoints[9];
                points[(int)JointIndex.HeadPart2] = unorderedPoints[10];

                points[(int)JointIndex.LeftShoulder] = unorderedPoints[11];
                points[(int)JointIndex.LeftElbow] = unorderedPoints[12];
                points[(int)JointIndex.LeftHand] = unorderedPoints[13];

                points[(int)JointIndex.RightShoulder] = unorderedPoints[14];
                points[(int)JointIndex.RightElbow] = unorderedPoints[15];
                points[(int)JointIndex.RightHand] = unorderedPoints[16];

                clip.poses[i] = new PointPose(points);
            }

            return clip;
        }

        public static PointClip Read2dClip(string filePath, float frameRate = 60.0f)
        {
            string path = Application.dataPath + "/" + filePath;

            var jsonString = File.ReadAllText(path);
            SimpleJSON.JSONArray frames = SimpleJSON.JSON.Parse(jsonString).AsArray;

            PointClip clip = new PointClip()
            {
                poses = new PointPose[frames.Count]
            };

            for (int i = 0; i < frames.Count; ++i)
            {
                SimpleJSON.JSONArray frame = frames[i].AsArray;

                // take the first character found in the sequence
                SimpleJSON.JSONNode character = frame[0];

                SimpleJSON.JSONArray x = character["x"].AsArray;
                SimpleJSON.JSONArray y = character["y"].AsArray;
                SimpleJSON.JSONArray confidences2d = character["confidence"].AsArray;

                Vector3[] unorderedPoints = new Vector3[x.Count];
                float[] unorderedConfidences = new float[(int)JointIndex.Count];

                Vector3[] points = new Vector3[(int)JointIndex.Count];
                float[] confidences = new float[(int)JointIndex.Count];

                for (int j = 0; j < x.Count; ++j)
                {
                    unorderedPoints[j] = new Vector3(x[j], -y[j], 0.0f) * 0.01f;
                    unorderedConfidences[j] = confidences2d[j];
                }

                void Write(JointIndex jointIndex, int rawIndex, Vector3[] unorderedPointsArray, Vector3[] pointsArray, float[] unorderedConfidencesArray, float[] confidencesArray)
                {
                    pointsArray[(int)jointIndex] = unorderedPointsArray[rawIndex];
                    confidencesArray[(int)jointIndex] = unorderedConfidencesArray[rawIndex];
                }

                Write(JointIndex.LeftHip, 11, unorderedPoints, points, unorderedConfidences, confidences);

                Write(JointIndex.LeftKnee, 13, unorderedPoints, points, unorderedConfidences, confidences);
                Write(JointIndex.LeftFoot, 15, unorderedPoints, points, unorderedConfidences, confidences);

                Write(JointIndex.RightHip, 12, unorderedPoints, points, unorderedConfidences, confidences);
                Write(JointIndex.RightKnee, 14, unorderedPoints, points, unorderedConfidences, confidences);
                Write(JointIndex.RightFoot, 16, unorderedPoints, points, unorderedConfidences, confidences);

                points[(int)JointIndex.Hips] = (points[(int)JointIndex.LeftHip] + points[(int)JointIndex.RightHip]) * 0.5f;
                confidences[(int)JointIndex.Hips] = (confidences[(int)JointIndex.LeftHip] + confidences[(int)JointIndex.RightHip]) * 0.5f;

                Write(JointIndex.LeftShoulder, 5, unorderedPoints, points, unorderedConfidences, confidences);
                Write(JointIndex.LeftElbow, 7, unorderedPoints, points, unorderedConfidences, confidences);
                Write(JointIndex.LeftHand, 9, unorderedPoints, points, unorderedConfidences, confidences);

                Write(JointIndex.RightShoulder, 6, unorderedPoints, points, unorderedConfidences, confidences);
                Write(JointIndex.RightElbow, 8, unorderedPoints, points, unorderedConfidences, confidences);
                Write(JointIndex.RightHand, 10, unorderedPoints, points, unorderedConfidences, confidences);

                points[(int)JointIndex.Neck] = (points[(int)JointIndex.LeftShoulder] + points[(int)JointIndex.RightShoulder]) * 0.5f;
                confidences[(int)JointIndex.Neck] = (confidences[(int)JointIndex.LeftShoulder] + confidences[(int)JointIndex.RightShoulder]) * 0.5f;

                points[(int)JointIndex.Spine] = (points[(int)JointIndex.Neck] + points[(int)JointIndex.Hips]) * 0.5f;
                confidences[(int)JointIndex.Spine] = (confidences[(int)JointIndex.Neck] + confidences[(int)JointIndex.Hips]) * 0.5f;

                Write(JointIndex.HeadPart1, 3, unorderedPoints, points, unorderedConfidences, confidences);
                Write(JointIndex.HeadPart2, 4, unorderedPoints, points, unorderedConfidences, confidences);

                clip.poses[i] = new PointPose(points, confidences);
            }

            return clip;
        }

        public Vector3 GetAvgPoint(JointIndex jointIndex, int poseIndex)
        {
            Vector3 point = Vector3.zero;
            int numSamples = 0;

            int offset = 10;
            for(int i = Mathf.Max(poseIndex - offset, 0); i < Mathf.Min(poseIndex + offset, poses.Length - 1); ++i)
            {
                point = point + poses[i][jointIndex];
                ++numSamples;
            }

            return point / numSamples;
        }

        public PointPose GetPoseAtTime(float time, float forceFrameRate = -1.0f, bool display = false)
        {
            float sampleRate = forceFrameRate < 0.0f ? frameRate : forceFrameRate;

            int frame = (int)(time * sampleRate);

            return poses[frame % (poses.Length - 1)];
        }

        public void DiscardLowConfidencePoint(float threshold)
        {
            for (int j = 0; j < (int)JointIndex.Count; ++j)
            {
                for (int i = 0; i < poses.Length; ++i)
                {
                    if (poses[i].confidences[j] < threshold)
                    {
                        int lastValidIndex = i - 1;

                        do
                        {
                            ++i;
                        }
                        while (i < poses.Length && poses[i].confidences[j] < threshold);

                        int firstValidIndex = i;

                        if (lastValidIndex < 0 && firstValidIndex >= poses.Length)
                        {
                            Debug.Log($"All keyframes for joint {(JointIndex)j} are invalid, taking the first keyframe as constant");

                            for (int k = lastValidIndex + 1; k < firstValidIndex; ++k)
                            {
                                poses[k].points[j] = poses[0].points[j];
                            }

                        }
                        else if (lastValidIndex < 0)
                        {
                            Vector3 firstValidPoint = poses[firstValidIndex].points[j];

                            for (int k = lastValidIndex + 1; k < firstValidIndex; ++k)
                            {
                                poses[k].points[j] = firstValidPoint;
                            }
                        }
                        else if (firstValidIndex >= poses.Length)
                        {
                            Vector3 lastValidPoint = poses[lastValidIndex].points[j];

                            for (int k = lastValidIndex + 1; k < firstValidIndex; ++k)
                            {
                                poses[k].points[j] = lastValidPoint;
                            }
                        }
                        else
                        {
                            Vector3 lastValidPoint = poses[lastValidIndex].points[j];
                            Vector3 firstValidPoint = poses[firstValidIndex].points[j];
                            int range = firstValidIndex - lastValidIndex;

                            for (int k = lastValidIndex + 1; k < firstValidIndex; ++k)
                            {
                                float ratio = (k - lastValidIndex) / (float)range;

                                poses[k].points[j] = Vector3.Lerp(lastValidPoint, firstValidPoint, ratio);
                            }
                        }

                        --i;
                    }
                }
            }
        }


        public float[] ComputeAvgBoneLengths()
        {
            float[] lengths = new float[(int)JointIndex.Count];

            for (int i = 0; i < lengths.Length; ++i)
            {
                lengths[i] = 0.0f;
            }

            for (int i = 0; i < poses.Length; ++i)
            {
                float[] poseLengths = poses[i].BoneLengths;

                for (int j = 0; j < poseLengths.Length; ++j)
                {
                    lengths[j] += poseLengths[j];
                }
            }

            for (int i = 0; i < lengths.Length; ++i)
            {
                lengths[i] /= poses.Length;
            }

            // force symmetry
            void EqualizeLengths(float[] boneLengths, JointIndex left, JointIndex right)
            {
                float avgLength = (boneLengths[(int)left] + boneLengths[(int)right]) * 0.5f;
                boneLengths[(int)left] = avgLength;
                boneLengths[(int)right] = avgLength;
            };

            EqualizeLengths(lengths, JointIndex.LeftKnee, JointIndex.RightKnee);
            EqualizeLengths(lengths, JointIndex.LeftFoot, JointIndex.RightFoot);

            EqualizeLengths(lengths, JointIndex.LeftElbow, JointIndex.RightElbow);
            EqualizeLengths(lengths, JointIndex.LeftHand, JointIndex.RightHand);

            return lengths;
        }

        public float[] ComputeSmoothPoseLengths2d(int sampleCount = 9)
        {
            float[] lengths = new float[poses.Length];
            float[] smoothLengths = new float[poses.Length];

            for (int i = 0; i < lengths.Length; ++i)
            {
                lengths[i] = poses[i].PoseLength2D;
            }

            int offset = (sampleCount - 1) / 2;
            for (int i = 0; i < lengths.Length; ++i)
            {
                float length = 0.0f;
                int samples = 0;
                for (int j = Mathf.Max(i - offset, 0); j <= Mathf.Min(i + offset, lengths.Length - 1); ++j)
                {
                    length += lengths[j];
                    ++samples;
                }

                smoothLengths[i] = length / samples;
            }

            return smoothLengths;
        }

        public Vector3[] RootTrajectory
        {
            get
            {
                Vector3[] rootTrajectory = new Vector3[poses.Length];

                for (int i = 0; i < poses.Length; ++i)
                {
                    rootTrajectory[i] = poses[i][JointIndex.Hips];
                }

                return rootTrajectory;
            }
        }

        public void CorrectWith2dProjection(ref PointClip clip2d, bool correctJoints)
        {
            // 1. Joint positions
            float[] boneLengths = ComputeAvgBoneLengths();
            float[] smoothPoseLengths2dClip = clip2d.ComputeSmoothPoseLengths2d();

            for (int i = 0; i < poses.Length; ++i)
            {
                PointPose pose2d = clip2d.poses[i].DeepCopy();
                pose2d.ScaleAndCenterToMatchReference(ref poses[i], smoothPoseLengths2dClip[i]);

                if (correctJoints)
                    poses[i].CorrectWith2DProjection(ref pose2d, boneLengths);
            }

            // 2. Root trajectory
            const int poseLengthAvgSampleCount = 25;
            const float cameraDistance = 1.5f;

            smoothPoseLengths2dClip = clip2d.ComputeSmoothPoseLengths2d(poseLengthAvgSampleCount);
            float[] smoothPoseLengths3dClip = ComputeSmoothPoseLengths2d(poseLengthAvgSampleCount);

            Vector3[] rootTrajectory2d = clip2d.RootTrajectory;
            {
                float scale = poses[0].PoseLength2D / smoothPoseLengths2dClip[0];
                Vector3 translation = poses[0][JointIndex.Hips] - rootTrajectory2d[0];

                for (int i = 0; i < rootTrajectory2d.Length; ++i)
                {
                    float ratio = smoothPoseLengths3dClip[i] / (smoothPoseLengths2dClip[i] * scale);

                    ref Vector3 root = ref rootTrajectory2d[i];
                    root = (root + translation) * scale;

                    float depth = cameraDistance * (ratio - 1.0f);
                    //root.z = depth;

                    poses[i].Translate(root - poses[i][JointIndex.Hips]);
                }
            }
        }

        public void DiscardPoses(int start, int end)
        {
            for (int i = start; i <= end; ++i)
            {
                float ratio = (i + 1 - start) / (float)(end - start + 2);

                for (int j = 0; j < (int)JointIndex.Count; ++j)
                {

                    poses[i].points[j] = Vector3.Lerp(poses[start - 1].points[j], poses[end + 1].points[j], ratio);
                }
            }
        }

        public void EqualizeJointLengths()
        {
            float[] avgBoneLengths = ComputeAvgBoneLengths();

            int numIterations = 10;
            float iterationRatio = 2.0f / numIterations;

            for (int i = 0; i < poses.Length; ++i)
            {
                for(int iteration = 0; iteration < numIterations; ++iteration)
                {
                    for (int j = 0; j < (int)JointIndex.Count; ++j)
                    {
                        poses[i].SetBoneLength(j, avgBoneLengths[j], iterationRatio);
                    }
                }



            }
        }

        public PointClip DeepCopy()
        {
            PointClip clip = new PointClip
            {
                poses = new PointPose[poses.Length]
            };

            for (int i = 0; i < poses.Length; ++i)
            {
                clip.poses[i] = poses[i].DeepCopy();
            }

            return clip;
        }
    }
}