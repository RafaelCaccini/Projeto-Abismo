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

    [Tooltip("Tempo entre sequências de ataque")]
    [SerializeField] private float tempoEntreSequencias = 2.5f;

    [Tooltip("Tempo de preparação antes de spawnar ondas (aviso visual)")]
    [SerializeField] private float tempoPreparacaoOnda = 1f;

    [Header("PRESSÃO CONSTANTE (EMPURRÃO PARA ESQUERDA)")]
    [Tooltip("Força constante aplicada no player para empurrar para esquerda")]
    [SerializeField] private float forcaEmpurraoConstante = 3f;

    [Tooltip("Usar força (AddForce) ou velocidade direta")]
    [SerializeField] private bool usarForcaFisica = true;

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
                break;

            case EstadoBoss.Atacando:
                pressaoAtiva = true; // Pressão ATIVA durante ataques
                IniciarSequenciaAtaque();
                break;

            case EstadoBoss.Atordoado:
                pressaoAtiva = false; // Pressão PARADA durante stun
                estaAtordoado = true;
                if (animator) { animator.SetTrigger(H_Stun); animator.SetBool(H_Atacando, false); }
                if (somStun) Play(somStun);
                if (spriteRenderer) spriteRenderer.color = corStun;
                break;

            case EstadoBoss.Morto:
                pressaoAtiva = false;
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

            if (logsDebug) Debug.Log($"[BossFase3] Iniciando sequência: {tipo}");

            // Prepara animação
            if (animator)
            {
                animator.SetInteger(H_TipoOnda, tipo == TipoOnda.Comum ? 0 : 1);
                animator.SetBool(H_Atacando, true);
            }

            // Tempo de preparação (aviso visual)
            if (somPreparacao) Play(somPreparacao);
            yield return new WaitForSeconds(tempoPreparacaoOnda);

            if (morto || estadoAtual != EstadoBoss.Atacando) yield break;

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

            // Aguarda antes da próxima sequência
            yield return new WaitForSeconds(tempoEntreSequencias);
        }

        rotinaAtaques = null;
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
    // STUN / VULNERABILIDADE
    // =====================================================

    public void TakeDamage(int dano, GameObject fonte) => ReceberDano(dano, fonte);

    public void ReceberDano(int quantidade, GameObject origem)
    {
        if (morto || quantidade <= 0) return;

        // Só pode levar dano quando ATORDOADO (stun)
        if (!estaAtordoado)
        {
            if (logsDebug) Debug.Log($"[BossFase3] Dano BLOQUEADO - Boss não está atordoado. Estado: {estadoAtual}");
            return;
        }

        // Durante stun, tem invulnerabilidade temporária
        if (Time.time < tempoUltimoDano + tempoInvulneravelStun) return;

        vidaAtual -= quantidade;
        tempoUltimoDano = Time.time;

        if (logsDebug) Debug.Log($"[BossFase3] DANO RECEBIDO: {quantidade} | HP: {vidaAtual}/{vidaMaxima}");

        // Feedback visual
        if (spriteRenderer) StartCoroutine(FlashDano());

        if (somDano) Play(somDano);

        if (vidaAtual <= 0)
        {
            morto = true;
            MudarEstado(EstadoBoss.Morto);
        }
    }

    private float tempoUltimoDano = -999f;

    private IEnumerator FlashDano()
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Chamado externamente (ex: player acerta weak point, ou evento de fase)
    public void AplicarDanoStun(int quantidade)
    {
        if (morto || estaAtordoado) return;

        danoAcumuladoStun += quantidade;

        if (logsDebug) Debug.Log($"[BossFase3] Dano stun acumulado: {danoAcumuladoStun}/{danoParaStun}");

        if (danoAcumuladoStun >= danoParaStun)
        {
            Atordoar();
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
                $"Ondas Ativas: {ondasAtivas.Count}"
            );
        }
#endif
    }
}