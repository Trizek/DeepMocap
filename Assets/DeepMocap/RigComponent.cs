using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepMocap
{
    [Serializable]
    public struct JointMapping
    {
        public JointIndex jointIndex;
        public Transform transform;
    }

    /// <summary>
    /// 
    /// Gateway between arbitrary rig and DeepMocap poses. It manages pose conversion between the following 3 spaces
    /// (each of them being subdivided into 2 spaces : local to parent, and global i.e. local to root)
    /// 
    /// * Actual rig space : literally all the GameObject transforms of the client rig. Naturally, this rig is made
    /// of an arbitrary hierarchy of any number of joints that can be bigger that the 16 DeepMocap joints. Ex:
    ///     Hips --> mapped to DeepMocap's Hips
    ///         Spine1
    ///             Spine2
    ///                 Spine3 --> mapped to DeepMocap's Spine
    /// Global and local transforms are respectively defined by the (position, rotation) and (localPosition, localRotation)
    /// members of the joints GameObject Transform. As a consequence the rotation convention of limbs is entirely defined
    /// by the user. Unity AnimationClip's animated joints are computed into that space
    /// 
    /// * Virtual rig space : the subset of rig joints that are mapped to DeepMocap joints. Their global transforms are the 
    /// same than in the actual rig space, but local transforms will be different when the hierarchy is different. In the
    /// example above, local transform of Spine3 is the inverse of global transform of Hips x global transform of Spine3.
    /// Whereas the local transform of Spine3 in actual rig space is the inverse of global transform of Spine2 x global 
    /// transform of Spine3.
    /// 
    /// * Mocap space : identical to virtual rig space, except rotations of joints are automatically computed in a determinist
    /// way from joints positions (with LookAt rotations)
    /// 
    /// </summary>
    public class RigComponent : MonoBehaviour
    {
        public List<JointMapping> jointMappings;

        public Transform[] RigTransforms
        {
            get
            {
                LazyInit();

                return m_RigTransforms;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="localPose"></param>
        /// <returns>Local pose in actual rig space</returns>
        public Pose RetargetMocapSpacePose(Pose localPose)
        {
            LazyInit();

            return ToLocalSpace(localPose.ToGlobalSpace() * m_MocapToRigSpace);
        }

        /// <summary>
        /// Local pose is assumed to be in actual rig space
        /// </summary>
        /// <param name="localPose"></param>
        public void ApplyLocalPose(Pose localPose)
        {
            LazyInit();
            
            m_RigTransforms[(int)JointIndex.Hips].localPosition = localPose[JointIndex.Hips].position;

            for (int i = 0; i <= (int)JointIndex.HeadPart1; ++i)
            {
                if (localPose.validity[i] == JointValidity.Valid)
                {
                    m_RigTransforms[i].localRotation = localPose.transforms[i].rotation;
                }
            }
        }

        void LazyInit()
        {
            if (m_RigTransforms == null)
            {
                m_RigTransforms = new Transform[(int)JointIndex.Count];
                foreach (var jointMapping in jointMappings)
                {
                    m_RigTransforms[(int)jointMapping.jointIndex] = jointMapping.transform;
                }

                m_BindPoseVirtualRigSpace = Pose.CreateGlobalPoseFromTransforms(m_RigTransforms);
                m_BindPoseMocapSpace = Pose.CreateGlobalPoseFromPointPose(new PointPose(m_RigTransforms));
                m_MocapToRigSpace = (m_BindPoseMocapSpace.GetInverse() * m_BindPoseVirtualRigSpace).RotationOnly();

                m_ActualRigInverseParentTransforms = new Pose((int)JointIndex.Count);

                for(int i = 0; i < (int)JointIndex.Count; ++i)
                {
                    // different from m_BindPoseVirtualRigSpace.Inverse, since RigTransforms can (and often) have more joints between DeepMocap joints, so the local transforms
                    // relative to parent are different
                    m_ActualRigInverseParentTransforms.transforms[i].rotation = m_RigTransforms[i].parent != null ? Quaternion.Inverse(m_RigTransforms[i].parent.rotation) : Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// Convert to the local space of the actual rig, not the local space of the virtual rig, which only contain
        /// a small subset of actual rig joints
        /// </summary>
        /// <param name="globalPose">In virtual rig space</param>
        /// <returns>Local pose in actual rig space</returns>
        Pose ToLocalSpace(Pose globalPose)
        {
            // Local pose with transforms relative to their DeepMocap hierarchy parent, not their real parent in the actual rig hierarchy which can have more intermediate joints
            Pose virtualLocalPose = globalPose.ToLocalSpace();

            Pose localPose = globalPose.DeepCopy();

            for(int i = 1; i < localPose.transforms.Length; ++i)
            {
                localPose.transforms[i] = m_ActualRigInverseParentTransforms.transforms[i] * m_BindPoseVirtualRigSpace.transforms[PointPose.Parents[i]] * virtualLocalPose.transforms[i];
            }

            return localPose;
        }

        Transform[] m_RigTransforms = null;

        // Both are global poses
        Pose m_BindPoseVirtualRigSpace;
        Pose m_BindPoseMocapSpace;

        Pose m_MocapToRigSpace;

        // Inverse transform of rig joints parent, this parent is not necessary a joint existing in DeepMocap hierarchy
        // For example JointIndex.Spine joint is often the child of another spine joint (Ex: SpineJoint1, SpineJoint2... etc) that is not mapped to any joint
        // in the DeepMocap hierarchy
        Pose m_ActualRigInverseParentTransforms;

 
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
