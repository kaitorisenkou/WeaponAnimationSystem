using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandShaker : MonoBehaviour {
    [SerializeField] Animator animator=default;
    [SerializeField] AnimationCurve shakeX = default;
    [SerializeField] AnimationCurve shakeY = default;
    [SerializeField] AnimationCurve shakeZ = default;
    Transform hip;
    bool first = true;
    Vector3 oldHipPosition;
    float time = 0;
    Vector3 originalLocalPos;

    void Start() {
        hip = animator.GetBoneTransform(HumanBodyBones.Hips);
        originalLocalPos = this.transform.localPosition;
    }
    
    void Update() {
        Vector3 hipPosition = hip.position;
        Vector3 curveShake = new Vector3(shakeX.Evaluate(time), shakeY.Evaluate(time), shakeZ.Evaluate(time)) * 0.01f;
        if (!first) {
            //this.transform.position = transform.TransformPoint(originalLocalPos + curveShake) + (hipPosition - oldHipPosition);
            this.transform.Translate(hipPosition - oldHipPosition);
        } else first = false;
        oldHipPosition = hipPosition;
        time += Time.deltaTime;
    }
}
