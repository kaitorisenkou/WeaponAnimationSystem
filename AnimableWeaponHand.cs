using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimableWeaponHand : AnimableWeaponMoveable {
    //override WeaponMotionPoint[] points = null;
    public AnimableWeapon weapon { private get; set; } = default;
    [SerializeField] bool isRight = false;
    public bool IsRight => isRight;
    [SerializeField] AnimableWeaponHandPoint defaultPoint = default;
    [SerializeField] AWHumanoidIK human = default;

    IEnumerable<(string name, AnimableWeaponHandPoint point)> points = new (string, AnimableWeaponHandPoint)[] { };
    public override string[] GetPointNames() {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) {
            if (!weapon) {
                var weaponSelected = Selection.gameObjects.Select(t => t.GetComponent<AnimableWeapon>()).FirstOrDefault();
                if (weaponSelected != null && weaponSelected) {
                    return new string[] { "default" }.Concat(weaponSelected.GetHandPoints().Where(t => isRight ? t.Right : t.Left).Select(t => t.name)).ToArray();
                } else return new string[] { "default" };
            }
        }
#endif
        if (points == null || !points.Any()) {
            points = weapon.GetHandPoints().Where(t => isRight ? t.Right : t.Left).Select(t => (t.name, t));
        }
        return new string[] { "default" }.Concat(points.Select(t => t.name)).ToArray();
    }

    public override void SetMove(string pointName, float timeSec, string bezierPointName = "") {
        if (!string.IsNullOrEmpty(bezierPointName)) {
            bezierPoint = GetPoint(bezierPointName);
            bezier = (bezierPoint != null);
        } else {
            bezier = false;
        }
        _setMove(pointName, timeSec);
    }
    public override void SetMove(string pointName, float timeSec, AnimationCurve animationCurve, string bezierPointName = "") {
        curve = animationCurve;
        SetMove(pointName, timeSec, bezierPointName);
    }
    void _setMove(string pointName, float timeSec) {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) {
            return;
        }
#endif

        AnimableWeaponHandPoint point;
        if ((point = GetPoint(pointName)) == null) {
            if (!disableWarning)
                Debug.LogWarning("handPoint \"" + pointName + "\" for " + this.gameObject.name + " is missing!");
            return;
        }
        
        nextPoint = point;
        speed = 1 / timeSec;

        prevPos = this.transform.localPosition;
        prevRot = this.transform.localRotation;
        /*
        nextPoint.GetGlovalPosition(isRight, out Vector3 pos, out Quaternion rot);
        if (rot.eulerAngles.x - prevRot.eulerAngles.x > 180) {
            prevRot *= Quaternion.Euler(-360, 0, 0);
        }
        if (rot.eulerAngles.x - prevRot.eulerAngles.x < 180) {
            prevRot *= Quaternion.Euler(360, 0, 0);
        }
        */
        if (this.isRight) {
            prevFingers = human.FingersValues;
            //human.SetFingerAngles(2, nextPoint.RightFingers);
        } else {
            prevFingers = human.FingersValues;
            //human.SetFingerAngles(1, nextPoint.LeftFingers);
        }
        //Debug.Log(pointName);
        lerpT = 0;
    }
    AnimableWeaponHandPoint GetPoint(string name) {
        //Debug.Log("points:" + points.Count());
        if (name == "default")
            return defaultPoint;
        if (points.Any(t => t.name == name)) {
            var p = points.FirstOrDefault(t => t.name == name);
            return p.point;
        }
        return null;
    }

    public void SetWeaponUse(AnimableWeapon weapon) {
        if (!weapon) return;

        this.weapon = weapon;
        points = weapon.GetHandPoints().Where(t => isRight ? t.Right : t.Left).Select(t => (t.name, t));
    }

    public AnimableWeaponHandPoint nextPoint { get; private set; } = null;
    Vector3 prevPos;
    Quaternion prevRot;
    float[] prevFingers;
    public float lerpT { get; private set; } = 2;
    float speed = 1f;
    bool bezier = false;
    AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    AnimableWeaponHandPoint bezierPoint;
    Transform parent;
    private void FixedUpdate() {
        if (!parent) {
            parent = this.transform.parent;
            if (!parent) {
                parent = this.transform;
            }
        }
        if (nextPoint) {
            nextPoint.GetGlovalPosition(isRight, out Vector3 pos, out Quaternion rot);
            if (lerpT <= 1) {
                float curvT = curve.Evaluate(lerpT);
                if (!bezier) {
                    this.transform.position = Vector3.Lerp(parent.TransformPoint(prevPos), pos, curvT);
                    this.transform.rotation = Quaternion.Lerp(parent.rotation*prevRot, rot, curvT);
                    /*
                    if (isRight) {
                        human.SetFingerAnglesLerp(2, prevFingers, nextPoint.RightFingers, curvT);
                    } else {
                        human.SetFingerAnglesLerp(1, prevFingers, nextPoint.LeftFingers, curvT);
                    }
                    */
                } else {
                    bezierPoint.GetGlovalPosition(isRight, out Vector3 bezPos, out Quaternion bezRot);
                    this.transform.position =
                        Vector3.Lerp(
                            Vector3.Lerp(parent.TransformPoint(prevPos), bezPos, curvT),
                            Vector3.Lerp(bezPos, pos, curvT),
                            curvT
                            );
                    this.transform.rotation =
                        Quaternion.Lerp(
                            Quaternion.Lerp(parent.rotation * prevRot, bezRot, curvT),
                            Quaternion.Lerp(parent.rotation * bezRot, rot, curvT),
                            curvT
                            );
                    /*
                    float[] fingerTgt1, fingerTgt2;
                    if (isRight) {
                        fingerTgt1 = prevFingers.Skip(5).Zip(nextPoint.RightFingers, (n, b) => Mathf.LerpUnclamped(n, b, lerpT)).Concat(prevFingers.Take(5)).ToArray();
                        fingerTgt2 = nextPoint.RightFingers.Zip(bezierPoint.RightFingers, (n, b) => Mathf.LerpUnclamped(n, b, lerpT)).ToArray();
                        human.SetFingerAnglesLerp(2, fingerTgt1, fingerTgt2, lerpT);
                        //human.SetFingerAnglesLerp(2, prevFingers, fingerTgt, curvT);
                    } else {
                        fingerTgt1 = prevFingers.Take(5).Zip(nextPoint.LeftFingers, (n, b) => Mathf.LerpUnclamped(n, b, lerpT)).Concat(prevFingers.Skip(5)).ToArray();
                        fingerTgt2 = nextPoint.LeftFingers.Zip(bezierPoint.LeftFingers, (n, b) => Mathf.LerpUnclamped(n, b, lerpT)).ToArray();
                        human.SetFingerAnglesLerp(1, fingerTgt1, fingerTgt2, lerpT);
                        //human.SetFingerAnglesLerp(1, prevFingers, fingerTgt, curvT);
                    }
                    */
                }
                if (isRight) {
                    human.SetFingerAnglesLerp(2, prevFingers, nextPoint.RightFingers, curvT);
                } else {
                    human.SetFingerAnglesLerp(1, prevFingers, nextPoint.LeftFingers, curvT);
                }
                lerpT += Time.deltaTime * speed;

            }
            if (lerpT > 1) {
                this.transform.position = pos;
                this.transform.rotation = rot; if (isRight) {
                    human.SetFingerAngles(2, nextPoint.RightFingers);
                } else {
                    human.SetFingerAngles(1, nextPoint.LeftFingers);
                }
            }
        }
        
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AnimableWeaponHand))]
public class AnimableWeaponHandEditor : Editor {
    bool preview = false;
    int index = 0;
    string[] names;
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        if (EditorApplication.isPlaying) {
            EditorGUILayout.LabelField("Preview");
            EditorGUI.indentLevel++;
            if(preview = EditorGUILayout.Toggle(preview)) {
                names = ((AnimableWeaponHand)target).GetPointNames();
                index = EditorGUILayout.Popup(index, names);
                ((AnimableWeaponHand)target).SetMove(names[index], 0);
            }

            EditorGUI.indentLevel--;
            if (((AnimableWeaponHand)target).nextPoint)
                EditorGUILayout.HelpBox(((AnimableWeaponHand)target).lerpT + "->" + ((AnimableWeaponHand)target).nextPoint.name, MessageType.None);
        } else {
            preview = false;
            EditorGUILayout.HelpBox("Play the game, then\npreview options are here.", MessageType.Info);
        }
    }
}
#endif