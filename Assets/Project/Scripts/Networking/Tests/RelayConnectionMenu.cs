#if KUCHARZE_ONLINE
using UnityEngine;

namespace ModularMovement
{
    // Gives the first online test a small connection panel without requiring a Canvas setup.
    [DisallowMultipleComponent, RequireComponent(typeof(RelayConnection))]
    public sealed class RelayConnectionMenu : MonoBehaviour
    {
        private RelayConnection connection;
        private string enteredCode = "";

        private void Awake() => connection = GetComponent<RelayConnection>();

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 360f, 250f), GUI.skin.box);
            GUILayout.Label("Online play");
            GUILayout.Label(connection.Status);
            if (connection.JoinCode.Length > 0)
            {
                GUILayout.Label("Join code: " + connection.JoinCode);
                if (GUILayout.Button("Copy code")) GUIUtility.systemCopyBuffer = connection.JoinCode;
            }
            GUI.enabled = !connection.IsBusy && !connection.IsListening;
            if (GUILayout.Button("Host")) connection.Host();
            enteredCode = GUILayout.TextField(enteredCode, 16);
            if (GUILayout.Button("Join")) connection.Join(enteredCode);
            GUI.enabled = true;
            if (connection.IsBusy || connection.IsListening)
                if (GUILayout.Button("Disconnect / Cancel")) connection.Disconnect();
            GUILayout.EndArea();
        }
    }
}
#endif
