using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class AWHumanoidIK : MonoBehaviour {
    [SerializeField] AnimableWeaponHand rightHand = default;
    [SerializeField] AnimableWeaponHand leftHand = default;
    [SerializeField] Transform rightElbow = default;
    [SerializeField] Transform leftElbow = default;
    
    public float lean = 0;
    public float bodyForward = 0f;

    //[SerializeField] Transform[] fingers;

    Animator animator;
    HumanPose humanPose;
    HumanPoseHandler handler;

    Transform headTrans;

    Quaternion headNegAngle = default;
    Quaternion headPosAngle = default;

    (Quaternion close, Quaternion open)[] fingerAngles = new (Quaternion, Quaternion)[30];
    float[] fingersValues = new float[10];
    public float[] FingersValues => fingersValues;

    Quaternion shoulderAngle;
    
    Quaternion spineTwistNegAngle;
    Quaternion spineTwistPosAngle;
    Quaternion chestTwistNegAngle;
    Quaternion chestTwistPosAngle;
    //Quaternion upperChestTwistNegAngle;
    //Quaternion upperChestTwistPosAngle;


    public void SetFingerAngles(float[] angles) {
        SetFingerAngles(0, angles);
    }
    /// <param name="handSide">0:both,1:left,2:right</param>
    /// <param name="angles"></param>
    public void SetFingerAngles(int handSide, float[] angles) {
        switch (handSide) {
            case 1:
                fingersValues = angles.Take(5).Concat(fingersValues.Skip(5)).ToArray();
                break;
            case 2:
                fingersValues = fingersValues.Take(5).Concat(angles.Take(5)).ToArray();
                break;
            default:
                fingersValues = angles;
                break;
        }
    }
    /// <param name="handSide">0:both,1:left,2:right</param>
    /// <param name="prev"></param>
    /// <param name="target"></param>
    /// <param name="t"></param>
    public void SetFingerAnglesLerp(int handSide, float[] prev, float[] target, float t) {
        float[] tgtTaken;
        switch (handSide) {
            case 1:
                tgtTaken = target.Take(5).Concat(fingersValues.Skip(5)).ToArray();
                break;
            case 2:
                tgtTaken = fingersValues.Take(5).Concat(target.Take(5)).ToArray();
                break;
            default:
                tgtTaken = target;
                break;
        }
        fingersValues = prev.Zip(tgtTaken, (prv, tgt) => Mathf.LerpUnclamped(prv, tgt, t)).ToArray();
    }

    int step = 0;
    private void Awake() {
        int[] fingerMuscles = {
            55,57,58,59,61,62,63,65,66,67,69,70,71,73,74,
            75,77,78,79,81,82,83,85,86,87,89,90,91,93,94
        };

        int[] spread = { 56, 60, 64, 68, 72, 76, 80, 84, 88, 92 };
        animator = this.GetComponent<Animator>();

        animator.enabled = false;
        handler = new HumanPoseHandler(animator.avatar, animator.transform);
        handler.GetHumanPose(ref humanPose);

        foreach (var i in spread) {
            humanPose.muscles[i] = -1;
        }
        foreach (var i in fingerMuscles) {
            humanPose.muscles[i] = -1;
        }
        handler.SetHumanPose(ref humanPose);
        var zeroAngle = Enumerable.Range(24, fingerAngles.Length).Select(t => animator.GetBoneTransform((HumanBodyBones)t).localRotation).ToArray();
        foreach (var i in fingerMuscles) {
            humanPose.muscles[i] = 1;
        }
        handler.SetHumanPose(ref humanPose);
        var oneAngle = Enumerable.Range(24, fingerAngles.Length).Select(t => animator.GetBoneTransform((HumanBodyBones)t).localRotation).ToArray();
        fingerAngles = zeroAngle.Zip(oneAngle, (t1, t2) => (t1, t2)).ToArray();
        
        humanPose.muscles[9] = 0;
        humanPose.muscles[10] = 0;
        handler.SetHumanPose(ref humanPose);
        headNegAngle = animator.GetBoneTransform(HumanBodyBones.Neck).localRotation;
        humanPose.muscles[9] = -1f;
        humanPose.muscles[10] = 0f;
        humanPose.muscles[47] = -1f;
        handler.SetHumanPose(ref humanPose);
        headPosAngle = animator.GetBoneTransform(HumanBodyBones.Neck).localRotation;
        shoulderAngle = animator.GetBoneTransform(HumanBodyBones.RightShoulder).localRotation;

        headTrans = animator.GetBoneTransform(HumanBodyBones.Head);
        
        animator.enabled = true;
    }
    void OnAnimatorIK() {
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0.5f);
        animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand.transform.position);
        animator.SetIKRotation(AvatarIKGoal.RightHand, rightHand.transform.rotation);
        if (rightElbow)
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbow.position);

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0.5f);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand.transform.position);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHand.transform.rotation);
        if (leftElbow)
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbow.position);


        for (int i = 0; i < fingerAngles.Length; i++) {
            HumanBodyBones bone = (HumanBodyBones)24 + i;
            float lerpT = fingersValues[i / 3];
            animator.SetBoneLocalRotation(bone, Quaternion.LerpUnclamped(fingerAngles[i].close, fingerAngles[i].open, lerpT));
        }
        
        animator.SetBoneLocalRotation(HumanBodyBones.RightShoulder, shoulderAngle);
        animator.SetBoneLocalRotation(HumanBodyBones.Neck, Quaternion.LerpUnclamped(headNegAngle, headPosAngle, lean + bodyForward / 4f));

        animator.SetLookAtWeight(1f, bodyForward, (1f - bodyForward) / 2f, 0, 0);
        animator.SetLookAtPosition(headTrans.position + Camera.main.transform.forward);
        
    }
    /*
    [ContextMenu("HumanPoseの配列に対応したリグ名を表示")]
    private void ShowMuscleList() {
        string[] muscleName = HumanTrait.MuscleName;
        int i = 0;
        while (i < HumanTrait.MuscleCount) {
            Debug.Log(i + " : " + muscleName[i]);
            i++;
        }
    }
    */
}
