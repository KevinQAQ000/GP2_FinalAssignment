using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector content.
        DrawDefaultInspector();

        MapGenerator script = (MapGenerator)target;// Get a reference to the MapGenerator script

        // Draw button
        if (GUILayout.Button("Generate Map"))
        {
            script.GenerateMap();// Call the GenerateMap method when the button is clicked
        }

        //if (GUILayout.Button("TestVertex"))
        //{
        //    script.TestVertex();
        //}
    }
}