using UnityEngine;
using TMPro;

public class LifeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lifeText;

    private PlayerController player;

    void Update()
    {
        if (lifeText == null) return;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player == null) return;

        lifeText.text = "Vida: " + player.CurrentLife;
    }
}