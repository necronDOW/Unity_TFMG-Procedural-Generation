using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator component = (MapGenerator)target;
        
        if (DrawDefaultInspector() && component.autoUpdate)
        {
            component.GenerateMap();
        }

        if (GUILayout.Button("Generate"))
        {
            component.GenerateMap();
        }
    }
}