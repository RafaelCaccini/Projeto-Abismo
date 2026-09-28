using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniBossFase1 : MonoBehaviour, IDamageable
{
    // =============================================
    // REFERÊNCIAS
    // =============================================

    [Header("Referências")]
    [SerializeField] private Transform jogador;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // =============================================
    // ARENA
    // =============================================

    [Header("Arena")]
    [SerializeField] private Transform pontoA;
    [SerializeField] private Transform pontoB;
    [SerializeField] private GameObject paredeEsquerda;
    [SerializeField] private GameObject paredeDireita;

    // =============================================
    // TELEPORTE
    // =============================================

    [Header("Teleporte")]
    [SerializeField] private float duracaoEfeitoTeleporte = 0.15f;
    [SerializeField] private GameObject efeitoTeleportePrefab;

    // =============================================
    // DETECÇÃO
    // =============================================

    [Header("Detecção")]
    [SerializeField] private float alcanceDeteccao = 10f;

    // =============================================
    // ESPINHOS
    // =============================================

    [Header("Espinhos")]
    [SerializeField] private GameObject prefabSpike;
    [SerializeField] private Transform inicioChao;
    [SerializeField] private Transform fimChao;
    [SerializeField] private Transform inicioTeto;
    [SerializeField] private Transform fimTeto;
    [SerializeField] private int quantidadeSpikes = 6;
    [SerializeField] private float tempoSpike = 2f;
    [SerializeField] private float atrasoAntesPisao = 0.35f;

    // =============================================
    // PROJETIL
    // =============================================

    [Header("Projetil")]
    [SerializeField] private GameObject prefabProjetil;
    [SerializeField] private Transform pontoTiro;
    [SerializeField] private int quantidadeProjetisAtirar = 4;
    [SerializeField] private float intervaloEntreTiros = 0.25f;
    [SerializeField] private float velocidadeProjetil = 8f;

    // =============================================
    // CONTATO
    // =============================================

    [Header("Contato")]
    [SerializeField] private int danoAoTocar = 2;
    [SerializeField] private float cooldownDanoContato = 0.6f;

    // =============================================
    // VIDA
    // =============================================

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 20;

    // =============================================
    // CICLO DE ATAQUES
    // =============================================

    [Header("Ciclo de Ataques")]
    [Tooltip("Tempo entre cada ataque")]
    [SerializeField] private float intervaloEntreAtaques = 2.5f;
    [Tooltip("Quantos ataques antes de teleportar para o outro ponto")]
    [SerializeField] private int ataquesPorPonto = 2;

    // =============================================
    // PAREDES
    // =============================================

    [Header("Paredes")]
    [SerializeField] private float wallEnterMargin = 0.1f;
    [SerializeField] private float wallReenableDistance = 1.2f;

    // =============================================
    // DEBUG
    // =============================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool mostrarGizmos = true;

    // =============================================
    // ESTADO INTERNO
    // =============================================

    private enum EstadoBoss { Idle, Lutando, Atacando, Morto }
    private EstadoBoss estado = EstadoBoss.Idle;

    private int vidaAtual;
    private bool morto = false;
    private bool lutaComecou = false;
    private bool ocupado = false;
    private bool estaNoPontoA = true;

    private int ataquesNoPontoAtual = 0;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector3 posicaoInicial;

    private Dictionary<Collider2D, float> ultimoDanoPorCollider = new Dictionary<Collider2D, float>();

    // Paredes
    private WallInfo leftWall;
    private WallInfo rightWall;

    // =============================================
    // WALL INFO
    // =============================================

    private class WallInfo
    {
        public GameObject go;
        public Collider2D col;
        public SpriteRenderer sr;
        public Color corOriginal;
        public bool isTriggerOriginal;
        public bool passedThrough = false;
        public bool reenabled = false;

        public WallInfo(GameObject g)
        {
            go = g;
            if (g == null) return;
            col = g.GetComponent<Collider2D>();
            sr = g.GetComponent<SpriteRenderer>();
            if (sr != null) corOriginal = sr.color;
            if (col != null) isTriggerOriginal = col.isTrigger;
        }

        public void AbrirParaEntrar()
        {
            if (go != null && !go.activeSelf) go.SetActive(true);
            if (sr != null)
            {
                var c = sr.color;
                c.a = 0.35f;
                sr.color = c;
            }
            if (col != null) col.isTrigger = true;
            passedThrough = false;
            reenabled = false;
        }

        public void ReativarColisao()
        {
            if (col != null) col.isTrigger = false;
            if (sr != null)
            {
                var c = corOriginal;
                c.a = 1f;
                sr.color = c;
            }
            reenabled = true;
        }

        public void Restaurar()
        {
            if (col != null) col.isTrigger = isTriggerOriginal;
            if (sr != null) sr.color = corOriginal;
            passedThrough = false;
            reenabled = false;
        }
    }

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        vidaAtual = vidaMaxima;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    // =============================================
    // START
    // =============================================

    private void Start()
    {
        posicaoInicial = transform.position; // ← usa a posição onde você colocou na cena

        if (jogador == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) jogador = go.transform;
        }

        if (paredeEsquerda != null) leftWall = new WallInfo(paredeEsquerda);
        if (paredeDireita != null) rightWall = new WallInfo(paredeDireita);

        // ← REMOVIDO: TeleportarPara(pontoA.position, false);

        if (debugLogs)
            Debug.Log($"[MiniBoss] Iniciado em {transform.position} | Vida: {vidaAtual}/{vidaMaxima}");
    }

    // =============================================
    // UPDATE
    // =============================================

    private void Update()
    {
        if (morto) return;
        if (jogador == null) return;

        float distancia = Vector2.Distance(transform.position, jogador.position);

        // Inicia a luta
        if (!lutaComecou && distancia <= alcanceDeteccao)
        {
            IniciarLuta();
        }

        if (lutaComecou)
            GerenciarParedes();
    }

    // =============================================
    // INICIAR LUTA
    // =============================================

    private void IniciarLuta()
    {
        lutaComecou = true;
        estado = EstadoBoss.Lutando;

        AtivarParedes();

        if (debugLogs)
            Debug.Log("[MiniBoss] ⚔️ Luta iniciada!");

        StartCoroutine(CicloDeLuta());
    }

    // =============================================
    // CICLO DE LUTA
    // =============================================

    private IEnumerator CicloDeLuta()
    {
        yield return new WaitForSeconds(1f);

        while (!morto)
        {
            yield return new WaitForSeconds(intervaloEntreAtaques);

            if (morto || ocupado) continue;

            // Executa ataque aleatório no ponto atual
            yield return StartCoroutine(ExecutarAtaque());

            ataquesNoPontoAtual++;

            // Após X ataques, teleporta para o outro ponto
            if (ataquesNoPontoAtual >= ataquesPorPonto)
            {
                ataquesNoPontoAtual = 0;
                yield return StartCoroutine(TeleportarParaOutroPonto());
            }
        }
    }

    // =============================================
    // ESCOLHER E EXECUTAR ATAQUE
    // =============================================

    private IEnumerator ExecutarAtaque()
    {
        if (morto) yield break;

        estado = EstadoBoss.Atacando;
        ocupado = true;

        // Escolhe aleatoriamente entre os dois ataques
        int escolha = Random.Range(0, 2);

        if (debugLogs)
            Debug.Log($"[MiniBoss] 🎲 Ataque escolhido: {(escolha == 0 ? "Espinhos" : "Projetil")} | Ponto: {(estaNoPontoA ? "A" : "B")}");

        switch (escolha)
        {
            case 0:
                yield return StartCoroutine(AtaqueEspinhos());
                break;
            case 1:
                yield return StartCoroutine(AtaqueProjetil());
                break;
        }

        ocupado = false;
        estado = EstadoBoss.Lutando;
    }

    // =============================================
    // ATAQUE: ESPINHOS
    // =============================================

    private IEnumerator AtaqueEspinhos()
    {
        if (debugLogs)
            Debug.Log("[MiniBoss] 🦔 Ataque: Espinhos!");

        // Olha para o jogador antes do pisão
        OlharParaJogador();

        if (animator != null)
            animator.SetTrigger("Pisao");

        yield return new WaitForSeconds(atrasoAntesPisao);

        // Spawna espinhos no chão
        if (prefabSpike != null && inicioChao != null && fimChao != null)
            SpawnarLinhaEspinhos(inicioChao.position, fimChao.position);

        // Spawna espinhos no teto
        if (prefabSpike != null && inicioTeto != null && fimTeto != null)
            SpawnarLinhaEspinhos(inicioTeto.position, fimTeto.position);

        yield return new WaitForSeconds(tempoSpike + 0.2f);
    }

    private void SpawnarLinhaEspinhos(Vector2 inicio, Vector2 fim)
    {
        if (quantidadeSpikes <= 1)
        {
            var s = Instantiate(prefabSpike, inicio, Quaternion.identity);
            IgnorarColisaoComBoss(s);
            Destroy(s, tempoSpike);
            return;
        }

        for (int i = 0; i < quantidadeSpikes; i++)
        {
            float p = (float)i / (quantidadeSpikes - 1);
            Vector2 pos = Vector2.Lerp(inicio, fim, p);
            var spike = Instantiate(prefabSpike, pos, Quaternion.identity);
            IgnorarColisaoComBoss(spike);
            Destroy(spike, tempoSpike);
        }
    }

    private void IgnorarColisaoComBoss(GameObject obj)
    {
        if (obj == null || col == null) return;
        Collider2D c = obj.GetComponent<Collider2D>();
        if (c != null) Physics2D.IgnoreCollision(c, col);
    }

    // =============================================
    // ATAQUE: PROJETIL
    // =============================================

    private IEnumerator AtaqueProjetil()
    {
        if (debugLogs)
            Debug.Log("[MiniBoss] 🔥 Ataque: Projetil!");

        OlharParaJogador();

        if (animator != null)
            animator.SetTrigger("Atirar");

        for (int i = 0; i < quantidadeProjetisAtirar; i++)
        {
            if (morto) yield break;
            if (prefabProjetil == null) break;

            Vector3 origem = pontoTiro != null ? pontoTiro.position : transform.position;

            if (jogador == null) break;

            Vector2 direcao = ((Vector2)jogador.position - (Vector2)origem).normalized;
            float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

            GameObject proj = Instantiate(prefabProjetil, origem, Quaternion.Euler(0f, 0f, angulo));

            IgnorarColisaoComBoss(proj);

            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null)
                rbProj.linearVelocity = direcao * velocidadeProjetil;

            yield return new WaitForSeconds(intervaloEntreTiros);
        }

        yield return new WaitForSeconds(0.2f);
    }

    // =============================================
    // TELEPORTE PARA O OUTRO PONTO
    // =============================================

    private IEnumerator TeleportarParaOutroPonto()
    {
        if (morto) yield break;

        Transform destino = estaNoPontoA ? pontoB : pontoA;
        if (destino == null) yield break;

        if (debugLogs)
            Debug.Log($"[MiniBoss] ✨ Teleportando para Ponto {(estaNoPontoA ? "B" : "A")}");

        // Efeito de saída
        if (efeitoTeleportePrefab != null)
            Instantiate(efeitoTeleportePrefab, transform.position, Quaternion.identity);

        // Pisca antes de sumir
        yield return StartCoroutine(PiscarTeleporte());

        // Teleporta
        TeleportarPara(destino.position, true);

        // Efeito de chegada
        if (efeitoTeleportePrefab != null)
            Instantiate(efeitoTeleportePrefab, transform.position, Quaternion.identity);

        estaNoPontoA = !estaNoPontoA;

        if (debugLogs)
            Debug.Log($"[MiniBoss] ✅ Chegou no Ponto {(estaNoPontoA ? "A" : "B")}");
    }

    private void TeleportarPara(Vector3 posicao, bool comEfeito)
    {
        transform.position = new Vector3(posicao.x, posicaoInicial.y, transform.position.z);
        OlharParaJogador();
    }

    private IEnumerator PiscarTeleporte()
    {
        if (spriteRenderer == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(duracaoEfeitoTeleporte);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(duracaoEfeitoTeleporte);
        }

        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(duracaoEfeitoTeleporte);
        spriteRenderer.enabled = true;
    }

    // =============================================
    // OLHAR PARA O JOGADOR
    // =============================================

    private void OlharParaJogador()
    {
        if (jogador == null || spriteRenderer == null) return;

        bool olhandoDireita = jogador.position.x > transform.position.x;
        spriteRenderer.flipX = !olhandoDireita;
    }

    // =============================================
    // PAREDES
    // =============================================

    private void AtivarParedes()
    {
        if (paredeEsquerda != null) paredeEsquerda.SetActive(true);
        if (paredeDireita != null) paredeDireita.SetActive(true);

        leftWall?.AbrirParaEntrar();
        rightWall?.AbrirParaEntrar();
    }

    private void GerenciarParedes()
    {
        if (jogador == null) return;

        GerenciarParede(leftWall, true);
        GerenciarParede(rightWall, false);
    }

    private void GerenciarParede(WallInfo wall, bool ehEsquerda)
    {
        if (wall == null || wall.reenabled) return;

        float wallX = wall.go.transform.position.x;

        if (!wall.passedThrough)
        {
            bool passou = ehEsquerda
                ? jogador.position.x > wallX + wallEnterMargin
                : jogador.position.x < wallX - wallEnterMargin;

            if (passou)
            {
                wall.passedThrough = true;

                if (debugLogs)
                    Debug.Log($"[MiniBoss] Player passou pela parede {(ehEsquerda ? "esquerda" : "direita")}");
            }
        }
        else
        {
            if (Mathf.Abs(jogador.position.x - wallX) >= wallReenableDistance)
            {
                wall.ReativarColisao();

                if (debugLogs)
                    Debug.Log($"[MiniBoss] Parede {(ehEsquerda ? "esquerda" : "direita")} reativada");
            }
        }
    }

    // =============================================
    // VIDA / DANO
    // =============================================

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto) return;
        if (fonte != null && fonte.CompareTag("Spike")) return;

        vidaAtual -= dano;

        if (debugLogs)
            Debug.Log($"[MiniBoss] 💥 Tomou {dano} | Vida: {vidaAtual}/{vidaMaxima}");

        if (animator != null)
            animator.SetTrigger("Hit");

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;
            Morrer();
        }
    }

    // =============================================
    // MORTE
    // =============================================

    private void Morrer()
    {
        morto = true;
        estado = EstadoBoss.Morto;

        StopAllCoroutines();

        leftWall?.Restaurar();
        rightWall?.Restaurar();

        if (animator != null)
            animator.SetTrigger("Morrer");

        if (debugLogs)
            Debug.Log("[MiniBoss] ☠️ Boss derrotado!");

        Destroy(gameObject, 1.5f);
    }

    // =============================================
    // DANO POR CONTATO
    // =============================================

    private void OnTriggerEnter2D(Collider2D other) => TentarDanoContato(other);
    private void OnCollisionEnter2D(Collision2D col) => TentarDanoContato(col.collider);

    private void TentarDanoContato(Collider2D alvo)
    {
        if (alvo == null || morto) return;
        if (!alvo.CompareTag("Player")) return;

        float agora = Time.time;
        if (ultimoDanoPorCollider.TryGetValue(alvo, out float ultimo))
            if (agora - ultimo < cooldownDanoContato) return;

        IDamageable dmg = alvo.GetComponent<IDamageable>();
        if (dmg == null) dmg = alvo.GetComponentInParent<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(danoAoTocar, gameObject);
            ultimoDanoPorCollider[alvo] = agora;

            if (debugLogs)
                Debug.Log($"[MiniBoss] 👊 Dano por contato: {danoAoTocar}");
        }
    }

    // =============================================
    // GIZMOS
    // =============================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos) return;

        // Alcance de detecção
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceDeteccao);

        // Ponto A
        if (pontoA != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pontoA.position, 0.4f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pontoA.position + Vector3.up * 0.6f, "Ponto A");
#endif
        }

        // Ponto B
        if (pontoB != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(pontoB.position, 0.4f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pontoB.position + Vector3.up * 0.6f, "Ponto B");
#endif
        }

        // Linha entre os pontos
        if (pontoA != null && pontoB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pontoA.position, pontoB.position);
        }

        // Estado em runtime
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Estado: {estado}\n" +
                $"Vida: {vidaAtual}/{vidaMaxima}\n" +
                $"Ponto: {(estaNoPontoA ? "A" : "B")}\n" +
                $"Ataques no ponto: {ataquesNoPontoAtual}/{ataquesPorPonto}"
            );
        }
#endif
    }
}