
namespace DeepMocap
{
    public enum JointIndex
    {
        Hips = 0,
        Spine,
        LeftShoulder,
        LeftElbow,
        LeftHand,
        RightShoulder,
        RightElbow,
        RightHand,
        LeftHip,
        LeftKnee,
        LeftFoot,
        RightHip,
        RightKnee,
        RightFoot,
        Neck,

        // HeadPart1 and HeadPart2 have different meaning between 2D and 3D poses
        // In 2D: HeadPart1 is left ear, HeadPart2 is right ear
        // In 3D: HeadPart1 is nose, HeadPart2 is top of the head
        // In both case, HeadPart2 is assumed to be an inanimated child of HeadPart1
        HeadPart1,
        HeadPart2,
        Count
    };
}