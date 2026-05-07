using Unity.VisualScripting;
using UnityEngine;

public class ParedeQuebravel : MonoBehaviour, IDamageable
{
    [Header("Configuração")]
    [SerializeField] private int maxLife = 3;

    [Header("Opcional")]
    [SerializeField] private GameObject efeitoDeQuebra;

    private int currentLife;
    private bool destroyed;

    void Start()
    {
        currentLife = maxLife;
    }

    public void TakeDamage(int amount, GameObject source)
    {
        if (destroyed || amount <= 0)
            return;

        PlayerController player = source.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        currentLife -= amount;

        Debug.Log($"[PAREDE] tomou {amount} | Vida restante: {currentLife}");

        if (currentLife <= 0)
            Break();
    }

    private void Break()
    {
        destroyed = true;

        Debug.Log("[PAREDE] Quebrada PERMANENTEMENTE INRREVERSIVELMENTE!");

        // efeito opicionaal de quebra uuiuiui
        if (efeitoDeQuebra != null)
            Instantiate(efeitoDeQuebra, transform.position, Quaternion.identity);

       // desativa colisao hehe
       Collider2D colisaoOtaria = GetComponent<Collider2D>();
       if (colisaoOtaria != null)
            colisaoOtaria.enabled = false;

       // desativa sprite
       SpriteRenderer srGuimyb = GetComponent<SpriteRenderer>();
        if (srGuimyb != null)
            srGuimyb.enabled = false;

        // destroi o objeto ai kkkkkkkkkkkkkk
        Destroy(gameObject);

    }
}
