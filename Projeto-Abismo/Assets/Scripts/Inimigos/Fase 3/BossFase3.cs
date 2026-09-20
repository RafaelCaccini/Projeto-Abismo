using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossVislumbra : MonoBehaviour, IDamageable
{
    // =============================================
    // REFERÊNCIAS
    // =============================================

    [Header("Referências")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Cortes da Arena")]
    [SerializeField] private Transform corteA;
    [SerializeField] private Transform corteB;
    [SerializeField] private Transform corteC;

    [Header("Mão")]
    [SerializeField] private Transform mao;
    [SerializeField] private Collider2D colliderMao;
    [SerializeField] private GameObject indicadorCorte;

    [Header("Projetil")]
    [SerializeField] private GameObject projetilPrefab;
    [SerializeField] private Transform pontoDisparo;

    // =============================================
    // SOCO
    // =============================================

    [Header("Soco")]
    [SerializeField] private float tempoAvisoSoco = 1.5f;
    [SerializeField] private float tempoPreparacaoSoco = 1f;
    [SerializeField] private float tempoDuracaoSoco = 0.3f;
    [SerializeField] private float tempoVulneravelAposSoco = 2f;
    [SerializeField] private int danoSoco = 2;
    [SerializeField] private float forcaEmpurraoSoco = 15f;
    [SerializeField] private string tagPlayer = "Player";

    // =============================================
    // BULLET HELL
    // =============================================

    [Header("Bullet Hell")]
    [SerializeField] private float velocidadeProjetil = 8f;
    [SerializeField] private int quantidadeLeque = 7;
    [SerializeField] private float anguloLeque = 120f;
    [SerializeField] private int quantidadeCirculo = 12;
    [SerializeField] private int ondas = 3;
    [SerializeField] private float intervaloOndas = 0.4f;
    [SerializeField] private float tempoBulletHell = 5f;

    // =============================================
    // VIDA
    // =============================================

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 6;
    [SerializeField] private float duracaoAnimacaoMorte = 1.5f;

    // =============================================
    // EFEITOS
    // =============================================

    [Header("Efeitos")]
    [SerializeField] private AudioClip somAviso;
    [SerializeField] private AudioClip somSoco;
    [SerializeField] private AudioClip somVulneravel;
    [SerializeField] private AudioClip somBulletHell;
    [SerializeField] private AudioClip somMorte;
    [SerializeField] private GameObject efeitoImpacto;
    [SerializeField] private GameObject efeitoMorte;

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
        PreparandoSoco,
        Socando,
        VulneravelPosSoco,
        BulletHell,
        Morto
    }

    private enum TipoAtaque { Soco, BulletHell }

    private EstadoBoss estado = EstadoBoss.Idle;

    private Rigidbody2D rb;
    private Transform player;
    private PlayerController playerController;

    private int vidaAtual;
    private bool morto = false;
    private bool vulneravel = false;
    private int corteEscolhido = -1; // 0=A, 1=B, 2=C
    private int cicloAtual = 0;
    private bool lequeInvertido = false;

    // Posições originais da mão para animação
    private Vector3 posOriginalMao;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        vidaAtual = vidaMaxima;

        if (colliderMao != null)
            colliderMao.enabled = false;

        if (indicadorCorte != null)
            indicadorCorte.SetActive(false);
    }

    // =============================================
    // START
    // =============================================

    private void Start()
    {
        BuscarPlayer();

        if (mao != null)
            posOriginalMao = mao.localPosition;

        ValidarReferencias();

        StartCoroutine(CicloDeAtaques());
    }

    // =============================================
    // VALIDAÇÃO
    // =============================================

    private void ValidarReferencias()
    {
        if (corteA == null) Debug.LogError("[BossVislumbra] ⚠️ Corte A não atribuído!");
        if (corteB == null) Debug.LogError("[BossVislumbra] ⚠️ Corte B não atribuído!");
        if (corteC == null) Debug.LogError("[BossVislumbra] ⚠️ Corte C não atribuído!");
        if (colliderMao == null) Debug.LogError("[BossVislumbra] ⚠️ Collider da Mão não atribuído!");
        if (projetilPrefab == null) Debug.LogWarning("[BossVislumbra] ⚠️ Projetil Prefab não atribuído!");
        if (indicadorCorte == null) Debug.LogWarning("[BossVislumbra] ⚠️ Indicador de Corte não atribuído!");

        Debug.Log($"[BossVislumbra] ✅ Boss iniciado | Vida: {vidaAtual}/{vidaMaxima}");
    }

    // =============================================
    // CICLO DE ATAQUES
    // =============================================

    private IEnumerator CicloDeAtaques()
    {
        yield return new WaitForSeconds(1f);

        while (!morto)
        {
            cicloAtual++;

            // A cada 3 ciclos faz Bullet Hell, senão faz Soco
            TipoAtaque proximo = (cicloAtual % 3 == 0)
                ? TipoAtaque.BulletHell
                : TipoAtaque.Soco;

            if (debugLogs)
                Debug.Log($"[BossVislumbra] 🔄 Ciclo {cicloAtual} — Ataque: {proximo}");

            if (proximo == TipoAtaque.Soco)
                yield return StartCoroutine(SequenciaSoco());
            else
                yield return StartCoroutine(SequenciaBulletHell());

            yield return new WaitForSeconds(1f);
        }
    }

    // =============================================
    // SEQUÊNCIA: SOCO
    // =============================================

    private IEnumerator SequenciaSoco()
    {
        // 1. Escolhe o corte
        corteEscolhido = EscolherCorte();
        Transform corteAlvo = ObterCorte(corteEscolhido);

        if (debugLogs)
            Debug.Log($"[BossVislumbra] 👊 Soco no corte: {NomeCorte(corteEscolhido)}");

        // 2. Mostra indicador no corte escolhido
        if (indicadorCorte != null && corteAlvo != null)
        {
            indicadorCorte.transform.position = corteAlvo.position;
            indicadorCorte.SetActive(true);
        }

        if (somAviso != null && audioSource != null)
            audioSource.PlayOneShot(somAviso);

        if (animator != null)
            animator.SetTrigger("Preparando");

        estado = EstadoBoss.PreparandoSoco;

        // 3. Espera o aviso (tempo para o player esquivar)
        yield return new WaitForSeconds(tempoAvisoSoco);

        // 4. Esconde indicador e prepara o soco
        if (indicadorCorte != null)
            indicadorCorte.SetActive(false);

        yield return new WaitForSeconds(tempoPreparacaoSoco);

        // 5. EXECUTA O SOCO
        estado = EstadoBoss.Socando;

        if (animator != null)
            animator.SetTrigger("Socando");

        if (somSoco != null && audioSource != null)
            audioSource.PlayOneShot(somSoco);

        // Move a mão para o corte
        if (mao != null && corteAlvo != null)
            yield return StartCoroutine(MoverMaoParaCorte(corteAlvo.position));

        // 6. Verifica se o player tomou dano
        VerificarDanoSoco(corteAlvo);

        yield return new WaitForSeconds(tempoDuracaoSoco);

        // 7. Mão volta — Boss fica vulnerável
        if (mao != null)
            yield return StartCoroutine(MoverMaoDeVolta());

        // 8. Abre vulnerabilidade
        estado = EstadoBoss.VulneravelPosSoco;
        AbrirVulnerabilidade();

        yield return new WaitForSeconds(tempoVulneravelAposSoco);

        // 9. Fecha vulnerabilidade
        FecharVulnerabilidade();
        estado = EstadoBoss.Idle;
    }

    // =============================================
    // VERIFICAR DANO DO SOCO
    // =============================================

    private void VerificarDanoSoco(Transform corteAlvo)
    {
        if (player == null || corteAlvo == null) return;

        // Verifica se o player está no corte atacado
        int cortePlayer = ObterCorteDoPlayer();

        if (debugLogs)
            Debug.Log($"[BossVislumbra] Player no corte: {NomeCorte(cortePlayer)} | Soco no corte: {NomeCorte(corteEscolhido)}");

        if (cortePlayer == corteEscolhido)
        {
            // Player tomou o soco!
            if (playerController != null)
            {
                playerController.TakeDamage(danoSoco, gameObject);

                // Empurra o player para o corte oposto
                EmpurrarPlayer();

                if (debugLogs)
                    Debug.Log($"[BossVislumbra] 💥 Player tomou soco! Dano: {danoSoco}");
            }

            if (efeitoImpacto != null)
                Instantiate(efeitoImpacto, corteAlvo.position, Quaternion.identity);
        }
        else
        {
            if (debugLogs)
                Debug.Log("[BossVislumbra] 🧍 Player esquivou do soco!");
        }
    }

    // =============================================
    // EMPURRAR PLAYER
    // =============================================

    private void EmpurrarPlayer()
    {
        if (player == null) return;

        Rigidbody2D rbPlayer = player.GetComponent<Rigidbody2D>();
        if (rbPlayer == null) return;

        // Empurra na direção oposta ao boss
        Vector2 direcao = (player.position - transform.position).normalized;
        direcao.y = 0.3f; // leve impulso para cima
        rbPlayer.AddForce(direcao * forcaEmpurraoSoco, ForceMode2D.Impulse);

        if (debugLogs)
            Debug.Log($"[BossVislumbra] ↗️ Player empurrado com força {forcaEmpurraoSoco}");
    }

    // =============================================
    // MOVIMENTO DA MÃO
    // =============================================

    private IEnumerator MoverMaoParaCorte(Vector3 destino)
    {
        if (mao == null) yield break;

        float duracao = 0.15f;
        float tempo = 0f;
        Vector3 origem = mao.position;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            mao.position = Vector3.Lerp(origem, destino, tempo / duracao);
            yield return null;
        }

        mao.position = destino;
    }

    private IEnumerator MoverMaoDeVolta()
    {
        if (mao == null) yield break;

        float duracao = 0.3f;
        float tempo = 0f;
        Vector3 origem = mao.position;
        Vector3 destino = transform.TransformPoint(posOriginalMao);

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            mao.position = Vector3.Lerp(origem, destino, tempo / duracao);
            yield return null;
        }

        mao.localPosition = posOriginalMao;
    }

    // =============================================
    // VULNERABILIDADE
    // =============================================

    private void AbrirVulnerabilidade()
    {
        vulneravel = true;

        if (colliderMao != null)
            colliderMao.enabled = true;

        if (somVulneravel != null && audioSource != null)
            audioSource.PlayOneShot(somVulneravel);

        if (animator != null)
            animator.SetBool("Vulneravel", true);

        if (debugLogs)
            Debug.Log("[BossVislumbra] 🟢 Mão ABERTA — Boss vulnerável!");
    }

    private void FecharVulnerabilidade()
    {
        vulneravel = false;

        if (colliderMao != null)
            colliderMao.enabled = false;

        if (animator != null)
            animator.SetBool("Vulneravel", false);

        if (debugLogs)
            Debug.Log("[BossVislumbra] 🔴 Mão FECHADA — Boss invulnerável!");
    }

    // =============================================
    // SEQUÊNCIA: BULLET HELL
    // =============================================

    private IEnumerator SequenciaBulletHell()
    {
        estado = EstadoBoss.BulletHell;

        if (debugLogs)
            Debug.Log("[BossVislumbra] 🌀 Iniciando Bullet Hell!");

        if (somBulletHell != null && audioSource != null)
            audioSource.PlayOneShot(somBulletHell);

        if (animator != null)
            animator.SetTrigger("BulletHell");

        float tempoDecorrido = 0f;
        int ondaAtual = 0;

        while (tempoDecorrido < tempoBulletHell && !morto)
        {
            // Alterna entre leque e círculo
            if (ondaAtual % 2 == 0)
                DispararLeque();
            else
                DispararCirculo();

            ondaAtual++;
            lequeInvertido = !lequeInvertido;

            yield return new WaitForSeconds(intervaloOndas);
            tempoDecorrido += intervaloOndas;
        }

        estado = EstadoBoss.Idle;

        if (debugLogs)
            Debug.Log("[BossVislumbra] ✅ Bullet Hell encerrado!");
    }

    // =============================================
    // PADRÃO: LEQUE
    // =============================================

    private void DispararLeque()
    {
        if (projetilPrefab == null || player == null) return;

        Vector3 origem = pontoDisparo != null
            ? pontoDisparo.position
            : transform.position;

        Vector2 direcaoBase = ((Vector2)player.position - (Vector2)origem).normalized;

        float anguloInicio = -anguloLeque / 2f;
        float passo = quantidadeLeque > 1
            ? anguloLeque / (quantidadeLeque - 1)
            : 0f;

        if (lequeInvertido)
            anguloInicio = -anguloInicio;

        for (int i = 0; i < quantidadeLeque; i++)
        {
            float angulo = anguloInicio + passo * i;
            if (lequeInvertido) angulo = -angulo;

            Vector2 dir = RotacionarVetor(direcaoBase, angulo);
            SpawnarProjetil(origem, dir);
        }

        if (debugLogs)
            Debug.Log($"[BossVislumbra] 🌊 Leque disparado ({quantidadeLeque} projeteis) | Invertido: {lequeInvertido}");
    }

    // =============================================
    // PADRÃO: CÍRCULO
    // =============================================

    private void DispararCirculo()
    {
        if (projetilPrefab == null) return;

        Vector3 origem = pontoDisparo != null
            ? pontoDisparo.position
            : transform.position;

        float passo = 360f / quantidadeCirculo;

        for (int i = 0; i < quantidadeCirculo; i++)
        {
            float angulo = passo * i;
            Vector2 dir = new Vector2(
                Mathf.Cos(angulo * Mathf.Deg2Rad),
                Mathf.Sin(angulo * Mathf.Deg2Rad)
            );
            SpawnarProjetil(origem, dir);
        }

        if (debugLogs)
            Debug.Log($"[BossVislumbra] ⭕ Círculo disparado ({quantidadeCirculo} projeteis)");
    }

    // =============================================
    // SPAWNAR PROJETIL
    // =============================================

    private void SpawnarProjetil(Vector3 origem, Vector2 direcao)
    {
        GameObject proj = Instantiate(projetilPrefab, origem, Quaternion.identity);

        Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
        if (rbProj != null)
            rbProj.linearVelocity = direcao * velocidadeProjetil;

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.AngleAxis(angulo, Vector3.forward);
    }

    // =============================================
    // HELPERS — CORTES
    // =============================================

    private int EscolherCorte()
    {
        // Tende a escolher o corte onde o player está
        int cortePlayer = ObterCorteDoPlayer();

        // 70% de chance de escolher o corte do player
        if (Random.value < 0.7f)
            return cortePlayer;

        // 30% aleatório
        return Random.Range(0, 3);
    }

    private int ObterCorteDoPlayer()
    {
        if (player == null) return 0;

        float px = player.position.x;

        float xA = corteA != null ? corteA.position.x : 0f;
        float xB = corteB != null ? corteB.position.x : 0f;
        float xC = corteC != null ? corteC.position.x : 0f;

        float distA = Mathf.Abs(px - xA);
        float distB = Mathf.Abs(px - xB);
        float distC = Mathf.Abs(px - xC);

        if (distA <= distB && distA <= distC) return 0;
        if (distB <= distA && distB <= distC) return 1;
        return 2;
    }

    private Transform ObterCorte(int indice)
    {
        return indice switch
        {
            0 => corteA,
            1 => corteB,
            2 => corteC,
            _ => corteB
        };
    }

    private string NomeCorte(int indice)
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
                Debug.Log($"[BossVislumbra] 🛡️ Dano BLOQUEADO — Boss invulnerável | Fonte: {fonte.name}");
            return;
        }

        vidaAtual -= dano;

        if (debugLogs)
            Debug.Log($"[BossVislumbra] 💥 Tomou {dano} de dano | Vida: {vidaAtual}/{vidaMaxima} | Fonte: {fonte.name}");

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

        if (indicadorCorte != null)
            indicadorCorte.SetActive(false);

        if (efeitoMorte != null)
            Instantiate(efeitoMorte, transform.position, Quaternion.identity);

        if (somMorte != null)
            AudioSource.PlayClipAtPoint(somMorte, transform.position);

        if (animator != null)
            animator.SetTrigger("Morrer");

        Debug.Log("[BossVislumbra] ☠️ Boss derrotado!");

        yield return new WaitForSeconds(duracaoAnimacaoMorte);

        Destroy(gameObject);
    }

    // =============================================
    // BUSCAR PLAYER
    // =============================================

    private void BuscarPlayer()
    {
        var go = GameObject.FindWithTag(tagPlayer);
        if (go != null)
        {
            player = go.transform;
            playerController = go.GetComponent<PlayerController>();

            if (debugLogs)
                Debug.Log($"[BossVislumbra] ✅ Player encontrado: {go.name}");
        }
        else
        {
            Debug.LogWarning("[BossVislumbra] ⚠️ Player NÃO encontrado!");
        }
    }

    // =============================================
    // GIZMOS
    // =============================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos) return;

        // Cortes
        Color[] coresCortes = { Color.red, Color.green, Color.blue };
        Transform[] cortes = { corteA, corteB, corteC };
        string[] nomes = { "A", "B", "C" };

        for (int i = 0; i < cortes.Length; i++)
        {
            if (cortes[i] == null) continue;

            Gizmos.color = coresCortes[i];
            Gizmos.DrawWireSphere(cortes[i].position, 0.5f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = coresCortes[i];
            UnityEditor.Handles.Label(
                cortes[i].position + Vector3.up * 0.7f,
                $"Corte {nomes[i]}"
            );
#endif
        }

        // Mão
        if (mao != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(mao.position, 0.4f);
        }

        // Ponto de disparo
        if (pontoDisparo != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pontoDisparo.position, 0.25f);
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