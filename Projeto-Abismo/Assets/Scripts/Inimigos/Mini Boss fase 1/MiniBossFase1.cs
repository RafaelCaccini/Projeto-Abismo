using System.Collections.Generic;
using UnityEngine;
using System.Collections;


public class MiniBossFase1 : MonoBehaviour, IDamageable
{
    [Header("Player")]
    public Transform jogador;

    [Header("Arena")]
    public Transform pontoEsquerda;
    public Transform pontoDireita;

    [Header("Paredes")]
    public GameObject paredeEsquerda;
    public GameObject paredeDireita;

    [Header("Movimento")]
    public float velocidadePerseguicao = 6f;
    public float tempoPulo = 0.5f; // duração base de um pulo/arc
    public float alturaPulo = 1.8f;

    [Header("Detecção")]
    public float alcanceDeteccao = 10f;

    [Header("Spikes")]
    public GameObject prefabSpike;
    public Transform inicioChao;
    public Transform fimChao;
    public Transform inicioTeto;
    public Transform fimTeto;
    public int quantidadeSpikes = 6;
    public float tempoSpike = 2f;
    public float atrasoAntesPisao = 0.35f; // tempo para animação de pisar antes de spawnar spikes

    [Header("Linha de pulos")]
    public int pulosVerticaisPorLinha = 3;
    public int pulosDiagonais = 3;
    public float intervaloEntrePulos = 0.18f;
    public float alturaVertical = 2.0f;
    public float duracaoPulo = 0.45f;

    [Header("Projetil")]
    public GameObject prefabProjetil;
    public Transform pontoTiro; // se nulo, usa a posição do boss
    public int quantidadeProjetisAtirar = 4;
    public float intervaloEntreTiros = 0.25f;
    public float velocidadeProjetil = 8f;
    public float tempoMoverParaExtremo = 0.35f; // tempo para ir até a extremidade antes de atirar

    [Header("Contato")]
    public int danoAoTocar = 2;
    public float cooldownDanoContato = 0.6f;

    [Header("Vida")]
    public int vidaMaxima = 20;
    private int vidaAtual;

    // ========================= Paredes controláveis =========================
    [Header("Paredes - Configuração")]
    [SerializeField] private float wallEnterMargin = 0.1f; // quanto além do X da parede o player deve ultrapassar para considerar "passou pela parede"
    [SerializeField] private float wallReenableDistance = 1.2f; // distância a partir da parede para reativar colisão (sair do "raio")

    // estado interno das paredes
    private class WallInfo
    {
        public GameObject go;
        public Collider2D col;
        public SpriteRenderer sr;
        public Color originalColor;
        public bool originalIsTrigger;
        public bool originalActive;
        public bool passedThrough = false;
        public bool reenabled = false;

        public WallInfo(GameObject g)
        {
            go = g;
            originalActive = g.activeSelf;
            col = g != null ? g.GetComponent<Collider2D>() : null;
            sr = g != null ? g.GetComponent<SpriteRenderer>() : null;
            if (sr != null) originalColor = sr.color;
            if (col != null) originalIsTrigger = col.isTrigger;
        }

        public void MakeTransparentOpen()
        {
            if (go != null && !go.activeSelf) go.SetActive(true);
            if (sr != null)
            {
                var c = sr.color;
                c.a = 0.35f;
                sr.color = c;
            }
            if (col != null)
            {
                col.isTrigger = true; // permite atravessar
            }
            passedThrough = false;
            reenabled = false;
        }

        public void ReenableCollision()
        {
            if (col != null)
                col.isTrigger = false;
            if (sr != null)
            {
                var c = originalColor;
                c.a = 1f;
                sr.color = c;
            }
            reenabled = true;
        }

        public void RestoreOriginal()
        {
            if (go != null) go.SetActive(originalActive);
            if (col != null) col.isTrigger = originalIsTrigger;
            if (sr != null) sr.color = originalColor;
            passedThrough = false;
            reenabled = false;
        }
    }

    private WallInfo leftWall;
    private WallInfo rightWall;

    private bool lutaComecou;
    private bool morto;
    private bool ocupado;
    private bool pulando;
    private Vector3 posicaoInicial;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float tempoUltimoAtaque = 0f;

    // Estados de IA
    private enum Estado { Idle, Perseguindo, Atacando }
    private Estado estadoAtual = Estado.Idle;

    // controle de dano por contato para não spammar
    private Dictionary<Collider2D, float> ultimoDanoPorCollider = new Dictionary<Collider2D, float>();

    void Start()
    {
        posicaoInicial = transform.position;
        rb = GetComponent<Rigidbody2D>();
        // Usamos movimento por interpolação (não física) então mantemos gravidade 0
        // e tornamos o Rigidbody2D Kinematic para evitar que a física interfira
        // na posição manual via transform.position (que causava o boss "entrar na terra")
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.isKinematic = true;
        }

        vidaAtual = vidaMaxima;

        if (jogador == null)
            jogador = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Inicializa estados das paredes (não altera ativo original aqui)
        if (paredeEsquerda != null)
            leftWall = new WallInfo(paredeEsquerda);

        if (paredeDireita != null)
            rightWall = new WallInfo(paredeDireita);

        // NÃO desativa as paredes aqui — elas serão configuradas quando a luta começar.
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (morto) return;
        if (jogador == null) return;

        float distancia = Vector2.Distance(transform.position, jogador.position);

        if (!lutaComecou && distancia <= alcanceDeteccao)
        {
            lutaComecou = true;
            AtivarParedes(); // ativa e deixa transparentes/atravessáveis no começo
            Debug.Log("LUTA INICIADA");
        }

        // Se a luta começou, gerencia reativação de colisão das paredes com base no jogador
        if (lutaComecou)
        {
            ManageWallsDuringFight();
        }

        // Gestão de estado
        switch (estadoAtual)
        {
            case Estado.Idle:
                if (distancia <= alcanceDeteccao)
                    estadoAtual = Estado.Perseguindo;
                break;

            case Estado.Perseguindo:
                MovimentoInteligente();
                GerenciarAtaques();
                break;

            case Estado.Atacando:
                GerenciarAtaques();
                break;
        }
    }

    // LateUpdate garante que o boss fique na altura correta (Y = posicaoInicial.y)
    // quando não está atacando. Atua como proteção contra interferência física
    // ou qualquer outro fator que possa mover o boss para baixo acidentalmente.
    void LateUpdate()
    {
        if (morto || jogador == null) return;
        if (estadoAtual == Estado.Atacando) return; // deixa os ataques controlarem o Y

        // Corrige Y se driftou por qualquer motivo
        if (Mathf.Abs(transform.position.y - posicaoInicial.y) > 0.01f)
        {
            transform.position = new Vector3(transform.position.x, posicaoInicial.y, transform.position.z);
        }
    }

    void ManageWallsDuringFight()
    {
        if (jogador == null) return;

        // checa cada parede individualmente: quando jogador "passou pela parede" e saiu do raio (distância) reativa colisão
        if (leftWall != null && !leftWall.reenabled)
        {
            // considerar "passou" se jogador estiver à direita da parede (ultrapassou seu X)
            float wallX = leftWall.go.transform.position.x;
            if (!leftWall.passedThrough)
            {
                if (jogador.position.x > wallX + wallEnterMargin)
                {
                    leftWall.passedThrough = true;
                    Debug.Log("[MiniBoss] Player passou pela parede esquerda.");
                }
            }
            else
            {
                // se já passou e saiu do "raio", reabilita colisão
                if (Mathf.Abs(jogador.position.x - wallX) >= wallReenableDistance)
                {
                    leftWall.ReenableCollision();
                    Debug.Log("[MiniBoss] Parede esquerda reativou colisão.");
                }
            }
        }

        if (rightWall != null && !rightWall.reenabled)
        {
            float wallX = rightWall.go.transform.position.x;
            if (!rightWall.passedThrough)
            {
                if (jogador.position.x < wallX - wallEnterMargin)
                {
                    rightWall.passedThrough = true;
                    Debug.Log("[MiniBoss] Player passou pela parede direita.");
                }
            }
            else
            {
                if (Mathf.Abs(jogador.position.x - wallX) >= wallReenableDistance)
                {
                    rightWall.ReenableCollision();
                    Debug.Log("[MiniBoss] Parede direita reativou colisão.");
                }
            }
        }
    }

    void MovimentoInteligente()
    {
        Vector2 direcao = (jogador.position - transform.position).normalized;

        // Movimento suave com limites de arena (apenas no eixo X)
        float novoX = Mathf.Clamp(transform.position.x + direcao.x * velocidadePerseguicao * Time.deltaTime,
                                pontoEsquerda.position.x,
                                pontoDireita.position.x);

        transform.position = new Vector3(novoX, posicaoInicial.y, transform.position.z);

        // Virar conforme a direção (usa flipX ao invés de localScale para não
        // interferir no BoxCollider2D — inverter scale faz o collider "saltar")
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direcao.x < 0;
        }
    }

    void GerenciarAtaques()
    {
        tempoUltimoAtaque += Time.deltaTime;

        // Decide ataque a cada ~3s (ajuste conforme quiser)
        if (estadoAtual == Estado.Atacando && tempoUltimoAtaque >= 2.8f && !ocupado)
        {
            tempoUltimoAtaque = 0f;
            estadoAtual = Estado.Perseguindo;
        }
        else if (tempoUltimoAtaque >= 3f && !ocupado)
        {
            EscolherAtaqueAleatorio();
            tempoUltimoAtaque = 0f;
        }
    }

    void EscolherAtaqueAleatorio()
    {
        int escolha = Random.Range(0, 4);

        switch (escolha)
        {
            case 0: // pular de um lado pro outro (Ponto A -> B)
                if (!ocupado)
                {
                    estadoAtual = Estado.Atacando;
                    StartCoroutine(PularEntrePontos(3)); // 3 saltos entre pontos, ajustável
                }
                break;

            case 1: // spawnar espinhos (chão + teto) com animação de pisar
                if (!ocupado)
                {
                    estadoAtual = Estado.Atacando;
                    StartCoroutine(PisaoSpawnSpikes());
                }
                break;

            case 2: // pulos em linhas (vários pulos verticais + diagonais)
                if (!ocupado)
                {
                    estadoAtual = Estado.Atacando;
                    StartCoroutine(PulosEmLinhas());
                }
                break;

            case 3: // atirar da extremidade
                if (!ocupado)
                {
                    estadoAtual = Estado.Atacando;
                    StartCoroutine(AtirarDaExtremidade());
                }
                break;
        }
    }

    IEnumerator PularEntrePontos(int repeticoes)
    {
        ocupado = true;

        for (int i = 0; i < repeticoes; i++)
        {
            // garante que a animação de pulo seja disparada a cada salto
            if (animator != null) animator.SetTrigger("Pular");

            float targetX = (transform.position.x <= (pontoEsquerda.position.x + pontoDireita.position.x) / 2)
                ? pontoDireita.position.x
                : pontoEsquerda.position.x;

            yield return JumpArc(targetX, alturaPulo, tempoPulo);

            yield return new WaitForSeconds(0.15f);
        }

        ocupado = false;
        estadoAtual = Estado.Perseguindo;
    }

    IEnumerator PulosEmLinhas()
    {
        ocupado = true;
        pulando = true;

        // 1) Pulinhos verticais no local atual (subir reto)
        for (int i = 0; i < pulosVerticaisPorLinha; i++)
        {
            if (animator != null) animator.SetTrigger("Pular");
            yield return JumpArc(transform.position.x, alturaVertical, duracaoPulo);
            yield return new WaitForSeconds(intervaloEntrePulos);
        }

        // 2) Depois, sequencia de pulos diagonais cruzando a arena (cair diagonalmente)
        for (int i = 0; i < pulosDiagonais; i++)
        {
            if (animator != null) animator.SetTrigger("Pular");

            float targetX = (transform.position.x <= (pontoEsquerda.position.x + pontoDireita.position.x) / 2)
                ? pontoDireita.position.x
                : pontoEsquerda.position.x;

            yield return JumpArc(targetX, alturaVertical * 1.0f, duracaoPulo * 1.05f);
            yield return new WaitForSeconds(intervaloEntrePulos);
        }

        pulando = false;
        ocupado = false;
        estadoAtual = Estado.Perseguindo;
    }

    // Interpola posição do boss entre start.x e targetX com arco parabólico
    IEnumerator JumpArc(float targetX, float altura, float duracao)
    {
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 end = new Vector3(targetX, posicaoInicial.y, transform.position.z);

        while (t < duracao)
        {
            float normalized = t / duracao;
            // interpola X e Y linearmente e adiciona arco parabólico no Y
            float x = Mathf.Lerp(start.x, end.x, normalized);
            float yLinear = Mathf.Lerp(start.y, end.y, normalized);
            float arc = 4f * altura * normalized * (1f - normalized); // pico no meio
            transform.position = new Vector3(x, yLinear + arc, start.z);

            // mantém facing adequado (flipX ao invés de localScale)
            if (spriteRenderer != null)
                spriteRenderer.flipX = end.x <= start.x;

            t += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
    }

    IEnumerator PisaoSpawnSpikes()
    {
        ocupado = true;
        if (animator != null) animator.SetTrigger("Pisao");

        // espera um pouco para sincronizar com animação
        yield return new WaitForSeconds(atrasoAntesPisao);

        // spawn no chão
        if (prefabSpike != null && inicioChao != null && fimChao != null)
        {
            SpawnLinhaSpikes(inicioChao.position, fimChao.position);
        }

        // spawn no teto
        if (prefabSpike != null && inicioTeto != null && fimTeto != null)
        {
            SpawnLinhaSpikes(inicioTeto.position, fimTeto.position);
        }

        // tempo para os spikes existirem e animação terminar
        yield return new WaitForSeconds(tempoSpike + 0.1f);

        ocupado = false;
        estadoAtual = Estado.Perseguindo;
    }

    void SpawnLinhaSpikes(Vector2 inicio, Vector2 fim)
    {
        Collider2D colliderBoss = GetComponent<Collider2D>();
        if (quantidadeSpikes <= 1)
        {
            GameObject s = Instantiate(prefabSpike, inicio, Quaternion.identity);
            if (s != null)
            {
                Collider2D c = s.GetComponent<Collider2D>();
                if (c != null && colliderBoss != null) Physics2D.IgnoreCollision(c, colliderBoss);
                Destroy(s, tempoSpike);
            }
            return;
        }

        for (int i = 0; i < quantidadeSpikes; i++)
        {
            float p = (float)i / (quantidadeSpikes - 1);
            Vector2 pos = Vector2.Lerp(inicio, fim, p);
            GameObject spike = Instantiate(prefabSpike, pos, Quaternion.identity);
            if (spike != null)
            {
                Collider2D c = spike.GetComponent<Collider2D>();
                if (c != null && colliderBoss != null) Physics2D.IgnoreCollision(c, colliderBoss);
                Destroy(spike, tempoSpike);
            }
        }
    }

    IEnumerator AtirarDaExtremidade()
    {
        ocupado = true;

        // Decide qual extremidade usar: aleatório entre esquerda/direita
        bool usarDireita = Random.value > 0.5f;
        Transform alvoExtremo = usarDireita ? pontoDireita : pontoEsquerda;
        if (alvoExtremo == null)
        {
            ocupado = false;
            estadoAtual = Estado.Perseguindo;
            yield break;
        }

        // Move suavemente até a extremidade (somente X)
        Vector3 inicio = transform.position;
        Vector3 destino = new Vector3(alvoExtremo.position.x, posicaoInicial.y, transform.position.z);
        float t = 0f;
        while (t < tempoMoverParaExtremo)
        {
            transform.position = Vector3.Lerp(inicio, destino, t / tempoMoverParaExtremo);
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = destino;

        // Ajusta facing para mirar no player (flipX ao invés de localScale)
        if (jogador != null && spriteRenderer != null)
            spriteRenderer.flipX = jogador.position.x >= transform.position.x;

        // Anima atirar
        if (animator != null) animator.SetTrigger("Atirar");

        // Atira X projéteis em direção ao jogador atual
        for (int i = 0; i < quantidadeProjetisAtirar; i++)
        {
            if (prefabProjetil == null) break;

            Vector3 spawnPos = (pontoTiro != null) ? pontoTiro.position : transform.position;
            GameObject proj = Instantiate(prefabProjetil, spawnPos, Quaternion.identity);

            // Ignora colisão entre o projétil e o boss para evitar destruição imediata
            Collider2D colliderBoss = GetComponent<Collider2D>();
            Collider2D projCollider = proj.GetComponent<Collider2D>();
            if (projCollider != null && colliderBoss != null)
            {
                Physics2D.IgnoreCollision(projCollider, colliderBoss);
            }

            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null && jogador != null)
            {
                Vector2 dir = (jogador.position - spawnPos).normalized;
                rbProj.linearVelocity = dir * velocidadeProjetil; // usar propriedade correta
                float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                proj.transform.rotation = Quaternion.Euler(0, 0, angulo);
            }

            // Se prefab não tiver Rigidbody, ainda tenta rotacionar pra aparência
            if (rbProj == null && jogador != null)
            {
                Vector2 dir = (jogador.position - spawnPos).normalized;
                float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                proj.transform.rotation = Quaternion.Euler(0, 0, angulo);
            }

            yield return new WaitForSeconds(intervaloEntreTiros);
        }

        // pequeno delay pós-ataque
        yield return new WaitForSeconds(0.2f);

        ocupado = false;
        estadoAtual = Estado.Perseguindo;
    }

    void AtivarParedes()
    {
        // Ativa visualmente e deixa ABERTAS (transparentes e isTrigger=true) para o começo da luta
        if (leftWall != null)
        {
            leftWall.MakeTransparentOpen();
        }
        if (rightWall != null)
        {
            rightWall.MakeTransparentOpen();
        }

        // Caso você queira garantir que o GameObject esteja ativo:
        if (paredeEsquerda != null) paredeEsquerda.SetActive(true);
        if (paredeDireita != null) paredeDireita.SetActive(true);
    }

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto) return;
        if (fonte != null && fonte.CompareTag("Spike")) return; // ignora dano vindo de spike

        vidaAtual -= dano;
        Debug.Log($"Boss tomou {dano} de dano. Vida: {vidaAtual}");

        if (vidaAtual <= 0)
            Morrer();
    }

    void Morrer()
    {
        morto = true;
        StopAllCoroutines();

        // quando boss morre, restaura estado original das paredes (permitir atravessar conforme origem)
        if (leftWall != null) leftWall.RestoreOriginal();
        if (rightWall != null) rightWall.RestoreOriginal();

        // tocar animação de morte aqui se quiser (notificar animator)
        Destroy(gameObject);
    }

    // Quando colidir/trigger com player: causar dano por contato (com cooldown por collider)
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealContactDamage(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealContactDamage(collision.collider);
    }

    private void TryDealContactDamage(Collider2D target)
    {
        if (target == null || !target.CompareTag("Player") || morto) return;

        float now = Time.time;
        if (ultimoDanoPorCollider.TryGetValue(target, out float lastTime))
        {
            if (now - lastTime < cooldownDanoContato) return;
        }

        IDamageable dmg = target.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(danoAoTocar, gameObject);
            ultimoDanoPorCollider[target] = now;
        }
        else
        {
            // alternativa: registrar para cooldown mesmo sem aplicar dano
            ultimoDanoPorCollider[target] = now;
        }
    }
}