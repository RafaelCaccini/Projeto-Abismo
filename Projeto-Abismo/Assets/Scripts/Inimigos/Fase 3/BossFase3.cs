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
    // PONTOS DA ARENA — ONDAS
    // =============================================

    [Header("Pontos da Arena - Ondas")]
    [Tooltip("Ponto esquerdo de origem e destino das ondas.")]
    [SerializeField] private Transform spawnA;

    [Tooltip("Ponto direito de origem e destino das ondas.")]
    [SerializeField] private Transform spawnB;

    // =============================================
    // PATRULHA AÉREA
    // =============================================

    [Header("Patrulha Aérea")]
    [Tooltip("Limite esquerdo do voo. Posicione este objeto na altura desejada.")]
    [SerializeField] private Transform pontoVooA;

    [Tooltip("Limite direito do voo. Posicione este objeto na altura desejada.")]
    [SerializeField] private Transform pontoVooB;

    [SerializeField] private float velocidadeVoo = 4f;
    [SerializeField] private float distanciaChegada = 0.05f;

    [Tooltip("Tempo que o boss espera ao chegar a cada extremidade.")]
    [SerializeField] private float tempoParadaExtremidade = 0.3f;

    // =============================================
    // PONTOS DO BULLET HELL
    // =============================================

    [Header("Bullet Hell - Pontos de Ataque")]
    [SerializeField] private Transform pontoAtaqueA;
    [SerializeField] private Transform pontoAtaqueB;
    [SerializeField] private Transform pontoAtaqueC;

    // =============================================
    // CENTRO DA ARENA
    // =============================================

    [Header("Centro da Arena")]
    [Tooltip("Local onde o boss pousa para ficar vulnerável.")]
    [SerializeField] private Transform centroArena;

    // =============================================
    // ATAQUE 01 — ONDAS
    // =============================================

    [Header("Ataque 01 - Ondas")]
    [SerializeField] private GameObject ondaPrefab;
    [SerializeField] private float velocidadeOnda = 6f;
    [SerializeField] private int danoOnda = 1;
    [SerializeField] private int quantidadeOndas = 2;
    [SerializeField] private float intervaloEntreOndas = 0.6f;

    // =============================================
    // ATAQUE 02 — BULLET HELL
    // =============================================

    [Header("Ataque 02 - Bullet Hell")]
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
    // EFEITOS E SONS
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
    // ESTADOS
    // =============================================

    private enum EstadoBoss
    {
        Idle,
        Voando,
        Parado,
        AtaqueOnda,
        AtaqueBulletHell,
        Caindo,
        Vulneravel,
        Morto
    }

    private enum TipoAtaque
    {
        Onda,
        BulletHell
    }

    private EstadoBoss estado = EstadoBoss.Idle;

    // =============================================
    // COMPONENTES
    // =============================================

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private Transform player;

    // =============================================
    // VARIÁVEIS INTERNAS
    // =============================================

    private int vidaAtual;
    private int cicloAtual;

    private bool morto;
    private bool vulneravel;
    private bool indoParaB = true;

    private Vector3 posicaoOriginal;

    private Coroutine rotinaVoo;
    private Coroutine rotinaAtaques;
    private Coroutine rotinaVulnerabilidade;

    // =============================================
    // AWAKE
    // =============================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (visual != null)
            sr = visual.GetComponent<SpriteRenderer>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        vidaAtual = vidaMaxima;

        // O movimento será controlado pelo script.
        // A gravidade não deve derrubar o boss sozinho.
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // O boss não pode receber colisões de ataque
        // durante o voo normal.
        if (col != null)
            col.enabled = false;
    }

    // =============================================
    // START
    // =============================================

    private void Start()
    {
        BuscarPlayer();

        if (!ValidarReferencias())
        {
            enabled = false;
            return;
        }

        posicaoOriginal = transform.position;

        // Inicia a animação de voo.
        if (animator != null)
        {
            animator.SetBool("Voando", true);
            animator.ResetTrigger("Pousar");
        }

        estado = EstadoBoss.Voando;

        // Inicia a patrulha aérea.
        IniciarVoo();

        // Inicia o ciclo de ataques.
        rotinaAtaques = StartCoroutine(CicloDeAtaques());

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Boss iniciado. " +
                $"Vida: {vidaAtual}/{vidaMaxima}"
            );
        }
    }

    // =============================================
    // VALIDAÇÃO DAS REFERÊNCIAS
    // =============================================

    private bool ValidarReferencias()
    {
        bool valido = true;

        if (pontoVooA == null)
        {
            Debug.LogError(
                "[BossFase3] PontoVooA não foi configurado!",
                this
            );

            valido = false;
        }

        if (pontoVooB == null)
        {
            Debug.LogError(
                "[BossFase3] PontoVooB não foi configurado!",
                this
            );

            valido = false;
        }

        if (spawnA == null || spawnB == null)
        {
            Debug.LogError(
                "[BossFase3] Configure SpawnA e SpawnB!",
                this
            );

            valido = false;
        }

        if (centroArena == null)
        {
            Debug.LogError(
                "[BossFase3] CentroArena não foi configurado!",
                this
            );

            valido = false;
        }

        if (pontoAtaqueA == null ||
            pontoAtaqueB == null ||
            pontoAtaqueC == null)
        {
            Debug.LogWarning(
                "[BossFase3] Um ou mais pontos do Bullet Hell " +
                "não foram configurados.",
                this
            );
        }

        if (ondaPrefab == null)
        {
            Debug.LogWarning(
                "[BossFase3] OndaPrefab não foi configurado!",
                this
            );
        }

        if (projetilPrefab == null)
        {
            Debug.LogWarning(
                "[BossFase3] ProjetilPrefab não foi configurado!",
                this
            );
        }

        if (velocidadeVoo <= 0f ||
            velocidadeOnda <= 0f ||
            velocidadeProjetil <= 0f ||
            velocidadeQueda <= 0f)
        {
            Debug.LogError(
                "[BossFase3] As velocidades precisam ser maiores que zero!",
                this
            );

            valido = false;
        }

        return valido;
    }

    // =============================================
    // PATRULHA AÉREA
    // =============================================

    private void IniciarVoo()
    {
        if (morto || vulneravel)
            return;

        if (pontoVooA == null || pontoVooB == null)
            return;

        // Impede duas rotinas de voo simultâneas.
        PararVoo();

        // Começa indo para o ponto mais próximo.
        float distanciaA = Vector2.Distance(
            transform.position,
            pontoVooA.position
        );

        float distanciaB = Vector2.Distance(
            transform.position,
            pontoVooB.position
        );

        indoParaB = distanciaB <= distanciaA;

        if (animator != null)
        {
            animator.SetBool("Voando", true);
            animator.ResetTrigger("Pousar");
        }

        rotinaVoo = StartCoroutine(PatrulharVoo());
    }

    private void PararVoo()
    {
        if (rotinaVoo != null)
        {
            StopCoroutine(rotinaVoo);
            rotinaVoo = null;
        }
    }

    private IEnumerator PatrulharVoo()
    {
        while (!morto)
        {
            // Durante a queda e a vulnerabilidade,
            // a patrulha não pode mover o boss.
            if (estado == EstadoBoss.Caindo ||
                estado == EstadoBoss.Vulneravel)
            {
                yield return null;
                continue;
            }

            Transform destino = indoParaB
                ? pontoVooB
                : pontoVooA;

            if (destino == null)
                break;

            estado = EstadoBoss.Voando;

            // Voa suavemente até o limite escolhido.
            transform.position = Vector3.MoveTowards(
                transform.position,
                destino.position,
                velocidadeVoo * Time.deltaTime
            );

            SincronizarRigidbody();

            // Vira o sprite para a direção do movimento.
            if (sr != null)
            {
                sr.flipX = destino.position.x <
                           transform.position.x;
            }

            // Verifica se chegou ao destino.
            if (Vector2.Distance(
                transform.position,
                destino.position
            ) <= distanciaChegada)
            {
                transform.position = destino.position;
                SincronizarRigidbody();

                estado = EstadoBoss.Parado;

                // Espera brevemente na extremidade.
                float tempo = 0f;

                while (tempo < tempoParadaExtremidade &&
                       !morto &&
                       estado != EstadoBoss.Caindo &&
                       estado != EstadoBoss.Vulneravel)
                {
                    tempo += Time.deltaTime;
                    yield return null;
                }

                // Inverte o sentido apenas se não foi
                // interrompido por uma queda.
                if (estado != EstadoBoss.Caindo &&
                    estado != EstadoBoss.Vulneravel &&
                    !morto)
                {
                    indoParaB = !indoParaB;
                }
            }

            yield return null;
        }

        rotinaVoo = null;
    }

    private void SincronizarRigidbody()
    {
        if (rb != null)
            rb.position = transform.position;
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

            // Ciclo 1: Onda
            // Ciclo 2: Onda
            // Ciclo 3: Bullet Hell + vulnerabilidade
            TipoAtaque proximo = cicloAtual % 3 == 0
                ? TipoAtaque.BulletHell
                : TipoAtaque.Onda;

            if (debugLogs)
            {
                Debug.Log(
                    $"[BossFase3] Ciclo {cicloAtual}: {proximo}"
                );
            }

            if (proximo == TipoAtaque.Onda)
            {
                yield return StartCoroutine(SequenciaOnda());
            }
            else
            {
                yield return StartCoroutine(SequenciaBulletHell());

                if (!morto)
                {
                    rotinaVulnerabilidade =
                        StartCoroutine(SequenciaVulneravel());

                    yield return rotinaVulnerabilidade;
                    rotinaVulnerabilidade = null;
                }
            }

            if (morto)
                yield break;

            yield return new WaitForSeconds(tempoEntreAtaques);
        }
    }

    // =============================================
    // ATAQUE 01 — ONDAS
    // =============================================

    private IEnumerator SequenciaOnda()
    {
        if (morto)
            yield break;

        estado = EstadoBoss.AtaqueOnda;

        bool sairDeA = Random.value > 0.5f;

        Transform origem = sairDeA ? spawnA : spawnB;
        Transform destino = sairDeA ? spawnB : spawnA;

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Onda: {origem.name} -> {destino.name}"
            );
        }

        // Cria as ondas com o intervalo configurado.
        int totalOndas = Mathf.Max(0, quantidadeOndas);

        for (int i = 0; i < totalOndas; i++)
        {
            if (morto)
                yield break;

            SpawnarOnda(origem, destino);

            if (i < totalOndas - 1)
            {
                yield return new WaitForSeconds(
                    Mathf.Max(0f, intervaloEntreOndas)
                );
            }
        }

        // Espera tempo suficiente para as ondas atravessarem
        // a arena antes de começar o próximo ataque.
        float distanciaArena = Vector2.Distance(
            origem.position,
            destino.position
        );

        float tempoTravessia = distanciaArena / velocidadeOnda;

        yield return new WaitForSeconds(tempoTravessia + 0.5f);

        if (!morto)
            estado = EstadoBoss.Voando;
    }

    // =============================================
    // CRIAR UMA ONDA
    // =============================================

    private void SpawnarOnda(Transform origem, Transform destino)
    {
        if (ondaPrefab == null ||
            origem == null ||
            destino == null)
        {
            return;
        }

        GameObject onda = Instantiate(
            ondaPrefab,
            origem.position,
            Quaternion.identity
        );

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
            // Alternativa para prefabs que usam Rigidbody2D.
            Rigidbody2D rbOnda = onda.GetComponent<Rigidbody2D>();

            if (rbOnda != null)
            {
                Vector2 direcao =
                    ((Vector2)destino.position -
                     (Vector2)origem.position).normalized;

                rbOnda.linearVelocity = direcao * velocidadeOnda;
            }
            else
            {
                Debug.LogWarning(
                    "[BossFase3] O prefab da onda não possui " +
                    "OndaBoss nem Rigidbody2D.",
                    onda
                );
            }
        }

        if (somOnda != null && audioSource != null)
            audioSource.PlayOneShot(somOnda);

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Onda criada: {origem.name} -> {destino.name}"
            );
        }
    }

    // =============================================
    // ATAQUE 02 — BULLET HELL
    // =============================================

    private IEnumerator SequenciaBulletHell()
    {
        if (morto)
            yield break;

        estado = EstadoBoss.AtaqueBulletHell;

        int indice = Random.Range(0, 3);
        Transform ponto = ObterPontoAtaque(indice);

        if (ponto == null)
        {
            Debug.LogWarning(
                "[BossFase3] O ponto selecionado para o Bullet Hell " +
                "não está configurado!",
                this
            );

            yield break;
        }

        if (somBulletHell != null && audioSource != null)
            audioSource.PlayOneShot(somBulletHell);

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Bullet Hell disparado do ponto {NomePonto(indice)}"
            );
        }

        int totalOndas = Mathf.Max(0, ondasBulletHell);

        for (int i = 0; i < totalOndas; i++)
        {
            if (morto)
                yield break;

            DispararLequeDoPonto(ponto);

            if (i < totalOndas - 1)
            {
                yield return new WaitForSeconds(
                    Mathf.Max(0f, intervaloOndasBulletHell)
                );
            }
        }

        if (!morto)
            estado = EstadoBoss.Voando;
    }

    // =============================================
    // DISPARAR PROJÉTEIS
    // =============================================

    private void DispararLequeDoPonto(Transform ponto)
    {
        if (projetilPrefab == null || ponto == null)
            return;

        if (player == null)
            BuscarPlayer();

        if (player == null)
        {
            Debug.LogWarning(
                "[BossFase3] Player não encontrado. " +
                "Bullet Hell cancelado.",
                this
            );

            return;
        }

        Vector2 direcaoBase =
            ((Vector2)player.position -
             (Vector2)ponto.position).normalized;

        int totalProjeteis = Mathf.Max(1, quantidadeProjeteis);

        float anguloInicio = -anguloEspalhamento / 2f;

        float passo = totalProjeteis > 1
            ? anguloEspalhamento / (totalProjeteis - 1)
            : 0f;

        for (int i = 0; i < totalProjeteis; i++)
        {
            float angulo = anguloInicio + passo * i;

            Vector2 direcao = RotacionarVetor(
                direcaoBase,
                angulo
            );

            GameObject projetil = Instantiate(
                projetilPrefab,
                ponto.position,
                Quaternion.identity
            );

            Rigidbody2D rbProjetil =
                projetil.GetComponent<Rigidbody2D>();

            if (rbProjetil != null)
            {
                rbProjetil.linearVelocity =
                    direcao * velocidadeProjetil;
            }
            else
            {
                Debug.LogWarning(
                    "[BossFase3] Um projétil não possui Rigidbody2D.",
                    projetil
                );
            }

            float anguloRotacao =
                Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

            projetil.transform.rotation = Quaternion.AngleAxis(
                anguloRotacao,
                Vector3.forward
            );
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Leque disparado: {totalProjeteis} projéteis."
            );
        }
    }

    // =============================================
    // VULNERABILIDADE — QUEDA NO CENTRO
    // =============================================

    private IEnumerator SequenciaVulneravel()
    {
        if (morto || centroArena == null)
            yield break;

        estado = EstadoBoss.Caindo;

        // Para o voo antes de iniciar a descida.
        PararVoo();

        if (debugLogs)
            Debug.Log("[BossFase3] Boss descendo para o centro.");

        if (somQueda != null && audioSource != null)
            audioSource.PlayOneShot(somQueda);

        yield return new WaitForSeconds(
            Mathf.Max(0f, tempoPreparacaoQueda)
        );

        if (morto)
            yield break;

        // Desce até o ponto central.
        yield return StartCoroutine(MoverParaCentro());

        if (morto)
            yield break;

        // Inicia a animação de pouso.
        if (animator != null)
        {
            animator.SetBool("Voando", false);
            animator.ResetTrigger("Pousar");
            animator.SetTrigger("Pousar");
        }

        // Ativa a colisão para permitir que o player ataque.
        if (col != null)
            col.enabled = true;

        vulneravel = true;
        estado = EstadoBoss.Vulneravel;

        if (sr != null)
            sr.color = corVulneravel;

        if (somVulneravel != null && audioSource != null)
            audioSource.PlayOneShot(somVulneravel);

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Boss vulnerável por {duracaoVulneravel}s."
            );
        }

        yield return new WaitForSeconds(
            Mathf.Max(0f, duracaoVulneravel)
        );

        if (morto)
            yield break;

        // Encerra a janela de dano.
        FecharVulnerabilidade();

        // Retorna à posição aérea e retoma a patrulha.
        yield return StartCoroutine(VoltarParaPosicaoOriginal());

        if (!morto)
        {
            estado = EstadoBoss.Voando;

            if (debugLogs)
                Debug.Log("[BossFase3] Boss retomou o voo.");
        }
    }

    // =============================================
    // MOVIMENTO ATÉ O CENTRO
    // =============================================

    private IEnumerator MoverParaCentro()
    {
        if (centroArena == null)
            yield break;

        Vector3 destino = centroArena.position;

        while (!morto &&
               Vector3.Distance(transform.position, destino) >
               distanciaChegada)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                destino,
                velocidadeQueda * Time.deltaTime
            );

            SincronizarRigidbody();

            yield return null;
        }

        if (!morto)
        {
            transform.position = destino;
            SincronizarRigidbody();
        }
    }

    // =============================================
    // VOLTAR A VOAR
    // =============================================

    private IEnumerator VoltarParaPosicaoOriginal()
    {
        Vector3 origem = transform.position;
        Vector3 destino = posicaoOriginal;

        float duracao = 0.8f;
        float tempo = 0f;

        if (animator != null)
        {
            animator.SetBool("Voando", true);
            animator.ResetTrigger("Pousar");
        }

        while (tempo < duracao && !morto)
        {
            tempo += Time.deltaTime;

            transform.position = Vector3.Lerp(
                origem,
                destino,
                Mathf.Clamp01(tempo / duracao)
            );

            SincronizarRigidbody();

            yield return null;
        }

        if (morto)
            yield break;

        transform.position = destino;
        SincronizarRigidbody();

        // Reinicia a patrulha entre os dois limites.
        IniciarVoo();
    }

    // =============================================
    // ENCERRAR VULNERABILIDADE
    // =============================================

    private void FecharVulnerabilidade()
    {
        vulneravel = false;

        if (col != null)
            col.enabled = false;

        if (sr != null)
            sr.color = corNormal;

        if (debugLogs)
            Debug.Log("[BossFase3] Boss voltou a ficar invulnerável.");
    }

    // =============================================
    // HELPERS — PONTOS DE ATAQUE
    // =============================================

    private Transform ObterPontoAtaque(int indice)
    {
        switch (indice)
        {
            case 0:
                return pontoAtaqueA;

            case 1:
                return pontoAtaqueB;

            case 2:
                return pontoAtaqueC;

            default:
                return pontoAtaqueB;
        }
    }

    private string NomePonto(int indice)
    {
        switch (indice)
        {
            case 0:
                return "A";

            case 1:
                return "B";

            case 2:
                return "C";

            default:
                return "?";
        }
    }

    private Vector2 RotacionarVetor(Vector2 vetor, float graus)
    {
        float radianos = graus * Mathf.Deg2Rad;

        return new Vector2(
            vetor.x * Mathf.Cos(radianos) -
            vetor.y * Mathf.Sin(radianos),

            vetor.x * Mathf.Sin(radianos) +
            vetor.y * Mathf.Cos(radianos)
        );
    }

    // =============================================
    // VIDA E DANO
    // =============================================

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto || dano <= 0)
            return;

        if (!vulneravel)
        {
            if (debugLogs)
            {
                string nomeFonte = fonte != null
                    ? fonte.name
                    : "Desconhecida";

                Debug.Log(
                    $"[BossFase3] Dano bloqueado. " +
                    $"Estado: {estado} | Fonte: {nomeFonte}"
                );
            }

            return;
        }

        vidaAtual -= dano;
        vidaAtual = Mathf.Max(0, vidaAtual);

        if (debugLogs)
        {
            string nomeFonte = fonte != null
                ? fonte.name
                : "Desconhecida";

            Debug.Log(
                $"[BossFase3] Recebeu {dano} de dano. " +
                $"Vida: {vidaAtual}/{vidaMaxima} | " +
                $"Fonte: {nomeFonte}"
            );
        }

        if (efeitoImpacto != null)
        {
            Instantiate(
                efeitoImpacto,
                transform.position,
                Quaternion.identity
            );
        }

        if (vidaAtual <= 0)
            Morrer();
    }

    // =============================================
    // MORTE
    // =============================================

    private void Morrer()
    {
        if (morto)
            return;

        morto = true;
        vulneravel = false;
        estado = EstadoBoss.Morto;

        // Interrompe voo, ataques e qualquer sequência ativa.
        StopAllCoroutines();

        rotinaVoo = null;
        rotinaAtaques = null;
        rotinaVulnerabilidade = null;

        StartCoroutine(MorrerRoutine());
    }

    private IEnumerator MorrerRoutine()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Desativa todos os colliders do boss.
        foreach (Collider2D colliderBoss in
                 GetComponentsInChildren<Collider2D>())
        {
            colliderBoss.enabled = false;
        }

        if (sr != null)
            sr.color = corNormal;

        if (efeitoMorte != null)
        {
            Instantiate(
                efeitoMorte,
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

        if (debugLogs)
            Debug.Log("[BossFase3] Boss derrotado!");

        yield return new WaitForSeconds(
            Mathf.Max(0f, duracaoAnimacaoMorte)
        );

        Destroy(gameObject);
    }

    // =============================================
    // BUSCAR PLAYER
    // =============================================

    private void BuscarPlayer()
    {
        GameObject objetoPlayer = GameObject.FindWithTag("Player");

        if (objetoPlayer != null)
        {
            player = objetoPlayer.transform;

            if (debugLogs)
            {
                Debug.Log(
                    $"[BossFase3] Player encontrado: {objetoPlayer.name}"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[BossFase3] Player não encontrado. " +
                "Verifique se ele possui a tag Player.",
                this
            );
        }
    }

    // =============================================
    // GIZMOS — VISUALIZAÇÃO NO EDITOR
    // =============================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos)
            return;

        // Limites do voo.
        if (pontoVooA != null && pontoVooB != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(pontoVooA.position, 0.4f);
            Gizmos.DrawWireSphere(pontoVooB.position, 0.4f);
            Gizmos.DrawLine(
                pontoVooA.position,
                pontoVooB.position
            );

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(
                pontoVooA.position + Vector3.up * 0.5f,
                "Limite do voo A"
            );

            UnityEditor.Handles.Label(
                pontoVooB.position + Vector3.up * 0.5f,
                "Limite do voo B"
            );
#endif
        }

        // Trajeto das ondas.
        if (spawnA != null && spawnB != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(spawnA.position, 0.4f);
            Gizmos.DrawWireSphere(spawnB.position, 0.4f);
            Gizmos.DrawLine(spawnA.position, spawnB.position);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(
                spawnA.position + Vector3.up * 0.6f,
                "SpawnA"
            );

            UnityEditor.Handles.Label(
                spawnB.position + Vector3.up * 0.6f,
                "SpawnB"
            );
#endif
        }

        // Pontos do Bullet Hell.
        Transform[] pontos =
        {
            pontoAtaqueA,
            pontoAtaqueB,
            pontoAtaqueC
        };

        Color[] cores =
        {
            Color.red,
            Color.blue,
            Color.magenta
        };

        string[] nomes =
        {
            "PontoAtaqueA",
            "PontoAtaqueB",
            "PontoAtaqueC"
        };

        for (int i = 0; i < pontos.Length; i++)
        {
            if (pontos[i] == null)
                continue;

            Gizmos.color = cores[i];

            Gizmos.DrawWireSphere(
                pontos[i].position,
                0.4f
            );

#if UNITY_EDITOR
            UnityEditor.Handles.color = cores[i];
            UnityEditor.Handles.Label(
                pontos[i].position + Vector3.up * 0.6f,
                nomes[i]
            );
#endif
        }

        // Centro da arena.
        if (centroArena != null)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                centroArena.position,
                0.5f
            );

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(
                centroArena.position + Vector3.up * 0.7f,
                "Centro da Arena"
            );
#endif
        }

        // Informações de estado durante o jogo.
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