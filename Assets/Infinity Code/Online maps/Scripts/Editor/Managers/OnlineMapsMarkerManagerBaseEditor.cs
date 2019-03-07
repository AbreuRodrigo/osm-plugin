/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public abstract class OnlineMapsMarkerManagerBaseEditor<T, U> : Editor 
    where T: OnlineMapsMarkerManagerBase<T, U>
    where U: OnlineMapsMarkerBase
{
    protected T manager;
    protected OnlineMaps map;
    protected SerializedProperty items;

    protected abstract void AddMarker();

    protected virtual void DrawItem(int i, ref int removedIndex)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        OnlineMapsMarkerBasePropertyDrawer.isRemoved = false;
        OnlineMapsMarkerBasePropertyDrawer.isEnabledChanged = null;
        EditorGUILayout.PropertyField(items.GetArrayElementAtIndex(i), new GUIContent("Marker " + (i + 1)));
        if (OnlineMapsMarkerBasePropertyDrawer.isRemoved) removedIndex = i;
        if (OnlineMapsMarkerBasePropertyDrawer.isEnabledChanged.HasValue) manager[i].enabled = OnlineMapsMarkerBasePropertyDrawer.isEnabledChanged.Value;

        EditorGUILayout.EndVertical();
    }

    private void DrawItems(ref bool dirty)
    {
        int removedIndex = -1;

        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < items.arraySize; i++) DrawItem(i, ref removedIndex);

        if (EditorGUI.EndChangeCheck()) dirty = true;

        if (removedIndex != -1)
        {
            manager.RemoveAt(removedIndex);
            dirty = true;
        }

        EditorGUILayout.Space();
    }

    protected virtual void DrawSettings(ref bool dirty)
    {
    }

    private void OnEnable()
    {
        manager = target as T;
        map = manager.GetComponent<OnlineMaps>();
        items = serializedObject.FindProperty("_items");
        OnEnableLate();

        serializedObject.ApplyModifiedProperties();
    }

    protected virtual void OnEnableLate()
    {
    }

    public override void OnInspectorGUI()
    {
        bool dirty = false;

        DrawSettings(ref dirty);
        DrawItems(ref dirty);

        if (GUILayout.Button("Add Marker"))
        {
            AddMarker();
            dirty = true;
        }

        serializedObject.ApplyModifiedProperties();

        if (dirty)
        {
            EditorUtility.SetDirty(target);
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            else map.Redraw();
        }
    }
}