using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Usa o TipoOnda definido em OndaBoss2.cs
using TipoOnda = OndaBoss2.TipoOnda;

/// <summary>
/// Boss Fase 3 - Comportamento conforme especificação:
/// 1. POSICIONAMENTO: Boss estático no canto direito da arena (enorme, não se move)
/// 2. ONDAS COMUNS: Vêm exclusivamente da direita para esquerda, menores, desviáveis com pulo
/// 3. ONDAS ROXAS: Maiores, NÃO desviáveis com pulo - OBRIGATÓRIO usar dash para atravessar
/// 4. PRESSÃO CONSTANTE: Boneca (player) empurrada constantemente para a esquerda
///    - Quando boss atordoado (stun), empurrão para
///    - Quando boss volta a atacar, empurrão volta
/// </summary>
public class BossFase3 : MonoBehaviour, IDamageable
{
    // =====================================================
    // ENUMS
    // =====================================================

    public enum EstadoBoss
    {
        Intro,          // Entrada na arena
        Atacando,       // Invocando ondas (comum ou roxa)
        Atordoado,      // Stun - vulnerável, pressão para
        Morto
    }

    // =====================================================
    // REFERÊNCIAS E CONFIGURAÇÃO - INSPECTOR
    // =====================================================

    [Header("REFERÊNCIAS")]
    [Tooltip("Animator do boss")]
    [SerializeField] private Animator animator;

    [Tooltip("SpriteRenderer principal")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("AudioSource para sons")]
    [SerializeField] private AudioSource audioSource;

    [Header("PONTOS DA ARENA")]
    [Tooltip("Posição onde o boss fica parado (canto direito)")]
    [SerializeField] private Transform posicaoBoss;

    [Tooltip("Spawn das ondas (lado direito da arena)")]
    [SerializeField] private Transform spawnOndas;

    [Tooltip("Limite esquerdo da arena (onde as ondas morrem)")]
    [SerializeField] private Transform limiteEsquerdoArena;

    [Tooltip("Centro da arena (referência para stun)")]
    [SerializeField] private Transform centroArena;

    [Header("PREFABS DAS ONDAS")]
    [Tooltip("Prefab da onda comum (menor, desviável com pulo)")]
    [SerializeField] private GameObject ondaComumPrefab;

    [Tooltip("Prefab da onda roxa (maior, precisa de dash)")]
    [SerializeField] private GameObject ondaRoxaPrefab;

    [Header("CONFIGURAÇÃO - ONDAS COMUNS")]
    [SerializeField] private float velocidadeOndaComum = 6f;
    [SerializeField] private int danoOndaComum = 1;
    [SerializeField] private int quantidadeOndasComuns = 3;
    [SerializeField] private float intervaloOndasComuns = 0.6f;
    [SerializeField] private float alturaOndaComum = 1.5f; // Altura para poder pular por cima

    [Header("CONFIGURAÇÃO - ONDAS ROXAS")]
    [SerializeField] private float velocidadeOndaRoxa = 5f;
    [SerializeField] private int danoOndaRoxa = 2;
    [SerializeField] private int quantidadeOndasRoxas = 2;
    [SerializeField] private float intervaloOndasRoxas = 1f;
    [SerializeField] private float alturaOndaRoxa = 4f; // Maior - não dá pra pular

    [Header("CICLO DE ATAQUE")]
    [Tooltip("Quantas sequências de ondas comuns antes de uma roxa")]
    [SerializeField] private int ondasComunsAntesRoxa = 2;

    [Tooltip("Tempo entre sequências de ataque (respiro para o player)")]
    [SerializeField] private float tempoEntreSequencias = 3f;

    [Tooltip("Tempo de preparação antes de spawnar ondas (aviso visual do boss)")]
    [SerializeField] private float tempoPreparacaoOnda = 1.5f;

    [Header("PRESSÃO CONSTANTE (EMPURRÃO DIRETO NO RIGIDBODY)")]
    [Tooltip("Força contínua aplicada no player para empurrar para esquerda (ativa durante ataques)")]
    [SerializeField] private float forcaEmpurraoConstante = 2f;

    [Tooltip("Usar força (AddForce) ou velocidade direta")]
    [SerializeField] private bool usarForcaFisica = true;

    [Header("TIPOS DE PRESSÃO (ALTERNAM A CADA SEQUÊNCIA)")]
    [Tooltip("Ordem: 0 = Bloco Empurrão, 1 = Chão Dano. Alterna automaticamente a cada sequência.")]
    [SerializeField] private int indicePressaoInicial = 0;

    [Header("PRESSÃO: BLOCO EMPURRÃO FÍSICO")]
    [Tooltip("GameObject com BoxCollider (isTrigger = TRUE) no lado direito. Empurra player pra esquerda enquanto ele atravessa.")]
    [SerializeField] private GameObject blocoEmpurrao;

    [Tooltip("Força de impulso aplicada no player (padrão: 12f - forte mas escapável)")]
    [SerializeField] private float forcaBlocoEmpurrao = 12f;

    [Tooltip("Se true, ativa/desativa automaticamente com o estado do boss")]
    [SerializeField] private bool blocoAutomatico = true;

    [Header("PRESSÃO: CHÃO COM DANO (FASE 2 STYLE)")]
    [Tooltip("GameObject do chão que causa dano (Collider2D isTrigger = TRUE). Cobre área onde o player pisa.")]
    [SerializeField] private GameObject chaoDano;

    [Tooltip("Dano por segundo enquanto player toca o chão (padrão: 1 - baixo pra dar tempo de reagir)")]
    [SerializeField] private int danoChaoPorSegundo = 1;

    [Tooltip("Tempo de aviso ANTES do chão ativar (pogo pisca no centro)")]
    [SerializeField] private float tempoAvisoChao = 1.5f;

    [Tooltip("Tempo que o chão fica ATIVO causando dano (padrão: 4s - janela justa)")]
    [SerializeField] private float tempoChaoAtivo = 4f;

    [Tooltip("Tempo de PAUSA entre ciclos do chão (respiro)")]
    [SerializeField] private float tempoPausaChao = 2f;

    [Tooltip("Prefab do aviso visual (pogo) no centro. Se vazio, cria seta procedural amarela.")]
    [SerializeField] private GameObject prefabPogoAviso;

    [Tooltip("Cor do aviso do pogo")]
    [SerializeField] private Color corPogoAviso = new Color(1f, 0.9f, 0.2f, 1f); // Amarelo ouro

    [Tooltip("Tempo de fade in/out do pogo")]
    [SerializeField] private float tempoFadePogo = 0.4f;

    [Header("STUN / VULNERABILIDADE")]
    [Tooltip("Vida máxima do boss")]
    [SerializeField] private int vidaMaxima = 15;

    [Tooltip("Dano necessário para atordoar o boss")]
    [SerializeField] private int danoParaStun = 5;

    [Tooltip("Duração do atordoamento (stun)")]
    [SerializeField] private float duracaoStun = 4f;

    [Tooltip("Tempo de invulnerabilidade após levar dano (durante stun)")]
    [SerializeField] private float tempoInvulneravelStun = 0.5f;

    [Header("VISUAL STUN")]
    [SerializeField] private Color corNormal = Color.white;
    [SerializeField] private Color corStun = Color.cyan;
    [SerializeField] private Color corAtacando = Color.red;

    [Header("ÁUDIO")]
    [SerializeField] private AudioClip somOndaComum;
    [SerializeField] private AudioClip somOndaRoxa;
    [SerializeField] private AudioClip somPreparacao;
    [SerializeField] private AudioClip somStun;
    [SerializeField] private AudioClip somRecuperacaoStun;
    [SerializeField] private AudioClip somMorte;
    [SerializeField] private AudioClip somDano;

    [Header("DEBUG")]
    [SerializeField] private bool logsDebug = true;
    [SerializeField] private bool mostrarGizmos = true;

    // =====================================================
    // HASHES DE ANIMAÇÃO
    // =====================================================

    private static readonly int H_Atacando = Animator.StringToHash("Atacando");
    private static readonly int H_Stun = Animator.StringToHash("Stun");
    private static readonly int H_Recuperar = Animator.StringToHash("Recuperar");
    private static readonly int H_Morte = Animator.StringToHash("Morte");
    private static readonly int H_TipoOnda = Animator.StringToHash("TipoOnda"); // 0=comum, 1=roxa

    // =====================================================
    // ESTADO INTERNO
    // =====================================================

    private Rigidbody2D rb;
    private Collider2D col;
    private EstadoBoss estadoAtual = EstadoBoss.Intro;
    private int vidaAtual;
    private bool morto = false;

    // Controle de ondas
    private int contadorSequenciasComuns = 0;
    private bool proximaEondaRoxa = false;

    // Stun
    private int danoAcumuladoStun = 0;
    private bool estaAtordoado = false;
    private Coroutine rotinaStun;

    // Pressão constante
    private PlayerController playerController;
    private Transform playerTransform;
    private bool pressaoAtiva = true;

    // Perigos ativos (para limpeza)
    private readonly List<GameObject> ondasAtivas = new List<GameObject>();

    // Bloco de empurrão
    private Collider2D blocoCol;
    private bool blocoAtivo = false;
    private Rigidbody2D blocoRb; // Para garantir que o bloco não se mova

    // Chão com dano (Fase 2 style)
    private Collider2D chaoCol;
    private bool chaoAtivo = false;
    private GameObject pogoAvisoInstancia;
    private Coroutine rotinaChaoDano;

    // Sistema de pressão alternada
    private int tipoPressaoAtual = 0; // 0 = Bloco Empurrão, 1 = Chão Dano

    // Coroutines
    private Coroutine rotinaAtaques;
    private Coroutine rotinaPressao;

    // =====================================================
    // PROPRIEDADES PÚBLICAS
    // =====================================================

    public EstadoBoss Estado => estadoAtual;
    public int Vida => vidaAtual;
    public int VidaMax => vidaMaxima;
    public float VidaPct => (float)vidaAtual / vidaMaxima;
    public bool EstaAtordoado => estaAtordoado;
    public bool PressaoAtiva => pressaoAtiva;
    public bool BlocoEmpurraoAtivo => blocoAtivo;
    public bool ChaoDanoAtivo => chaoAtivo;
    public string PressaoAtualNome => tipoPressaoAtual == 0 ? "Bloco Empurrão" : "Chão Dano";

    // =====================================================
    // UNITY: AWAKE
    // =====================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (!rb || !col) { enabled = false; return; }

        // Physics - Boss é estático (kinematic)
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;

        // Componentes
        animator ??= GetComponentInChildren<Animator>();
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
        audioSource ??= GetComponent<AudioSource>();
        if (audioSource) { audioSource.playOnAwake = false; audioSource.loop = false; audioSource.spatialBlend = 0.5f; }

        // Player
        BuscarPlayer();

        // Bloco de empurrão
        ConfigurarBlocoEmpurrao();

        // Chão com dano
        ConfigurarChaoDano();

        // Inicializa tipo de pressão (0 = Bloco, 1 = Chão)
        tipoPressaoAtual = Mathf.Clamp(indicePressaoInicial, 0, 1);

        // Validações
        ValidarReferencias();

        vidaAtual = vidaMaxima;

        if (logsDebug) Debug.Log($"[BossFase3] Awake OK | Boss em: {posicaoBoss?.position} | Spawn: {spawnOndas?.position} | Vida: {vidaAtual}/{vidaMaxima}");
    }

    // =====================================================
    // UNITY: START
    // =====================================================

    private void Start()
    {
        // Posiciona o boss no canto direito
        if (posicaoBoss != null)
        {
            transform.position = posicaoBoss.position;
            rb.position = posicaoBoss.position;
        }

        // Inicia máquina de estados
        StartCoroutine(RotinaIntro());

        // Inicia pressão constante
        rotinaPressao = StartCoroutine(RotinaPressaoConstante());
    }

    // =====================================================
    // UNITY: FIXED UPDATE - Trava posição do boss
    // =====================================================

    private void FixedUpdate()
    {
        if (morto) return;

        // Boss é ESTÁTICO - trava na posicaoBoss
        if (posicaoBoss != null)
        {
            Vector3 targetPos = posicaoBoss.position;
            if (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = targetPos;
                rb.position = targetPos;
                rb.linearVelocity = Vector2.zero;
            }
        }

        // Verifica se player está dentro do trigger do bloco de empurrão
        if (blocoAtivo && blocoCol != null && playerController != null)
        {
            VerificarBlocoPlayerOverlap();
        }

        // Verifica se player está no chão com dano
        if (chaoAtivo && chaoCol != null && playerController != null)
        {
            VerificarChaoDanoPlayer();
        }
    }

    private void VerificarBlocoPlayerOverlap()
    {
        // OverlapCollider detecta se o player está dentro do trigger do bloco
        var results = new Collider2D[5];
        int count = blocoCol.Overlap(new ContactFilter2D().NoFilter(), results);

        for (int i = 0; i < count; i++)
        {
            if (results[i] != null && results[i].CompareTag("Player"))
            {
                EmpurrarPlayer(results[i].gameObject);
                break;
            }
        }
    }

    // =====================================================
    // UNITY: UPDATE
    // =====================================================

    private void Update()
    {
        if (morto) return;

        BuscarPlayer(); // Garante referência
        MaquinaEstadosUpdate();

#if UNITY_EDITOR
        // Debug keys
        if (Input.GetKeyDown(KeyCode.F1)) ReceberDano(1, gameObject);
        if (Input.GetKeyDown(KeyCode.F2)) ReceberDano(danoParaStun, gameObject);
        if (Input.GetKeyDown(KeyCode.F3)) ForcarStun();
        if (Input.GetKeyDown(KeyCode.F4)) { pressaoAtiva = !pressaoAtiva; Debug.Log($"[BossFase3] Pressão: {(pressaoAtiva ? "ATIVA" : "PARADA")}"); }
#endif
    }

    // =====================================================
    // BUSCAR PLAYER
    // =====================================================

    private void BuscarPlayer()
    {
        if (playerTransform != null) return;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
            playerController = p.GetComponent<PlayerController>();
            if (logsDebug) Debug.Log($"[BossFase3] Player encontrado: {p.name}");
        }
    }

    // =====================================================
    // VALIDAÇÃO
    // =====================================================

    private void ValidarReferencias()
    {
        bool valido = true;

        if (posicaoBoss == null) { Debug.LogError("[BossFase3] PosicaoBoss não configurado!", this); valido = false; }
        if (spawnOndas == null) { Debug.LogError("[BossFase3] SpawnOndas não configurado!", this); valido = false; }
        if (limiteEsquerdoArena == null) { Debug.LogError("[BossFase3] LimiteEsquerdoArena não configurado!", this); valido = false; }
        if (ondaComumPrefab == null) { Debug.LogWarning("[BossFase3] OndaComumPrefab não configurado!", this); }
        if (ondaRoxaPrefab == null) { Debug.LogWarning("[BossFase3] OndaRoxaPrefab não configurado!", this); }
        if (velocidadeOndaComum <= 0f || velocidadeOndaRoxa <= 0f) { Debug.LogError("[BossFase3] Velocidades das ondas devem ser > 0!", this); valido = false; }
        if (forcaEmpurraoConstante <= 0f) { Debug.LogWarning("[BossFase3] ForcaEmpurraoConstanta <= 0, pressão desativada.", this); pressaoAtiva = false; }

        if (!valido) enabled = false;
    }

    // =====================================================
    // MÁQUINA DE ESTADOS
    // =====================================================

    private void MudarEstado(EstadoBoss novo)
    {
        if (estadoAtual == novo) return;
        if (logsDebug) Debug.Log($"[BossFase3] ESTADO: {estadoAtual} → {novo}");
        estadoAtual = novo;
        OnEstadoEntrada(novo);
    }

    private void OnEstadoEntrada(EstadoBoss e)
    {
        switch (e)
        {
            case EstadoBoss.Intro:
                if (animator) animator.SetTrigger("Intro");
                pressaoAtiva = false; // Sem pressão durante intro
                DefinirBlocoEmpurraoAtivo(false);
                DefinirChaoDanoAtivo(false);
                break;

            case EstadoBoss.Atacando:
                pressaoAtiva = true; // Pressão ATIVA durante ataques
                // Ativa SÓ o tipo de pressão atual (alternado)
                AtivarPressaoAtual();
                IniciarSequenciaAtaque();
                break;

            case EstadoBoss.Atordoado:
                pressaoAtiva = false; // Pressão PARADA durante stun
                DefinirBlocoEmpurraoAtivo(false);
                DefinirChaoDanoAtivo(false); // Para chão com dano
                estaAtordoado = true;
                if (animator) { animator.SetTrigger(H_Stun); animator.SetBool(H_Atacando, false); }
                if (somStun) Play(somStun);
                if (spriteRenderer) spriteRenderer.color = corStun;
                break;

            case EstadoBoss.Morto:
                pressaoAtiva = false;
                DefinirBlocoEmpurraoAtivo(false);
                DefinirChaoDanoAtivo(false);
                StartCoroutine(RotinaMorte());
                break;
        }
    }

    private void MaquinaEstadosUpdate()
    {
        switch (estadoAtual)
        {
            case EstadoBoss.Intro:
                // Espera corrotina de intro terminar
                break;

            case EstadoBoss.Atacando:
                // Espera corrotina de ataque terminar (volta para Atacando ou vai para Stun)
                break;

            case EstadoBoss.Atordoado:
                // Espera corrotina de stun terminar
                break;
        }
    }

    // =====================================================
    // INTRO
    // =====================================================

    private IEnumerator RotinaIntro()
    {
        MudarEstado(EstadoBoss.Intro);

        if (somPreparacao) Play(somPreparacao);

        yield return new WaitForSeconds(2f);

        if (!morto)
        {
            MudarEstado(EstadoBoss.Atacando);
        }
    }

    // =====================================================
    // CICLO DE ATAQUES
    // =====================================================

    private void IniciarSequenciaAtaque()
    {
        if (rotinaAtaques != null) StopCoroutine(rotinaAtaques);
        rotinaAtaques = StartCoroutine(RotinaSequenciaAtaque());
    }

    private IEnumerator RotinaSequenciaAtaque()
    {
        while (!morto && estadoAtual == EstadoBoss.Atacando)
        {
            // Decide tipo de onda
            TipoOnda tipo = proximaEondaRoxa ? TipoOnda.Roxa : TipoOnda.Comum;

            if (logsDebug) Debug.Log($"[BossFase3] Iniciando sequência: {tipo} | Pressão: {(tipoPressaoAtual == 0 ? "BLOCO EMPURRÃO" : "CHÃO DANO")}");

            // Prepara animação
            if (animator)
            {
                animator.SetInteger(H_TipoOnda, tipo == TipoOnda.Comum ? 0 : 1);
                animator.SetBool(H_Atacando, true);
            }

            // Tempo de preparação (aviso visual do boss)
            if (somPreparacao) Play(somPreparacao);
            yield return new WaitForSeconds(tempoPreparacaoOnda);

            if (morto || estadoAtual != EstadoBoss.Atacando) yield break;

            // Ativa a pressão CORRETA para esta sequência
            AtivarPressaoAtual();

            // Spawna as ondas
            if (tipo == TipoOnda.Comum)
            {
                yield return StartCoroutine(SpawnarOndas(TipoOnda.Comum));
                contadorSequenciasComuns++;

                if (contadorSequenciasComuns >= ondasComunsAntesRoxa)
                {
                    proximaEondaRoxa = true;
                    contadorSequenciasComuns = 0;
                }
            }
            else
            {
                yield return StartCoroutine(SpawnarOndas(TipoOnda.Roxa));
                proximaEondaRoxa = false;
            }

            if (morto || estadoAtual != EstadoBoss.Atacando) yield break;

            // Finaliza animação de ataque
            if (animator) animator.SetBool(H_Atacando, false);

            // ALTERNA o tipo de pressão para a PRÓXIMA sequência
            AlternarPressao();

            // Aguarda antes da próxima sequência (respiro para o player)
            yield return new WaitForSeconds(tempoEntreSequencias);
        }

        rotinaAtaques = null;
    }

    private void AtivarPressaoAtual()
    {
        // FORÇA desativar AMBOS primeiro (garantia total)
        if (blocoEmpurrao != null) { blocoEmpurrao.SetActive(false); blocoAtivo = false; }
        if (chaoDano != null) { chaoDano.SetActive(false); chaoAtivo = false; }
        if (rotinaChaoDano != null) { StopCoroutine(rotinaChaoDano); rotinaChaoDano = null; }
        RemoverPogoAviso();

        // Ativa SÓ o atual
        if (tipoPressaoAtual == 0)
        {
            // Bloco Empurrão - ativa instantâneo
            if (blocoEmpurrao != null) { blocoEmpurrao.SetActive(true); blocoAtivo = true; }
            if (logsDebug) Debug.Log($"[BossFase3] 🔵 Pressão ATIVA: BLOCO EMPURRÃO (força {forcaBlocoEmpurrao})");
        }
        else
        {
            // Chão Dano - inicia corrotina (pogo aparece instantâneo, chão ativa junto)
            if (rotinaChaoDano != null) StopCoroutine(rotinaChaoDano);
            rotinaChaoDano = StartCoroutine(RotinaAtivarChaoDano());
            if (logsDebug) Debug.Log($"[BossFase3] 🔴 Pressão ATIVA: CHÃO DANO ({danoChaoPorSegundo} dano/s por {tempoChaoAtivo}s)");
        }
    }

    private void AlternarPressao()
    {
        tipoPressaoAtual = (tipoPressaoAtual + 1) % 2; // Alterna 0 ↔ 1
        if (logsDebug) Debug.Log($"[BossFase3] ⚡ Próxima pressão será: {(tipoPressaoAtual == 0 ? "BLOCO EMPURRÃO" : "CHÃO DANO")}");
    }

    // =====================================================
    // SPAWN DE ONDAS
    // =====================================================

    private IEnumerator SpawnarOndas(TipoOnda tipo)
    {
        GameObject prefab = tipo == TipoOnda.Comum ? ondaComumPrefab : ondaRoxaPrefab;
        float velocidade = tipo == TipoOnda.Comum ? velocidadeOndaComum : velocidadeOndaRoxa;
        int dano = tipo == TipoOnda.Comum ? danoOndaComum : danoOndaRoxa;
        int quantidade = tipo == TipoOnda.Comum ? quantidadeOndasComuns : quantidadeOndasRoxas;
        float intervalo = tipo == TipoOnda.Comum ? intervaloOndasComuns : intervaloOndasRoxas;
        float altura = tipo == TipoOnda.Comum ? alturaOndaComum : alturaOndaRoxa;
        AudioClip som = tipo == TipoOnda.Comum ? somOndaComum : somOndaRoxa;

        if (prefab == null || spawnOndas == null || limiteEsquerdoArena == null)
        {
            if (logsDebug) Debug.LogWarning($"[BossFase3] Prefab ou pontos ausentes para {tipo}");
            yield break;
        }

        // Calcula posições Y para distribuir as ondas verticalmente
        float yBase = spawnOndas.position.y;
        float yMin = yBase - altura / 2f;
        float yMax = yBase + altura / 2f;

        for (int i = 0; i < quantidade; i++)
        {
            if (morto || estadoAtual != EstadoBoss.Atacando) yield break;

            // Posição Y variada
            float yPos = quantidade > 1
                ? Mathf.Lerp(yMin, yMax, (float)i / (quantidade - 1))
                : yBase;

            Vector3 posSpawn = new Vector3(spawnOndas.position.x, yPos, spawnOndas.position.z);

            // Instancia a onda
            GameObject onda = Instantiate(prefab, posSpawn, Quaternion.identity);
            ondasAtivas.Add(onda);

            // Configura a onda
            ConfigurarOnda(onda, tipo, velocidade, dano);

            // Som
            if (som && audioSource) audioSource.PlayOneShot(som);

            if (logsDebug) Debug.Log($"[BossFase3] Onda {tipo} spawnada #{i+1} em Y={yPos:F1}");

            // Intervalo entre ondas da mesma sequência
            if (i < quantidade - 1)
            {
                yield return new WaitForSeconds(intervalo);
            }
        }

        // Aguarda todas as ondas atravessarem a arena antes da próxima sequência
        yield return StartCoroutine(AguardarOndasTerminarem());
    }

    private void ConfigurarOnda(GameObject onda, TipoOnda tipo, float velocidade, int dano)
    {
        // Tenta pegar script OndaBoss2 ou similar
        var ondaScript = onda.GetComponent<OndaBoss2>();
        if (ondaScript != null)
        {
            // Passa o tipo corretamente (4 parametros)
            // IMPORTANTE: Força destino no mesmo Y do spawn para movimento HORIZONTAL puro
            Vector3 destinoHorizontal = new Vector3(
                limiteEsquerdoArena.position.x,
                onda.transform.position.y, // Mesmo Y da onda = movimento horizontal
                limiteEsquerdoArena.position.z
            );
            ondaScript.Inicializar(destinoHorizontal, velocidade, dano, tipo);
        }
        else
        {
            // Fallback: usa Rigidbody2D direto
            var rbOnda = onda.GetComponent<Rigidbody2D>();
            if (rbOnda != null)
            {
                // Direção HORIZONTAL apenas (mesmo Y)
                Vector2 dir = Vector2.left; // Sempre da direita para esquerda
                rbOnda.gravityScale = 0f;
                rbOnda.freezeRotation = true;
                rbOnda.linearVelocity = dir * velocidade;
            }

            // Adiciona dano via trigger
            var colisor = onda.GetComponent<Collider2D>();
            if (colisor != null) colisor.isTrigger = true;

            // Tag para identificar tipo
            onda.tag = tipo == TipoOnda.Roxa ? "OndaRoxa" : "OndaComum";
        }

        // Escala visual baseada no tipo (OndaBoss2 ja faz isso no Inicializar, mas mantemos como fallback)
        if (tipo == TipoOnda.Roxa)
        {
            onda.transform.localScale *= 1.5f; // Onda roxa é maior
            // Cor roxa
            var sr = onda.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.6f, 0.2f, 0.8f, 1f);
        }
        else
        {
            // Onda comum - cor normal/azulada
            var sr = onda.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        }
    }

    private IEnumerator AguardarOndasTerminarem()
    {
        float tempoMaximo = 10f; // Timeout de segurança
        float tempoDecorrido = 0f;

        while (tempoDecorrido < tempoMaximo && !morto)
        {
            // Limpa referências nulas
            for (int i = ondasAtivas.Count - 1; i >= 0; i--)
            {
                if (ondasAtivas[i] == null)
                    ondasAtivas.RemoveAt(i);
            }

            // Se não há mais ondas, termina
            if (ondasAtivas.Count == 0)
                yield break;

            tempoDecorrido += Time.deltaTime;
            yield return null;
        }

        // Força limpeza se timeout
        foreach (var onda in ondasAtivas)
            if (onda != null) Destroy(onda);
        ondasAtivas.Clear();
    }

    // =====================================================
    // PRESSÃO CONSTANTE (EMPURRÃO PARA ESQUERDA)
    // =====================================================

    private IEnumerator RotinaPressaoConstante()
    {
        while (!morto)
        {
            if (pressaoAtiva && playerController != null && playerTransform != null)
            {
                AplicarEmpurraoEsquerda();
            }

            yield return new WaitForFixedUpdate(); // Aplica no FixedUpdate via corrotina
        }
    }

    private void AplicarEmpurraoEsquerda()
    {
        if (playerController == null) return;

        // Não empurra se player estiver no chão encostado na parede esquerda
        // (opcional: adicionar verificação de limite da arena)

        Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        if (usarForcaFisica)
        {
            // Aplica força contínua para a esquerda
            playerRb.AddForce(Vector2.left * forcaEmpurraoConstante, ForceMode2D.Force);
        }
        else
        {
            // Aplica velocidade direta (mantém Y)
            float novaVelX = Mathf.Min(playerRb.linearVelocity.x, -forcaEmpurraoConstante);
            playerRb.linearVelocity = new Vector2(novaVelX, playerRb.linearVelocity.y);
        }
    }

    // Método público para ativar/desativar pressão (chamado por eventos)
    public void DefinirPressaoAtiva(bool ativa)
    {
        pressaoAtiva = ativa;
        if (logsDebug) Debug.Log($"[BossFase3] Pressão constante: {(ativa ? "ATIVADA" : "DESATIVADA")}");
    }

    // =====================================================
    // BLOCO DE EMPURRÃO FÍSICO
    // =====================================================

    private void ConfigurarBlocoEmpurrao()
    {
        if (blocoEmpurrao == null)
        {
            if (logsDebug) Debug.LogWarning("[BossFase3] BlocoEmpurrao não configurado no Inspector.");
            return;
        }

        blocoCol = blocoEmpurrao.GetComponent<Collider2D>();
        if (blocoCol == null)
        {
            Debug.LogError("[BossFase3] BlocoEmpurrao precisa ter um Collider2D!", this);
            return;
        }

        // IMPORTANTE: isTrigger = TRUE para o player atravessar e receber força
        blocoCol.isTrigger = true;

        // Adiciona Rigidbody2D Kinematic no bloco para detectar triggers
        blocoRb = blocoEmpurrao.GetComponent<Rigidbody2D>();
        if (blocoRb == null)
        {
            blocoRb = blocoEmpurrao.AddComponent<Rigidbody2D>();
        }
        blocoRb.bodyType = RigidbodyType2D.Kinematic;
        blocoRb.gravityScale = 0f;
        blocoRb.freezeRotation = true;
        blocoRb.linearVelocity = Vector2.zero;

        // Inicia desativado
        blocoEmpurrao.SetActive(false);
        blocoAtivo = false;

        if (logsDebug) Debug.Log($"[BossFase3] Bloco de empurrão configurado: {blocoEmpurrao.name} | isTrigger={blocoCol.isTrigger} (player atravessa) | RB Kinematic={blocoRb.bodyType}");
    }

    private void DefinirBlocoEmpurraoAtivo(bool ativo)
    {
        if (blocoEmpurrao == null) return;

        if (!blocoAutomatico && ativo)
        {
            return;
        }

        if (blocoAtivo == ativo) return;

        blocoEmpurrao.SetActive(ativo);
        blocoAtivo = ativo;

        if (logsDebug) Debug.Log($"[BossFase3] Bloco de empurrão: {(ativo ? "ATIVADO" : "DESATIVADO")}");
    }

    // Método público para controle manual do bloco (se blocoAutomatico = false)
    public void DefinirBlocoEmpurraoManual(bool ativo)
    {
        DefinirBlocoEmpurraoAtivo(ativo);
    }

    private void EmpurrarPlayer(GameObject playerObj)
    {
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        // Aplica força forte para a esquerda
        Vector2 forca = Vector2.left * forcaBlocoEmpurrao;
        playerRb.AddForce(forca, ForceMode2D.Impulse);

        if (logsDebug) Debug.Log($"[BossFase3] Bloco empurrou player com força {forcaBlocoEmpurrao}");
    }

    // =====================================================
    // CHÃO COM DANO (ESTILO FASE 2)
    // =====================================================

    private void ConfigurarChaoDano()
    {
        if (chaoDano == null)
        {
            if (logsDebug) Debug.LogWarning("[BossFase3] ChaoDano não configurado no Inspector.");
            return;
        }

        chaoCol = chaoDano.GetComponent<Collider2D>();
        if (chaoCol == null)
        {
            Debug.LogError("[BossFase3] ChaoDano precisa ter um Collider2D!", this);
            return;
        }

        // IMPORTANTE: isTrigger = TRUE para detectar player em cima sem bloquear
        chaoCol.isTrigger = true;

        // Adiciona Rigidbody2D Kinematic para detectar triggers
        var chaoRb = chaoDano.GetComponent<Rigidbody2D>();
        if (chaoRb == null)
        {
            chaoRb = chaoDano.AddComponent<Rigidbody2D>();
        }
        chaoRb.bodyType = RigidbodyType2D.Kinematic;
        chaoRb.gravityScale = 0f;
        chaoRb.freezeRotation = true;
        chaoRb.linearVelocity = Vector2.zero;

        // Inicia desativado
        chaoDano.SetActive(false);
        chaoAtivo = false;

        if (logsDebug) Debug.Log($"[BossFase3] Chão com dano configurado: {chaoDano.name} | isTrigger={chaoCol.isTrigger}");
    }

    private void DefinirChaoDanoAtivo(bool ativo)
    {
        if (chaoDano == null) return;

        if (chaoAtivo == ativo) return;

        if (ativo)
        {
            if (rotinaChaoDano != null) StopCoroutine(rotinaChaoDano);
            rotinaChaoDano = StartCoroutine(RotinaAtivarChaoDano());
        }
        else
        {
            if (rotinaChaoDano != null) StopCoroutine(rotinaChaoDano);
            rotinaChaoDano = null;
            chaoDano.SetActive(false);
            chaoAtivo = false;
            RemoverPogoAviso();
        }

        if (logsDebug) Debug.Log($"[BossFase3] Chão com dano: {(ativo ? "ATIVANDO" : "DESATIVADO")}");
    }

    private IEnumerator RotinaAtivarChaoDano()
    {
        // 1. Pogo aparece IMEDIATAMENTE (sem delay)
        if (prefabPogoAviso != null && centroArena != null)
        {
            pogoAvisoInstancia = Instantiate(prefabPogoAviso, centroArena.position, Quaternion.identity);
            var sr = pogoAvisoInstancia.GetComponent<SpriteRenderer>();
            if (sr == null) sr = pogoAvisoInstancia.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = CriarSpritePogo();
            sr.color = corPogoAviso;
            sr.sortingOrder = 20;
            pogoAvisoInstancia.transform.localScale = Vector3.one * 2f;
        }

        if (morto) { RemoverPogoAviso(); yield break; }

        // 2. Ativa o chão com dano JÁ (pogo já tá visível)
        chaoDano.SetActive(true);
        chaoAtivo = true;

        if (logsDebug) Debug.Log($"[BossFase3] Chão com dano ATIVO por {tempoChaoAtivo}s (pogo visível)");

        // 3. Mantém ativo pelo tempo configurado - pogo fica visível o tempo TODO
        yield return new WaitForSeconds(tempoChaoAtivo);

        if (morto) yield break;

        // 4. Desativa o chão E remove pogo junto
        chaoDano.SetActive(false);
        chaoAtivo = false;
        RemoverPogoAviso();

        if (logsDebug) Debug.Log($"[BossFase3] Chão com dano DESATIVADO (pogo removido)");

        // 5. NÃO reativa automaticamente - alternância controla
    }

    private void RemoverPogoAviso()
    {
        if (pogoAvisoInstancia != null)
        {
            Destroy(pogoAvisoInstancia);
            pogoAvisoInstancia = null;
        }
    }

    private Sprite CriarSpritePogo()
    {
        int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dx = x - s / 2f;
                float dy = y - s / 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                // Triângulo apontando para cima (seta)
                if (dy > 0 && Mathf.Abs(dx) < (s / 2f - dy) * 0.8f)
                    px[y * s + x] = Color.white;
                else
                    px[y * s + x] = Color.clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    private void VerificarChaoDanoPlayer()
    {
        // OverlapCollider detecta se o player está tocando o chão
        var results = new Collider2D[5];
        int count = chaoCol.Overlap(new ContactFilter2D().NoFilter(), results);

        for (int i = 0; i < count; i++)
        {
            if (results[i] != null && results[i].CompareTag("Player"))
            {
                AplicarDanoChao(results[i].gameObject);
                break;
            }
        }
    }

    private void AplicarDanoChao(GameObject playerObj)
    {
        PlayerController pc = playerObj.GetComponent<PlayerController>();
        if (pc == null) return;

        // Aplica dano por tempo (já que é chamado no FixedUpdate, divide por fixedDeltaTime aproximado)
        // Melhor: usa corrotina separada ou Time.fixedDeltaTime
        pc.TakeDamage(danoChaoPorSegundo, gameObject);

        if (logsDebug) Debug.Log($"[BossFase3] Chão causou {danoChaoPorSegundo} de dano no player");
    }

    // =====================================================
    // DANO
    // =====================================================

    public void TakeDamage(int dano, GameObject fonte) => ReceberDano(dano, fonte);

    public void ReceberDano(int quantidade, GameObject origem)
    {
        if (morto || quantidade <= 0) return;

        // LEVA DANO NORMAL SEMPRE
        // Se estiver atordoado, leva dano extra (crítico)
        bool foiCritico = estaAtordoado;
        int danoFinal = quantidade;

        if (foiCritico)
        {
            // Durante stun, tem invulnerabilidade temporária entre hits
            if (Time.time < tempoUltimoDano + tempoInvulneravelStun) return;
            danoFinal = quantidade * 2; // Dano dobrado no stun
            tempoUltimoDano = Time.time;
        }

        vidaAtual -= danoFinal;

        if (logsDebug) Debug.Log($"[BossFase3] DANO RECEBIDO: {danoFinal} (critico: {foiCritico}) | HP: {vidaAtual}/{vidaMaxima}");

        // Feedback visual
        if (spriteRenderer) StartCoroutine(FlashDano(foiCritico));

        if (somDano) Play(somDano);

        if (vidaAtual <= 0)
        {
            morto = true;
            MudarEstado(EstadoBoss.Morto);
        }
    }

    private float tempoUltimoDano = -999f;

    private IEnumerator FlashDano(bool critico = false)
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        Color flashColor = critico ? Color.cyan : Color.white;
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.05f);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Chamado externamente (ex: player acerta weak point, ou evento de fase)
    // Aplica dano normal + acumula stun
    public void AplicarDanoStun(int quantidade)
    {
        if (morto) return;

        // Aplica dano normal
        ReceberDano(quantidade, gameObject);

        // Acumula stun
        if (!estaAtordoado)
        {
            danoAcumuladoStun += quantidade;

            if (logsDebug) Debug.Log($"[BossFase3] Dano stun acumulado: {danoAcumuladoStun}/{danoParaStun}");

            if (danoAcumuladoStun >= danoParaStun)
            {
                Atordoar();
            }
        }
    }

    private void Atordoar()
    {
        if (morto || estaAtordoado) return;

        danoAcumuladoStun = 0;
        MudarEstado(EstadoBoss.Atordoado);

        // Para rotina de ataques
        if (rotinaAtaques != null)
        {
            StopCoroutine(rotinaAtaques);
            rotinaAtaques = null;
        }

        // Limpa ondas ativas
        foreach (var onda in ondasAtivas)
            if (onda != null) Destroy(onda);
        ondasAtivas.Clear();

        // Inicia contagem de stun
        if (rotinaStun != null) StopCoroutine(rotinaStun);
        rotinaStun = StartCoroutine(RotinaStun());
    }

    private IEnumerator RotinaStun()
    {
        yield return new WaitForSeconds(duracaoStun);

        if (morto) yield break;

        // Recupera do stun
        RecuperarStun();
    }

    private void RecuperarStun()
    {
        estaAtordoado = false;

        if (animator) { animator.SetTrigger(H_Recuperar); animator.SetBool(H_Atacando, false); }
        if (somRecuperacaoStun) Play(somRecuperacaoStun);
        if (spriteRenderer) spriteRenderer.color = corNormal;

        // Volta a atacar
        MudarEstado(EstadoBoss.Atacando);
    }

    // Debug: força stun
    private void ForcarStun()
    {
        if (!morto && !estaAtordoado) Atordoar();
    }

    // =====================================================
    // MORTE
    // =====================================================

    private IEnumerator RotinaMorte()
    {
        if (logsDebug) Debug.Log("[BossFase3] Boss MORTO!");

        // Para tudo
        if (rotinaAtaques != null) StopCoroutine(rotinaAtaques);
        if (rotinaStun != null) StopCoroutine(rotinaStun);
        if (rotinaPressao != null) StopCoroutine(rotinaPressao);

        // Limpa ondas
        foreach (var onda in ondasAtivas)
            if (onda != null) Destroy(onda);
        ondasAtivas.Clear();

        // Animação
        if (animator) animator.SetTrigger(H_Morte);
        if (somMorte) Play(somMorte);

        // Desativa colisão
        if (col) col.enabled = false;

        yield return new WaitForSeconds(2f);

        Destroy(gameObject);
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos) return;

        // Posição do boss
        if (posicaoBoss != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(posicaoBoss.position, 1.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(posicaoBoss.position + Vector3.up * 2f, "BOSS FASE 3 (ESTÁTICO)");
#endif
        }

        // Spawn ondas
        if (spawnOndas != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnOndas.position, 0.5f);
            Gizmos.DrawLine(spawnOndas.position, new Vector3(spawnOndas.position.x, spawnOndas.position.y + alturaOndaComum / 2f, spawnOndas.position.z));
            Gizmos.DrawLine(spawnOndas.position, new Vector3(spawnOndas.position.x, spawnOndas.position.y - alturaOndaComum / 2f, spawnOndas.position.z));
#if UNITY_EDITOR
            UnityEditor.Handles.Label(spawnOndas.position + Vector3.up * 1f, "Spawn Ondas Comuns");
#endif
        }

        // Limite esquerdo
        if (limiteEsquerdoArena != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(limiteEsquerdoArena.position, 0.5f);
            Gizmos.DrawLine(limiteEsquerdoArena.position + Vector3.up * 5f, limiteEsquerdoArena.position + Vector3.down * 5f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(limiteEsquerdoArena.position + Vector3.up * 6f, "LIMITE ESQUERDO (ondas morrem)");
#endif
        }

        // Centro arena
        if (centroArena != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centroArena.position, 1f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(centroArena.position + Vector3.up * 1.5f, "Centro Arena");
#endif
        }

        // Seta indicando direção das ondas (direita -> esquerda)
        if (spawnOndas != null && limiteEsquerdoArena != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Vector3 meio = Vector3.Lerp(spawnOndas.position, limiteEsquerdoArena.position, 0.5f);
            Gizmos.DrawRay(meio + Vector3.up * 3f, Vector3.left * 3f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(meio + Vector3.up * 3.5f, "ONDAS: DIREITA → ESQUERDA");
#endif
        }

        // Bloco de empurrão
        if (blocoEmpurrao != null && blocoCol != null)
        {
            Gizmos.color = blocoAtivo ? Color.magenta : new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawWireCube(blocoEmpurrao.transform.position, blocoCol.bounds.size);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(blocoEmpurrao.transform.position + Vector3.up * 1f, $"BLOCO EMPURRÃO ({(blocoAtivo ? "ATIVO" : "INATIVO")}) | Força: {forcaBlocoEmpurrao}");
#endif
        }

        // Chão com dano
        if (chaoDano != null && chaoCol != null)
        {
            Gizmos.color = chaoAtivo ? Color.red : new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireCube(chaoDano.transform.position, chaoCol.bounds.size);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(chaoDano.transform.position + Vector3.up * 1f, $"CHÃO DANO ({(chaoAtivo ? "ATIVO" : "INATIVO")}) | Dano/s: {danoChaoPorSegundo}");
#endif
        }

        // Pogo aviso
        if (centroArena != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centroArena.position, 0.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(centroArena.position + Vector3.up * 1.5f, "POGO AVISO CENTRO");
#endif
        }

        // Debug estado
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                $"Estado: {estadoAtual}\n" +
                $"Vida: {vidaAtual}/{vidaMaxima}\n" +
                $"Atordoado: {estaAtordoado}\n" +
                $"Dano Stun: {danoAcumuladoStun}/{danoParaStun}\n" +
                $"Próxima Roxa: {proximaEondaRoxa}\n" +
                $"Sequências Comuns: {contadorSequenciasComuns}/{ondasComunsAntesRoxa}\n" +
                $"Pressão: {(pressaoAtiva ? "ATIVA" : "PARADA")}\n" +
                $"Tipo Atual: {(tipoPressaoAtual == 0 ? "🔵 BLOCO EMPURRÃO" : "🔴 CHÃO DANO")}\n" +
                $"Próximo: {(tipoPressaoAtual == 0 ? "🔴 CHÃO DANO" : "🔵 BLOCO EMPURRÃO")}\n" +
                $"Bloco: {(blocoAtivo ? "ATIVO" : "INATIVO")} (Força: {forcaBlocoEmpurrao})\n" +
                $"Chão: {(chaoAtivo ? "ATIVO" : "INATIVO")} (Dano/s: {danoChaoPorSegundo}, {tempoChaoAtivo}s)\n" +
                $"Ondas Ativas: {ondasAtivas.Count}"
            );
        }
#endif
    }
}