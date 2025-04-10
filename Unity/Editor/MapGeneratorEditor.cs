﻿using UnityEngine;
using System.Collections.Generic;

using UnityEditor;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldGenerator generator = (WorldGenerator)target;

        if (GUILayout.Button("Generate Map"))
        {
            generator.GenerateWorld();
            EditorUtility.SetDirty(generator);
        }

        if (GUILayout.Button("Clean Up"))
        {
            WorldGeneratorUtility.CleanUp(generator.transform);
            EditorUtility.SetDirty(generator);
        }

        if (GUILayout.Button("Display Main Map"))
        {
            if (generator.GlobalNoiseMap == null || generator.GlobalColourMap == null)
            {
                generator.GenerateWorld();
            }
            generator.DisplayMainMap();
            EditorUtility.SetDirty(generator);
        }

        if (GUILayout.Button("Clear Preview"))
        {
            generator.ClearMainMap();
            EditorUtility.SetDirty(generator);
        }
    }
}
