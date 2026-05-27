using System.Collections;
using UnityEngine;

public class MiniBoss : MonoBehaviour, IDamageable
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
    public float velocidade = 8f;
    public float alturaPulo = 4f;
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

    [Header("Vida")]
    public int vidaMaxima = 20;

    private int vidaAtual;

    private bool lutaComecou;
    private bool morto;

    private bool pulando;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        vidaAtual = vidaMaxima;

        if (jogador == null)
        {
            GameObject p =
                GameObject.FindGameObjectWithTag("Player");

            if (p != null)
                jogador = p.transform;
        }

        if (paredeEsquerda != null)
            paredeEsquerda.SetActive(false);

        if (paredeDireita != null)
            paredeDireita.SetActive(false);
    }

    void Update()
    {
        if (morto)
            return;

        if (jogador == null)
            return;

        DetectarJogador();

        if (!lutaComecou)
            return;

        VirarPlayer();

        MovimentoConstante();
    }

    // =====================================
    // DETECTAR PLAYER
    // =====================================

    void DetectarJogador()
    {
        if (lutaComecou)
            return;

        float dist =
            Vector2.Distance(
                transform.position,
                jogador.position
            );

        if (dist <= alcanceDeteccao)
        {
            lutaComecou = true;

            if (paredeEsquerda != null)
                paredeEsquerda.SetActive(true);

            if (paredeDireita != null)
                paredeDireita.SetActive(true);

            StartCoroutine(RotinaPulo());

            StartCoroutine(RotinaSpikes());

            Debug.Log("LUTA INICIADA");
        }
    }

    // =====================================
    // MOVIMENTO
    // =====================================

    void MovimentoConstante()
    {
        if (pulando)
            return;

        float dir =
            jogador.position.x >
            transform.position.x
            ? 1
            : -1;

        Vector3 pos =
            transform.position;

        pos.x +=
            dir *
            velocidade *
            Time.deltaTime;

        float minX =
            pontoEsquerda.position.x;

        float maxX =
            pontoDireita.position.x;

        pos.x =
            Mathf.Clamp(
                pos.x,
                minX,
                maxX
            );

        transform.position = pos;
    }

    // =====================================
    // PULO
    // =====================================

    IEnumerator RotinaPulo()
    {
        while (!morto)
        {
            yield return new WaitForSeconds(2f);

            yield return Pular();
        }
    }

    IEnumerator Pular()
    {
        pulando = true;

        Vector2 inicio =
            transform.position;

        Vector2 destino =
            jogador.position.x >
            transform.position.x
            ? pontoDireita.position
            : pontoEsquerda.position;

        float tempo = 0f;

        while (tempo < tempoPulo)
        {
            float t =
                tempo / tempoPulo;

            float y =
                alturaPulo *
                4 *
                t *
                (1 - t);

            transform.position =
                Vector2.Lerp(
                    inicio,
                    destino,
                    t
                ) +
                Vector2.up * y;

            tempo += Time.deltaTime;

            yield return null;
        }

        transform.position = destino;

        pulando = false;
    }

    // =====================================
    // SPIKES
    // =====================================

    IEnumerator RotinaSpikes()
    {
        while (!morto)
        {
            yield return new WaitForSeconds(1.5f);

            bool teto =
                Random.Range(0, 2) == 0;

            SpawnSpikes(teto);
        }
    }

    void SpawnSpikes(bool teto)
    {
        Transform inicio =
            teto ? inicioTeto : inicioChao;

        Transform fim =
            teto ? fimTeto : fimChao;

        for (int i = 0; i < quantidadeSpikes; i++)
        {
            float t =
                quantidadeSpikes == 1
                ? 0.5f
                : (float)i /
                  (quantidadeSpikes - 1);

            Vector2 pos =
                Vector2.Lerp(
                    inicio.position,
                    fim.position,
                    t
                );

            GameObject spike =
                Instantiate(
                    prefabSpike,
                    pos,
                    Quaternion.identity
                );

            if (teto)
            {
                spike.transform.rotation =
                    Quaternion.Euler(0, 0, 180);
            }

            Destroy(
                spike,
                tempoSpike
            );
        }
    }

    // =====================================
    // VIRAR
    // =====================================

    void VirarPlayer()
    {
        Vector3 scale =
            transform.localScale;

        if (
            jogador.position.x >
            transform.position.x
        )
        {
            scale.x =
                Mathf.Abs(scale.x);
        }
        else
        {
            scale.x =
                -Mathf.Abs(scale.x);
        }

        transform.localScale = scale;
    }

    // =====================================
    // VIDA
    // =====================================

    public void TakeDamage(
        int dano,
        GameObject fonte
    )
    {
        if (morto)
            return;

        vidaAtual -= dano;

        Debug.Log(
            "Boss tomou dano: " +
            vidaAtual
        );

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    void Morrer()
    {
        morto = true;

        StopAllCoroutines();

        if (paredeEsquerda != null)
            paredeEsquerda.SetActive(false);

        if (paredeDireita != null)
            paredeDireita.SetActive(false);

        Destroy(gameObject);
    }
}