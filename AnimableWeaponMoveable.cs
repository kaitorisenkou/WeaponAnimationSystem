using UnityEngine;
#if UNITY_EDITOR
#endif

public abstract class AnimableWeaponMoveable : MonoBehaviour {
    [SerializeField] protected bool disableWarning = false;
    public abstract string[] GetPointNames();
    public abstract void SetMove(string pointName, float timeSec, string bezierPointName = "");
    public abstract void SetMove(string pointName, float timeSec, AnimationCurve animationCurve, string bezierPointName = "");
}