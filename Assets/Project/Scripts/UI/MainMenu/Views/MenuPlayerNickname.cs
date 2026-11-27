using UnityEngine;
using UnityEngine.UI;

namespace ModularMovement
{
    // Edits and remembers a nickname before the player hosts or joins a game.
    [DisallowMultipleComponent]
    public sealed class MenuPlayerNickname : MonoBehaviour
    {
        [SerializeField] private InputField input;
        public bool HasValidNickname => input != null && PlayerNicknameProfile.IsValid(input.text);

        private void OnEnable()
        {
            if (input == null) return;
            input.characterLimit = PlayerNicknameProfile.MaximumLength;
            input.onValidateInput = ValidateCharacter;
            input.SetTextWithoutNotify(PlayerNicknameProfile.SavedName);
            input.onValueChanged.AddListener(RememberDraft);
            input.onEndEdit.AddListener(FinishEditing);
        }

        private static char ValidateCharacter(string text, int index, char character)
            => PlayerNicknameProfile.IsAllowed(character) ? character : '\0';

        private static void RememberDraft(string value) => PlayerNicknameProfile.Save(value);

        private void FinishEditing(string value)
        {
            input.SetTextWithoutNotify(PlayerNicknameProfile.Normalize(value));
            PlayerNicknameProfile.Save(input.text);
            PlayerPrefs.Save();
        }

        public bool TryCommit()
        {
            if (!HasValidNickname) return false;
            FinishEditing(input.text);
            return true;
        }

        public void SetEditable(bool editable)
        {
            if (input != null) input.interactable = editable;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.onValueChanged.RemoveListener(RememberDraft);
            input.onEndEdit.RemoveListener(FinishEditing);
            PlayerPrefs.Save();
        }
    }
}
