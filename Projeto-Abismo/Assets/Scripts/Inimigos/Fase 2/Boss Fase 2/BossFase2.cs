using System;
using System.Collections;
using UnityEngine;

public class BossFase2 : MonoBehaviour, IDamageable
{
    // =============================================
    // REFERÊNCIAS
    // =============================================

    [Header("Referências")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform pontoDisparo;

    // =============================================
    // VOO
    // =============================================

    [Header("Voo")]
    [SerializeField] private Transform limiteEsquerdo;
    [SerializeField] private Transform limiteDireito;
    [SerializeField] private float alturaVoo = -193.32f;
    [SerializeField] private float velocidadeVoo = 4f;

    // =============================================
    // ATAQUE
    // =============================================

    [Header("Ataque")]
    [SerializeField] private GameObject projetilPrefab;
    [SerializeField] private float cooldownAtaque = 2f;
    [SerializeField] private int maxAtaques = 4;
    [SerializeField] private float velocidadeProjetil = 8f;

    // =============================================
    // QUEDA
    // =============================================

    [Header("Queda")]
    [SerializeField] private float velocidadeQueda = 10f;
    [SerializeField] private float alturaChao = -198.32f;
    [SerializeField] private string tagChao = "Ground";

    // =============================================
    // VULNERÁVEL
    // =============================================

    [Header("Vulnerável")]
    [SerializeField] private float duracaoNoChao = 3f;
    [SerializeField] private Color corVulneravel = Color.red;
    [SerializeField] private Color corNormal = Color.white;

    // =============================================
    // VIDA
    // =============================================

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 10;
    [SerializeField] private float duracaoAnimacaoMorte = 1.5f;

    // =============================================
    // EFEITOS
    // =============================================

    [Header("Efeitos")]
    [SerializeField] private GameObject efeitoMortePrefab;
    [SerializeField] private AudioClip somAtaque;
    [SerializeField] private AudioClip somQueda;
    [SerializeField] private AudioClip somVulneravel;
    [SerializeField] private AudioClip somMorte;

    // =============================================
    // DEBUG
    // =============================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool mostrarGizmos = true;

    // =============================================
    // ESTADO INTERNO
    // =============================================

    private enum EstadoBoss
    {
        Voando,
        Caindo,
        NoChao,
        Morto
    }

    private EstadoBoss estado = EstadoBoss.Voando;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform player;
    private Collider2D col;

    private int vidaAtual;

    private int contadorAtaques = 0;

    private float timerAtaque = 0f;
    private float timerChao = 0f;

    private bool movendoDireita = true;
    private bool morto = false;
    private bool vulneravel = false;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (visual != null)
            sr = visual.GetComponent<SpriteRenderer>();
        else
            sr = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        vidaAtual = vidaMaxima;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // O movimento do boss é controlado pelo script.
            // Não queremos a física empurrando ou prendendo o boss.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    // =============================================
    // START
    // =============================================

    private void Start()
    {
        BuscarPlayer();

        // =============================================
        // VALIDAÇÃO
        // =============================================

        if (limiteEsquerdo == null)
            Debug.LogError("[BossFase2] ⚠️ Limite Esquerdo não atribuído!");

        if (limiteDireito == null)
            Debug.LogError("[BossFase2] ⚠️ Limite Direito não atribuído!");

        if (projetilPrefab == null)
            Debug.LogWarning("[BossFase2] ⚠️ Projetil Prefab não atribuído!");

        if (pontoDisparo == null)
            Debug.LogWarning("[BossFase2] ⚠️ Ponto Disparo não atribuído!");

        if (player == null)
            Debug.LogWarning("[BossFase2] ⚠️ Player não encontrado!");

        // =============================================
        // POSIÇÃO INICIAL
        // =============================================

        Vector2 pos = rb.position;

        pos.y = alturaVoo;

        rb.position = pos;

        // Começa indo para a direita
        movendoDireita = true;

        AtualizarFlip(1f);

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase2] ✅ Boss iniciado | " +
                $"Vida: {vidaAtual} | " +
                $"Altura Voo: {alturaVoo} | " +
                $"Altura Chão: {alturaChao}"
            );
        }
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (morto)
            return;

        if (player == null)
            BuscarPlayer();

        switch (estado)
        {
            case EstadoBoss.Voando:
                HandleVoando();
                break;

            case EstadoBoss.Caindo:
                HandleCaindo();
                break;

            case EstadoBoss.NoChao:
                HandleNoChao();
                break;
        }

        AtualizarAnimacoes();
    }

    // =============================================
    // FIXED UPDATE
    // =============================================

    private void FixedUpdate()
    {
        if (morto)
            return;

        switch (estado)
        {
            case EstadoBoss.Voando:
                MovimentoVoo();
                break;

            case EstadoBoss.Caindo:
                MovimentoQueda();
                break;

            case EstadoBoss.NoChao:
                PararMovimento();
                break;
        }
    }

    // =============================================
    // ESTADO: VOANDO
    // =============================================

    private void HandleVoando()
    {
        timerAtaque += Time.deltaTime;

        if (timerAtaque >= cooldownAtaque)
        {
            Atirar();

            contadorAtaques++;

            timerAtaque = 0f;

            if (debugLogs)
            {
                Debug.Log(
                    $"[BossFase2] 🔥 Ataque " +
                    $"{contadorAtaques}/{maxAtaques}"
                );
            }

            if (contadorAtaques >= maxAtaques)
            {
                TransicionarParaQueda();
            }
        }
    }

    // =============================================
    // MOVIMENTO DO VOO
    // =============================================

    private void MovimentoVoo()
    {
        if (limiteEsquerdo == null || limiteDireito == null)
            return;

        // Pega os limites reais
        float limiteEsq = limiteEsquerdo.position.x;
        float limiteDir = limiteDireito.position.x;

        // Segurança caso estejam invertidos no Inspector
        if (limiteEsq > limiteDir)
        {
            float temp = limiteEsq;

            limiteEsq = limiteDir;
            limiteDir = temp;
        }

        // =============================================
        // POSIÇÃO ATUAL
        // =============================================

        Vector2 pos = rb.position;

        // =============================================
        // MOVIMENTO HORIZONTAL
        // =============================================

        if (movendoDireita)
        {
            pos.x += velocidadeVoo * Time.fixedDeltaTime;
        }
        else
        {
            pos.x -= velocidadeVoo * Time.fixedDeltaTime;
        }

        // =============================================
        // MANTÉM ALTURA DE VOO
        // =============================================

        pos.y = alturaVoo;

        // =============================================
        // LIMITE DIREITO
        // =============================================

        if (movendoDireita && pos.x >= limiteDir)
        {
            pos.x = limiteDir;

            movendoDireita = false;

            AtualizarFlip(-1f);

            if (debugLogs)
            {
                Debug.Log(
                    "[BossFase2] ↩️ " +
                    "Limite DIREITO alcançado. " +
                    "Invertendo para esquerda."
                );
            }
        }

        // =============================================
        // LIMITE ESQUERDO
        // =============================================

        else if (!movendoDireita && pos.x <= limiteEsq)
        {
            pos.x = limiteEsq;

            movendoDireita = true;

            AtualizarFlip(1f);

            if (debugLogs)
            {
                Debug.Log(
                    "[BossFase2] ↪️ " +
                    "Limite ESQUERDO alcançado. " +
                    "Invertendo para direita."
                );
            }
        }

        // =============================================
        // APLICA POSIÇÃO
        // =============================================

        rb.position = pos;
    }

    // =============================================
    // DISPARO
    // =============================================

    private void Atirar()
    {
        if (projetilPrefab == null)
        {
            if (debugLogs)
                Debug.LogWarning(
                    "[BossFase2] Tentou atirar, " +
                    "mas Projetil Prefab está vazio!"
                );

            return;
        }

        if (player == null)
        {
            if (debugLogs)
                Debug.LogWarning(
                    "[BossFase2] Tentou atirar, " +
                    "mas Player está null!"
                );

            return;
        }

        Vector3 origem = pontoDisparo != null
            ? pontoDisparo.position
            : transform.position;

        Vector2 direcao =
            ((Vector2)player.position - (Vector2)origem).normalized;

        GameObject proj =
            Instantiate(
                projetilPrefab,
                origem,
                Quaternion.identity
            );

        Rigidbody2D rbProj =
            proj.GetComponent<Rigidbody2D>();

        if (rbProj != null)
        {
            rbProj.linearVelocity =
                direcao * velocidadeProjetil;
        }

        float angulo =
            Mathf.Atan2(direcao.y, direcao.x)
            * Mathf.Rad2Deg;

        proj.transform.rotation =
            Quaternion.AngleAxis(
                angulo,
                Vector3.forward
            );

        if (somAtaque != null && audioSource != null)
        {
            audioSource.PlayOneShot(somAtaque);
        }

        if (animator != null)
        {
            animator.SetTrigger("Atacar");
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase2] 🔥 Projetil disparado " +
                $"em direção a {player.name}"
            );
        }
    }

    // =============================================
    // TRANSIÇÃO PARA QUEDA
    // =============================================

    private void TransicionarParaQueda()
    {
        estado = EstadoBoss.Caindo;

        rb.linearVelocity = Vector2.zero;

        if (somQueda != null && audioSource != null)
        {
            audioSource.PlayOneShot(somQueda);
        }

        if (animator != null)
        {
            animator.SetTrigger("Caindo");
        }

        if (debugLogs)
        {
            Debug.Log(
                "[BossFase2] ⬇️ Iniciando queda!"
            );
        }
    }

    // =============================================
    // ESTADO: CAINDO
    // =============================================

    private void HandleCaindo()
    {
        // A queda agora é controlada pelo próprio script.
        // Não dependemos de colisão com o chão.

        if (rb.position.y <= alturaChao)
        {
            Vector2 pos = rb.position;

            pos.y = alturaChao;

            rb.position = pos;

            TransicionarParaChao();
        }
    }

    private void MovimentoQueda()
    {
        Vector2 pos = rb.position;

        pos.y -= velocidadeQueda * Time.fixedDeltaTime;

        // Impede passar abaixo do chão
        if (pos.y < alturaChao)
            pos.y = alturaChao;

        rb.position = pos;
    }

    // =============================================
    // PARAR MOVIMENTO
    // =============================================

    private void PararMovimento()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // =============================================
    // TRANSIÇÃO PARA CHÃO
    // =============================================

    private void TransicionarParaChao()
    {
        estado = EstadoBoss.NoChao;

        timerChao = 0f;

        vulneravel = true;

        rb.linearVelocity = Vector2.zero;

        if (sr != null)
            sr.color = corVulneravel;

        if (somVulneravel != null && audioSource != null)
        {
            audioSource.PlayOneShot(somVulneravel);
        }

        if (animator != null)
        {
            animator.SetTrigger("NoChao");
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase2] 🟢 No chão — " +
                $"VULNERÁVEL por {duracaoNoChao}s!"
            );
        }
    }

    // =============================================
    // ESTADO: NO CHÃO
    // =============================================

    private void HandleNoChao()
    {
        timerChao += Time.deltaTime;

        if (
            debugLogs &&
            Mathf.FloorToInt(timerChao) !=
            Mathf.FloorToInt(timerChao - Time.deltaTime)
        )
        {
            Debug.Log(
                $"[BossFase2] ⏱️ No chão: " +
                $"{timerChao:F1}/{duracaoNoChao}s"
            );
        }

        if (timerChao >= duracaoNoChao)
        {
            TransicionarParaVoo();
        }
    }

    // =============================================
    // TRANSIÇÃO PARA VOO
    // =============================================

    private void TransicionarParaVoo()
    {
        estado = EstadoBoss.Voando;

        contadorAtaques = 0;

        timerAtaque = 0f;

        vulneravel = false;

        rb.linearVelocity = Vector2.zero;

        // =============================================
        // RESTAURA ALTURA
        // =============================================

        Vector2 pos = rb.position;

        pos.y = alturaVoo;

        rb.position = pos;

        // =============================================
        // NOVO CICLO
        // =============================================

        movendoDireita = true;

        AtualizarFlip(1f);

        // =============================================
        // COR
        // =============================================

        if (sr != null)
            sr.color = corNormal;

        // =============================================
        // ANIMAÇÃO
        // =============================================

        if (animator != null)
        {
            animator.SetTrigger("Voando");
        }

        if (debugLogs)
        {
            Debug.Log(
                "[BossFase2] 🦋 Voltando a voar!"
            );
        }
    }

    // =============================================
    // ANIMAÇÕES
    // =============================================

    private void AtualizarAnimacoes()
    {
        if (animator == null)
            return;

        animator.SetBool(
            "IsFlying",
            estado == EstadoBoss.Voando
        );

        animator.SetBool(
            "IsGrounded",
            estado == EstadoBoss.NoChao
        );

        animator.SetBool(
            "IsVulnerable",
            vulneravel
        );
    }

    // =============================================
    // FLIP VISUAL
    // =============================================

    private void AtualizarFlip(float direcaoX)
    {
        if (visual == null)
            return;

        Vector3 escala = visual.localScale;

        escala.x =
            Mathf.Abs(escala.x) *
            (direcaoX >= 0f ? 1f : -1f);

        visual.localScale = escala;
    }

    // =============================================
    // VIDA / DANO
    // =============================================

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto)
            return;

        if (!vulneravel)
        {
            if (debugLogs)
            {
                string nomeFonte =
                    fonte != null
                        ? fonte.name
                        : "Desconhecido";

                Debug.Log(
                    $"[BossFase2] 🛡️ Dano BLOQUEADO " +
                    $"(invulnerável) | " +
                    $"Estado: {estado} | " +
                    $"Fonte: {nomeFonte}"
                );
            }

            return;
        }

        vidaAtual -= dano;

        if (debugLogs)
        {
            string nomeFonte =
                fonte != null
                    ? fonte.name
                    : "Desconhecido";

            Debug.Log(
                $"[BossFase2] 💥 Tomou {dano} de dano | " +
                $"Vida: {vidaAtual}/{vidaMaxima} | " +
                $"Fonte: {nomeFonte}"
            );
        }

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;

            StartCoroutine(
                MorrerRoutine()
            );
        }
    }

    // =============================================
    // MORTE
    // =============================================

    private IEnumerator MorrerRoutine()
    {
        morto = true;

        vulneravel = false;

        estado = EstadoBoss.Morto;

        rb.linearVelocity = Vector2.zero;

        foreach (
            var c in
            GetComponentsInChildren<Collider2D>()
        )
        {
            c.enabled = false;
        }

        if (sr != null)
            sr.color = corNormal;

        if (efeitoMortePrefab != null)
        {
            Instantiate(
                efeitoMortePrefab,
                transform.position,
                Quaternion.identity
            );
        }

        if (somMorte != null)
        {
            AudioSource.PlayClipAtPoint(
                somMorte,
                transform.position
            );
        }

        if (animator != null)
        {
            animator.SetTrigger("Morrer");
        }

        Debug.Log(
            "[BossFase2] ☠️ Boss morreu!"
        );

        yield return new WaitForSeconds(
            duracaoAnimacaoMorte
        );

        Destroy(gameObject);
    }

    // =============================================
    // BUSCAR PLAYER
    // =============================================

    private void BuscarPlayer()
    {
        GameObject go =
            GameObject.FindWithTag("Player");

        if (go != null)
        {
            player = go.transform;

            if (debugLogs)
            {
                Debug.Log(
                    $"[BossFase2] ✅ Player encontrado: " +
                    $"{go.name}"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[BossFase2] ⚠️ Player NÃO encontrado! " +
                "Verifique a tag 'Player'."
            );
        }
    }

    // =============================================
    // GIZMOS
    // =============================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos)
            return;

        // =============================================
        // LINHA DO VOO
        // =============================================

        if (
            limiteEsquerdo != null &&
            limiteDireito != null
        )
        {
            Gizmos.color = Color.cyan;

            Vector3 esqVoo =
                new Vector3(
                    limiteEsquerdo.position.x,
                    alturaVoo,
                    0f
                );

            Vector3 dirVoo =
                new Vector3(
                    limiteDireito.position.x,
                    alturaVoo,
                    0f
                );

            Gizmos.DrawLine(
                esqVoo,
                dirVoo
            );

            Gizmos.DrawWireSphere(
                esqVoo,
                0.3f
            );

            Gizmos.DrawWireSphere(
                dirVoo,
                0.3f
            );
        }

        // =============================================
        // LINHA DO CHÃO
        // =============================================

        Gizmos.color = Color.green;

        Vector3 chaoEsq =
            new Vector3(
                transform.position.x - 10f,
                alturaChao,
                0f
            );

        Vector3 chaoDir =
            new Vector3(
                transform.position.x + 10f,
                alturaChao,
                0f
            );

        Gizmos.DrawLine(
            chaoEsq,
            chaoDir
        );

        // =============================================
        // LABELS
        // =============================================

#if UNITY_EDITOR

        UnityEditor.Handles.color = Color.green;

        UnityEditor.Handles.Label(
            new Vector3(
                transform.position.x + 10.2f,
                alturaChao,
                0f
            ),
            $"Chão: {alturaChao}"
        );

        UnityEditor.Handles.Label(
            new Vector3(
                transform.position.x + 10.2f,
                alturaVoo,
                0f
            ),
            $"Voo: {alturaVoo}"
        );

#endif

        // =============================================
        // PONTO DE DISPARO
        // =============================================

        if (pontoDisparo != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                pontoDisparo.position,
                0.25f
            );
        }

        // =============================================
        // DEBUG RUNTIME
        // =============================================

#if UNITY_EDITOR

        if (Application.isPlaying)
        {
            UnityEditor.Handles.color =
                Color.yellow;

            UnityEditor.Handles.Label(
                transform.position +
                Vector3.up * 2f,

                $"Estado: {estado}\n" +
                $"Vida: {vidaAtual}/{vidaMaxima}\n" +
                $"Vulnerável: {vulneravel}\n" +
                $"Ataques: {contadorAtaques}/{maxAtaques}\n" +
                $"Direção: {(movendoDireita ? "Direita" : "Esquerda")}"
            );
        }

#endif
    }
}
