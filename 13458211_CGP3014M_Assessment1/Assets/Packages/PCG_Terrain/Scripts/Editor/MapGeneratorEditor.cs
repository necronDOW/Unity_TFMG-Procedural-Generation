using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator component = (MapGenerator)target;
        
        if (DrawDefaultInspector() && component.autoUpdate)
            component.DrawMap_Editor();

        if (GUILayout.Button("Generate"))
            component.DrawMap_Editor();
    }
}