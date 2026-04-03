using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class InteractionUI : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private TextMeshProUGUI promptText;

        private void OnEnable()
        {
            PlayerInteractor.OnInteractableFound += Show;
            PlayerInteractor.OnInteractableLost += Hide;

            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
        }

        private void OnDisable()
        {
            PlayerInteractor.OnInteractableFound -= Show;
            PlayerInteractor.OnInteractableLost -= Hide;
        }

        private void Show(IInteractable interactable)
        {
            if (promptText != null)
            {
                promptText.text = interactable.InteractionPrompt;
            }

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }
        }

        private void Hide()
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
        }
    }
}
