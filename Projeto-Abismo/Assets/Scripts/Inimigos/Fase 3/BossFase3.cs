using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossFase3 : MonoBehaviour, IDamageable
{
    // =====================================================
    // REFERÊNCIAS
    // =====================================================

    [Header("Referências")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    // =====================================================
    // PONTOS DA ARENA - ONDAS
    // =====================================================

    [Header("Pontos da Arena - Ondas")]
    [Tooltip("Origem esquerda das ondas.")]
    [SerializeField] private Transform spawnA;

    [Tooltip("Origem direita das ondas.")]
    [SerializeField] private Transform spawnB;

    // =====================================================
    // PATRULHA AÉREA
    // =====================================================

    [Header("Patrulha Aérea")]
    [Tooltip("Limite esquerdo do voo.")]
    [SerializeField] private Transform pontoVooA;

    [Tooltip("Limite direito do voo.")]
    [SerializeField] private Transform pontoVooB;

    [SerializeField] private float velocidadeVoo = 4f;
    [SerializeField] private float distanciaChegada = 0.05f;

    [Tooltip("Tempo de espera nas extremidades.")]
    [SerializeField] private float tempoParadaExtremidade = 0.3f;

    // =====================================================
    // PONTOS DO BULLET HELL
    // =====================================================

    [Header("Bullet Hell - Pontos de Ataque")]
    [SerializeField] private Transform pontoAtaqueA;
    [SerializeField] private Transform pontoAtaqueB;
    [SerializeField] private Transform pontoAtaqueC;

    // =====================================================
    // CENTRO DA ARENA
    // =====================================================

    [Header("Centro da Arena")]
    [Tooltip("Local onde o chefe pousa para ficar vulnerável.")]
    [SerializeField] private Transform centroArena;

    // =====================================================
    // ATAQUE 01 - ONDAS
    // =====================================================

    [Header("Ataque 01 - Ondas")]
    [SerializeField] private GameObject ondaPrefab;

    [SerializeField] private float velocidadeOnda = 5.5f;
    [SerializeField] private int danoOnda = 1;

    [Tooltip("Quantidade de ondas por ataque.")]
    [SerializeField] private int quantidadeOndas = 2;

    [Tooltip("Intervalo entre as ondas.")]
    [SerializeField] private float intervaloEntreOndas = 0.85f;

    // =====================================================
    // ATAQUE 02 - BULLET HELL
    // =====================================================

    [Header("Ataque 02 - Bullet Hell")]
    [SerializeField] private GameObject projetilPrefab;

    [SerializeField] private float velocidadeProjetil = 6.5f;

    [Tooltip("Use um valor ímpar para ter um projétil central.")]
    [SerializeField] private int quantidadeProjeteis = 7;

    [Tooltip("Abertura total do leque de projéteis.")]
    [SerializeField] private float anguloEspalhamento = 60f;

    [Tooltip("Quantidade de disparos por sequência de Bullet Hell.")]
    [SerializeField] private int ondasBulletHell = 2;

    [Tooltip("Intervalo entre os disparos do Bullet Hell.")]
    [SerializeField] private float intervaloOndasBulletHell = 0.85f;

    [Tooltip("Tempo máximo que cada projétil pode existir.")]
    [SerializeField] private float tempoMaximoVidaProjetil = 4f;

    // =====================================================
    // VULNERABILIDADE
    // =====================================================

    [Header("Vulnerabilidade")]
    [SerializeField] private float duracaoVulneravel = 3f;
    [SerializeField] private float velocidadeQueda = 8f;

    [SerializeField] private Color corVulneravel = Color.red;
    [SerializeField] private Color corNormal = Color.white;

    // =====================================================
    // VIDA
    // =====================================================

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 8;
    [SerializeField] private float duracaoAnimacaoMorte = 1.5f;

    // =====================================================
    // EFEITOS E SONS
    // =====================================================

    [Header("Efeitos")]
    [SerializeField] private AudioClip somOnda;
    [SerializeField] private AudioClip somBulletHell;
    [SerializeField] private AudioClip somQueda;
    [SerializeField] private AudioClip somVulneravel;
    [SerializeField] private AudioClip somMorte;

    [SerializeField] private GameObject efeitoMorte;
    [SerializeField] private GameObject efeitoImpacto;

    // =====================================================
    // TEMPOS
    // =====================================================

    [Header("Tempos")]
    [SerializeField] private float tempoEntreAtaques = 2f;
    [SerializeField] private float tempoPreparacaoQueda = 1f;

    // =====================================================
    // DEBUG
    // =====================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool mostrarGizmos = true;

    // =====================================================
    // ESTADOS
    // =====================================================

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

    // =====================================================
    // COMPONENTES
    // =====================================================

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private Transform player;

    // =====================================================
    // VARIÁVEIS INTERNAS
    // =====================================================

    private int vidaAtual;
    private int cicloAtual;

    private bool morto;
    private bool vulneravel;
    private bool indoParaB = true;

    // Só permite iniciar a vulnerabilidade se o Bullet Hell
    // realmente tiver sido executado.
    private bool bulletHellExecutado;

    private Vector3 posicaoOriginal;

    private Coroutine rotinaVoo;
    private Coroutine rotinaAtaques;
    private Coroutine rotinaVulnerabilidade;

    // Guarda as ondas e os projéteis que ainda existem.
    private readonly List<GameObject> perigosAtivos =
        new List<GameObject>();

    // =====================================================
    // AWAKE
    // =====================================================

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

        vidaMaxima = Mathf.Max(1, vidaMaxima);
        vidaAtual = vidaMaxima;

        // O movimento será controlado pelo script.
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // O chefe só poderá receber ataques quando vulnerável.
        if (col != null)
            col.enabled = false;
    }

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        BuscarPlayer();

        if (!ValidarReferencias())
        {
            enabled = false;
            return;
        }

        posicaoOriginal = transform.position;

        if (animator != null)
        {
            animator.SetBool("Voando", true);
            animator.ResetTrigger("Pousar");
        }

        estado = EstadoBoss.Voando;

        IniciarVoo();

        rotinaAtaques = StartCoroutine(CicloDeAtaques());

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Iniciado. Vida: {vidaAtual}/{vidaMaxima}",
                this
            );
        }
    }

    // =====================================================
    // VALIDAR REFERÊNCIAS
    // =====================================================

    private bool ValidarReferencias()
    {
        bool valido = true;

        if (pontoVooA == null || pontoVooB == null)
        {
            Debug.LogError(
                "[BossFase3] Configure PontoVooA e PontoVooB!",
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

        if (pontoAtaqueA == null &&
            pontoAtaqueB == null &&
            pontoAtaqueC == null)
        {
            Debug.LogWarning(
                "[BossFase3] Nenhum ponto do Bullet Hell foi configurado.",
                this
            );
        }

        if (velocidadeVoo <= 0f ||
            velocidadeOnda <= 0f ||
            velocidadeProjetil <= 0f ||
            velocidadeQueda <= 0f)
        {
            Debug.LogError(
                "[BossFase3] Todas as velocidades precisam ser maiores que zero!",
                this
            );

            valido = false;
        }

        return valido;
    }

    // =====================================================
    // PATRULHA AÉREA
    // =====================================================

    private void IniciarVoo()
    {
        if (morto || vulneravel)
            return;

        if (pontoVooA == null || pontoVooB == null)
            return;

        PararVoo();

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
            if (estado != EstadoBoss.Voando &&
                estado != EstadoBoss.Parado)
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

            transform.position = Vector3.MoveTowards(
                transform.position,
                destino.position,
                velocidadeVoo * Time.deltaTime
            );

            SincronizarRigidbody();

            // Mantém a orientação do sprite.
            if (sr != null)
            {
                sr.flipX =
                    destino.position.x < transform.position.x;
            }

            float distancia = Vector2.Distance(
                transform.position,
                destino.position
            );

            if (distancia <= distanciaChegada)
            {
                transform.position = destino.position;
                SincronizarRigidbody();

                estado = EstadoBoss.Parado;

                float tempo = 0f;

                while (tempo < tempoParadaExtremidade &&
                       !morto &&
                       estado == EstadoBoss.Parado)
                {
                    tempo += Time.deltaTime;
                    yield return null;
                }

                if (!morto && estado == EstadoBoss.Parado)
                {
                    indoParaB = !indoParaB;
                    estado = EstadoBoss.Voando;
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

    // =====================================================
    // CICLO DE ATAQUES
    // =====================================================


    private IEnumerator CicloDeAtaques()
    {
        // Dá tempo para o jogador se posicionar.
        yield return new WaitForSeconds(2f);

        while (!morto)
        {
            cicloAtual++;

            // Dois ataques de ondas para cada sequência de tiros.
            TipoAtaque proximo = cicloAtual % 3 == 0
                ? TipoAtaque.BulletHell
                : TipoAtaque.Onda;

            if (debugLogs)
            {
                Debug.Log(
                    $"[BossFase3] Ciclo {cicloAtual}: {proximo}",
                    this
                );
            }

            // Executa apenas a ação selecionada.
            if (proximo == TipoAtaque.Onda)
            {
                yield return StartCoroutine(SequenciaOnda());
            }
            else
            {
                yield return StartCoroutine(SequenciaBulletHell());

                if (morto)
                    yield break;

                // Só pousa se realmente conseguiu disparar.
                if (bulletHellExecutado)
                {
                    rotinaVulnerabilidade =
                        StartCoroutine(SequenciaVulneravel());

                    yield return rotinaVulnerabilidade;
                    rotinaVulnerabilidade = null;
                }
            }

            if (morto)
                yield break;

            // Descanso entre as ações completas.
            yield return new WaitForSeconds(
                Mathf.Max(1f, tempoEntreAtaques)
            );
        }
    }

    // =====================================================
    // ATAQUE 01 - ONDAS
    // =====================================================

    private IEnumerator SequenciaOnda()
    {
        if (morto || ondaPrefab == null)
            yield break;

        // Interrompe a patrulha para o ataque ser uma ação isolada.
        PararVoo();
        estado = EstadoBoss.AtaqueOnda;

        bool sairDeA = Random.value > 0.5f;

        Transform origem = sairDeA ? spawnA : spawnB;
        Transform destino = sairDeA ? spawnB : spawnA;

        if (origem == null || destino == null)
        {
            Debug.LogWarning(
                "[BossFase3] Não foi possível criar a onda: " +
                "origem ou destino ausente.",
                this
            );

            estado = EstadoBoss.Voando;
            IniciarVoo();
            yield break;
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Ondas: {origem.name} -> {destino.name}",
                this
            );
        }

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

        // Aguarda as ondas atravessarem a arena.
        float distanciaArena = Vector2.Distance(
            origem.position,
            destino.position
        );

        float tempoMaximo =
            distanciaArena / Mathf.Max(0.1f, velocidadeOnda) + 1f;

        yield return StartCoroutine(
            AguardarPerigosTerminarem(tempoMaximo)
        );

        if (morto)
            yield break;

        // O chefe só volta a voar quando as ondas terminam.
        estado = EstadoBoss.Voando;
        IniciarVoo();
    }

    // =====================================================
    // CRIAR UMA ONDA
    // =====================================================

    private void SpawnarOnda(Transform origem, Transform destino)
    {
        if (morto || ondaPrefab == null ||
            origem == null || destino == null)
        {
            return;
        }

        GameObject onda = Instantiate(
            ondaPrefab,
            origem.position,
            Quaternion.identity
        );

        RegistrarPerigo(onda);

        // O nome correto da classe é OndaBoss2.
        OndaBoss2 ondaScript = onda.GetComponent<OndaBoss2>();

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
            // Movimento alternativo para um prefab com Rigidbody2D.
            Rigidbody2D rbOnda = onda.GetComponent<Rigidbody2D>();

            if (rbOnda != null)
            {
                Vector2 direcao =
                    ((Vector2)destino.position -
                     (Vector2)origem.position).normalized;

                rbOnda.gravityScale = 0f;
                rbOnda.linearVelocity = direcao * velocidadeOnda;
            }
            else
            {
                Debug.LogWarning(
                    "[BossFase3] O prefab da onda não possui " +
                    "OndaBoss2 nem Rigidbody2D.",
                    onda
                );
            }
        }

        if (somOnda != null && audioSource != null)
            audioSource.PlayOneShot(somOnda);

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Onda criada: {origem.name} -> {destino.name}",
                this
            );
        }
    }

    // =====================================================
    // ATAQUE 02 - BULLET HELL
    // =====================================================

    private IEnumerator SequenciaBulletHell()
    {
        if (morto)
            yield break;

        bulletHellExecutado = false;

        if (projetilPrefab == null)
        {
            Debug.LogWarning(
                "[BossFase3] Bullet Hell cancelado: ProjetilPrefab ausente.",
                this
            );

            yield break;
        }

        if (player == null)
            BuscarPlayer();

        if (player == null)
        {
            Debug.LogWarning(
                "[BossFase3] Bullet Hell cancelado: Player não encontrado.",
                this
            );

            yield break;
        }

        Transform ponto = ObterPontoAtaqueAleatorio();

        if (ponto == null)
        {
            Debug.LogWarning(
                "[BossFase3] Bullet Hell cancelado: nenhum ponto de ataque válido.",
                this
            );

            yield break;
        }

        // Para o movimento aéreo durante o ataque.
        PararVoo();
        estado = EstadoBoss.AtaqueBulletHell;

        if (somBulletHell != null && audioSource != null)
            audioSource.PlayOneShot(somBulletHell);

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Bullet Hell iniciado pelo {ponto.name}.",
                this
            );
        }

        int totalDisparos = Mathf.Max(1, ondasBulletHell);

        for (int i = 0; i < totalDisparos; i++)
        {
            if (morto)
                yield break;

            if (!DispararLequeDoPonto(ponto))
            {
                // Não deixa o chefe preso no ar caso o ataque
                // não consiga ser executado.
                if (!morto)
                {
                    estado = EstadoBoss.Voando;
                    IniciarVoo();
                }

                yield break;
            }

            if (i < totalDisparos - 1)
            {
                yield return new WaitForSeconds(
                    Mathf.Max(0f, intervaloOndasBulletHell)
                );
            }
        }

        bulletHellExecutado = true;

        // Todos os projéteis têm uma vida máxima configurada.
        // Espera até desaparecerem antes de iniciar a vulnerabilidade.
        yield return StartCoroutine(
            AguardarPerigosTerminarem(
                Mathf.Max(0.1f, tempoMaximoVidaProjetil) + 0.25f
            )
        );

        if (morto)
            yield break;

        // Não reinicia o voo aqui: a sequência seguinte é a queda
        // para a vulnerabilidade.
        estado = EstadoBoss.Voando;

        if (debugLogs)
        {
            Debug.Log(
                "[BossFase3] Bullet Hell concluído. " +
                "Iniciando sequência de vulnerabilidade.",
                this
            );
        }
    }

    // =====================================================
    // DISPARAR LEQUE DE PROJÉTEIS
    // =====================================================


    private bool DispararLequeDoPonto(Transform ponto)
    {
        if (morto || projetilPrefab == null || ponto == null)
        {
            Debug.LogWarning(
                "[BossFase3] Disparo cancelado: prefab ou ponto ausente.",
                this
            );
            return false;
        }

        // Atualiza a referência se o jogador ainda não foi encontrado.
        if (player == null)
            BuscarPlayer();

        if (player == null)
            return false;

        // Calcula a direção do ponto de disparo até o jogador.
        Vector2 origem = ponto.position;
        Vector2 alvo = player.position;
        Vector2 direcaoBase = alvo - origem;

        if (direcaoBase.sqrMagnitude < 0.001f)
            return false;

        direcaoBase.Normalize();

        // Cinco projéteis formam um leque legível e equilibrado.
        int totalProjeteis = Mathf.Max(1, quantidadeProjeteis);

        // Garante um projétil central.
        if (totalProjeteis % 2 == 0)
            totalProjeteis++;

        float abertura = Mathf.Clamp(anguloEspalhamento, 0f, 120f);
        float anguloInicial = -abertura / 2f;

        float passo = totalProjeteis > 1
            ? abertura / (totalProjeteis - 1)
            : 0f;

        for (int i = 0; i < totalProjeteis; i++)
        {
            float angulo = anguloInicial + passo * i;

            Vector2 direcao = RotacionarVetor(
                direcaoBase,
                angulo
            ).normalized;

            GameObject projetil = Instantiate(
                projetilPrefab,
                ponto.position,
                Quaternion.identity
            );

            RegistrarPerigo(projetil);

            // Impede que projéteis antigos fiquem na arena para sempre.
            Destroy(
                projetil,
                Mathf.Max(0.5f, tempoMaximoVidaProjetil)
            );

            Rigidbody2D rbProjetil =
                projetil.GetComponent<Rigidbody2D>();

            if (rbProjetil == null)
                rbProjetil = projetil.GetComponentInChildren<Rigidbody2D>();

            if (rbProjetil == null)
            {
                Debug.LogError(
                    "[BossFase3] O prefab do projétil não possui " +
                    "Rigidbody2D. Adicione o componente ao objeto raiz.",
                    projetil
                );

                Destroy(projetil);
                continue;
            }

            // Configura o projétil para se mover sem cair pela gravidade.
            rbProjetil.bodyType = RigidbodyType2D.Dynamic;
            rbProjetil.gravityScale = 0f;
            rbProjetil.angularVelocity = 0f;
            rbProjetil.linearVelocity = direcao * velocidadeProjetil;

            // Gira o visual para acompanhar o deslocamento.
            float rotacao = Mathf.Atan2(
                direcao.y,
                direcao.x
            ) * Mathf.Rad2Deg;

            projetil.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                rotacao
            );
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[BossFase3] Disparou um leque em direção ao jogador. " +
                $"Projéteis: {totalProjeteis}; origem: {ponto.name}.",
                this
            );
        }

        return true;
    }


    // =====================================================
    // CONTROLE DOS PERIGOS ATIVOS
    // =====================================================

    private void RegistrarPerigo(GameObject perigo)
    {
        if (perigo != null && !perigosAtivos.Contains(perigo))
            perigosAtivos.Add(perigo);
    }

    private void LimparReferenciasPerigos()
    {
        for (int i = perigosAtivos.Count - 1; i >= 0; i--)
        {
            if (perigosAtivos[i] == null)
                perigosAtivos.RemoveAt(i);
        }
    }

    private IEnumerator AguardarPerigosTerminarem(float tempoMaximo)
    {
        float tempoDecorrido = 0f;

        tempoMaximo = Mathf.Max(0f, tempoMaximo);

        while (!morto && tempoDecorrido < tempoMaximo)
        {
            LimparReferenciasPerigos();

            // O ataque terminou quando todas as ondas ou projéteis
            // registrados deixarem de existir.
            if (perigosAtivos.Count == 0)
                yield break;

            tempoDecorrido += Time.deltaTime;

            yield return null;
        }

        if (morto)
            yield break;

        // Segurança: remove os perigos que excederam o tempo limite.
        for (int i = perigosAtivos.Count - 1; i >= 0; i--)
        {
            if (perigosAtivos[i] != null)
                Destroy(perigosAtivos[i]);
        }

        perigosAtivos.Clear();

        // Dá um frame para o Unity processar as destruições
        // antes de liberar a próxima ação.
        yield return null;
    }

    // =====================================================
    // VULNERABILIDADE - QUEDA NO CENTRO
    // =====================================================

    private IEnumerator SequenciaVulneravel()
    {
        if (morto || centroArena == null)
            yield break;

        // Interrompe a patrulha antes da descida.
        PararVoo();

        vulneravel = false;
        estado = EstadoBoss.Caindo;

        if (debugLogs)
            Debug.Log("[BossFase3] Chefe descendo para o centro.", this);

        if (somQueda != null && audioSource != null)
            audioSource.PlayOneShot(somQueda);

        yield return new WaitForSeconds(
            Mathf.Max(0f, tempoPreparacaoQueda)
        );

        if (morto)
            yield break;

        yield return StartCoroutine(MoverParaCentro());

        if (morto)
            yield break;

        if (animator != null)
        {
            animator.SetBool("Voando", false);
            animator.ResetTrigger("Pousar");
            animator.SetTrigger("Pousar");
        }

        // Ativa a colisão para permitir os ataques do jogador.
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
                $"[BossFase3] Vulnerável por {duracaoVulneravel} segundos.",
                this
            );
        }

        yield return new WaitForSeconds(
            Mathf.Max(0f, duracaoVulneravel)
        );

        if (morto)
            yield break;

        FecharVulnerabilidade();

        yield return StartCoroutine(VoltarParaPosicaoOriginal());

        if (!morto)
        {
            estado = EstadoBoss.Voando;

            if (debugLogs)
                Debug.Log("[BossFase3] Chefe retomou a patrulha.", this);
        }
    }

    // =====================================================
    // MOVIMENTO ATÉ O CENTRO
    // =====================================================

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

    // =====================================================
    // VOLTAR A VOAR
    // =====================================================

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

        // Reinicia a patrulha somente depois de voltar ao alto.
        estado = EstadoBoss.Voando;
        IniciarVoo();
    }

    // =====================================================
    // ENCERRAR VULNERABILIDADE
    // =====================================================

    private void FecharVulnerabilidade()
    {
        vulneravel = false;

        if (col != null)
            col.enabled = false;

        if (sr != null)
            sr.color = corNormal;

        if (debugLogs)
            Debug.Log("[BossFase3] Chefe voltou a ficar invulnerável.", this);
    }

    // =====================================================
    // ESCOLHER PONTO DO BULLET HELL
    // =====================================================

    private Transform ObterPontoAtaqueAleatorio()
    {
        List<Transform> pontosValidos = new List<Transform>();

        if (pontoAtaqueA != null)
            pontosValidos.Add(pontoAtaqueA);

        if (pontoAtaqueB != null)
            pontosValidos.Add(pontoAtaqueB);

        if (pontoAtaqueC != null)
            pontosValidos.Add(pontoAtaqueC);

        if (pontosValidos.Count == 0)
            return null;

        int indice = Random.Range(0, pontosValidos.Count);

        return pontosValidos[indice];
    }

    // =====================================================
    // ROTACIONAR VETOR
    // =====================================================

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

    // =====================================================
    // VIDA E DANO
    // =====================================================

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
                    $"[BossFase3] Dano bloqueado. Estado: {estado} | " +
                    $"Fonte: {nomeFonte}",
                    this
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
                $"Vida: {vidaAtual}/{vidaMaxima} | Fonte: {nomeFonte}",
                this
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

    // =====================================================
    // MORTE
    // =====================================================

    private void Morrer()
    {
        if (morto)
            return;

        morto = true;
        vulneravel = false;
        estado = EstadoBoss.Morto;

        StopAllCoroutines();

        rotinaVoo = null;
        rotinaAtaques = null;
        rotinaVulnerabilidade = null;

        // Remove todas as ondas e os projéteis restantes.
        for (int i = perigosAtivos.Count - 1; i >= 0; i--)
        {
            if (perigosAtivos[i] != null)
                Destroy(perigosAtivos[i]);
        }

        perigosAtivos.Clear();

        StartCoroutine(MorrerRoutine());
    }

    private IEnumerator MorrerRoutine()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Desativa os colliders do chefe.
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
            Debug.Log("[BossFase3] Chefe derrotado!", this);

        yield return new WaitForSeconds(
            Mathf.Max(0f, duracaoAnimacaoMorte)
        );

        Destroy(gameObject);
    }

    // =====================================================
    // BUSCAR PLAYER
    // =====================================================

    private void BuscarPlayer()
    {
        GameObject objetoPlayer = GameObject.FindWithTag("Player");

        if (objetoPlayer != null)
        {
            player = objetoPlayer.transform;

            if (debugLogs)
            {
                Debug.Log(
                    $"[BossFase3] Player encontrado: {objetoPlayer.name}",
                    this
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[BossFase3] Player não encontrado. " +
                "Verifique a tag Player no objeto do jogador.",
                this
            );
        }
    }

    // =====================================================
    // GIZMOS
    // =====================================================

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

        // Caminho das ondas.
        if (spawnA != null && spawnB != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(spawnA.position, 0.4f);
            Gizmos.DrawWireSphere(spawnB.position, 0.4f);

            Gizmos.DrawLine(
                spawnA.position,
                spawnB.position
            );

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

        // Pontos possíveis de disparo.
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

        // Estado durante o jogo.
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = Color.white;

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Estado: {estado}\n" +
                $"Vida: {vidaAtual}/{vidaMaxima}\n" +
                $"Vulnerável: {vulneravel}\n" +
                $"Ciclo: {cicloAtual}\n" +
                $"Perigos ativos: {perigosAtivos.Count}"
            );
        }
#endif
    }
}