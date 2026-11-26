using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModularMovement
{
    // Holds the four team credits so their names and roles can be filled in the Inspector.
    public sealed class MenuCredits : MonoBehaviour
    {
        [Serializable]
        public sealed class TeamMember
        {
            public string Name = "Your name";
            [TextArea(2, 4)] public string Role = "Your role";
        }

        [SerializeField] private TeamMember[] team = { new TeamMember(), new TeamMember(), new TeamMember(), new TeamMember() };
        [SerializeField] private Text[] nameLabels;
        [SerializeField] private Text[] roleLabels;

        private void OnEnable() => Refresh();
        private void OnValidate() => Refresh();

        private void Refresh()
        {
            if (team == null) return;
            for (int index = 0; index < team.Length; index++)
            {
                if (team[index] == null) continue;
                if (nameLabels != null && index < nameLabels.Length && nameLabels[index] != null)
                    nameLabels[index].text = team[index].Name;
                if (roleLabels != null && index < roleLabels.Length && roleLabels[index] != null)
                    roleLabels[index].text = team[index].Role;
            }
        }
    }
}
