using UnityEngine;
using TMPro;

public class LifeUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private TextMeshProUGUI lifeText;

    void Update()
    {
        if (player == null || lifeText == null) return;

        lifeText.text = "Vida: " + player.CurrentLife;
    }
}