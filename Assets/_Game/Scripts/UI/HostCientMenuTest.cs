using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HostCientMenuTest : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;


    private void Awake()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        clientButton.onClick.AddListener(OnClientClicked);
    }

    private void OnHostClicked()
    {
        NetworkManager.Singleton.StartHost();
        Hide();
    }

    private void OnClientClicked()
    {
        NetworkManager.Singleton.StartClient();
        Hide();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
