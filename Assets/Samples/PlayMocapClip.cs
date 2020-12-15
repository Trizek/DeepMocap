using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeepMocap;

[RequireComponent(typeof(RigComponent))]
public class PlayMocapClip : MonoBehaviour
{
    public string ClipName = "federer";
    public float ClipFrameRate = 60.0f;
    public bool isSlowMo = true;

    RigComponent rigComponent;

    PointClip pointClip2d;
    PointClip pointClip3d;

    PointClip correctedPointClip3d;

    Clip clip;

    public float time = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        rigComponent = GetComponent<RigComponent>();

        (pointClip2d, pointClip3d) = PointClip.ReadClips($"Samples/{ClipName}", ClipFrameRate);

        correctedPointClip3d = pointClip3d.DeepCopy();
        correctedPointClip3d.CorrectWith2dProjection(ref pointClip2d, true);

        if (isSlowMo)
        {
            clip = Clip.CreateFromPointClip(ref correctedPointClip3d, 0.4f, 0.25f);
        }
        else
        {
            clip = Clip.CreateFromPointClip(ref correctedPointClip3d, 0.2f, 0.1f);
            clip.SmoothLimbs();
        }

        clip = clip.RetargetMocapClipToRig(rigComponent);
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        pointClip2d.GetPoseAtTime(time, ClipFrameRate).DebugDraw(transform);
        pointClip3d.GetPoseAtTime(time, ClipFrameRate).DebugDraw(transform * new AffineTransform(){ position = Vector3.right, rotation = Quaternion.identity });
        correctedPointClip3d.GetPoseAtTime(time, ClipFrameRate).DebugDraw(transform * new AffineTransform(){ position = Vector3.right * 2.0f, rotation = Quaternion.identity });

        rigComponent.ApplyLocalPose(clip.SampleLocalPose(time, ClipFrameRate));
        
    }
}
