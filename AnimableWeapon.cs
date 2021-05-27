using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimableWeapon : MonoBehaviour {
    [SerializeField] AnimableWeaponAnimation[] weaponAnimations;
    public AnimableWeaponAnimation[] WeaponAnimations => weaponAnimations;

    [SerializeField] AnimableWeaponHand rightHand;
    [SerializeField] AnimableWeaponHand leftHand;
    public AnimableWeaponHand RightHand => rightHand;
    public AnimableWeaponHand LeftHand => leftHand;

    AnimableWeaponMoveable[] partsChildren = { };
    AnimableWeaponHandPoint[] handPoints = { };

    [SerializeField] UnityEvent[] events = default;
    public UnityEvent[] Events => events;

    CancellationTokenSource cancelToken;

    private void Awake() {
        GetParts();//<-これでパーツをpartsChildrenに代入する
        rightHand.SetWeaponUse(this);
        leftHand.SetWeaponUse(this);
    }
    private void Start() {
        RightHand.SetMove("Grip", 0);
        LeftHand.SetMove("Grip", 0);
    }
    private void OnEnable() {
        rightHand.SetWeaponUse(this);
        leftHand.SetWeaponUse(this);
    }


    public IEnumerable<AnimableWeaponMoveable> GetParts() {
        if (!partsChildren.Any()) {
            partsChildren = new AnimableWeaponMoveable[] { RightHand, LeftHand }.Concat(GetComponentsInChildren<AnimableWeaponMoveable>()).ToArray();
        }
        return partsChildren;
    }
    public IEnumerable<AnimableWeaponMoveable> GetPart(string partName) {
        return GetParts().Where(t => t.name == partName);
    }
    public IEnumerable<AnimableWeaponHandPoint> GetHandPoints() {
        if (!handPoints.Any()) {
            handPoints = GetComponentsInChildren<AnimableWeaponHandPoint>();
        }
        return handPoints;
    }
    public void StartAnimation(string animationName, float delay = 0) => 
        StartAnimation(GetAnimationIndex(animationName), delay);
    public void StartAnimation(int index, float delay = 0) =>
        StartAnimation(weaponAnimations[index].GetAnimationTime(), index, delay);
    public void StartAnimation(float timeSeconds, string animationName, float delaySecond = 0) =>
        StartAnimation(
            timeSeconds,
            GetAnimationIndex(animationName),
            delaySecond
            );

    public void StartAnimation(float timeSeconds, int index, float delaySecond = 0) {
        cancelToken = weaponAnimations[index].Play(this, timeSeconds, delaySecond);
    }

    public int GetAnimationIndex(string animationName) {
        var reg = new Regex(@"\([^>]*\)");
        return weaponAnimations.Select((item, index) => (item, index)).FirstOrDefault(t => reg.Replace(t.item.name, "") == animationName).index;
    }

    public void EventTest() {
        Debug.Log("eventTest");
    }

    public void AbortAnimation() {
        if (cancelToken != null)
            cancelToken.Cancel();
    }
    
}
