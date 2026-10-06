using UnityEditor;
using Player.Movement.Core.Events;

namespace Player.Movement.Editor
{
    // Custom Inspector for PlayerBus that removes the default read-only 'Script' field.
    // Must be located inside an 'Editor' folder so Unity excludes it from standalone game builds.
    [CustomEditor(typeof(PlayerBus))]
    public class PlayerBusEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw all serialized properties while explicitly hiding the default 'm_Script' field
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
