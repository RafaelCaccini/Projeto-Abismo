using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss Fase 2 - Comportamento:
/// 1. VOAR - Patrulha entre waypoints aéreos (INVULNERÁVEL)
/// 2. ATACAR - Alterna entre Fogo no Chão e Projéteis enquanto voa
/// 3. CAIR - Desce no centro da arena (FICA VULNERÁVEL)
/// 4. CHÃO - Player ataca o boss aqui
/// 5. SUBIR - Volta a voar e repete
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossFase2 : MonoBehaviour, IDamageable
{
    // =====================================
    // ENUMS
    // =====================================

    public enum EstadoBoss
    {
        Intro,          // Entrada na arena
        Voando,         // Patrulha aérea - INVULNERÁVEL
        Atacando,       // Executando ataque (fogo/projétil) - INVULNERÁVEL
        Caindo,         // Descendo para o centro
        NoChao,         // VULNERÁVEL - player bate
        Subindo,        // Levantando para voar novamente
        Morto
    }

    public enum TipoAtaque
    {
        FogoChao,
        Projetil
    }

    // =====================================
    // CONFIGURAÇÕES - INSPECTOR (PORTUGUÊS)
    // =====================================

    [Header("REFERÊNCIAS")]
    [Tooltip("Animator do boss")]
    [SerializeField] private Animator animator;

    [Tooltip("SpriteRenderer principal")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("AudioSource para sons")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Transform do jogador (vazio = busca por tag 'Player')")]
    [SerializeField] private Transform jogador;

    [Header("PONTOS DA ARENA (ARRASTE NO INSPECTOR)")]
    [Tooltip("4 cantos da arena - origem dos projéteis")]
    [SerializeField] private Transform[] cantosArena;

    [Tooltip("Waypoints de voo - boss patrulha entre estes no ar")]
    [SerializeField] private Transform[] waypointsVoo;

    [Tooltip("Centro da arena - onde o boss cai e fica vulnerável")]
    [SerializeField] private Transform centroArena;

    [Header("VOO (PATRULHA AÉREA)")]
    [Tooltip("Velocidade de voo")]
    [SerializeField] private float velocidadeVoo = 4f;

    [Tooltip("Aceleração/desaceleração")]
    [SerializeField] private float aceleracaoVoo = 3f;

    [Tooltip("Amplitude da oscilação vertical (sensação de voo)")]
    [SerializeField] private float amplitudeHover = 0.4f;

    [Tooltip("Frequência da oscilação")]
    [SerializeField] private float frequenciaHover = 1.2f;

    [Tooltip("Suavização ao virar")]
    [SerializeField] private float suavidadeVirada = 0.2f;

    [Header("ATAQUE: FOGO NO CHÃO")]
    [Tooltip("Prefab do efeito de fogo (vazio = procedural)")]
    [SerializeField] private GameObject prefabFogo;

    [Tooltip("Modo: false = pontos manuais, true = distribuição inteligente entre 2 pontos")]
    [SerializeField] private bool fogoInteligente = true;

    [Tooltip("Ponto ESQUERDO do fogo (obrigatório se fogoInteligente=true)")]
    [SerializeField] private Transform pontoFogoEsquerdo;

    [Tooltip("Ponto DIREITO do fogo (obrigatório se fogoInteligente=true)")]
    [SerializeField] private Transform pontoFogoDireito;

    [Tooltip("Quantos fogos criar entre os 2 pontos (modo inteligente)")]
    [SerializeField] private int quantidadeFogo = 5;

    [Tooltip("Espaçamento mínimo entre fogos (modo inteligente)")]
    [SerializeField] private float espacamentoMinimo = 1.5f;

    [Tooltip("Variação aleatória na posição X (modo inteligente)")]
    [SerializeField] private float variacaoX = 0.3f;

    [Tooltip("Pontos manuais de fogo (usado se fogoInteligente=false)")]
    [SerializeField] private Transform[] pontosFogoChao;

    [Tooltip("Tempo de aviso antes do fogo aparecer")]
    [SerializeField] private float tempoAvisoFogo = 1.2f;

    [Tooltip("Tempo que o fogo fica ativo")]
    [SerializeField] private float tempoFogoAtivo = 2.5f;

    [Tooltip("Tempo para o fogo desaparecer")]
    [SerializeField] private float tempoFadeFogo = 0.4f;

    [Tooltip("Dano do fogo")]
    [SerializeField] private int danoFogo = 2;

    [Tooltip("Layer do jogador")]
    [SerializeField] private LayerMask layerJogador;

    [Tooltip("Prefab de aviso visual do fogo (opcional)")]
    [SerializeField] private GameObject prefabAvisoFogo;

    [Tooltip("Cooldown entre ataques de fogo")]
    [SerializeField] private float cooldownFogo = 7f;

    [Header("ATAQUE: PROJÉTIL")]
    [Tooltip("Prefab do projétil (vazio = procedural)")]
    [SerializeField] private GameObject prefabProjetil;

    [Tooltip("Velocidade do projétil")]
    [SerializeField] private float velocidadeProjetil = 12f;

    [Tooltip("Tempo de aviso no canto antes de atirar")]
    [SerializeField] private float tempoAvisoProjetil = 0.8f;

    [Tooltip("Quantos projéteis por ataque")]
    [SerializeField] private int projetisPorAtaque = 2;

    [Tooltip("Intervalo entre projéteis múltiplos")]
    [SerializeField] private float intervaloProjetil = 0.4f;

    [Tooltip("Dano do projétil")]
    [SerializeField] private int danoProjetil = 1;

    [Tooltip("Tempo de vida do projétil")]
    [SerializeField] private float tempoVidaProjetil = 4f;

    [Tooltip("Prefab de aviso no canto (opcional)")]
    [SerializeField] private GameObject prefabAvisoProjetil;

    [Tooltip("Cooldown entre ataques de projétil")]
    [SerializeField] private float cooldownProjetil = 5f;

    [Header("CICLO: VOAR → CAIR → CHÃO → SUBIR")]
    [Tooltip("Quantos ataques aéreos antes de cair no chão")]
    [SerializeField] private int ataquesAntesDeCair = 3;

    [Tooltip("Tempo máximo voando antes de cair (segundos)")]
    [SerializeField] private float tempoMaxVoando = 20f;

    [Tooltip("Velocidade de queda")]
    [SerializeField] private float velocidadeQueda = 8f;

    [Tooltip("Tempo que fica no chão vulnerável")]
    [SerializeField] private float tempoNoChao = 5f;

    [Tooltip("Velocidade de subida")]
    [SerializeField] private float velocidadeSubida = 6f;

    [Header("VIDA E FASES")]
    [Tooltip("Vida máxima")]
    [SerializeField] private int vidaMaxima = 100;

    [Tooltip("Vida para entrar na Fase 2 (mais agressivo)")]
    [SerializeField] private int limiarFase2 = 60;

    [Tooltip("Vida para entrar na Fase 3 (muito agressivo)")]
    [SerializeField] private int limiarFase3 = 30;

    [Tooltip("Pode morrer?")]
    [SerializeField] private bool podeMorrer = true;

    [Tooltip("Tempo de invulnerabilidade ao levar dano")]
    [SerializeField] private float tempoInvulneravel = 0.4f;

    [Header("DANO DE CONTATO")]
    [Tooltip("Dano ao tocar o player")]
    [SerializeField] private int danoContato = 2;

    [Tooltip("Cooldown entre danos de contato")]
    [SerializeField] private float cooldownContato = 1f;

    [Header("ÁUDIO")]
    [SerializeField] private AudioClip somVooLoop;
    [SerializeField] private AudioClip somAvisoFogo;
    [SerializeField] private AudioClip somSpawnFogo;
    [SerializeField] private AudioClip somAvisoProjetil;
    [SerializeField] private AudioClip somTiroProjetil;
    [SerializeField] private AudioClip somQueda;
    [SerializeField] private AudioClip somImpactoChao;
    [SerializeField] private AudioClip somSubida;
    [SerializeField] private AudioClip somDano;
    [SerializeField] private AudioClip somMorte;
    [SerializeField] private AudioClip somMudancaFase;

    [Header("DEBUG")]
    [SerializeField] private bool logsDebug = true;
    [SerializeField] private bool desenharGizmos = true;
    [SerializeField] private int forcarFase = 0; // 0=normal, 1=fase2, 2=fase3

    // =====================================
    // HASHES DE ANIMAÇÃO
    // =====================================

    private static readonly int H_Voando = Animator.StringToHash("Voando");
    private static readonly int H_FogoChao = Animator.StringToHash("FogoChao");
    private static readonly int H_Projetil = Animator.StringToHash("Projetil");
    private static readonly int H_Caindo = Animator.StringToHash("Caindo");
    private static readonly int H_NoChao = Animator.StringToHash("NoChao");
    private static readonly int H_Subindo = Animator.StringToHash("Subindo");
    private static readonly int H_Hit = Animator.StringToHash("Hit");
    private static readonly int H_Morte = Animator.StringToHash("Morte");
    private static readonly int H_Fase = Animator.StringToHash("Fase");
    private static readonly int H_Velocidade = Animator.StringToHash("Velocidade");

    // =====================================
    // ESTADO INTERNO
    // =====================================

    private Rigidbody2D rb;
    private Collider2D col;
    private EstadoBoss estadoAtual = EstadoBoss.Intro;
    private int vidaAtual;
    private int faseAtual = 1;
    private bool morto = false;
    private bool invulneravel = true; // Começa invulnerável
    private float tempoUltimoDano;
    private float tempoUltimoContato;

    // Timers de ataque
    private float cdFogo;
    private float cdProjetil;
    private float timerDecisao;
    private float timerEstado;

    // Controle de voo
    private int waypointAtual = -1;
    private Vector2 posicaoAlvo;
    private float tempoHover;
    private float velVirada;

    // Ciclo voar→cair
    private int contadorAtaquesAereos = 0;
    private float tempoVoando = 0f;

    // Ataques
    private TipoAtaque proximoAtaque;
    private List<GameObject> avisosAtivos = new();
    private List<GameObject> fogosAtivos = new();

    // Player
    private PlayerController jogadorCtrl;

    // POSIÇÃO INICIAL TRAVADA
    private Vector3 posicaoSpawn;

    // =====================================
    // PROPRIEDADES PÚBLICAS
    // =====================================

    public EstadoBoss Estado => estadoAtual;
    public int Vida => vidaAtual;
    public int VidaMax => vidaMaxima;
    public int Fase => faseAtual;
    public float VidaPct => (float)vidaAtual / vidaMaxima;
    public bool EstaVulneravel => estadoAtual == EstadoBoss.NoChao && !invulneravel;

    // =====================================
    // UNITY: AWAKE
    // =====================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (!rb || !col) { enabled = false; return; }

        // TRAVA POSIÇÃO INICIAL - NUNCA MEXER
        posicaoSpawn = transform.position;

        // Physics
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 5f;
        rb.linearDamping = 1.5f;

        // Componentes
        animator ??= GetComponentInChildren<Animator>();
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
        audioSource ??= GetComponent<AudioSource>();
        if (audioSource) { audioSource.playOnAwake = false; audioSource.loop = false; audioSource.spatialBlend = 0.5f; }

        // Jogador
        if (!jogador)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) jogador = p.transform;
        }
        if (jogador) jogadorCtrl = jogador.GetComponent<PlayerController>();

        // Valida arrays
        ValidarArrays();

        if (logsDebug) Debug.Log($"[BossFase2] Awake | Spawn: {posicaoSpawn} | Waypoints: {waypointsVoo?.Length ?? 0} | Cantos: {cantosArena?.Length ?? 0} | Fogo: {pontosFogoChao?.Length ?? 0} | Centro: {centroArena != null}");
    }

    // =====================================
    // UNITY: START
    // =====================================

    private void Start()
    {
        vidaAtual = vidaMaxima;
        faseAtual = CalcularFase(vidaAtual);

        // RESETA PARA POSIÇÃO EXATA DO EDITOR
        transform.position = posicaoSpawn;
        rb.position = posicaoSpawn;
        rb.linearVelocity = Vector2.zero;

        // Alvo inicial = posição atual (boss PARADO no spawn)
        posicaoAlvo = posicaoSpawn;

        // Inicia máquina de estados
        StartCoroutine(RotinaIntro());

        if (logsDebug) Debug.Log($"[BossFase2] Start OK | Boss em: {transform.position} | Fase: {faseAtual}");
    }

    // =====================================
    // UNITY: UPDATE
    // =====================================

    private void Update()
    {
        if (morto) return;

        AtualizarTimers();
        VerificarMudancaFase();
        if (!jogador) BuscarJogador();
        MaquinaEstadosUpdate();
        AtualizarAnimacoes();

#if UNITY_EDITOR
        // Debug keys
        if (Input.GetKeyDown(KeyCode.F1)) ReceberDano(10, gameObject);
        if (Input.GetKeyDown(KeyCode.F2)) { vidaAtual = limiarFase2 + 1; VerificarMudancaFase(); }
        if (Input.GetKeyDown(KeyCode.F3)) { vidaAtual = limiarFase3 + 1; VerificarMudancaFase(); }
        if (Input.GetKeyDown(KeyCode.F4)) { DefinirProximoAtaque(); ExecutarAtaque(); }
        if (Input.GetKeyDown(KeyCode.F5)) ForcarCair(); // Força cair no chão
#endif
    }

    // =====================================
    // UNITY: FIXEDUPDATE
    // =====================================

    private void FixedUpdate()
    {
        if (morto) return;

        switch (estadoAtual)
        {
            case EstadoBoss.Voando:
            case EstadoBoss.Atacando:
                HandleVoo();
                break;

            case EstadoBoss.Caindo:
                HandleQueda();
                break;

            case EstadoBoss.Subindo:
                HandleSubida();
                break;
        }
    }

    // =====================================
    // MÁQUINA DE ESTADOS PRINCIPAL
    // =====================================

    private void MudarEstado(EstadoBoss novo)
    {
        if (estadoAtual == novo) return;
        if (logsDebug) Debug.Log($"[BossFase2] ESTADO: {estadoAtual} → {novo}");
        estadoAtual = novo;
        timerEstado = 0f;
        OnEstadoEntrada(novo);
    }

    private void OnEstadoEntrada(EstadoBoss e)
    {
        switch (e)
        {
            case EstadoBoss.Voando:
                // Inicia patrulha aérea
                ProximoWaypoint();
                if (somVooLoop && audioSource) { audioSource.clip = somVooLoop; audioSource.loop = true; audioSource.Play(); }
                invulneravel = true;
                break;

            case EstadoBoss.Atacando:
                // Executa ataque escolhido
                invulneravel = true;
                if (proximoAtaque == TipoAtaque.FogoChao)
                    StartCoroutine(RotinaFogoChao());
                else
                    StartCoroutine(RotinaProjetil());
                break;

            case EstadoBoss.Caindo:
                // Para voo e começa queda
                if (audioSource) audioSource.Stop();
                rb.linearVelocity = Vector2.zero;
                if (somQueda) Play(somQueda);
                if (animator) animator.SetTrigger(H_Caindo);
                invulneravel = true;
                break;

            case EstadoBoss.NoChao:
                // CHEGOU NO CHÃO - VULNERÁVEL!
                rb.linearVelocity = Vector2.zero;
                if (somImpactoChao) Play(somImpactoChao);
                if (animator) animator.SetTrigger(H_NoChao);
                invulneravel = false; // AGORA PODE LEVAR DANO!
                if (logsDebug) Debug.Log($"[BossFase2] >>> NO CHÃO - VULNERÁVEL POR {tempoNoChao}s <<<");
                break;

            case EstadoBoss.Subindo:
                // Levanta para voar novamente
                if (somSubida) Play(somSubida);
                if (animator) animator.SetTrigger(H_Subindo);
                invulneravel = true;
                break;

            case EstadoBoss.Morto:
                StartCoroutine(RotinaMorte());
                break;
        }
    }

    private void MaquinaEstadosUpdate()
    {
        timerEstado += Time.deltaTime;

        switch (estadoAtual)
        {
            case EstadoBoss.Voando:
                // Conta tempo voando
                tempoVoando += Time.deltaTime;

                // Decide atacar
                if (timerDecisao <= 0f && PodeAtacar())
                {
                    DefinirProximoAtaque();
                    MudarEstado(EstadoBoss.Atacando);
                }
                break;

            case EstadoBoss.Atacando:
                // Espera corrotina de ataque terminar (volta para Voando via callback)
                break;

            case EstadoBoss.Caindo:
                // Espera chegar no chão (FixedUpdate detecta)
                break;

            case EstadoBoss.NoChao:
                // Tempo vulnerável
                if (timerEstado >= tempoNoChao)
                {
                    MudarEstado(EstadoBoss.Subindo);
                }
                break;

            case EstadoBoss.Subindo:
                // Espera chegar na altura de voo (FixedUpdate detecta)
                break;
        }
    }

    private bool PodeAtacar() => cdFogo <= 0f || cdProjetil <= 0f;

    private void DefinirProximoAtaque()
    {
        var opcoes = new List<TipoAtaque>();
        if (cdFogo <= 0f) opcoes.Add(TipoAtaque.FogoChao);
        if (cdProjetil <= 0f) opcoes.Add(TipoAtaque.Projetil);

        if (opcoes.Count == 0) { proximoAtaque = TipoAtaque.FogoChao; return; } // fallback

        proximoAtaque = opcoes[Random.Range(0, opcoes.Count)];
        timerDecisao = Random.Range(2f, 4f); // Próxima decisão em 2-4s
    }

    private void ExecutarAtaque()
    {
        MudarEstado(EstadoBoss.Atacando);
    }

    // Chamado ao terminar ataque aéreo
    private void AtaqueAereoTerminou()
    {
        contadorAtaquesAereos++;

        // Verifica se deve cair no chão
        bool deveCair = contadorAtaquesAereos >= ataquesAntesDeCair || tempoVoando >= tempoMaxVoando;

        if (deveCair)
        {
            contadorAtaquesAereos = 0;
            tempoVoando = 0f;
            MudarEstado(EstadoBoss.Caindo);
        }
        else
        {
            MudarEstado(EstadoBoss.Voando);
        }
    }

    // Forçar cair (debug)
    private void ForcarCair()
    {
        if (estadoAtual == EstadoBoss.Voando || estadoAtual == EstadoBoss.Atacando)
        {
            contadorAtaquesAereos = ataquesAntesDeCair;
            MudarEstado(EstadoBoss.Caindo);
        }
    }

    // =====================================
    // MOVIMENTOS
    // =====================================

    private void HandleVoo()
    {
        if (waypointsVoo == null || waypointsVoo.Length == 0) return;

        Vector2 pos = transform.position;
        Vector2 dir = (posicaoAlvo - pos).normalized;
        float dist = Vector2.Distance(pos, posicaoAlvo);

        // Hover (oscilação vertical)
        tempoHover += Time.fixedDeltaTime * frequenciaHover;
        float hover = Mathf.Sin(tempoHover) * amplitudeHover;
        Vector2 alvoComHover = posicaoAlvo + Vector2.up * hover;

        // Virada suave
        float angAlvo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float angAtual = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        float angSuave = Mathf.SmoothDampAngle(angAtual, angAlvo, ref velVirada, suavidadeVirada);
        Vector2 dirSuave = new(Mathf.Cos(angSuave * Mathf.Deg2Rad), Mathf.Sin(angSuave * Mathf.Deg2Rad));

        // Move
        Vector2 vel = dirSuave * velocidadeVoo;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, vel, aceleracaoVoo * Time.fixedDeltaTime);

        // Flip sprite
        if (spriteRenderer && Mathf.Abs(rb.linearVelocity.x) > 0.1f) spriteRenderer.flipX = rb.linearVelocity.x < 0f;

        // Anim
        if (animator) { animator.SetBool(H_Voando, true); animator.SetFloat(H_Velocidade, rb.linearVelocity.magnitude / velocidadeVoo); }

        // Próximo waypoint
        if (dist < 0.5f) ProximoWaypoint();

        // Clamp Y razoável
        float baseY = waypointsVoo[0].position.y;
        transform.position = new(transform.position.x, Mathf.Clamp(transform.position.y, baseY - 2f, baseY + 2f), transform.position.z);
    }

    private void ProximoWaypoint()
    {
        if (waypointsVoo.Length == 0) return;

        Vector2 atual = transform.position;
        float melhorDist = float.MaxValue;
        int melhorIdx = 0;

        for (int i = 0; i < waypointsVoo.Length; i++)
        {
            float d = Vector2.Distance(atual, waypointsVoo[i].position);
            if (d < melhorDist) { melhorDist = d; melhorIdx = i; }
        }

        // Se já está nesse, pega outro
        if (melhorIdx == waypointAtual && waypointsVoo.Length > 1)
        {
            int r; do r = Random.Range(0, waypointsVoo.Length); while (r == waypointAtual);
            waypointAtual = r;
        }
        else waypointAtual = melhorIdx;

        posicaoAlvo = waypointsVoo[waypointAtual].position;
        posicaoAlvo.y += Random.Range(-0.8f, 0.8f);

        if (logsDebug) Debug.Log($"[BossFase2] Waypoint {waypointAtual} = {posicaoAlvo} (dist {melhorDist:F1})");
    }

    private void HandleQueda()
    {
        // Cai reto para o centroArena
        Vector2 centro = centroArena ? (Vector2)centroArena.position : (Vector2)posicaoSpawn;
        Vector2 pos = rb.position;

        // Move só Y para baixo
        pos.y = Mathf.MoveTowards(pos.y, centro.y, velocidadeQueda * Time.fixedDeltaTime);
        rb.position = pos;

        // Chegou no chão?
        if (Mathf.Abs(pos.y - centro.y) < 0.1f)
        {
            rb.position = new Vector2(centro.x, centro.y);
            MudarEstado(EstadoBoss.NoChao);
        }
    }

    private void HandleSubida()
    {
        // Sobe para o waypoint mais próximo
        if (waypointsVoo.Length == 0) { MudarEstado(EstadoBoss.Voando); return; }

        Vector2 pos = rb.position;
        Vector2 alvo = waypointsVoo[waypointAtual >= 0 ? waypointAtual : 0].position;

        // Move Y para cima
        pos.y = Mathf.MoveTowards(pos.y, alvo.y, velocidadeSubida * Time.fixedDeltaTime);
        rb.position = pos;

        // Chegou na altura?
        if (Mathf.Abs(pos.y - alvo.y) < 0.2f)
        {
            rb.position = new Vector2(pos.x, alvo.y);
            MudarEstado(EstadoBoss.Voando);
        }
    }

    // =====================================
    // ATAQUES AÉREOS
    // =====================================

    private IEnumerator RotinaFogoChao()
    {
        if (animator) animator.SetTrigger(H_FogoChao);
        Play(somAvisoFogo);

        // Gera posições dos fogos
        Vector3[] posicoesFogo;
        if (fogoInteligente && pontoFogoEsquerdo && pontoFogoDireito)
        {
            posicoesFogo = GerarPosicoesFogoInteligente();
        }
        else
        {
            // Modo manual (compatibilidade)
            var escolhidos = EscolherAleatorios(pontosFogoChao, quantidadeFogo);
            posicoesFogo = new Vector3[escolhidos.Length];
            for (int i = 0; i < escolhidos.Length; i++) posicoesFogo[i] = escolhidos[i].position;
        }

        // Avisos nas posições
        yield return MostrarAvisosPosicoes(posicoesFogo, prefabAvisoFogo, tempoAvisoFogo, Color.yellow);

        // Spawn fogo nas posições
        Play(somSpawnFogo);
        foreach (var pos in posicoesFogo) SpawnFogo(pos);
        yield return new WaitForSeconds(tempoFogoAtivo);

        // Fade
        yield return FadeFogos();

        // Ataque aéreo terminou
        AtaqueAereoTerminou();
    }

    /// <summary>
    /// Distribui fogos inteligentemente entre ponto esquerdo e direito
    /// </summary>
    private Vector3[] GerarPosicoesFogoInteligente()
    {
        if (!pontoFogoEsquerdo || !pontoFogoDireito) return new Vector3[0];

        Vector3 esquerda = pontoFogoEsquerdo.position;
        Vector3 direita = pontoFogoDireito.position;

        // Garante que esquerda está à esquerda
        if (esquerda.x > direita.x) { var tmp = esquerda; esquerda = direita; direita = tmp; }

        float larguraTotal = direita.x - esquerda.x;
        int qtd = Mathf.Max(1, quantidadeFogo);

        // Calcula quantos cabem com espaçamento mínimo
        int maxCabem = Mathf.FloorToInt(larguraTotal / espacamentoMinimo) + 1;
        qtd = Mathf.Min(qtd, maxCabem);

        var posicoes = new Vector3[qtd];

        if (qtd == 1)
        {
            // Um fogo no meio
            posicoes[0] = Vector3.Lerp(esquerda, direita, 0.5f);
        }
        else
        {
            // Distribui uniformemente com variação aleatória
            float passo = larguraTotal / (qtd - 1);
            for (int i = 0; i < qtd; i++)
            {
                float t = (float)i / (qtd - 1);
                float x = Mathf.Lerp(esquerda.x, direita.x, t);
                x += Random.Range(-variacaoX, variacaoX);
                // Clampa dentro dos limites
                x = Mathf.Clamp(x, esquerda.x + 0.5f, direita.x - 0.5f);

                posicoes[i] = new Vector3(x, esquerda.y, esquerda.z);
            }
        }

        if (logsDebug) Debug.Log($"[BossFase2] Fogo inteligente: {qtd} fogos entre x={esquerda.x:F1} e x={direita.x:F1}");

        return posicoes;
    }

    private void SpawnFogo(Vector3 pos)
    {
        GameObject f = prefabFogo ? Instantiate(prefabFogo, pos, Quaternion.identity) : CriarFogoProcedural(pos);
        fogosAtivos.Add(f);

        // Usa o novo script standalone DanoFogoBoss2
        var dmg = f.GetComponent<DanoFogoBoss2>();
        if (!dmg) dmg = f.AddComponent<DanoFogoBoss2>();
        dmg.Configurar(danoFogo, 0.5f, layerJogador);
    }

    private GameObject CriarFogoProcedural(Vector3 pos)
    {
        var go = new GameObject("FogoChao");
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.5f, 2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CriarSpriteFogo();
        sr.color = new Color(1f, 0.3f, 0f, 0.9f);
        sr.sortingOrder = 5;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1f, 1.5f);

        // Usa o novo script standalone DanoFogoBoss2
        var dmg = go.AddComponent<DanoFogoBoss2>();
        dmg.Configurar(danoFogo, 0.5f, layerJogador);

        go.AddComponent<PulsarFogo>();
        return go;
    }

    private IEnumerator FadeFogos()
    {
        float t = 0f;
        var srs = new List<SpriteRenderer>();
        foreach (var f in fogosAtivos) if (f) { var sr = f.GetComponent<SpriteRenderer>(); if (sr) srs.Add(sr); var c = f.GetComponent<Collider2D>(); if (c) c.enabled = false; }

        while (t < tempoFadeFogo)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / tempoFadeFogo);
            foreach (var sr in srs) if (sr) { var c = sr.color; c.a = a; sr.color = c; }
            foreach (var f in fogosAtivos) if (f) f.transform.position += Vector3.down * Time.deltaTime * 1.5f;
            yield return null;
        }
        foreach (var f in fogosAtivos) if (f) Destroy(f);
        fogosAtivos.Clear();
    }

    private IEnumerator RotinaProjetil()
    {
        if (animator) animator.SetTrigger(H_Projetil);

        for (int i = 0; i < projetisPorAtaque; i++)
        {
            if (cantosArena == null || cantosArena.Length == 0) break;
            var canto = cantosArena[Random.Range(0, cantosArena.Length)];

            // Aviso no canto
            yield return MostrarAviso(canto.position, prefabAvisoProjetil, tempoAvisoProjetil);

            // Atira
            Play(somTiroProjetil);
            Vector2 dir = ((Vector2)(jogador ? jogador.position : (centroArena ? centroArena.position : canto.position + Vector3.left * 10f))) - (Vector2)canto.position;
            dir.Normalize();
            AtirarProjetil(canto.position, dir);

            if (i < projetisPorAtaque - 1) yield return new WaitForSeconds(intervaloProjetil);
        }

        AtaqueAereoTerminou();
    }

    private void AtirarProjetil(Vector3 pos, Vector2 dir)
    {
        GameObject p = prefabProjetil ? Instantiate(prefabProjetil, pos, Quaternion.identity) : CriarProjetilProcedural(pos, dir);
        var scr = p.GetComponent<ProjetilBoss>();
        if (!scr) scr = p.AddComponent<ProjetilBoss>();
        scr.Iniciar(dir, velocidadeProjetil, danoProjetil, tempoVidaProjetil, layerJogador);
    }

    private GameObject CriarProjetilProcedural(Vector3 pos, Vector2 dir)
    {
        var go = new GameObject("ProjetilBoss");
        go.transform.position = pos;
        go.transform.right = dir;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CriarSpriteProjetil();
        sr.color = Color.red;
        sr.sortingOrder = 5;

        var rb2 = go.AddComponent<Rigidbody2D>();
        rb2.gravityScale = 0f;
        rb2.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb2.linearVelocity = dir * velocidadeProjetil;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.25f;

        var scr = go.AddComponent<ProjetilBoss>();
        scr.Iniciar(dir, velocidadeProjetil, danoProjetil, tempoVidaProjetil, layerJogador);

        Destroy(go, tempoVidaProjetil);
        return go;
    }

    // =====================================
    // FASES E MORTE
    // =====================================

    private int CalcularFase(int hp)
    {
        if (forcarFase > 0) return forcarFase;
        if (hp <= limiarFase3) return 3;
        if (hp <= limiarFase2) return 2;
        return 1;
    }

    private void VerificarMudancaFase()
    {
        int novaFase = CalcularFase(vidaAtual);
        if (novaFase > faseAtual)
        {
            faseAtual = novaFase;
            StartCoroutine(RotinaMudancaFase());
        }
    }

    private IEnumerator RotinaMudancaFase()
    {
        var estadoAnterior = estadoAtual;
        MudarEstado(EstadoBoss.Voando); // Para tudo

        if (animator) { animator.SetTrigger(H_Fase); animator.SetInteger("Fase", faseAtual); }
        Play(somMudancaFase);

        float t = 0f, dur = 1.2f;
        Color orig = spriteRenderer ? spriteRenderer.color : Color.white;
        Color flash = faseAtual == 2 ? Color.red : Color.magenta;

        while (t < dur)
        {
            t += Time.deltaTime;
            float f = Mathf.Sin(t * 25f) * 0.5f + 0.5f;
            if (spriteRenderer) spriteRenderer.color = Color.Lerp(orig, flash, f);
            yield return null;
        }
        if (spriteRenderer) spriteRenderer.color = orig;

        // Ajusta parâmetros
        if (faseAtual == 2)
        {
            velocidadeVoo *= 1.25f; cooldownFogo *= 0.75f; cooldownProjetil *= 0.75f;
            quantidadeFogo = Mathf.Min(quantidadeFogo + 1, pontosFogoChao?.Length ?? 99);
            projetisPorAtaque = Mathf.Min(projetisPorAtaque + 1, 5);
        }
        else if (faseAtual == 3)
        {
            velocidadeVoo *= 1.4f; cooldownFogo *= 0.5f; cooldownProjetil *= 0.5f;
            quantidadeFogo += 2; projetisPorAtaque += 2;
            tempoAvisoFogo *= 0.7f; tempoAvisoProjetil *= 0.7f;
        }

        if (logsDebug) Debug.Log($"[BossFase2] Fase {faseAtual} aplicada");
        MudarEstado(EstadoBoss.Voando);
    }

    private IEnumerator RotinaMorte()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (col) col.enabled = false;
        if (audioSource && audioSource.clip == somVooLoop) audioSource.Stop();
        Play(somMorte);
        if (animator) animator.SetTrigger(H_Morte);
        LimparTudo();
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    // =====================================
    // DANO
    // =====================================

    public void TakeDamage(int amount, GameObject source) => ReceberDano(amount, source);
    public void ReceberDano(int quantidade, GameObject origem)
    {
        if (morto || invulneravel || quantidade <= 0) return;

        // SÓ LEVA DANO SE ESTIVER NO CHÃO!
        if (estadoAtual != EstadoBoss.NoChao)
        {
            if (logsDebug) Debug.Log($"[BossFase2] Dano BLOQUEADO - Estado: {estadoAtual} (só leva dano no chão)");
            return;
        }

        vidaAtual -= quantidade;
        tempoUltimoDano = Time.time;
        invulneravel = true;

        if (animator) animator.SetTrigger(H_Hit);
        Play(somDano);
        StartCoroutine(FlashDano());

        if (logsDebug) Debug.Log($"[BossFase2] DANO RECEBIDO: {quantidade} | HP: {vidaAtual}/{vidaMaxima}");

        if (vidaAtual <= 0 && podeMorrer) { morto = true; MudarEstado(EstadoBoss.Morto); }
    }

    private IEnumerator FlashDano()
    {
        if (!spriteRenderer) yield break;
        var o = spriteRenderer.color;
        for (int i = 0; i < 3; i++) { spriteRenderer.color = Color.white; yield return new WaitForSeconds(0.04f); spriteRenderer.color = o; yield return new WaitForSeconds(0.04f); }
        invulneravel = false;
    }

    private void OnCollisionStay2D(Collision2D c)
    {
        if (morto || !c.gameObject.CompareTag("Player") || Time.time < tempoUltimoContato + cooldownContato) return;
        var pc = c.gameObject.GetComponentInParent<PlayerController>() ?? c.gameObject.GetComponent<PlayerController>();
        if (pc) { pc.TakeDamage(danoContato, gameObject); tempoUltimoContato = Time.time; }
    }
    private void OnCollisionEnter2D(Collision2D c) => OnCollisionStay2D(c);

    // =====================================
    // HELPERS
    // =====================================

    private void AtualizarTimers()
    {
        if (cdFogo > 0f) cdFogo -= Time.deltaTime;
        if (cdProjetil > 0f) cdProjetil -= Time.deltaTime;
        if (timerDecisao > 0f) timerDecisao -= Time.deltaTime;
        if (invulneravel && Time.time > tempoUltimoDano + tempoInvulneravel) invulneravel = false;
    }

    private void AtualizarAnimacoes()
    {
        if (!animator) return;
        animator.SetBool(H_Voando, estadoAtual == EstadoBoss.Voando);
    }

    private void BuscarJogador()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) { jogador = p.transform; jogadorCtrl = p.GetComponent<PlayerController>(); }
    }

    private void Play(AudioClip c) { if (c && audioSource) audioSource.PlayOneShot(c); }

    private void LimparTudo()
    {
        foreach (var w in avisosAtivos) if (w) Destroy(w);
        avisosAtivos.Clear();
        foreach (var f in fogosAtivos) if (f) Destroy(f);
        fogosAtivos.Clear();
    }

    private T[] EscolherAleatorios<T>(T[] arr, int qtd) where T : class
    {
        if (arr == null || arr.Length == 0) return new T[0];
        var pool = new List<T>(arr);
        var res = new List<T>();
        qtd = Mathf.Min(qtd, pool.Count);
        for (int i = 0; i < qtd; i++) { int idx = Random.Range(0, pool.Count); res.Add(pool[idx]); pool.RemoveAt(idx); }
        return res.ToArray();
    }

    private IEnumerator MostrarAvisos(Transform[] pts, GameObject prefab, float tempo, Color cor)
    {
        foreach (var p in pts)
        {
            var w = prefab ? Instantiate(prefab, p.position, Quaternion.identity) : CriarAvisoCirculo(p.position, cor);
            if (w) avisosAtivos.Add(w);
        }
        float t = 0f;
        while (t < tempo)
        {
            t += Time.deltaTime;
            float a = (Mathf.Sin(t * 12f) + 1f) * 0.5f * 0.6f + 0.4f;
            foreach (var w in avisosAtivos) if (w) { var sr = w.GetComponent<SpriteRenderer>(); if (sr) { var c = sr.color; c.a = a; sr.color = c; } }
            yield return null;
        }
        foreach (var w in avisosAtivos) if (w) Destroy(w);
        avisosAtivos.Clear();
    }

    /// <summary>
    /// Versão para posições Vector3[] (usado pelo fogo inteligente)
    /// </summary>
    private IEnumerator MostrarAvisosPosicoes(Vector3[] posicoes, GameObject prefab, float tempo, Color cor)
    {
        foreach (var pos in posicoes)
        {
            var w = prefab ? Instantiate(prefab, pos, Quaternion.identity) : CriarAvisoCirculo(pos, cor);
            if (w) avisosAtivos.Add(w);
        }
        float t = 0f;
        while (t < tempo)
        {
            t += Time.deltaTime;
            float a = (Mathf.Sin(t * 12f) + 1f) * 0.5f * 0.6f + 0.4f;
            foreach (var w in avisosAtivos) if (w) { var sr = w.GetComponent<SpriteRenderer>(); if (sr) { var c = sr.color; c.a = a; sr.color = c; } }
            yield return null;
        }
        foreach (var w in avisosAtivos) if (w) Destroy(w);
        avisosAtivos.Clear();
    }

    private IEnumerator MostrarAviso(Vector3 pos, GameObject prefab, float tempo)
    {
        var w = prefab ? Instantiate(prefab, pos, Quaternion.identity) : CriarAvisoLinha(pos);
        if (w) avisosAtivos.Add(w);
        Play(somAvisoProjetil);
        yield return new WaitForSeconds(tempo);
        if (w) { Destroy(w); avisosAtivos.Remove(w); }
    }

    private GameObject CriarAvisoCirculo(Vector3 pos, Color cor)
    {
        var go = new GameObject("AvisoFogo");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CriarSpriteCirculo();
        sr.color = cor;
        sr.sortingOrder = 10;
        go.transform.localScale = Vector3.one * 1.8f;
        return go;
    }

    private GameObject CriarAvisoLinha(Vector3 pos)
    {
        var go = new GameObject("AvisoProjetil");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.12f; lr.endWidth = 0.03f;
        lr.startColor = Color.yellow; lr.endColor = new Color(1f, 1f, 0f, 0f);
        lr.positionCount = 2;
        Vector3 alvo = jogador ? jogador.position : (centroArena ? centroArena.position : pos + Vector3.left * 10f);
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, alvo - pos);
        lr.sortingOrder = 10;
        go.AddComponent<PiscarLinha>();
        return go;
    }

    private void ValidarArrays()
    {
        if ((cantosArena == null || cantosArena.Length == 0) && logsDebug) Debug.LogWarning("[BossFase2] Cantos da arena não configurados!");

        if (fogoInteligente)
        {
            if (!pontoFogoEsquerdo && logsDebug) Debug.LogWarning("[BossFase2] Ponto Fogo Esquerdo não configurado (modo inteligente)!");
            if (!pontoFogoDireito && logsDebug) Debug.LogWarning("[BossFase2] Ponto Fogo Direito não configurado (modo inteligente)!");
        }
        else if ((pontosFogoChao == null || pontosFogoChao.Length == 0) && logsDebug) Debug.LogWarning("[BossFase2] Pontos de fogo no chão não configurados!");

        if ((waypointsVoo == null || waypointsVoo.Length == 0) && logsDebug) Debug.LogWarning("[BossFase2] Waypoints de voo não configurados!");
        if (!centroArena && logsDebug) Debug.LogWarning("[BossFase2] Centro da arena não configurado - boss cairá no spawn!");
    }

    private IEnumerator RotinaIntro()
    {
        MudarEstado(EstadoBoss.Intro);
        if (animator) animator.SetTrigger("Intro");
        Play(somMudancaFase);
        yield return new WaitForSeconds(1.5f);
        MudarEstado(EstadoBoss.Voando);
    }

    private void OnDrawGizmosSelected()
    {
        if (!desenharGizmos) return;

        if (waypointsVoo != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var w in waypointsVoo) if (w) { Gizmos.DrawWireSphere(w.position, 0.4f); Gizmos.DrawLine(transform.position, w.position); }
            for (int i = 0; i < waypointsVoo.Length; i++)
                if (waypointsVoo[i] && waypointsVoo[(i + 1) % waypointsVoo.Length])
                    Gizmos.DrawLine(waypointsVoo[i].position, waypointsVoo[(i + 1) % waypointsVoo.Length].position);
        }
        if (cantosArena != null) { Gizmos.color = Color.yellow; foreach (var c in cantosArena) if (c) Gizmos.DrawWireCube(c.position, Vector3.one); }

        // Fogo inteligente - mostra os 2 pontos e a distribuição
        if (fogoInteligente)
        {
            if (pontoFogoEsquerdo && pontoFogoDireito)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pontoFogoEsquerdo.position, 0.5f);
                Gizmos.DrawWireSphere(pontoFogoDireito.position, 0.5f);
                Gizmos.DrawLine(pontoFogoEsquerdo.position, pontoFogoDireito.position);

                // Mostra onde os fogos vão nascer (preview)
                Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
                Vector3 esq = pontoFogoEsquerdo.position;
                Vector3 dir = pontoFogoDireito.position;
                if (esq.x > dir.x) { var tmp = esq; esq = dir; dir = tmp; }
                float largura = dir.x - esq.x;
                int qtd = Mathf.Max(1, quantidadeFogo);
                int maxCabem = Mathf.FloorToInt(largura / espacamentoMinimo) + 1;
                qtd = Mathf.Min(qtd, maxCabem);

                if (qtd == 1)
                {
                    Gizmos.DrawWireSphere(Vector3.Lerp(esq, dir, 0.5f), 0.4f);
                }
                else
                {
                    float passo = largura / (qtd - 1);
                    for (int i = 0; i < qtd; i++)
                    {
                        float t = (float)i / (qtd - 1);
                        float x = Mathf.Lerp(esq.x, dir.x, t);
                        Gizmos.DrawWireSphere(new Vector3(x, esq.y, esq.z), 0.4f);
                    }
                }
            }
        }
        else if (pontosFogoChao != null)
        {
            Gizmos.color = Color.red;
            foreach (var f in pontosFogoChao) if (f) Gizmos.DrawWireSphere(f.position, 0.3f);
        }

        if (centroArena) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(centroArena.position, 1f); Gizmos.DrawIcon(centroArena.position, "d_UnityEditor.ConsoleWindow", true); }
        Gizmos.color = new Color(0, 1, 1, 0.15f);
        Gizmos.DrawWireCube(posicaoSpawn, new Vector3(16f, 8f, 1f));
    }

    // =====================================
    // SPRITES PROCEDURAIS
    // =====================================

    private Sprite CriarSpriteCirculo()
    {
        int s = 64; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); var px = new Color[s * s];
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) { float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f)); px[y * s + x] = d < s / 2f ? new Color(1f, 0.5f, 0f, 1f - d / (s / 2f)) : Color.clear; }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CriarSpriteFogo()
    {
        int s = 64; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); var px = new Color[s * s];
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float dx = Mathf.Abs(x - s / 2f) / (s * 0.3f);
            float dy = (s / 3f - y) / (s * 0.4f);
            if (dy > 0 && dx < dy * 0.7f) px[y * s + x] = new Color(1f, 0.2f + dy * 0.5f, 0f, dy + Random.Range(-0.1f, 0.1f));
            else px[y * s + x] = Color.clear;
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.2f), 100f);
    }

    private Sprite CriarSpriteProjetil()
    {
        int s = 32; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); var px = new Color[s * s];
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) { float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f)) / (s / 2f); px[y * s + x] = d < 1f ? new Color(1f, 0.1f, 0f, 1f - d) : Color.clear; }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }
}

// =====================================
// COMPONENTES AUXILIARES
// =====================================

public class PulsarFogo : MonoBehaviour { Vector3 b; void Start() => b = transform.localScale; void Update() => transform.localScale = new Vector3(b.x, b.y + Mathf.Sin(Time.time * 4f) * 0.15f, b.z); }

public class ProjetilBoss : MonoBehaviour
{
    Vector2 dir; float vel; float dmg; float vida; LayerMask layer; float t; Rigidbody2D rb;
    public void Iniciar(Vector2 d, float v, float dmg_, float life, LayerMask l) { dir = d.normalized; vel = v; dmg = dmg_; vida = life; layer = l; rb = GetComponent<Rigidbody2D>(); if (rb) rb.linearVelocity = dir * vel; transform.right = dir; }
    void Update() { t += Time.deltaTime; if (t >= vida) Destroy(gameObject); }
    void OnTriggerEnter2D(Collider2D o) { if (((1 << o.gameObject.layer) & layer) == 0) return; if (!o.CompareTag("Player")) return; var pc = o.GetComponentInParent<PlayerController>() ?? o.GetComponent<PlayerController>(); if (pc) { pc.TakeDamage((int)dmg, gameObject); Destroy(gameObject); } }
}

public class PiscarLinha : MonoBehaviour { LineRenderer lr; void Start() => lr = GetComponent<LineRenderer>(); void Update() { if (lr) { float a = (Mathf.Sin(Time.time * 10f) + 1f) * 0.5f; var sc = lr.startColor; var ec = lr.endColor; sc.a = a; ec.a = a * 0.5f; lr.startColor = sc; lr.endColor = ec; } } }