using UnityEngine;
using System.Collections;

public class CavaleiroFlamejante : MonoBehaviour
{
    [Header("REFERÊNCIAS")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform visual;
    [SerializeField] private GameObject colliderEspada;
    [SerializeField] private GameObject colliderEscudo;

    private Rigidbody2D rb;

    [Header("MOVIMENTO")]
    [SerializeField] private float velocidade = 4f;
    [SerializeField] private float velocidadePerseguicao = 5.8f;

    [Header("DETECÇÃO")]
    [SerializeField] private float rangeDeteccao = 8f;      // Range só para INICIAR a perseguição
    [SerializeField] private Vector2 offsetDeteccao = new Vector2(0, 0.5f);

    [Header("ATAQUE")]
    [SerializeField] private float rangeAtaque = 2.2f;
    [SerializeField] private Vector2 offsetAtaque = new Vector2(0, 0.5f);

    [Header("DANO")]
    [SerializeField] private int danoEspada = 3;
    [SerializeField] private int vidaMaxima = 10;
    private int vidaAtual;

    [Header("ESCUDO")]
    [SerializeField] private int vidaEscudo = 3;
    private int vidaAtualEscudo;
    private bool escudoQuebrado;

    [Header("TIMERS")]
    [SerializeField] private float tempoAtaque = 0.45f;
    [SerializeField] private float cooldownAtaque = 1.4f;

    private bool atacando;
    private bool podeAtacar = true;
    private bool isChasing = false;        // ← NOVO: Perseguição persistente

    private float ultimaDirecao = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        vidaAtual = vidaMaxima;
        vidaAtualEscudo = vidaEscudo;
        escudoQuebrado = false;

        if (colliderEspada != null) colliderEspada.SetActive(false);
        if (colliderEscudo != null) colliderEscudo.SetActive(true);

        Debug.Log("🔥 Cavaleiro Flamejante - Perseguição PERSISTENTE");
    }

    private void Update()
    {
        if (player == null) return;

        Vector2 centroDeteccao = (Vector2)transform.position + offsetDeteccao;
        Vector2 centroAtaque = (Vector2)transform.position + offsetAtaque;

        float distDeteccao = Vector2.Distance(centroDeteccao, player.position);
        float distAtaque = Vector2.Distance(centroAtaque, player.position);

        float direcao = player.position.x > transform.position.x ? 1f : -1f;

        // Flip do visual
        if (visual != null && direcao != ultimaDirecao)
        {
            Vector3 escala = visual.localScale;
            escala.x = Mathf.Abs(escala.x) * direcao;
            visual.localScale = escala;
            ultimaDirecao = direcao;
        }

        // Ativa a perseguição quando entra no range
        if (!isChasing && distDeteccao <= rangeDeteccao)
        {
            isChasing = true;
            Debug.Log("⚡ Cavaleiro começou a perseguir o Player!");
        }

        // === DENTRO DO RANGE DE ATAQUE ===
        if (distAtaque <= rangeAtaque)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (podeAtacar && !atacando)
                StartCoroutine(RotinaAtaque());

            return;
        }

        // === PERSEGUIÇÃO PERSISTENTE ===
        if (isChasing && !atacando)
        {
            float vel = (distDeteccao > rangeAtaque * 2f) ? velocidadePerseguicao : velocidade;
            rb.linearVelocity = new Vector2(direcao * vel, rb.linearVelocity.y);
        }
        else if (!isChasing)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private IEnumerator RotinaAtaque()
    {
        atacando = true;
        podeAtacar = false;

        Debug.Log("⚔️ Cavaleiro atacando!");
        if (colliderEspada != null) colliderEspada.SetActive(true);

        yield return new WaitForSeconds(tempoAtaque);

        if (colliderEspada != null) colliderEspada.SetActive(false);

        atacando = false;

        yield return new WaitForSeconds(cooldownAtaque);
        podeAtacar = true;
    }

    public void TomarDano(int dano)
    {
        if (vidaAtual <= 0) return;

        if (!escudoQuebrado)
        {
            vidaAtualEscudo--;
            Debug.Log($"🛡️ Escudo: {vidaAtualEscudo}/{vidaEscudo}");

            if (vidaAtualEscudo <= 0)
                QuebrarEscudo();
            return;
        }

        vidaAtual -= dano;
        Debug.Log($"💥 Cavaleiro tomou {dano} | Vida: {vidaAtual}/{vidaMaxima}");

        if (vidaAtual <= 0)
            Morrer();
    }

    private void QuebrarEscudo()
    {
        escudoQuebrado = true;
        Debug.Log("💥 ESCUDO QUEBRADO!");
        if (colliderEscudo != null) colliderEscudo.SetActive(false);
    }

    public void RegenerarEscudo()
    {
        if (!escudoQuebrado) return;

        escudoQuebrado = false;
        vidaAtualEscudo = vidaEscudo;
        Debug.Log("✨ Escudo Regenerado!");
        if (colliderEscudo != null) colliderEscudo.SetActive(true);
    }

    private void Morrer()
    {
        Debug.Log("☠️ Cavaleiro morreu");
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && colliderEspada != null && colliderEspada.activeInHierarchy)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(danoEspada, gameObject);
        }

        if (other.CompareTag("Player") && colliderEscudo != null && colliderEscudo.activeInHierarchy)
        {
            TomarDano(1);
        }
    }

    private void OnTriggerStay2D(Collider2D other)  
    {
        if (other.CompareTag("LuzLampiao"))
            RegenerarEscudo();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + offsetDeteccao, rangeDeteccao);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + offsetAtaque, rangeAtaque);
    }
}