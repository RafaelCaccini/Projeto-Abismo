// Projeto-Abismo/Assets/Scripts/Inimigos/Mini Boss fase 1/MiniBossFase1.cs
using System.Collections;
using UnityEngine;

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
    public float velocidade = 3f;
    public float velocidadePerseguicao = 6f;
    public float alturaPulo = 1.5f;
    public float tempoPulo = 0.5f;

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

    [Header("Projetil")]
    public GameObject prefabProjetil;
    public Transform pontoTiro;
    public float intervaloEntreTiros = 0.3f;
    public float velocidadeProjetil = 8f;

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

    void Start()
    {
        posicaoInicial = transform.position;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        vidaAtual = vidaMaxima;

        if (jogador == null)
            jogador = GameObject.FindGameObjectWithTag("Player").transform;

        if (paredeEsquerda != null)
            paredeEsquerda.SetActive(false);

        if (paredeDireita != null)
            paredeDireita.SetActive(false);

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (morto)
            return;

        if (jogador == null)
            return;

        // Verifica se o player está no alcance
        float distancia = Vector2.Distance(transform.position, jogador.position);

        if (!lutaComecou && distancia <= alcanceDeteccao)
        {
            lutaComecou = true;
            AtivarParedes();
            //IniciarRotinasAtaques();
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

        // Movimento suave com limites de arena
        float novoX = Mathf.Clamp(transform.position.x + direcao.x * velocidadePerseguicao * Time.deltaTime,
                                pontoEsquerda.position.x,
                                pontoDireita.position.x);

        transform.position = new Vector3(novoX, posicaoInicial.y, 0);

        // Virar conforme a direção
        if (direcao.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void GerenciarAtaques()
    {
        // Atualiza timer para ataques
        tempoUltimoAtaque += Time.deltaTime;

        // Verifica se é hora de atacar
        if (tempoUltimoAtaque >= 3f)
        {
            EscolherAtaqueAleatorio();
            tempoUltimoAtaque = 0f;
        }
    }

    void EscolherAtaqueAleatorio()
    {
        int escolha = Random.Range(0, 3);

        switch (escolha)
        {
            case 0: // Pular
                if (!pulando && !ocupado)
                {
                    animator.SetTrigger("Pulo");
                    StartCoroutine(Jump());
                }
                break;

            case 1: // Atirar
                if (!pulando && !ocupado)
                {
                    animator.SetTrigger("Atirar");
                    StartCoroutine(Shoot());
                }
                break;

            case 2: // Lançar espinhos
                if (!pulando && !ocupado)
                {
                    animator.SetTrigger("Pisao");
                    StartCoroutine(SpawnSpikes());
                }
                break;
        }
    }

    IEnumerator Jump()
    {
        ocupado = true;
        pulando = true;

        // Movimento de pulo suave
        float tempo = 0f;
        Vector2 inicio = transform.position;
        Vector2 destino = new Vector2(transform.position.x, posicaoInicial.y + alturaPulo);

        while (tempo < tempoPulo)
        {
            float t = tempo / tempoPulo;
            Vector2 pos = Vector2.Lerp(inicio, destino, t);
            transform.position = pos;
            tempo += Time.deltaTime;
            yield return null;
        }

        pulando = false;
        ocupado = false;
    }

    IEnumerator Shoot()
    {
        ocupado = true;

        // Atira em sequência
        for (int i = 0; i < 3 && !pulando; i++)
        {
            if (prefabProjetil == null || pontoTiro == null)
                break;

            Vector2 direcao = (jogador.position - pontoTiro.position).normalized;
            GameObject proj = Instantiate(prefabProjetil, pontoTiro.position, Quaternion.identity);

            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null)
            {
                rbProj.linearVelocity = direcao * velocidadeProjetil;
            }

            float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.Euler(0, 0, angulo);

            Destroy(proj, 5f);

            yield return new WaitForSeconds(intervaloEntreTiros);
        }

        ocupado = false;
    }

    IEnumerator SpawnSpikes()
    {
        ocupado = true;
        animator.SetTrigger("Pisao");

        // Lança espinhos no chão
        Transform inicio = inicioChao;
        Transform fim = fimChao;

        if (inicio == null || fim == null || prefabSpike == null)
            yield break;

        Collider2D colliderBoss = GetComponent<Collider2D>();

        for (int i = 0; i < quantidadeSpikes; i++)
        {
            float t = (float)i / (quantidadeSpikes - 1);
            Vector2 pos = Vector2.Lerp(inicio.position, fim.position, t);

            GameObject spike = Instantiate(prefabSpike, pos, Quaternion.identity);
            Collider2D colliderSpike = spike.GetComponent<Collider2D>();

            if (colliderSpike != null && colliderBoss != null)
                Physics2D.IgnoreCollision(colliderSpike, colliderBoss);

            Destroy(spike, tempoSpike);
        }

        ocupado = false;
    }

    void AtivarParedes()
    {
        if (paredeEsquerda != null) paredeEsquerda.SetActive(true);
        if (paredeDireita != null) paredeDireita.SetActive(true);
    }

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto || fonte.CompareTag("Spike"))
            return;

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

        Destroy(gameObject);
    }
}