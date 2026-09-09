using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InimigoExplosivo : MonoBehaviour, IDamageable
{
    // =============================================
    // REFERÊNCIAS
    // =============================================

    [Header("Referências")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D[] colliders;

    private Transform player;

    // =============================================
    // DETECÇÃO
    // =============================================

    [Header("Detecção")]
    [SerializeField] private float raioDeteccao = 8f;
    [SerializeField] private float raioExplosao = 3f;
    [SerializeField] private float distanciaParaExplodir = 0.8f;
    [SerializeField] private string tagPlayer = "Player";

    // =============================================
    // MOVIMENTO
    // =============================================

    [Header("Movimento")]
    [SerializeField] private float velocidadePerseguicao = 3f;

    // =============================================
    // EXPLOSÃO
    // =============================================

    [Header("Explosão")]
    [SerializeField] private int danoExplosao = 2;

    [Tooltip("Tempo entre começar a preparação e realmente explodir.")]
    [SerializeField] private float tempoAteExplodir = 1.5f;

    [Tooltip("Efeito visual opcional. O inimigo funciona sem ele.")]
    [SerializeField] private GameObject efeitoExplosaoPrefab;

    [SerializeField] private AudioClip somExplosao;
    [SerializeField] private AudioClip somDeteccao;

    // =============================================
    // MORTE
    // =============================================

    [Header("Morte")]
    [SerializeField] private int vidaMaxima = 2;

    [Tooltip("Tempo que a animação Morrer terá para terminar.")]
    [SerializeField] private float duracaoAnimacaoMorte = 0.5f;

    // =============================================
    // FEEDBACK VISUAL
    // =============================================

    [Header("Feedback Visual")]
    [SerializeField] private Color corNormal = Color.white;
    [SerializeField] private Color corPerigo = Color.red;

    [Tooltip("Velocidade da piscada durante a preparação.")]
    [SerializeField] private float velocidadePiscada = 8f;

    // =============================================
    // DEBUG
    // =============================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool mostrarGizmos = true;

    // =============================================
    // ESTADOS
    // =============================================

    private enum Estado
    {
        Patrulhando,
        Perseguindo,
        PreparandoExplosao,
        Explodido,
        Morto
    }

    private Estado estadoAtual = Estado.Patrulhando;

    // =============================================
    // CONTROLE
    // =============================================

    private int vidaAtual;

    private bool explosaoExecutada = false;
    private bool morto = false;

    private Coroutine sequenciaExplosaoCoroutine;
    private Coroutine piscadaCoroutine;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        colliders = GetComponentsInChildren<Collider2D>();

        if (visual != null)
            sr = visual.GetComponent<SpriteRenderer>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        vidaAtual = vidaMaxima;

        BuscarPlayer();

        if (rb == null)
        {
            Debug.LogError(
                "[InimigoExplosivo] Rigidbody2D não encontrado."
            );
        }
    }

    // =============================================
    // START
    // =============================================

    private void Start()
    {
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;
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
        {
            BuscarPlayer();
            return;
        }

        switch (estadoAtual)
        {
            case Estado.Patrulhando:
                VerificarDeteccao();
                break;

            case Estado.Perseguindo:
                AtualizarPerseguicao();
                break;

            case Estado.PreparandoExplosao:
                // A coroutine controla esse estado.
                break;

            case Estado.Explodido:
                // Nada.
                break;

            case Estado.Morto:
                // Nada.
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

        if (estadoAtual != Estado.Perseguindo)
            return;

        MoverParaPlayer();
    }

    // =============================================
    // DETECÇÃO
    // =============================================

    private void VerificarDeteccao()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ❌ VerificarDeteccao: PLAYER É NULL!"
            );
            return;
        }

        float distancia = Vector2.Distance(
            transform.position,
            player.position
        );

        Debug.Log(
            $"[EXPLOSIVO] 🔎 Detecção | " +
            $"Distância={distancia:F2} | " +
            $"Raio={raioDeteccao} | " +
            $"Estado={estadoAtual}"
        );

        if (distancia <= raioDeteccao)
        {
            estadoAtual = Estado.Perseguindo;

            Debug.Log(
                "[EXPLOSIVO] 🟢 PLAYER DETECTADO! " +
                "Mudando para PERSEGUINDO."
            );

            TocarSomDeteccao();
        }
    }

    // =============================================
    // PERSEGUIÇÃO
    // =============================================

    private void AtualizarPerseguicao()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ❌ AtualizarPerseguicao: PLAYER É NULL!"
            );

            BuscarPlayer();
            return;
        }

        float distancia = Vector2.Distance(
            transform.position,
            player.position
        );

        Debug.Log(
            $"[EXPLOSIVO] 🏃 Perseguindo | " +
            $"Distância={distancia:F2} | " +
            $"LimiteExplosão={distanciaParaExplodir}"
        );

        if (distancia > raioDeteccao * 1.5f)
        {
            estadoAtual = Estado.Patrulhando;

            PararMovimento();

            Debug.Log(
                "[EXPLOSIVO] 🔵 PLAYER SAIU DO ALCANCE."
            );

            return;
        }

        if (distancia <= distanciaParaExplodir)
        {
            Debug.Log(
                "[EXPLOSIVO] 🔥 DISTÂNCIA PARA EXPLOSÃO ATINGIDA!"
            );

            IniciarPreparacaoExplosao();
        }
    }

    // =============================================
    // MOVIMENTO
    // =============================================

    private void MoverParaPlayer()
    {
        if (player == null || rb == null)
            return;

        Vector2 direcao =
            ((Vector2)player.position - rb.position)
            .normalized;

        rb.linearVelocity =
            new Vector2(
                direcao.x * velocidadePerseguicao,
                rb.linearVelocity.y
            );

        AtualizarFlip(direcao.x);
    }

    private void PararMovimento()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector2.zero;
    }

    // =============================================
    // FLIP
    // =============================================

    private void AtualizarFlip(float direcaoX)
    {
        if (visual == null)
            return;

        if (Mathf.Abs(direcaoX) < 0.01f)
            return;

        Vector3 escala =
            visual.localScale;

        escala.x =
            Mathf.Abs(escala.x) *
            (direcaoX >= 0f ? 1f : -1f);

        visual.localScale = escala;
    }

    // =============================================
    // PREPARAÇÃO DA EXPLOSÃO
    // =============================================

    private void IniciarPreparacaoExplosao()
    {
        if (morto)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ❌ Tentou iniciar explosão, " +
                "mas morto=true."
            );
            return;
        }

        if (explosaoExecutada)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ❌ Explosão já foi executada."
            );
            return;
        }

        if (estadoAtual == Estado.PreparandoExplosao)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ⚠️ Já está preparando explosão."
            );
            return;
        }

        estadoAtual = Estado.PreparandoExplosao;

        Debug.Log(
            $"[EXPLOSIVO] 💣💣💣 PREPARAÇÃO DA EXPLOSÃO INICIADA! " +
            $"Vai explodir em {tempoAteExplodir:F2}s."
        );

        PararMovimento();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;

            Debug.Log(
                $"[EXPLOSIVO] Rigidbody alterado para Kinematic."
            );
        }
        else
        {
            Debug.LogError(
                "[EXPLOSIVO] ❌ Rigidbody2D É NULL!"
            );
        }

        TocarTriggerSeguro("Explodindo");

        if (piscadaCoroutine != null)
            StopCoroutine(piscadaCoroutine);

        piscadaCoroutine = StartCoroutine(
            PiscarPerigo()
        );

        if (sequenciaExplosaoCoroutine != null)
            StopCoroutine(
                sequenciaExplosaoCoroutine
            );

        Debug.Log(
            "[EXPLOSIVO] ▶️ Iniciando Coroutine SequenciaExplosao."
        );

        sequenciaExplosaoCoroutine =
            StartCoroutine(
                SequenciaExplosao()
            );
    }

    // =============================================
    // SEQUÊNCIA DA EXPLOSÃO
    // =============================================

    private IEnumerator SequenciaExplosao()
    {
        Debug.Log(
            $"[EXPLOSIVO] ⏱️ Coroutine iniciou. " +
            $"Esperando {tempoAteExplodir:F2}s..."
        );

        yield return new WaitForSeconds(
            tempoAteExplodir
        );

        Debug.Log(
            $"[EXPLOSIVO] ⏰ TEMPO ACABOU! " +
            $"morto={morto} | " +
            $"estado={estadoAtual} | " +
            $"explosaoExecutada={explosaoExecutada}"
        );

        if (morto)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ❌ Não vai explodir porque morto=true."
            );

            yield break;
        }

        Debug.Log(
            "[EXPLOSIVO] 🚨 CHAMANDO ExecutarExplosao()!"
        );

        ExecutarExplosao();
    }

    // =============================================
    // EXPLOSÃO
    // =============================================

    private void ExecutarExplosao()
    {
        Debug.Log(
            "[EXPLOSIVO] 💥💥💥 EXECUTAR EXPLOSÃO!"
        );

        if (explosaoExecutada)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ❌ Explosão já executada!"
            );
            return;
        }

        if (morto)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ❌ Explosão cancelada porque morto=true."
            );
            return;
        }

        explosaoExecutada = true;

        estadoAtual = Estado.Explodido;

        Debug.Log(
            $"[EXPLOSIVO] 💥 EXPLOSÃO CONFIRMADA! " +
            $"Posição={transform.position} | " +
            $"Raio={raioExplosao} | " +
            $"Dano={danoExplosao}"
        );

        if (piscadaCoroutine != null)
        {
            StopCoroutine(piscadaCoroutine);
            piscadaCoroutine = null;
        }

        RestaurarCor();

        PararMovimento();

        Debug.Log(
            "[EXPLOSIVO] 🔴 Colliders sendo desativados."
        );

        DesativarColliders();

        if (efeitoExplosaoPrefab != null)
        {
            Debug.Log(
                "[EXPLOSIVO] ✨ Instanciando efeito visual."
            );

            Instantiate(
                efeitoExplosaoPrefab,
                transform.position,
                Quaternion.identity
            );
        }
        else
        {
            Debug.Log(
                "[EXPLOSIVO] ℹ️ Nenhum efeito visual configurado."
            );
        }

        if (somExplosao != null)
        {
            AudioSource.PlayClipAtPoint(
                somExplosao,
                transform.position
            );
        }

        Debug.Log(
            "[EXPLOSIVO] ☢️ CHAMANDO AplicarDanoArea()!"
        );

        AplicarDanoArea();

        Debug.Log(
            "[EXPLOSIVO] ☠️ Iniciando sequência de morte."
        );

        StartCoroutine(
            FinalizarDepoisDaExplosao()
        );
    }

    // =============================================
    // FINALIZAÇÃO DA EXPLOSÃO
    // =============================================

    private IEnumerator FinalizarDepoisDaExplosao()
    {
        // Marca como morto somente agora.
        // A explosão já aconteceu.
        morto = true;

        // Toca animação de morte/explosão
        TocarTriggerSeguro("Morrer");

        Log(
            "[InimigoExplosivo] Animação Morrer iniciada após explosão."
        );

        // Dá tempo para animação tocar
        yield return new WaitForSeconds(
            duracaoAnimacaoMorte
        );

        Destruir();
    }

    // =============================================
    // DANO EM ÁREA
    // =============================================

    private void AplicarDanoArea()
    {
        Debug.Log(
            $"[EXPLOSIVO] ☢️ BUSCANDO ALVOS | " +
            $"Centro={transform.position} | " +
            $"Raio={raioExplosao}"
        );

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                raioExplosao
            );

        Debug.Log(
            $"[EXPLOSIVO] ☢️ OverlapCircleAll encontrou " +
            $"{hits.Length} COLLIDER(S)."
        );

        if (hits.Length == 0)
        {
            Debug.LogWarning(
                "[EXPLOSIVO] ❌ NENHUM COLLIDER FOI ENCONTRADO NA EXPLOSÃO!"
            );
        }

        HashSet<IDamageable> alvosAtingidos =
            new HashSet<IDamageable>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            Debug.Log(
                $"[EXPLOSIVO] 🔍 Collider encontrado: " +
                $"Objeto={hit.gameObject.name} | " +
                $"Tag={hit.gameObject.tag} | " +
                $"Layer={LayerMask.LayerToName(hit.gameObject.layer)}"
            );

            PlayerController pc =
                hit.GetComponentInParent<PlayerController>();

            if (pc == null)
            {
                Debug.Log(
                    $"[EXPLOSIVO] ⚪ {hit.gameObject.name} " +
                    $"não possui PlayerController no objeto/pai."
                );

                continue;
            }

            Debug.Log(
                $"[EXPLOSIVO] 🟢 PLAYER ENCONTRADO! " +
                $"Objeto={pc.gameObject.name} | " +
                $"Tag={pc.gameObject.tag}"
            );

            if (!pc.CompareTag(tagPlayer))
            {
                Debug.LogWarning(
                    $"[EXPLOSIVO] ⚠️ PlayerController encontrado, " +
                    $"mas a TAG está errada! " +
                    $"Tag atual={pc.gameObject.tag} | " +
                    $"Esperada={tagPlayer}"
                );

                continue;
            }

            // Tenta obter via interface IDamageable (top-level, da pasta Inimigos/).
            // Se o cast falhar — por exemplo, se alguém reintroduzir uma
            // interface IDamageable aninhada dentro de PlayerController — usamos
            // um fallback direto chamando TakeDamage no próprio PlayerController.
            IDamageable alvo =
                pc as IDamageable;

            if (alvo == null)
            {
                Debug.LogWarning(
                    "[EXPLOSIVO] ⚠️ Cast para IDamageable retornou null. " +
                    "Usando fallback direto: PlayerController.TakeDamage()."
                );

                pc.TakeDamage(
                    danoExplosao,
                    gameObject
                );

                Debug.Log(
                    "[EXPLOSIVO] ✅ TakeDamage() chamado via fallback direto."
                );

                continue;
            }

            if (alvosAtingidos.Contains(alvo))
            {
                Debug.Log(
                    "[EXPLOSIVO] ⚪ Player já recebeu dano nesta explosão."
                );

                continue;
            }

            alvosAtingidos.Add(alvo);

            Debug.Log(
                $"[EXPLOSIVO] 💥💥 APLICANDO DANO! " +
                $"Alvo={pc.gameObject.name} | " +
                $"Dano={danoExplosao}"
            );

            alvo.TakeDamage(
                danoExplosao,
                gameObject
            );

            Debug.Log(
                "[EXPLOSIVO] ✅ TakeDamage() FOI CHAMADO!"
            );
        }

        Debug.Log(
            $"[EXPLOSIVO] ☢️ FIM DA EXPLOSÃO | " +
            $"Alvos atingidos={alvosAtingidos.Count}"
        );
    }

    // =============================================
    // DANO RECEBIDO
    // =============================================

    public void TakeDamage(
        int dano,
        GameObject fonte
    )
    {
        if (morto)
            return;

        // Depois que começou a preparação,
        // o inimigo fica comprometido com a explosão.
        if (estadoAtual ==
            Estado.Explodido)
            return;

        vidaAtual -= dano;

        Log(
            "[InimigoExplosivo] Tomou " +
            dano +
            " de dano. Vida: " +
            vidaAtual +
            "/" +
            vidaMaxima
        );

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;

            MorrerSemExplodir();

            return;
        }

        // Feedback de dano
        TocarTriggerSeguro("Hit");
    }

    // =============================================
    // MORTE POR DANO
    // =============================================

    private void MorrerSemExplodir()
    {
        if (morto)
            return;

        morto = true;

        estadoAtual =
            Estado.Morto;

        // Cancela sequência de explosão
        if (sequenciaExplosaoCoroutine != null)
        {
            StopCoroutine(
                sequenciaExplosaoCoroutine
            );

            sequenciaExplosaoCoroutine = null;
        }

        // Cancela piscada
        if (piscadaCoroutine != null)
        {
            StopCoroutine(
                piscadaCoroutine
            );

            piscadaCoroutine = null;
        }

        RestaurarCor();

        PararMovimento();

        if (rb != null)
            rb.bodyType =
                RigidbodyType2D.Kinematic;

        DesativarColliders();

        TocarTriggerSeguro("Morrer");

        Log(
            "[InimigoExplosivo] Morreu por dano sem explodir."
        );

        StartCoroutine(
            DestruirDepoisDaAnimacao()
        );
    }

    // =============================================
    // DESTRUIR APÓS ANIMAÇÃO
    // =============================================

    private IEnumerator DestruirDepoisDaAnimacao()
    {
        yield return new WaitForSeconds(
            duracaoAnimacaoMorte
        );

        Destruir();
    }

    private void Destruir()
    {
        Destroy(gameObject);
    }

    // =============================================
    // COLLIDERS
    // =============================================

    private void DesativarColliders()
    {
        if (colliders == null)
            return;

        foreach (Collider2D c in colliders)
        {
            if (c != null)
                c.enabled = false;
        }
    }

    // =============================================
    // ANIMAÇÕES
    // =============================================

    private void AtualizarAnimacoes()
    {
        if (animator == null)
            return;

        bool correndo =
            estadoAtual ==
            Estado.Perseguindo;

        SetBoolSeguro(
            "IsRun",
            correndo
        );
    }

    private void TocarTriggerSeguro(
        string nome
    )
    {
        if (animator == null)
            return;

        if (!TemParametro(
            animator,
            nome
        ))
            return;

        animator.ResetTrigger(nome);
        animator.SetTrigger(nome);
    }

    private void SetBoolSeguro(
        string nome,
        bool valor
    )
    {
        if (animator == null)
            return;

        if (!TemParametro(
            animator,
            nome
        ))
            return;

        animator.SetBool(
            nome,
            valor
        );
    }

    private bool TemParametro(
        Animator anim,
        string nome
    )
    {
        foreach (
            AnimatorControllerParameter parametro
            in anim.parameters
        )
        {
            if (parametro.name == nome)
                return true;
        }

        return false;
    }

    // =============================================
    // FEEDBACK DE PERIGO
    // =============================================

    private IEnumerator PiscarPerigo()
    {
        if (sr == null)
            yield break;

        float timer = 0f;

        while (
            estadoAtual ==
            Estado.PreparandoExplosao
            && !morto
        )
        {
            timer +=
                Time.deltaTime *
                velocidadePiscada;

            float intensidade =
                (Mathf.Sin(timer) + 1f) /
                2f;

            sr.color =
                Color.Lerp(
                    corNormal,
                    corPerigo,
                    intensidade
                );

            yield return null;
        }

        RestaurarCor();
    }

    private void RestaurarCor()
    {
        if (sr != null)
            sr.color = corNormal;
    }

    // =============================================
    // PLAYER
    // =============================================

    private void BuscarPlayer()
    {
        GameObject playerObj =
            GameObject.FindWithTag(
                tagPlayer
            );

        if (playerObj != null)
            player =
                playerObj.transform;
    }

    // =============================================
    // SOM
    // =============================================

    private void TocarSomDeteccao()
    {
        if (
            somDeteccao != null &&
            audioSource != null
        )
        {
            audioSource.PlayOneShot(
                somDeteccao
            );
        }
    }

    // =============================================
    // DEBUG
    // =============================================

    private void Log(string mensagem)
    {
        if (debugLogs)
            Debug.Log(mensagem);
    }

    // =============================================
    // GIZMOS
    // =============================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos)
            return;

        // Detecção
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            raioDeteccao
        );

        // Explosão
        Gizmos.color =
            Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            raioExplosao
        );

        // Distância para iniciar preparação
        Gizmos.color =
            Color.magenta;

        Gizmos.DrawWireSphere(
            transform.position,
            distanciaParaExplodir
        );
    }
}