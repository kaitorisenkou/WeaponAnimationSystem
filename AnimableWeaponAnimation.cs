using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New WeaponAnimation", menuName = "Create WeaponAnimation")]
public class AnimableWeaponAnimation : ScriptableObject {
    [SerializeField] protected WeaponAnimationNode[] nodes = { };
    public virtual WeaponAnimationNode[] Nodes => nodes;
    public virtual void Sort() {
        if (nodes == null || !nodes.Any()) {
            return;
        }
        nodes = nodes.OrderBy(t => t.StartTime/*+t.TimeLengthSecond*/).ToArray();
    }

    public CancellationTokenSource Play(AnimableWeapon weapon, float playTimeSec) =>
        Play(weapon, playTimeSec, 0);
    public CancellationTokenSource Play(AnimableWeapon weapon) => Play(weapon, 1f, 0);

    public virtual CancellationTokenSource Play(AnimableWeapon weapon, float playTimeSec, float delaySec) {
        if (!Nodes.Any()) {
            if (this.name != "doNothing") Debug.LogWarning("Nodes are empty!");
            return null;
        }
        float speed = playTimeSec > 0 ? GetAnimationTime() / playTimeSec : Mathf.Infinity;

        float lineTime = 0;
        var parts = weapon.GetParts();
        var line = Nodes.Select(t => {
            float delay = Mathf.Clamp(t.StartTime - lineTime, 0, t.StartTime);
            lineTime = t.StartTime;
            var part = parts.FirstOrDefault(p => p.name == t.PartName);
            return (part, delay);
        }).ToArray();

        /*
        //Debug.Log(weapon + ":");
        for (int i = 0; i < line.Length; i++) {
            //Debug.Log("    " + line[i].part + " " + Nodes[i].NodeType + ", delay:" + line[i].delay);
        }
        */

        CancellationTokenSource source = new CancellationTokenSource();
        _ = _play(weapon, delaySec, speed, line, source.Token);
        return source;
    }

    private async UniTaskVoid _play(AnimableWeapon weapon,float delay, float speed, (AnimableWeaponMoveable, float)[] line,CancellationToken token) {
        await UniTask.Delay(delayTimeSpan: System.TimeSpan.FromSeconds(Mathf.Abs(delay)));

        await UniTask.SwitchToThreadPool();
        for(int i = 0; i < Nodes.Length; i++) {
            if (token.IsCancellationRequested) {
                break;
            }
            await UniTask.Delay(delayTimeSpan: System.TimeSpan.FromSeconds(Mathf.Max(line[i].Item2 / speed, 0)));
            switch ((int)Nodes[i].NodeType) {
                case 0://move
                    line[i].Item1.SetMove(Nodes[i].TargetPointName, Nodes[i].TimeLengthSecond / speed, Nodes[i].BezierPointName);
                    break;
                case 1://toggleEnable
                    var go = line[i].Item1.gameObject;
                    MeshRenderer renderer;
                    if(renderer = go.GetComponent<MeshRenderer>()) {
                        renderer.enabled = (Nodes[i].EnableSet);
                    } else {
                        go.gameObject.SetActive(Nodes[i].EnableSet);
                    }
                    break;
                case 2://event
                    weapon.Events[Nodes[i].EventIndex].Invoke();
                    break;
            }
        }
    }

    public virtual float GetAnimationTime() {
        var last = Nodes.OrderByDescending(t => t.StartTime + t.TimeLengthSecond).First();
        return last.StartTime + last.TimeLengthSecond;
    }
}
[System.Serializable]
public class WeaponAnimationNode {
    [SerializeField] string partName;

    [SerializeField] AnimNodeType nodeType = AnimNodeType.move;

    [SerializeField] float startTime;
    [SerializeField] float timeLengthSecond;
    [SerializeField] string targetPointName;
    [SerializeField] string bezierPointName;

    [SerializeField] bool enableSet;

    [SerializeField] int eventIndex = default;

    public AnimNodeType NodeType => nodeType;

    public string PartName => partName;
    public float StartTime => startTime;
    public float TimeLengthSecond => timeLengthSecond;
    public string TargetPointName => targetPointName;
    public string BezierPointName => bezierPointName;

    public bool EnableSet => enableSet;
    public int EventIndex => eventIndex;
    

    public enum AnimNodeType {
        move,
        toggleEnable,
        unityEvent
    }
}
