#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectGenerator))]
public class ObjectGeneratorEditor : Editor
{
   public override void OnInspectorGUI()
   {
      DrawDefaultInspector();

      ObjectGenerator generator = (ObjectGenerator)target;
      if (GUILayout.Button("Generate Objects"))
      {
         generator.GenerateObjectsButton();
      }

      if (GUILayout.Button("Clean Up Objects"))
         {
            generator.CleanUpObjectsButton();
         }
   }
}
#endif
