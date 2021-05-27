using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimableWeaponHandPoint : MonoBehaviour {
    [SerializeField] bool rightHand = true;
    [SerializeField] Vector3 rightPos = default;
    [SerializeField] Vector3 rightAngle = default;
    [SerializeField] float[] rightFingers = { 0, 0, 0, 0, 0 };
    public float[] RightFingers => rightFingers;
    [Space(5), SerializeField] bool leftHand = true;
    [SerializeField] Vector3 leftPos = default;
    [SerializeField] Vector3 leftAngle = default;
    [SerializeField] float[] leftFingers = { 0, 0, 0, 0, 0 };
    public float[] LeftFingers => leftFingers;
    public bool Right => rightHand;
    public bool Left => leftHand;

    private void Start() {
        //Debug.Log(gameObject.name+"righthand:"+transform.TransformPoint(rightPos));
        //Debug.Log(gameObject.name + "lefthand:" + transform.TransformPoint(leftPos));
    }

    public void GetGlovalPosition(bool isRight, out Vector3 pos, out Quaternion rot) {
        pos = this.transform.position;
        rot = transform.rotation;
        if (isRight) {
            if (!rightHand) return;
            pos = transform.TransformPoint(rightPos);
            rot = transform.rotation * Quaternion.Euler(rightAngle);
        } else {
            if (!leftHand) return;
            
            pos = transform.TransformPoint(leftPos);
            rot = transform.rotation * Quaternion.Euler(leftAngle);
        }
    }
    /*
    private void OnDrawGizmosSelected() {
#if UNITY_EDITOR
        if (Selection.activeGameObject != this.gameObject)
            return;
#endif
        if (rightHand) {
            GetGlovalPosition(true, out Vector3 posRight, out Quaternion rotRight);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(posRight, 0.05f);
        }
        if (leftHand) {
            GetGlovalPosition(false, out Vector3 posLeft, out Quaternion rotLeft);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(posLeft, 0.05f);

        }
    }*/

}

#if UNITY_EDITOR
[CustomEditor(typeof(AnimableWeaponHandPoint))]
public class AWHandPointEditor : Editor {
    AnimableWeaponHandPoint targetCasted => (AnimableWeaponHandPoint)target;
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("You can see preview on AnimableWeaponHand.", MessageType.None);

        AnimableWeapon wep;
        if (wep = GetWeapon()) {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("RightHand")) {
                Selection.activeGameObject = wep.RightHand.gameObject;
            }
            if (GUILayout.Button("LeftHand")) {
                Selection.activeGameObject = wep.LeftHand.gameObject;

            }
            GUILayout.EndHorizontal();
        }
    }
    AnimableWeapon GetWeapon() {
        return targetCasted.GetComponentInParent<AnimableWeapon>();
    }
}
#endif