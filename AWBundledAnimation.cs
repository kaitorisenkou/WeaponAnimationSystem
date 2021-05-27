using UnityEngine;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New BundledAnimation", menuName = "Create BundledAnimation")]
public class AWBundledAnimation : AnimableWeaponAnimation {
    [SerializeField] AWBundleElement[] animations = default;

    private WeaponAnimationNode[] nodesOT = null;
    public override WeaponAnimationNode[] Nodes => nodesOT;

    public override void Sort() {
        foreach (var i in animations) i.animation.Sort();
    }
    public override CancellationTokenSource Play(AnimableWeapon weapon, float playTimeSec, float delaySec) {
        //_ = Nodes;
        //base.Play(weapon, playTimeSec, delaySec);

        

        CancellationTokenSource source = new CancellationTokenSource();
        _ = _play(weapon, playTimeSec, delaySec, source.Token);
        return source;
    }
    private async UniTaskVoid _play(AnimableWeapon weapon, float playTimeSec, float delaySec, CancellationToken token) {
        float speed = playTimeSec > 0 ? GetAnimationTime() / playTimeSec : Mathf.Infinity;
        float lastTime = 0;
        CancellationTokenSource tokenEach;
        foreach (var i in animations) {
            tokenEach = i.animation.Play(weapon, i.timeSeconds / speed, lastTime + delaySec);
            if (token.IsCancellationRequested) {
                tokenEach.Cancel();
                break;
            }
            lastTime += i.timeSeconds / speed;
        }
        nodesOT = new WeaponAnimationNode[] { };
        await UniTask.Yield();
    }

    public override float GetAnimationTime() {
        return animations.Sum(t => t.timeSeconds);
    }

    [System.Serializable]
    class AWBundleElement {
        [SerializeField] public AnimableWeaponAnimation animation = default;
        [SerializeField] public float timeSeconds = 0.1f;
        public static implicit operator AnimableWeaponAnimation(AWBundleElement element) {
            return element.animation;
        }
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(AWBundledAnimation), true)]
public class AWBundledAnimEditor : Editor {
    public override void OnInspectorGUI() {
        this.serializedObject.UpdateIfRequiredOrScript();
        var iterator = this.serializedObject.GetIterator();
        for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false) {
            if (iterator.propertyPath == "nodes") {
                continue;
            }
            using (new EditorGUI.DisabledScope(iterator.propertyPath == "m_Script")) {
                EditorGUILayout.PropertyField(iterator, true);
            }
        }
        this.serializedObject.ApplyModifiedProperties();
    }
}
#endif