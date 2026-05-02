using System.Collections;
using UnityEngine;

public class MiniBoss : MonoBehaviour, IDamageable
{
    [Header("referencias")]
    public Transform jogador;

    [Header("deteccao")]
    public float alcanceDeteccao = 8f;

    [Header("ritmo combate")]
    public float tempoEntreAtaques = 1.2f;
    private bool primeiroAtaque = true;

    [Header("movimento")]
    public float velocidadeMovimento = 8f;
    public float alturaPulo = 3f;

    [Header("arena")]
    public Transform pontoEsquerda;
    public Transform pontoDireita;

    [Header("spikes")]
    public GameObject prefabSpike;

    [Header("spikes chao")]
    public Transform inicioChao;
    public Transform fimChao;
    public int quantidadeChao = 5;

    [Header("spikes teto")]
    public Transform inicioTeto;
    public Transform fimTeto;
    public int quantidadeTeto = 5;

    [Header("tempo spikes")]
    public float tempoVidaSpike = 2f;
    public float delaySpawnSpike = 0.1f;

    [Header("vida")]
    public int vidaMaxima = 20;
    private int vidaAtual;
    private bool morto;

    [Header("dano contato")]
    public int danoContato = 1;
    public float cooldownContato = 1f;
    private float ultimoDanoContato;

    private int direcaoX = 1;
    private bool atacando;
    private bool jogadorDetectado;
    private int ultimoAtaque = -1;

    void Start()
    {
        vidaAtual = vidaMaxima;

        if (jogador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                jogador = p.transform;
            else
                Debug.LogError("Player nao encontrado");
        }
    }

    void Update()
    {
        if (morto || jogador == null) return;

        float distancia = Vector2.Distance(transform.position, jogador.position);

        if (distancia <= alcanceDeteccao)
            jogadorDetectado = true;

        if (jogadorDetectado && !atacando)
            StartCoroutine(RotinaAtaque());
    }

    IEnumerator RotinaAtaque()
    {
        atacando = true;

        if (!primeiroAtaque)
            yield return new WaitForSeconds(tempoEntreAtaques);

        int ataque = EscolherAtaque();

        switch (ataque)
        {
            case 1:
                yield return AtaqueParabola();
                break;

            case 2:
                yield return AtaqueDiagonal();
                break;

            case 3:
                yield return AtaqueEspinhos(true);
                break;

            case 4:
                yield return AtaqueEspinhos(false);
                break;
        }

        primeiroAtaque = false;
        ultimoAtaque = ataque;
        atacando = false;
    }

    int EscolherAtaque()
    {
        int atk;
        do
        {
            atk = Random.Range(1, 5);
        } while (atk == ultimoAtaque);

        return atk;
    }

    // ================= ATAQUES =================

    IEnumerator AtaqueParabola()
    {
        if (pontoEsquerda == null || pontoDireita == null) yield break;

        Vector2 inicio = transform.position;
        Vector2 fim = LadoOposto();

        float duracao = 0.7f;
        float tempo = 0;

        while (tempo < duracao)
        {
            float t = tempo / duracao;
            transform.position = Parabola(inicio, fim, alturaPulo, t);

            tempo += Time.deltaTime;
            yield return null;
        }

        transform.position = fim;
        LimitarNaArena();
    }

    IEnumerator AtaqueDiagonal()
    {
        float duracao = 1.2f;
        float tempo = 0;

        Vector2 dir = new Vector2(direcaoX, 1).normalized;

        while (tempo < duracao)
        {
            transform.Translate(dir * velocidadeMovimento * Time.deltaTime);

            // bounce vertical
            if (transform.position.y >= 4f)
                dir.y = -1;
            else if (transform.position.y <= 0.5f)
                dir.y = 1;

            LimitarNaArena();

            tempo += Time.deltaTime;
            yield return null;
        }

        yield return AtaqueParabola();
    }

    IEnumerator AtaqueEspinhos(bool chao)
    {
        yield return SpawnSpikes(chao);
    }

    // ================= SPIKES =================

    IEnumerator SpawnSpikes(bool chao)
    {
        Transform inicio = chao ? inicioChao : inicioTeto;
        Transform fim = chao ? fimChao : fimTeto;
        int qtd = chao ? quantidadeChao : quantidadeTeto;

        if (inicio == null || fim == null || prefabSpike == null)
            yield break;

        GameObject[] spikes = new GameObject[qtd];

        for (int i = 0; i < qtd; i++)
        {
            float t = qtd == 1 ? 0.5f : (float)i / (qtd - 1);
            Vector2 pos = Vector2.Lerp(inicio.position, fim.position, t);

            GameObject s = Instantiate(prefabSpike, pos, Quaternion.identity);

            if (!chao)
                s.transform.rotation = Quaternion.Euler(0, 0, 180);

            spikes[i] = s;

            yield return new WaitForSeconds(delaySpawnSpike);
        }

        yield return new WaitForSeconds(tempoVidaSpike);

        foreach (var s in spikes)
            if (s != null) Destroy(s);
    }

    // ================= MOVIMENTO =================

    void LimitarNaArena()
    {
        if (pontoEsquerda == null || pontoDireita == null) return;

        float minX = pontoEsquerda.position.x;
        float maxX = pontoDireita.position.x;

        Vector3 pos = transform.position;

        if (pos.x <= minX)
        {
            pos.x = minX;
            direcaoX = 1;
        }
        else if (pos.x >= maxX)
        {
            pos.x = maxX;
            direcaoX = -1;
        }

        transform.position = pos;
    }

    Vector2 Parabola(Vector2 inicio, Vector2 fim, float altura, float t)
    {
        float p = t * 2 - 1;
        float y = altura * (1 - p * p);
        return Vector2.Lerp(inicio, fim, t) + new Vector2(0, y);
    }

    Vector2 LadoOposto()
    {
        float dE = Mathf.Abs(transform.position.x - pontoEsquerda.position.x);
        float dD = Mathf.Abs(transform.position.x - pontoDireita.position.x);

        return dE < dD ? pontoDireita.position : pontoEsquerda.position;
    }

    // ================= VIDA =================

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (morto) return;

        vidaAtual -= dano;

        Debug.Log($"boss tomou {dano} de {fonte.name} | vida: {vidaAtual}");

        if (vidaAtual <= 0)
            Morrer();
    }

    void Morrer()
    {
        morto = true;
        StopAllCoroutines();
        Destroy(gameObject);
    }

    // ================= CONTATO =================

    void OnCollisionStay2D(Collision2D col)
    {
        DarDano(col.gameObject);
    }

    void OnTriggerStay2D(Collider2D col)
    {
        DarDano(col.gameObject);
    }

    void DarDano(GameObject alvo)
    {
        if (Time.time < ultimoDanoContato + cooldownContato) return;

        IDamageable dmg = alvo.GetComponent<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(danoContato, gameObject);
            ultimoDanoContato = Time.time;

            Debug.Log($"boss deu {danoContato} em {alvo.name}");
        }
    }
}