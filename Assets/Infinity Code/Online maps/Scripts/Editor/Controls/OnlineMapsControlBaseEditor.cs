/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsControlBase), true)]
public abstract class OnlineMapsControlBaseEditor<T> : OnlineMapsFormattedEditor
    where T : OnlineMapsControlBase
{
    protected OnlineMaps map;
    protected T control;

    protected LayoutItem warningLayoutItem;

    protected SerializedProperty pAllowUserControl;
    protected SerializedProperty pAllowZoom;
    protected SerializedProperty pInvertTouchZoom;
    protected SerializedProperty pZoomInOnDoubleClick;
    protected SerializedProperty pZoomMode;
    private SerializedProperty pSmoothZoom;

    protected override void CacheSerializedFields()
    {
        pAllowUserControl = serializedObject.FindProperty("allowUserControl");
        pAllowZoom = serializedObject.FindProperty("allowZoom");
        pInvertTouchZoom = serializedObject.FindProperty("invertTouchZoom");
        pZoomInOnDoubleClick = serializedObject.FindProperty("zoomInOnDoubleClick");
        pZoomMode = serializedObject.FindProperty("zoomMode");
        pSmoothZoom = serializedObject.FindProperty("smoothZoom");
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        warningLayoutItem = rootLayoutItem.Create("WarningArea");
        rootLayoutItem.Create(pAllowUserControl);
        LayoutItem lZoom = rootLayoutItem.Create(pAllowZoom);
        lZoom.drawGroup = LayoutItem.Group.valueOn;
        lZoom.Create(pZoomMode);
        lZoom.Create(pZoomInOnDoubleClick);
        lZoom.Create(pInvertTouchZoom);
        lZoom.Create(pSmoothZoom);
    }

    private static OnlineMaps GetOnlineMaps(OnlineMapsControlBase control)
    {
        OnlineMaps map = control.GetComponent<OnlineMaps>();

        if (map == null)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.HelpBox("Problem detected:\nCan not find OnlineMaps component.", MessageType.Error);

            if (GUILayout.Button("Add OnlineMaps Component"))
            {
                map = control.gameObject.AddComponent<OnlineMaps>();
                UnityEditorInternal.ComponentUtility.MoveComponentUp(map);
            }

            EditorGUILayout.EndVertical();
        }
        return map;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        map = null;
        control = null;
    }

    protected override void OnEnableBefore()
    {
        base.OnEnableBefore();

        control = (T)target;
        map = GetOnlineMaps(control);
        if (control.GetComponent<OnlineMapsMarkerManager>() == null) control.gameObject.AddComponent<OnlineMapsMarkerManager>();
    }

    protected override void OnSetDirty()
    {
        base.OnSetDirty();

        EditorUtility.SetDirty(map);
        EditorUtility.SetDirty(control);

        if (Application.isPlaying) map.Redraw();
    }
}