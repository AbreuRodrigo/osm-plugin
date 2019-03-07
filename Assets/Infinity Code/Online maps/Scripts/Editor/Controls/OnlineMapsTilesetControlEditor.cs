/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (OnlineMapsTileSetControl), true)]
public class OnlineMapsTilesetControlEditor : OnlineMapsControlBaseDynamicMeshEditor<OnlineMapsTileSetControl>
{
    private bool showShaders;
    private Shader defaultTilesetShader;
    private SerializedProperty pCheckMarker2DVisibility;
    private SerializedProperty pTileMaterial;
    private SerializedProperty pMarkerMaterial;
    private SerializedProperty pTilesetShader;
    private SerializedProperty pMarkerShader;
    private SerializedProperty pDrawingShader;
    private SerializedProperty pColliderType;
    private SerializedProperty pMipmapForTiles;
    private SerializedProperty pSizeInScene;

    protected override void CacheSerializedFields()
    {
        base.CacheSerializedFields();

        pCheckMarker2DVisibility = serializedObject.FindProperty("checkMarker2DVisibility");
        pTileMaterial = serializedObject.FindProperty("tileMaterial");
        pMarkerMaterial = serializedObject.FindProperty("markerMaterial");
        pTilesetShader = serializedObject.FindProperty("tilesetShader");
        pMarkerShader = serializedObject.FindProperty("markerShader");
        pDrawingShader = serializedObject.FindProperty("drawingShader");
        pColliderType = serializedObject.FindProperty("colliderType");
        pMipmapForTiles = serializedObject.FindProperty("_mipmapForTiles");
        pSizeInScene = serializedObject.FindProperty("sizeInScene");
    }

    private void CheckCameraDistance()
    {
        if (EditorApplication.isPlaying) return;

        Camera tsCamera = pActiveCamera.objectReferenceValue != null ? pActiveCamera.objectReferenceValue as Camera : Camera.main;

        if (tsCamera == null) return;

        Vector3 mapCenter = map.transform.position + new Vector3(control.sizeInScene.x / -2, 0, control.sizeInScene.y / 2);
        float distance = (tsCamera.transform.position - mapCenter).magnitude * 1.5f;
        if (distance <= tsCamera.farClipPlane) return;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.HelpBox("Potential problem detected:\n\"Camera - Clipping Planes - Far\" is too small.", MessageType.Warning);

        if (GUILayout.Button("Fix Clipping Planes - Far")) tsCamera.farClipPlane = distance;

        EditorGUILayout.EndVertical();
    }

    private void CheckColliderType()
    {
        if (EditorApplication.isPlaying) return;

        if (pColliderType.enumValueIndex != (int) OnlineMapsTileSetControl.OnlineMapsColliderType.box && pColliderType.enumValueIndex != (int) OnlineMapsTileSetControl.OnlineMapsColliderType.flatBox) return;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.HelpBox("Potential problem detected:\nWhen using BoxCollider, can be a problem in interaction with a map with elevation.", MessageType.Warning);
        if (GUILayout.Button("Set Collider Type - Full Mesh")) pColliderType.enumValueIndex = (int) OnlineMapsTileSetControl.OnlineMapsColliderType.fullMesh;

        EditorGUILayout.EndVertical();
    }

    private void DrawMoveCamera()
    {
        if (!GUILayout.Button("Move camera to center of Tileset")) return;
        if (map == null) return;

        Camera tsCamera = pActiveCamera.objectReferenceValue != null ? pActiveCamera.objectReferenceValue as Camera : Camera.main;

        if (tsCamera == null)
        {
            Debug.Log("Camera is null");
            return;
        }

        GameObject go = tsCamera.gameObject;
        float minSide = Mathf.Min(control.sizeInScene.x * map.transform.lossyScale.x, control.sizeInScene.y * map.transform.lossyScale.z);
        Vector3 position = map.transform.position + map.transform.rotation * new Vector3(control.sizeInScene.x / -2 * map.transform.lossyScale.x, minSide, control.sizeInScene.y / 2 * map.transform.lossyScale.z);
        go.transform.position = position;
        go.transform.rotation = map.transform.rotation * Quaternion.Euler(90, 180, 0);
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        warningLayoutItem.Create("checkCameraDistance", CheckCameraDistance);

        rootLayoutItem.Create(pSizeInScene).priority = -1;
        rootLayoutItem["marker2DMode"].Create(pCheckMarker2DVisibility).OnValidateDraw += () => pMarker2DMode.enumValueIndex == (int)OnlineMapsMarker2DMode.flat;
        rootLayoutItem.Create(pColliderType).disabledInPlaymode = true;
        rootLayoutItem.Create("colliderWarning", CheckColliderType);

        GenerateMaterialsLayout();

        rootLayoutItem.Create("moveCamera", DrawMoveCamera);
    }

    private void GenerateMaterialsLayout()
    {
        LayoutItem mats = rootLayoutItem.Create("materialsAndShaders");
        mats.drawGroup = LayoutItem.Group.validated;
        mats.drawGroupBorder = true;
        mats.OnValidateDrawChilds += () => showShaders;
        mats.action += () => { showShaders = GUILayout.Toggle(showShaders, "Materials & Shaders", EditorStyles.foldout); };
        mats.Create(pTileMaterial);
        mats.Create(pMarkerMaterial);
        mats.Create(pTilesetShader).OnChanged += () =>
        {
            if (pTilesetShader.objectReferenceValue == null) pTilesetShader.objectReferenceValue = defaultTilesetShader;
        };
        mats.Create(pMarkerShader).OnChanged += () =>
        {
            if (pMarkerShader.objectReferenceValue == null) pMarkerShader.objectReferenceValue = Shader.Find("Transparent/Diffuse");
        };
        mats.Create(pDrawingShader).OnChanged += () =>
        {
            if (pDrawingShader.objectReferenceValue == null) pDrawingShader.objectReferenceValue = Shader.Find("Infinity Code/Online Maps/Tileset DrawingElement");
        };
        mats.Create(pMipmapForTiles);
    }

    protected override void OnEnableLate()
    {
        base.OnEnableLate();

        defaultTilesetShader = Shader.Find("Infinity Code/Online Maps/Tileset");

        if (pTilesetShader.objectReferenceValue == null) pTilesetShader.objectReferenceValue = defaultTilesetShader;
        if (pMarkerShader.objectReferenceValue == null) pMarkerShader.objectReferenceValue = Shader.Find("Transparent/Diffuse");
        if (pDrawingShader.objectReferenceValue == null) pDrawingShader.objectReferenceValue = Shader.Find("Infinity Code/Online Maps/Tileset DrawingElement");
    }

    private void OnSceneGUI()
    {
        if (Application.isPlaying) return;

        OnlineMaps map = control.GetComponent<OnlineMaps>();
        if (map == null) return;
        Quaternion rotation = map.transform.rotation;
        Vector3 scale = map.transform.lossyScale;
        Vector3[] points = new Vector3[5];
        points[0] = points[4] = map.transform.position;
        points[1] = points[0] + rotation * new Vector3(-control.sizeInScene.x * scale.x, 0, 0);
        points[2] = points[1] + rotation * new Vector3(0, 0, control.sizeInScene.y * scale.z);
        points[3] = points[0] + rotation * new Vector3(0, 0, control.sizeInScene.y * scale.z);
        Handles.DrawSolidRectangleWithOutline(points, new Color(1, 1, 1, 0.3f), Color.black);

        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = {textColor = Color.black}
        };

        Handles.Label(points[0] + rotation * new Vector3(control.sizeInScene.x / -2 * scale.x, 0, control.sizeInScene.y / 2 * scale.z), "Tileset Map", style);
    }
}