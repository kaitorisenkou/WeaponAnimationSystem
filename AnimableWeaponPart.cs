using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimableWeaponPart : AnimableWeaponMoveable {
    [SerializeField] AnimableWeaponMotionPoint[] points = default;
    AnimableWeaponMotionPoint defaultPoint;

    AnimableWeaponMotionPoint nextPoint;
    Vector3 prevPos;
    Quaternion prevRot;
    float lerpT = 2;
    float speed = 1f;
    bool bezier = false;
    AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    AnimableWeaponMotionPoint bezierPoint;

    private void Awake() {
        defaultPoint = new AnimableWeaponMotionPoint("default", transform.localPosition, transform.localRotation);
        nextPoint = defaultPoint;
    }
    private void Start() {
    }

    private void FixedUpdate() {
        if (lerpT <= 1) {
            float curvT = curve.Evaluate(lerpT);
            nextPoint.GetLocalPosition(transform, out Vector3 pos, out Quaternion rot);

            if (!bezier) {
                this.transform.localPosition = Vector3.Lerp(prevPos, pos, curvT);
                this.transform.localRotation = Quaternion.Lerp(prevRot, rot, curvT);
            } else {
                bezierPoint.GetLocalPosition(transform, out Vector3 bezPos, out Quaternion bezRot);
                this.transform.localPosition =
                    Vector3.Lerp(
                        Vector3.Lerp(prevPos, bezPos, curvT),
                        Vector3.Lerp(bezPos, pos, curvT),
                        curvT
                        );
                this.transform.localRotation =
                    Quaternion.Lerp(
                        Quaternion.Lerp(prevRot, bezRot, curvT),
                        Quaternion.Lerp(bezRot, rot, curvT),
                        curvT
                        );
            }

            lerpT += Time.deltaTime * speed;
            if (lerpT > 1) {
                this.transform.localPosition = pos;
                this.transform.localRotation = rot;
            }
        }
    }

    public override void SetMove(string pointName, float timeSec, string bezierPointName = "") {
        if (!string.IsNullOrEmpty(bezierPointName)) {
            bezierPoint = GetPoint(bezierPointName);
            bezier = true;
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

        speed = 1 / timeSec;
        AnimableWeaponMotionPoint point;
        if ((point = GetPoint(pointName)) == null) {
            if (!disableWarning)
                Debug.LogWarning("motionPoint \"" + pointName + "\" of " + this.gameObject.name + " is missing!");
            return;
        }
        nextPoint = point;

        prevPos = this.transform.localPosition;
        prevRot = this.transform.localRotation;
        lerpT = 0;
    }

    AnimableWeaponMotionPoint GetPoint(string name) {
        if (name == "default")
            return defaultPoint;
        if (points.Any()) {
            return points.FirstOrDefault(t => t.Name == name);
        }
        return defaultPoint;
    }
    public override string[] GetPointNames() {
        return new string[] { "default" }.Concat(points.Select(t => t.Name)).ToArray();
    }

    MeshFilter meshFilter;
    private void OnDrawGizmosSelected() {
#if UNITY_EDITOR
        if (Selection.activeGameObject != this.gameObject)
            return;
#endif
        if (!meshFilter) {
            var obj = this.transform;
            while(!(meshFilter = obj.GetComponent<MeshFilter>())) {
                if(!(obj = obj.parent)) {
                    return;
                }
            }
        }

        Mesh mesh = meshFilter.sharedMesh;
        float h = 0, s = 1, v = 1f;
        if (points != null)
            foreach (var point in points) {
                point.GetGlovalPosition(transform, out Vector3 pos, out Quaternion rot);

                Gizmos.color = Color.HSVToRGB(h, s, v);
                Gizmos.DrawMesh(mesh, pos, rot, this.transform.lossyScale);
#if UNITY_EDITOR
                Handles.Label(pos, point.Name);
#endif
                h += 1.0f / points.Length;
            }
    }
}
public struct WeaponMotionMode {
    [SerializeField] string name;
    [SerializeField] AnimationCurve curve;
    public string Name => name;
    public AnimationCurve Curve => curve;
}

[System.Serializable]
public class AnimableWeaponMotionPoint {
    [SerializeField] string name;
    [SerializeField] Transform targetTransform;
    [SerializeField, HideInInspector] Vector3 position;
    [SerializeField, HideInInspector] Quaternion rotation;


    public string Name => name;
    //public Vector3 Position => position;
    //public Quaternion Rotation => rotation;
    public Transform TargetTransform => targetTransform;

    public AnimableWeaponMotionPoint(string name, Vector3 pos, Quaternion rot) {
        this.name = name;
        this.position = pos;
        this.rotation = rot;
        targetTransform = null;
    }

    public void GetGlovalPosition(Transform objTransform, out Vector3 pos, out Quaternion rot) {
        if (targetTransform) {
            pos = targetTransform.position;
            rot = targetTransform.rotation;
            return;
        }
        Transform parent = objTransform.parent;
        if (!parent) parent = objTransform;
        pos = parent.TransformPoint(position);
        rot = (parent.rotation * rotation).normalized;
    }
    public void GetLocalPosition(Transform objTransform, out Vector3 pos, out Quaternion rot) {
        Transform parent = objTransform.parent;
        if (!parent) parent = objTransform;

        if (targetTransform) {
            pos = parent.InverseTransformPoint(targetTransform.position);
            rot = Quaternion.Inverse(parent.transform.rotation) * targetTransform.rotation;
            return;
        }
        pos = position;
        rot = rotation.normalized;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(AnimableWeaponPart))]
public class AWMPEditor : Editor {
    AnimableWeaponPart targetCasted => (AnimableWeaponPart)target;
    int pointEdit = -1;

    SerializedProperty pointsProp, disableWarnProp;
    private void OnEnable() {
        pointsProp = serializedObject.FindProperty("points");
        disableWarnProp = serializedObject.FindProperty("disableWarning");
    }
    public override void OnInspectorGUI() {
        disableWarnProp.boolValue = EditorGUILayout.Toggle("Disable Warning",disableWarnProp.boolValue);

        EditorGUILayout.LabelField("Points");
        EditorGUI.indentLevel++;
        {
            int delete = -1;

            for (int i = 0; i < pointsProp.arraySize; i++) {
                var item = pointsProp.GetArrayElementAtIndex(i);

                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(item, false);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false))) {
                        delete = i;
                    }
                }
                GUILayout.EndHorizontal();

                if (item.isExpanded) {
                    EditorGUI.indentLevel++;

                    int depth = -1;
                    var iterator = /*item*/pointsProp.GetArrayElementAtIndex(i);
                    while (iterator.NextVisible(true) || depth == -1) {
                        if (depth != -1 && iterator.depth != depth) break;
                        depth = iterator.depth;
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    
                    if (EditorGUILayout.Toggle("Edit Position", pointEdit == i, EditorStyles.radioButton)) {
                        pointEdit = i;
                    } else {
                        if (pointEdit == i)
                            pointEdit = -1;
                    }
                    if (pointEdit == i) {
                        if (item.FindPropertyRelative("targetTransform").objectReferenceValue) {
                            EditorGUILayout.HelpBox("Position and Rotation are decided by targetTransform, and these settings will ignored.\nUnassign that to activate these settings.", MessageType.Info);
                        }
                        EditorGUI.indentLevel++;
                        {
                            SerializedProperty posProp = item.FindPropertyRelative("position");
                            //EditorGUILayout.PropertyField(posProp);
                            Vector3 positionLaw = EditorGUILayout.Vector3Field("Position", posProp.vector3Value);
                            posProp.vector3Value = positionLaw.Round(5);
                            SerializedProperty rotProp = item.FindPropertyRelative("rotation");
                            Vector3 rotationEuler = EditorGUILayout.Vector3Field("Rotation", rotProp.quaternionValue.eulerAngles);
                            rotProp.quaternionValue = Quaternion.Euler(rotationEuler.Round(5));
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            if (delete >= 0) {
                pointsProp.DeleteArrayElementAtIndex(delete);
            }
        }
        EditorGUI.indentLevel--;

        GUILayout.BeginHorizontal();
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false))) {
                pointsProp.arraySize++;
            }
        }
        GUILayout.EndHorizontal();
        

        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();
    }
    private void OnSceneGUI() {
        if (pointEdit < 0) return;

        var ele = pointsProp.GetArrayElementAtIndex(pointEdit);
        var pointPosProp = ele.FindPropertyRelative("position");
        var pointRotProp = ele.FindPropertyRelative("rotation");

        Transform parent = targetCasted.transform.parent;
        if (!parent) parent = targetCasted.transform;

        Vector3 pointPos = parent.TransformPoint(pointPosProp.vector3Value);
        Quaternion pointRot = parent.rotation * pointRotProp.quaternionValue.normalized;

        pointPosProp.vector3Value = parent.InverseTransformPoint(Handles.PositionHandle(pointPos, Quaternion.identity));
        pointRotProp.quaternionValue = Quaternion.Inverse(parent.rotation)* Handles.RotationHandle(pointRot, pointPos);

        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();
    }
}
#endif
public static class Vector3Ext {
    public static Vector3 Round(this Vector3 self, int digit) {
        float digit10 = Mathf.Pow(10, digit);
        return new Vector3(
            Mathf.Round(self.x * digit10) / digit10,
            Mathf.Round(self.y * digit10) / digit10,
            Mathf.Round(self.z * digit10) / digit10
        );
    }
    public static Vector3 Round(this Vector3 self) => Round(self, 0);
}