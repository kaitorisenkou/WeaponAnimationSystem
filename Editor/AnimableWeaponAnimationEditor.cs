using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

public class AnimableWeaponAnimationEditor : EditorWindow {
    static AnimableWeaponAnimationEditor window;
    static AnimableWeapon selectedWeapon;
    static int selectedAnim = 0;
    static WeaponAnimationNode selectedNode = null;

    Vector2 editPanelScroll = Vector2.zero;
    Vector2 linePanelScroll = Vector2.zero;

    [MenuItem("Window/WeaponAnimationEditor")]
    static void Open() {
        GetWindow<AnimableWeaponAnimationEditor>();
    }
    private void OnEnable() {
        if (!window)
            window = GetWindow<AnimableWeaponAnimationEditor>();
    }
    private void OnGUI() {
        if (Selection.gameObjects.Any()) {
            GameObject selected = Selection.gameObjects.First();
            AnimableWeapon weapon;
            if (weapon = selected.GetComponentInParent<AnimableWeapon>()) {
                selectedWeapon = weapon;
            }
        }
        if (!selectedWeapon) {
            EditorGUILayout.LabelField("Nothing Selected");
            return;
        }
        EditorGUILayout.LabelField("Editing:" + selectedWeapon.gameObject.name);
        SerializedObject serializedObject = new SerializedObject(selectedWeapon);
        SerializedProperty animationsProp = serializedObject.FindProperty("weaponAnimations");
        //EditorGUILayout.LabelField(animationsProp.arraySize + "");

        

        selectedWeapon.WeaponAnimations[selectedAnim].Sort();
        var nodes = selectedWeapon.WeaponAnimations[selectedAnim].Nodes.Select((val, index) => (val, index));
        var parts = selectedWeapon.GetParts().ToArray();
        var partsNames = parts.Select((t, index) => (t.name, index));

        var anim = new SerializedObject(animationsProp.GetArrayElementAtIndex(selectedAnim).objectReferenceValue);

        GUILayout.BeginHorizontal();
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MaxWidth(window.position.size.x / 3), GUILayout.MinWidth(220f));
            editPanelScroll = GUILayout.BeginScrollView(editPanelScroll);
            {
                //編集パネル
                if (selectedWeapon.WeaponAnimations[selectedAnim].GetType() == typeof(AnimableWeaponAnimation)) {
                    if (GUILayout.Button("Add Node")) {
                        var nodeProp = anim.FindProperty("nodes");
                        nodeProp.InsertArrayElementAtIndex(0);
                        anim.ApplyModifiedProperties();
                        //配列が変わったので取得しなおし
                        selectedWeapon.WeaponAnimations[selectedAnim].Sort();
                        nodes = selectedWeapon.WeaponAnimations[selectedAnim].Nodes.Select((val, index) => (val, index));
                        parts = selectedWeapon.GetParts().ToArray();
                        partsNames = parts.Select((t, index) => (t.name, index));
                        selectedNode = selectedWeapon.WeaponAnimations[selectedAnim].Nodes[0];
                    }
                    if (selectedNode == null || !nodes.Any(t => t.val == selectedNode)) {
                        EditorGUILayout.LabelField("Select a node.");
                        EditorGUILayout.LabelField("--->");
                        selectedNode = null;
                    } else {
                        GUILayout.Space(15);
                        var nodeIndex = nodes.First(t => t.val == selectedNode).index;
                        var node = anim.FindProperty("nodes").GetArrayElementAtIndex(nodeIndex);

                        var partNameProp = node.FindPropertyRelative("partName");
                        int partIndex = partsNames.FirstOrDefault(t => t.name == partNameProp.stringValue).index;
                        var newPartIndex = EditorGUILayout.Popup("Part", partIndex, partsNames.Select(t => t.name).ToArray());
                        partNameProp.stringValue = partsNames.FirstOrDefault(t => t.index == newPartIndex).name;

                        var typeProp = node.FindPropertyRelative("nodeType");
                        typeProp.enumValueIndex = (int)(WeaponAnimationNode.AnimNodeType)EditorGUILayout.EnumPopup((WeaponAnimationNode.AnimNodeType)typeProp.enumValueIndex);
                        var staTimProp = node.FindPropertyRelative("startTime");
                        var LenTimProp = node.FindPropertyRelative("timeLengthSecond");
                        staTimProp.floatValue = EditorGUILayout.FloatField("Time", staTimProp.floatValue);
                        switch (typeProp.enumValueIndex) {
                            case 0:
                                var tgtPoiProp = node.FindPropertyRelative("targetPointName");
                                var bezierProp = node.FindPropertyRelative("bezierPointName");
                                var points = parts[newPartIndex].GetPointNames();

                                LenTimProp.floatValue = EditorGUILayout.FloatField("Length", LenTimProp.floatValue);

                                if (tgtPoiProp.stringValue == "") tgtPoiProp.stringValue = points[0];
                                int selected = points.Select((t, i) => (t, i)).FirstOrDefault(t => t.t == tgtPoiProp.stringValue).i;
                                tgtPoiProp.stringValue = points[EditorGUILayout.Popup("Target Point", selected, points)];

                                points = new string[] { "(none)" }.Concat(points).ToArray();
                                if (bezierProp.stringValue == "") bezierProp.stringValue = "none";
                                selected = points.Select((t, i) => (t, i)).FirstOrDefault(t => t.t == bezierProp.stringValue).i;
                                bezierProp.stringValue = points[EditorGUILayout.Popup("Bezier Point", selected, points)];
                                if (bezierProp.stringValue == "(none)") bezierProp.stringValue = "";
                                break;
                            case 1:
                                LenTimProp.floatValue = 0;
                                var enableProp = node.FindPropertyRelative("enableSet");
                                int a = EditorGUILayout.Popup(enableProp.boolValue ? 1 : 0, new string[] { "Disable", "Enable" });
                                enableProp.boolValue = a == 1;
                                break;
                            case 2:
                                LenTimProp.floatValue = 0;
                                var events = selectedWeapon.Events;
                                if (!events.Any()) {
                                    EditorGUILayout.HelpBox("Assign events first at the Weapon component.", MessageType.Warning);
                                    break;
                                }
                                var eventIndex = node.FindPropertyRelative("eventIndex");
                                eventIndex.intValue = EditorGUILayout.IntField("Index", eventIndex.intValue);
                                if (eventIndex.intValue >= events.Length) eventIndex.intValue = events.Length - 1;
                                string methods = "";
                                for (int i = 0; i < events[eventIndex.intValue].GetPersistentEventCount(); i++) {
                                    methods += events[eventIndex.intValue].GetPersistentMethodName(i) + "\n";
                                }
                                if (methods == "") methods = "none";
                                EditorGUILayout.HelpBox(methods, MessageType.None);
                                break;
                        }
                        GUILayout.Space(15);
                        if (GUILayout.Button("Delete This Node")) {
                            var nodeProp = anim.FindProperty("nodes");
                            int index = nodes.FirstOrDefault(t => t.val == selectedNode).index;
                            nodeProp.DeleteArrayElementAtIndex(index);
                        }

                        anim.ApplyModifiedProperties();
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            linePanelScroll = GUILayout.BeginScrollView(linePanelScroll);
            GUILayout.BeginVertical(GUI.skin.box,GUILayout.ExpandHeight(true));
            {
                GUILayout.BeginHorizontal();
                {
                    if (animationsProp.arraySize > 0) {
                        string[] animNames = new bool[animationsProp.arraySize].Select((t, i) => animationsProp.GetArrayElementAtIndex(i).objectReferenceValue.name).ToArray();
                        selectedAnim = EditorGUILayout.Popup(selectedAnim, animNames, GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Delete This Animation", GUILayout.ExpandWidth(false))) {
                            animationsProp.DeleteArrayElementAtIndex(selectedAnim);
                            animationsProp.MoveArrayElement(selectedAnim, animationsProp.arraySize - 1);
                            animationsProp.arraySize = animationsProp.arraySize - 1;
                            serializedObject.ApplyModifiedProperties();
                            if (selectedAnim >= animationsProp.arraySize) selectedAnim = animationsProp.arraySize - 1;
                        }
                    } else {
                        EditorGUILayout.LabelField("No Animtions", GUILayout.ExpandWidth(false));
                    }

                    if (GUILayout.Button("Create New Animation", GUILayout.ExpandWidth(false))) {
                        animationsProp.InsertArrayElementAtIndex(animationsProp.arraySize);
                        var obj = ScriptableObject.CreateInstance<AnimableWeaponAnimation>();
                        string path = "Assets/NewWeaponAnimation";
                        int dabuValue = 0;
                        while (AssetDatabase.LoadAssetAtPath<AnimableWeaponAnimation>((dabuValue > 0 ? path + dabuValue : path) + ".asset")) {
                            dabuValue++;
                        }
                        AssetDatabase.CreateAsset(obj, ((dabuValue > 0 ? path + dabuValue : path) + ".asset"));
                        animationsProp.GetArrayElementAtIndex(animationsProp.arraySize - 1).objectReferenceValue = obj;
                        serializedObject.ApplyModifiedProperties();
                    }

                }
                GUILayout.EndHorizontal();

                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                if (selectedWeapon.WeaponAnimations[selectedAnim].GetType() == typeof(AnimableWeaponAnimation)) {
                    if (!nodes.Any()) {
                    } else {

                        GUILayout.BeginHorizontal();
                        var last = nodes.OrderByDescending(t => t.val.StartTime + t.val.TimeLengthSecond).First().val;
                        int lastPos = Mathf.CeilToInt((last.StartTime + last.TimeLengthSecond) * 10);
                        for (int i = 0; i <= lastPos; i++) {
                            GUILayout.Box("", GUILayout.Width(1), GUILayout.Height(15));
                            GUILayout.Label(i / 10f + "sec", GUILayout.Width(191.66f));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                        var nodesPlace = nodes.ToList();
                        while (nodesPlace.Any()) {
                            GUILayout.BeginHorizontal();
                            int i = 0;
                            float nextTime = 0;
                            while (i < nodesPlace.Count()) {
                                if (nextTime <= nodesPlace[i].val.StartTime) {
                                    float space = nodesPlace[i].val.StartTime - nextTime;
                                    GUILayout.Space(space * 2000);
                                    var exColor = GUI.color;
                                    if (selectedNode == nodesPlace[i].val) GUI.color = new Color(0.5f, 0.6f, 0.7f);

                                    GUIContent content = new GUIContent(nodesPlace[i].val.PartName);
                                    float width = Mathf.Max(0.02f, nodesPlace[i].val.TimeLengthSecond) * 2000 - 3.66f;
                                    switch (nodesPlace[i].val.NodeType) {
                                        case WeaponAnimationNode.AnimNodeType.move:
                                            content.image = (Texture)EditorGUIUtility.Load("MoveTool"); break;
                                        case WeaponAnimationNode.AnimNodeType.toggleEnable:
                                            content.image = (Texture)EditorGUIUtility.Load("animationvisibilitytoggleon"); break;
                                    }
                                    if (content.text.Length > (int)(width / 12)) content.text = content.text.Substring(0, (int)(width / 12) - 2) + "…";
                                    if (GUILayout.Button(
                                        content, GUI.skin.box,
                                        GUILayout.Width(width),
                                        GUILayout.Height(20))
                                        ) {
                                        selectedNode = nodesPlace[i].val;
                                    }

                                    GUI.color = exColor;
                                    nextTime = nodesPlace[i].val.StartTime + Mathf.Max(0.03f, nodesPlace[i].val.TimeLengthSecond);
                                    nodesPlace.RemoveAt(i);
                                } else {
                                    i++;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                    }
                } else {
                    GUILayout.Label("This editor supports super-class of AWAnimation only.");
                }
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
        }
        GUILayout.EndHorizontal();
    }
}
