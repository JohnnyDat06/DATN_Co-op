using UnityEngine;
using TMPro;
using Game.Interactions;

namespace Game.UI
{
    public class InteractionUI : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private TextMeshProUGUI promptText;

        private void OnEnable()
        {
            PlayerInteraction.OnInteractableFound += Show;
            PlayerInteraction.OnInteractableLost += Hide;
            visualRoot.SetActive(false);
        }

        private void OnDisable()
        {
            PlayerInteraction.OnInteractableFound -= Show;
            PlayerInteraction.OnInteractableLost -= Hide;
        }

        private void Show(IInteractable interactable)
        {
            if (promptText != null)
            {
                promptText.text = interactable.InteractionPrompt;
            }
            visualRoot.SetActive(true);
        }

        private void Hide()
        {
            visualRoot.SetActive(false);
        }
    }
}
