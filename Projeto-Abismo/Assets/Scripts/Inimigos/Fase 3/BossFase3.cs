using System.Collections;
using UnityEngine;

public class BossFase3 : MonoBehaviour, IDamageable
{
    // =============================================
    // REFERÊNCIAS
    // =============================================

    [Header("Referências")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    // =============================================
    // PONTOS DA ARENA
    // =============================================

    [Header("Pontos da Arena")]
    [Tooltip("Ponto esquerdo — spawn e destino das ondas")]
    [SerializeField] private Transform spawnA;

    [Tooltip("Ponto direito — spawn e destino das ondas")]
    [SerializeField] private Transform spawnB;

    [Tooltip("Ponto A do Bullet Hell")]
    [SerializeField] private Transform pontoAtaqueA;

    [Tooltip("Ponto B do Bullet Hell")]
    [SerializeField] private Transform pontoAtaqueB;

    [Tooltip("Ponto C do Bullet Hell")]
    [SerializeField] private Transform pontoAtaqueC;

    [Tooltip("Centro da arena — onde o boss cai e fica vulnerável")]
    [SerializeField] private Transform centroArena;

    // =============================================
    // ONDA
    // =============================================

    [Header("Onda")]
    [SerializeField] private GameObject ondaPrefab;
    [SerializeField] private float velocidadeOnda = 6f;
    [SerializeField] private int danoOnda = 1;
    [SerializeField] private int quantidadeOndas = 2;
    [SerializeField] private float intervaloEntreOndas = 0.6f;

    // =============================================
    // BULLET HELL
    // =============================================

    [Header("Bullet Hell")]
    [SerializeField] private GameObject projetilPrefab;
    [SerializeField] private float velocidadeProjetil = 7f;
    [SerializeField] private int quantidadeProjeteis = 8;
    [SerializeField] private float anguloEspalhamento = 90f;
    [SerializeField] private int ondasBulletHell = 3;
    [SerializeField] private float intervaloOndasBulletHell = 0.4f;

    // =============================================
    // VULNERABILIDADE
    // =============================================

    [Header("Vulnerabilidade")]
    [SerializeField] private float duracaoVulneravel = 3f;
    [SerializeField] private float velocidadeQueda = 8f;
    [SerializeField] private Color corVulneravel = Color.red;
    [SerializeField] private Color corNormal = Color.white;

    // =============================================
    // VIDA
    // =============================================

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 8;
    [SerializeField] private float duracaoAnimacaoMorte = 1.5f;

    // =============================================
    // EFEITOS
    // =============================================

    [Header("Efeitos")]
    [SerializeField] private AudioClip somOnda;
    [SerializeField] private AudioClip somBulletHell;
    [SerializeField] private AudioClip somQueda;
    [SerializeField] private AudioClip somVulneravel;
    [SerializeField] private AudioClip somMorte;
    [SerializeField] private GameObject efeitoMorte;
    [SerializeField] private GameObject efeitoImpacto;

    // =============================================
    // TEMPOS
    // =============================================

    [Header("Tempos")]
    [SerializeField] private float tempoEntreAtaques = 1.5f;
    [SerializeField] private float tempoPreparacaoQueda = 1f;

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
        Idle,
        AtaqueOnda,
        AtaqueBulletHell,
        Caindo,
        Vulneravel,
        Morto
    }

    private enum TipoAtaque { Onda, BulletHell }

    private EstadoBoss estado = EstadoBoss.Idle;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private Transform player;
    private PlayerController playerController;

    private int vidaAtual;
    private bool morto = false;
    private bool vulneravel = false;
    private int cicloAtual = 0;

    // Posição original do boss (no ar)
    private Vector3 posicaoOriginal;

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

        // Boss flutua — sem gravidade
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Desativa collider físico inicialmente
        if (col != null)
            col.enabled = false;
    }

    // =============================================
    // START
    // =============================================

    private void Start()
    {
        BuscarPlayer();
        ValidarReferencias();

        posicaoOriginal = transform.position;

        StartCoroutine(CicloDeAtaques());
    }

    // =============================================
    // VALIDAÇÃO
    // =============================================

    private void ValidarReferencias()
    {
        if (spawnA == null) Debug.LogError("[BossFase3] ⚠️ SpawnA não atribuído!");
        if (spawnB == null) Debug.LogError("[BossFase3] ⚠️ SpawnB não atribuído!");
        if (pontoAtaqueA == null) Debug.LogError("[BossFase3] ⚠️ Ponto Ataque A não atribuído!");
        if (pontoAtaqueB == null) Debug.LogError("[BossFase3] ⚠️ Ponto Ataque B não atribuído!");
        if (pontoAtaqueC == null) Debug.LogError("[BossFase3] ⚠️ Ponto Ataque C não atribuído!");
        if (centroArena == null) Debug.LogError("[BossFase3] ⚠️ Centro da Arena não atribuído!");
        if (ondaPrefab == null) Debug.LogWarning("[BossFase3] ⚠️ Onda Prefab não atribuído!");
        if (projetilPrefab == null) Debug.LogWarning("[BossFase3] ⚠️ Projetil Prefab não atribuído!");

        Debug.Log($"[BossFase3] ✅ Boss Fase 3 iniciado | Vida: {vidaAtual}/{vidaMaxima}");
    }

    // =============================================
    // CICLO DE ATAQUES
    // =============================================

    private IEnumerator CicloDeAtaques()
    {
        yield return new WaitForSeconds(1.5f);

        while (!morto)
        {
            cicloAtual++;

            // Alterna: Onda → Onda → BulletHell → cai → repete
            TipoAtaque proximo = (cicloAtual % 3 == 0)
                ? TipoAtaque.BulletHell
                : TipoAtaque.Onda;

            if (debugLogs)
                Debug.Log($"[BossFase3] 🔄 Ciclo {cicloAtual} — Ataque: {proximo}");

            if (proximo == TipoAtaque.Onda)
                yield return StartCoroutine(SequenciaOnda());
            else
                yield return StartCoroutine(SequenciaBulletHell());

            // Após cada ciclo completo (3 ataques) — cai e fica vulnerável
            if (cicloAtual % 3 == 0)
                yield return StartCoroutine(SequenciaVulneravel());

            yield return new WaitForSeconds(tempoEntreAtaques);
        }
    }

    // =============================================
    // SEQUÊNCIA: ONDA
    // =============================================

    private IEnumerator SequenciaOnda()
    {
        estado = EstadoBoss.AtaqueOnda;

        if (animator != null)
            animator.SetTrigger("Onda");

        // Escolhe aleatoriamente SpawnA ou SpawnB
        bool sairDeA = Random.value > 0.5f;
        Transform spawnOrigem = sairDeA ? spawnA : spawnB;
        Transform spawnDestino = sairDeA ? spawnB : spawnA;

        if (debugLogs)
            Debug.Log($"[BossFase3] 🌊 Onda saindo de {(sairDeA ? "SpawnA" : "SpawnB")} → {(sairDeA ? "SpawnB" : "SpawnA")}");

        // Spawna as ondas com intervalo
        for (int i = 0; i < quantidadeOndas; i++)
        {
            SpawnarOnda(spawnOrigem, spawnDestino);

            if (i < quantidadeOndas - 1)
                yield return new WaitForSeconds(intervaloEntreOndas);
        }

        // Espera as ondas atravessarem a arena
        float larguraArena = spawnA != null && spawnB != null
            ? Vector2.Distance(spawnA.position, spawnB.position)
            : 20f;

        float tempoTraverssia = larguraArena / velocidadeOnda + 0.5f;
        yield return new WaitForSeconds(tempoTraverssia);

        estado = EstadoBoss.Idle;
    }

    // =============================================
    // SPAWNAR ONDA
    // =============================================

    private void SpawnarOnda(Transform origem, Transform destino)
    {
        if (ondaPrefab == null || origem == null || destino == null) return;

        GameObject onda = Instantiate(
            ondaPrefab,
            origem.position,
            Quaternion.identity
        );

        // Passa os dados para o script da onda
        OndaBoss ondaScript = onda.GetComponent<OndaBoss>();
        if (ondaScript != null)
        {
            ondaScript.Inicializar(
                destino.position,
                velocidadeOnda,
                danoOnda
            );
        }
        else
        {
            // Fallback: move por Rigidbody se não tiver script
            Rigidbody2D rbOnda = onda.GetComponent<Rigidbody2D>();
            if (rbOnda != null)
            {
                Vector2 direcao = (destino.position - origem.position).normalized;
                rbOnda.linearVelocity = direcao * velocidadeOnda;
            }
        }

        if (somOnda != null && audioSource != null)
            audioSource.PlayOneShot(somOnda);

        if (debugLogs)
            Debug.Log($"[BossFase3] 🌊 Onda spawnada em {origem.name} → {destino.name}");
    }

    // =============================================
    // SEQUÊNCIA: BULLET HELL
    // =============================================

    private IEnumerator SequenciaBulletHell()
    {
        estado = EstadoBoss.AtaqueBulletHell;

        // Escolhe um dos 3 pontos aleatoriamente
        int pontoEscolhido = Random.Range(0, 3);
        Transform pontoAtaque = ObterPontoAtaque(pontoEscolhido);

        if (debugLogs)
            Debug.Log($"[BossFase3] 💥 Bullet Hell no ponto: {NomePonto(pontoEscolhido)}");

        if (animator != null)
            animator.SetTrigger("BulletHell");

        if (somBulletHell != null && audioSource != null)
            audioSource.PlayOneShot(somBulletHell);

        // Dispara múltiplas ondas de projeteis do ponto escolhido
        for (int i = 0; i < ondasBulletHell; i++)
        {
            DispararLequeDoNponto(pontoAtaque);
            yield return new WaitForSeconds(intervaloOndasBulletHell);
        }

        estado = EstadoBoss.Idle;
    }

    // =============================================
    // DISPARAR LEQUE DO PONTO
    // =============================================

    private void DispararLequeDoNponto(Transform ponto)
    {
        if (projetilPrefab == null || ponto == null || player == null) return;

        Vector2 direcaoBase = ((Vector2)player.position - (Vector2)ponto.position).normalized;

        float anguloInicio = -anguloEspalhamento / 2f;
        float passo = quantidadeProjeteis > 1
            ? anguloEspalhamento / (quantidadeProjeteis - 1)
            : 0f;

        for (int i = 0; i < quantidadeProjeteis; i++)
        {
            float angulo = anguloInicio + passo * i;
            Vector2 dir = RotacionarVetor(direcaoBase, angulo);

            GameObject proj = Instantiate(
                projetilPrefab,
                ponto.position,
                Quaternion.identity
            );

            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null)
                rbProj.linearVelocity = dir * velocidadeProjetil;

            float anguloRot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.AngleAxis(anguloRot, Vector3.forward);
        }

        if (debugLogs)
            Debug.Log($"[BossFase3] 🔥 Leque de {quantidadeProjeteis} projeteis disparado de {ponto.name}");
    }

    // =============================================
    // SEQUÊNCIA: VULNERÁVEL (CAI NO CENTRO)
    // =============================================

    private IEnumerator SequenciaVulneravel()
    {
        estado = EstadoBoss.Caindo;

        if (debugLogs)
            Debug.Log("[BossFase3] ⬇️ Boss caindo para o centro!");

        if (somQueda != null && audioSource != null)
            audioSource.PlayOneShot(somQueda);

        if (animator != null)
            animator.SetTrigger("Caindo");

        yield return new WaitForSeconds(tempoPreparacaoQueda);

        // Move o boss suavemente até o centro
        yield return StartCoroutine(MoverParaCentro());

        // Ativa collider físico para o player poder bater
        if (col != null)
            col.enabled = true;

        // Fica vulnerável
        vulneravel = true;

        if (sr != null)
            sr.color = corVulneravel;

        if (somVulneravel != null && audioSource != null)
            audioSource.PlayOneShot(somVulneravel);

        if (animator != null)
            animator.SetTrigger("Vulneravel");

        estado = EstadoBoss.Vulneravel;

        if (debugLogs)
            Debug.Log($"[BossFase3] 🟢 VULNERÁVEL por {duracaoVulneravel}s!");

        yield return new WaitForSeconds(duracaoVulneravel);

        // Fecha vulnerabilidade e volta para posição original
        FecharVulnerabilidade();

        yield return StartCoroutine(VoltarParaPosicaoOriginal());

        estado = EstadoBoss.Idle;

        if (debugLogs)
            Debug.Log("[BossFase3] 🦋 Boss voltou — ciclo reiniciando!");
    }

    // =============================================
    // MOVER PARA CENTRO
    // =============================================

    private IEnumerator MoverParaCentro()
    {
        if (centroArena == null) yield break;

        Vector3 origem = transform.position;
        Vector3 destino = centroArena.position;
        float duracao = Vector3.Distance(origem, destino) / velocidadeQueda;
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            transform.position = Vector3.Lerp(origem, destino, tempo / duracao);
            yield return null;
        }

        transform.position = destino;
    }

    // =============================================
    // VOLTAR PARA POSIÇÃO ORIGINAL
    // =============================================

    private IEnumerator VoltarParaPosicaoOriginal()
    {
        Vector3 origem = transform.position;
        Vector3 destino = posicaoOriginal;
        float duracao = 0.8f;
        float tempo = 0f;

        if (animator != null)
            animator.SetTrigger("Subindo");

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            transform.position = Vector3.Lerp(origem, destino, tempo / duracao);
            yield return null;
        }

        transform.position = destino;
    }

    // =============================================
    // FECHAR VULNERABILIDADE
    // =============================================

    private void FecharVulnerabilidade()
    {
        vulneravel = false;

        if (col != null)
            col.enabled = false;

        if (sr != null)
            sr.color = corNormal;

        if (animator != null)
            animator.SetBool("Vulneravel", false);

        if (debugLogs)
            Debug.Log("[BossFase3] 🔴 Vulnerabilidade encerrada!");
    }

    // =============================================
    // HELPERS
    // =============================================

    private Transform ObterPontoAtaque(int indice)
    {
        return indice switch
        {
            0 => pontoAtaqueA,
            1 => pontoAtaqueB,
            2 => pontoAtaqueC,
            _ => pontoAtaqueB
        };
    }

    private string NomePonto(int indice)
    {
        return indice switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            _ => "?"
        };
    }

    private Vector2 RotacionarVetor(Vector2 v, float graus)
    {
        float rad = graus * Mathf.Deg2Rad;
        return new Vector2(
            v.x * Mathf.Cos(rad) - v.y * Mathf.Sin(rad),
            v.x * Mathf.Sin(rad) + v.y * Mathf.Cos(rad)
        );
    }

    // =============================================
    // VIDA / DANO
    // =============================================

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto) return;

        if (!vulneravel)
        {
            if (debugLogs)
                Debug.Log($"[BossFase3] 🛡️ Dano BLOQUEADO | Estado: {estado} | Fonte: {fonte.name}");
            return;
        }

        vidaAtual -= dano;

        if (debugLogs)
            Debug.Log($"[BossFase3] 💥 Tomou {dano} | Vida: {vidaAtual}/{vidaMaxima} | Fonte: {fonte.name}");

        if (animator != null)
            animator.SetTrigger("Hit");

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;
            StartCoroutine(MorrerRoutine());
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

        StopAllCoroutines();
        StartCoroutine(MorrerRoutine_Internal());
        yield break;
    }

    private IEnumerator MorrerRoutine_Internal()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        if (sr != null)
            sr.color = corNormal;

        if (efeitoMorte != null)
            Instantiate(efeitoMorte, transform.position, Quaternion.identity);

        if (somMorte != null)
            AudioSource.PlayClipAtPoint(somMorte, transform.position);

        if (animator != null)
            animator.SetTrigger("Morrer");

        Debug.Log("[BossFase3] ☠️ Boss Fase 3 derrotado!");

        yield return new WaitForSeconds(duracaoAnimacaoMorte);

        Destroy(gameObject);
    }

    // =============================================
    // BUSCAR PLAYER
    // =============================================

    private void BuscarPlayer()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null)
        {
            player = go.transform;
            playerController = go.GetComponent<PlayerController>();

            if (debugLogs)
                Debug.Log($"[BossFase3] ✅ Player encontrado: {go.name}");
        }
        else
        {
            Debug.LogWarning("[BossFase3] ⚠️ Player NÃO encontrado!");
        }
    }

    // =============================================
    // GIZMOS
    // =============================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos) return;

        // SpawnA e SpawnB — linha da onda
        if (spawnA != null && spawnB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(spawnA.position, spawnB.position);
            Gizmos.DrawWireSphere(spawnA.position, 0.4f);
            Gizmos.DrawWireSphere(spawnB.position, 0.4f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(spawnA.position + Vector3.up * 0.6f, "SpawnA");
            UnityEditor.Handles.Label(spawnB.position + Vector3.up * 0.6f, "SpawnB");
#endif
        }

        // Pontos de ataque Bullet Hell
        Color[] cores = { Color.red, Color.green, Color.blue };
        Transform[] pontos = { pontoAtaqueA, pontoAtaqueB, pontoAtaqueC };
        string[] nomes = { "PontoA", "PontoB", "PontoC" };

        for (int i = 0; i < pontos.Length; i++)
        {
            if (pontos[i] == null) continue;
            Gizmos.color = cores[i];
            Gizmos.DrawWireSphere(pontos[i].position, 0.4f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = cores[i];
            UnityEditor.Handles.Label(pontos[i].position + Vector3.up * 0.6f, nomes[i]);
#endif
        }

        // Centro da arena
        if (centroArena != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centroArena.position, 0.5f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(centroArena.position + Vector3.up * 0.7f, "Centro");
#endif
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
                $"Vulnerável: {vulneravel}\n" +
                $"Ciclo: {cicloAtual}"
            );
        }
#endif
    }
}