using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneFollower : MonoBehaviour {
    [SerializeField] Animator targetAnimator = default;
    [SerializeField] HumanBodyBones bone = default;
    [SerializeField] Vector3 relativePosition = default;
    [SerializeField] float followStep = 1;
    //[SerializeField] bool autoPositioning = false;

    Transform boneTransform;
    void Start() {
        boneTransform = targetAnimator.GetBoneTransform(bone);
        if (!boneTransform) {
            this.enabled = false;
            return;
        }
        /*
        if (autoPositioning) {
            relativePosition = transform.InverseTransformPoint(transform.position - boneTransform.position);
        }*/
    }
    private void LateUpdate() {
        this.transform.position = Vector3.Lerp(transform.position, boneTransform.position + boneTransform.rotation * relativePosition, followStep);
    }
}
