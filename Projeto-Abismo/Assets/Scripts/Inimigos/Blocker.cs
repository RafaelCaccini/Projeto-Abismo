using UnityEngine;

public class Blocker : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform pontoFogo;
    [SerializeField] private GameObject projetilPrefab;
    [SerializeField] private Transform player;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float arcForce = 8f;

    [Header("Debug")]
    [SerializeField] private bool esconderPontoFogo = true;

    private float timer;

    void Start()
    {
        if (esconderPontoFogo && pontoFogo != null)
        {
            var sr = pontoFogo.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = false;
        }
    }

    void Update()
    {
        // sempre tenta garantir player
        if (player == null)
        {
            FindPlayer();
        }

        // sem player = não ataca, mas continua rodando
        if (player == null) return;

        timer += Time.deltaTime;

        if (timer >= attackCooldown)
        {
            // tenta atacar; só reseta o timer se um ataque foi realmente disparado
            bool fired = Attack();
            if (fired)
                timer = 0f;
            else
                // se falhou, tenta novamente mais cedo (metade do cooldown)
                timer = attackCooldown * 0.5f;
        }
    }

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;
    }

    // retorna true se um projétil foi efetivamente instanciado
    bool Attack()
    {
        if (player == null || pontoFogo == null || projetilPrefab == null)
        {
            Debug.LogWarning("Blocker: impossível atacar - referência ausente.");
            return false;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        bool useArc = distance > 4f ? true : Random.value > 0.5f;

        if (useArc)
            ShootArc();
        else
            ShootStraight();

        return true;
    }

    void ShootStraight()
    {
        Vector2 direction = (player.position - pontoFogo.position).normalized;

        GameObject proj = Instantiate(projetilPrefab, pontoFogo.position, Quaternion.identity);

        var p = proj.GetComponent<ProjetilBlocker>();
        if (p == null)
        {
            Debug.LogError("❌ Prefab sem ProjetilBlocker!");
            return;
        }

        p.Launch(direction, projectileSpeed, 0f);
    }

    void ShootArc()
    {
        Vector2 direction = (player.position - pontoFogo.position).normalized;

        // leve inclinação pra cima
        Vector2 arcDirection = new Vector2(direction.x, direction.y + 0.5f).normalized;

        GameObject proj = Instantiate(projetilPrefab, pontoFogo.position, Quaternion.identity);

        var p = proj.GetComponent<ProjetilBlocker>();
        if (p == null)
        {
            Debug.LogError("❌ Prefab sem ProjetilBlocker!");
            return;
        }

        p.Launch(arcDirection, arcForce, 0.8f);
    }
}