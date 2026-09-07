using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class InimigoExplode : MonoBehaviour, IDamageable
{
    // =====================================
    // REFER�NCIAS
    // =====================================

    [Header("REFER�NCIAS")]
    [Tooltip("Transform do Player. Se nulo, ser� buscado por Tag 'Player'.")]
    [SerializeField] private Transform player;

    [SerializeField] private Animator animator;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Source de �udio para os efeitos sonoros")]
    [SerializeField] private AudioSource audioSource;

    // =====================================
    // MOVIMENTO
    // =====================================

    [Header("MOVIMENTO")]
    [Tooltip("Velocidade de patrulha do inimigo")]
    [SerializeField] private float velocidadePatrulha = 1.5f;

    [Tooltip("Velocidade ao perseguir o player")]
    [SerializeField] private float velocidadePerseguicao = 3f;

    [Tooltip("Dist�ncia para parar de seguir o player antes de explodir")]
    [SerializeField] private float distanciaParar = 0.5f;

    // =====================================
    // DETEC��O
    // =====================================

    [Header("DETEC��O")]
    [Tooltip("Raio de detec��o do player")]
    [SerializeField] private float raioDeteccao = 8f;

    [Tooltip("LayerMask para detectar o player")]
    [SerializeField] private LayerMask playerLayer = 1 << 8;

    [Tooltip("Tag usada para encontrar o player")]
    [SerializeField] private string playerTag = "Player";

    // =====================================
    // EXPLOS�O
    // =====================================

    [Header("EXPLOS�O")]
    [Tooltip("Dist�ncia m�nima do player para disparar a explos�o")]
    [SerializeField] private float distanciaExplodir = 1.5f;

    [Tooltip("Dano causado pela explos�o")]
    [SerializeField] private int danoExplosao = 3;

    [Tooltip("Raio da explos�o")]
    [SerializeField] private float raioExplosao = 2f;

    [Tooltip("Tempo de contagem regressiva antes da explos�o")]
    [SerializeField] private float tempoContagemRegressiva = 0.8f;

    [Tooltip("Tempo para destruir o GameObject ap�s a explos�o")]
    [SerializeField] private float delayDestruicao = 0.5f;

    // =====================================
    // VIDA
    // =====================================

    [Header("VIDA")]
    [Tooltip("Pontos de vida do inimigo")]
    [SerializeField] private int vida = 3;

    [Tooltip("Se true, o inimigo explode ao morrer")]
    [SerializeField] private bool explodeAoMorrer = true;

    // =====================================
    // DANO POR CONTATO
    // =====================================

    [Header("DANO POR CONTATO")]
    [Tooltip("Dano causado ao tocar no player")]
    [SerializeField] private int danoContato = 1;

    [Tooltip("Cooldown entre aplica��es de dano por contato")]
    [SerializeField] private float cooldownContato = 1f;

    // =====================================
    // �UDIO
    // =====================================

    [Header("�UDIO")]
    [SerializeField] private AudioClip somDetectar;

    [SerializeField] private AudioClip somContagemRegressiva;

    [SerializeField] private AudioClip somExplosao;

    // =====================================
    // DEBUG
    // =====================================

    [Header("DEBUG")]
    [SerializeField] private bool debugLogs = true;

    [SerializeField] private bool mostrarGizmos = true;

    // =====================================
    // CONTROLE
    // =====================================

    private Rigidbody2D rb;

    private CircleCollider2D col;

    private Transform visualTransform;

    private int vidaAtual;

    private bool morto = false;

    private bool explodindo = false;

    private bool olhandoDireita = true;

    private float ultimoDanoTime = -999f;

    private static readonly int Anim_Explodindo =
        Animator.StringToHash("Explodindo");

    private static readonly int Anim_Morrer =
        Animator.StringToHash("Morrer");

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        col = GetComponent<CircleCollider2D>();

        if (rb == null)
        {
            Debug.LogError(
                "[InimigoExplode] Rigidbody2D ausente!"
            );

            enabled = false;

            return;
        }

        if (col == null)
        {
            Debug.LogError(
                "[InimigoExplode] CircleCollider2D ausente!"
            );

            enabled = false;

            return;
        }

        // Configura��es de f�sica
        rb.gravityScale = 0f;

        rb.freezeRotation = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        rb.bodyType = RigidbodyType2D.Dynamic;

        // O CircleCollider2D permanece como
        // collider f�sico para dano por contato

        // Busca componentes opcionais
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;

            audioSource.loop = false;
        }

        // Visual transform (cria se necess�rio)
        if (spriteRenderer != null)
            visualTransform = spriteRenderer.transform;
    }

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        vidaAtual = vida;

        BuscarPlayer();

        AtualizarDirecaoVisual();

        if (debugLogs)
        {
            Debug.Log(
                "[InimigoExplode] Inicializado | Vida: "
                + vidaAtual
            );
        }
    }

    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        if (morto)
            return;

        if (player == null)
            BuscarPlayer();
    }

    // =====================================
    // FIXED UPDATE
    // =====================================

    private void FixedUpdate()
    {
        if (morto)
            return;

        if (explodindo)
            return;

        if (player == null)
        {
            Patrulhar();

            return;
        }

        // Verifica dist�ncia para explos�o
        float distancia =
            Vector2.Distance(
                transform.position,
                player.position
            );

        if (distancia <= distanciaExplodir)
        {
            IniciarExplosao();

            return;
        }

        // Persegue o player
        PerseguirPlayer(distancia);
    }

    // =====================================
    // PATRULHAR
    // =====================================

    private void Patrulhar()
    {
        // Move na dire��o atual
        rb.linearVelocity =
            new Vector2(
                (olhandoDireita ? 1f : -1f) *
                velocidadePatrulha,
                rb.linearVelocity.y
            );
    }

    // =====================================
    // PERSEGUIR PLAYER
    // =====================================

    private void PerseguirPlayer(float distancia)
    {
        Vector2 direcao =
            (
                player.position -
                transform.position
            ).normalized;

        // Vira para o player
        if (direcao.x > 0.1f)
            olhandoDireita = true;

        if (direcao.x < -0.1f)
            olhandoDireita = false;

        AtualizarDirecaoVisual();

        // Move
        if (distancia > distanciaParar)
        {
            rb.linearVelocity =
                new Vector2(
                    direcao.x * velocidadePerseguicao,
                    rb.linearVelocity.y
                );
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // =====================================
    // FLIP VISUAL
    // =====================================

    private void AtualizarDirecaoVisual()
    {
        if (visualTransform == null)
            return;

        Vector3 escala = visualTransform.localScale;

        escala.x =
            olhandoDireita
            ? Mathf.Abs(escala.x)
            : -Mathf.Abs(escala.x);

        visualTransform.localScale = escala;
    }

    // =====================================
    // BUSCAR PLAYER
    // =====================================

    private void BuscarPlayer()
    {
        if (player != null)
            return;

        GameObject playerObj =
            GameObject.FindGameObjectWithTag(
                playerTag
            );

        if (playerObj != null)
            player = playerObj.transform;
    }

    // =====================================
    // COLIS�O COM PLAYER (DANO POR CONTATO)
    // =====================================

    private void OnCollisionStay2D(
        Collision2D collision
    )
    {
        if (morto || explodindo)
            return;

        if (!collision.gameObject.CompareTag(playerTag))
            return;

        // Dano por contato enquanto n�o est� explodindo
        if (Time.time < ultimoDanoTime + cooldownContato)
            return;

        PlayerController pc =
            collision.gameObject
            .GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.TakeDamage(
                danoContato,
                gameObject
            );

            ultimoDanoTime = Time.time;
        }
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        // Dano imediato ao primeiro contato
        if (morto || explodindo)
            return;

        if (!collision.gameObject.CompareTag(playerTag))
            return;

        if (Time.time < ultimoDanoTime + cooldownContato)
            return;

        PlayerController pc =
            collision.gameObject
            .GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.TakeDamage(
                danoContato,
                gameObject
            );

            ultimoDanoTime = Time.time;
        }
    }

    // =====================================
    // INICIAR EXPLOS�O
    // =====================================

    private void IniciarExplosao()
    {
        if (explodindo || morto)
            return;

        explodindo = true;

        // Para o movimento
        rb.linearVelocity = Vector2.zero;

        // Desativa o collider para n�o bloquear
        if (col != null)
            col.enabled = false;

        // Anima��o de contagem regressiva
        if (animator != null)
        {
            animator.ResetTrigger(Anim_Morrer);

            animator.SetBool(Anim_Explodindo, true);
        }

        // Som de contagem regressiva
        if (
            somContagemRegressiva != null &&
            audioSource != null
        )
            audioSource.PlayOneShot
                (somContagemRegressiva);

        if (debugLogs)
        {
            Debug.Log(
                "[InimigoExplode] Iniciando explos�o"
            );
        }

        StartCoroutine(ContagemRegressiva());
    }

    // =====================================
    // CONTAGEM REGRESSIVA
    // =====================================

    private IEnumerator ContagemRegressiva()
    {
        yield return new WaitForSeconds(
            tempoContagemRegressiva
        );

        Explodir();
    }

    // =====================================
    // EXPLOS�O
    // =====================================

    private void Explodir()
    {
        if (debugLogs)
        {
            Debug.Log(
                "[InimigoExplode] Explodindo!"
            );
        }

        // Desativa todos os colliders filhos
        foreach (
            Collider2D c
            in GetComponentsInChildren<Collider2D>()
        )
        {
            c.enabled = false;
        }

        // Anima��o de explos�o
        if (animator != null)
        {
            animator.ResetTrigger(Anim_Explodindo);

            animator.SetTrigger(Anim_Morrer);
        }

        // Som de explos�o
        if (
            somExplosao != null &&
            audioSource != null
        )
            audioSource.PlayOneShot(somExplosao);

        // Dano em �rea
        AplicarDanoArea();

        // Destr�i o GameObject
        Destroy(
            gameObject,
            delayDestruicao
        );
    }

    // =====================================
    // DANO EM �REA
    // =====================================

    private void AplicarDanoArea()
    {
        Vector2 posicaoExplosao =
            new Vector2(
                transform.position.x,
                transform.position.y
            );

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                posicaoExplosao,
                raioExplosao,
                playerLayer
            );

        if (hits.Length == 0)
        {
            // Fallback: busca por tag se a layer n�o encontrar
            hits =
                Physics2D.OverlapCircleAll(
                    posicaoExplosao,
                    raioExplosao
                );
        }

        foreach (
            Collider2D hit
            in hits
        )
        {
            if (hit == null)
                continue;

            if (hit.CompareTag(playerTag))
            {
                IDamageable alvo =
                    hit.GetComponent<IDamageable>();

                if (alvo != null)
                {
                    alvo.TakeDamage(
                        danoExplosao,
                        gameObject
                    );

                    if (debugLogs)
                    {
                        Debug.Log(
                            "[InimigoExplode] Player atingido "
                            + hit.name
                            + " | Dano: "
                            + danoExplosao
                        );
                    }
                }

                PlayerController pc =
                    hit.GetComponent<PlayerController>();

                if (pc != null)
                {
                    pc.TakeDamage(
                        danoExplosao,
                        gameObject
                    );
                }
            }
        }
    }

    // =====================================
    // TOMAR DANO
    // =====================================

    public void TakeDamage(
        int amount,
        GameObject source
    )
    {
        if (morto || explodindo)
            return;

        if (amount <= 0)
            return;

        vidaAtual -= amount;

        if (debugLogs)
        {
            Debug.Log(
                "[InimigoExplode] Tomou "
                + amount
                + " de dano | Vida: "
                + vidaAtual
            );
        }

        if (vidaAtual <= 0)
        {
            if (explodeAoMorrer)
            {
                IniciarExplosao();
            }
            else
            {
                Morrer();
            }
        }
    }

    // =====================================
    // MORRER (SEM EXPLOS�O)
    // =====================================

    private void Morrer()
    {
        morto = true;

        rb.linearVelocity = Vector2.zero;

        rb.bodyType = RigidbodyType2D.Kinematic;

        if (col != null)
            col.enabled = false;

        if (animator != null)
        {
            animator.ResetTrigger(Anim_Explodindo);

            animator.SetTrigger(Anim_Morrer);
        }

        Destroy(gameObject, delayDestruicao);
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos)
            return;

        // Detection radius
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            raioDeteccao
        );

        // Explosion radius
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            raioExplosao
        );

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        Gizmos.DrawSphere(
            transform.position,
            raioExplosao
        );

        // Explode proximity distance
        if (player != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(
                transform.position,
                distanciaExplodir
            );
        }
    }
}