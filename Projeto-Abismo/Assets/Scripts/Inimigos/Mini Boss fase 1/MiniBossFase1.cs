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

    private bool lutaComecou;
    private bool morto;
    private bool ocupado;
    private bool pulando;
    private Vector3 posicaoInicial;
    private Rigidbody2D rb;
    private Animator animator;
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
        // Usamos movimento por interpolação (não física) então mantemos gravidade 0 para evitar conflitos
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }

        vidaAtual = vidaMaxima;

        if (jogador == null)
            jogador = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (paredeEsquerda != null)
            paredeEsquerda.SetActive(false);

        if (paredeDireita != null)
            paredeDireita.SetActive(false);

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (morto) return;
        if (jogador == null) return;

        float distancia = Vector2.Distance(transform.position, jogador.position);

        if (!lutaComecou && distancia <= alcanceDeteccao)
        {
            lutaComecou = true;
            AtivarParedes();
            Debug.Log("LUTA INICIADA");
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

    void MovimentoInteligente()
    {
        Vector2 direcao = (jogador.position - transform.position).normalized;

        // Movimento suave com limites de arena (apenas no eixo X)
        float novoX = Mathf.Clamp(transform.position.x + direcao.x * velocidadePerseguicao * Time.deltaTime,
                                pontoEsquerda.position.x,
                                pontoDireita.position.x);

        transform.position = new Vector3(novoX, posicaoInicial.y, 0);

        // Virar conforme a direção
        if (direcao.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (direcao.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void GerenciarAtaques()
    {
        tempoUltimoAtaque += Time.deltaTime;

        // Decide ataque a cada ~3s (ajuste conforme quiser)
        if (estadoAtual == Estado.Atacando && tempoUltimoAtaque >= 2.8f)
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

            // mantém facing adequado
            if (end.x > start.x) transform.localScale = new Vector3(1, 1, 1);
            else transform.localScale = new Vector3(-1, 1, 1);

            t += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
    }

    IEnumerator PisaoSpawnSpikes()
    {
        ocupado = true;
        animator.SetTrigger("Pisao");

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

        // Ajusta facing para mirar no player
        if (jogador != null)
            transform.localScale = (jogador.position.x >= transform.position.x) ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);

        // Anima atirar
        animator.SetTrigger("Atirar");

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

        if (paredeEsquerda != null) paredeEsquerda.SetActive(false);
        if (paredeDireita != null) paredeDireita.SetActive(false);

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