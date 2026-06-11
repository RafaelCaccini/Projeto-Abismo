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
    public float velocidade = 3f;
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

    [Header("Vida")]
    public int vidaMaxima = 20;

    private int vidaAtual;

    private bool lutaComecou;
    private bool morto;
    private float distanciaMinimaDoPlayer = 1.5f;
    private bool podeAndar = true;

    private bool pulando;
    private float yInicial;
    private Rigidbody2D rb;



    void Start()
    {
        yInicial = transform.position.y;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

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

        if (podeAndar && !pulando)
        {
            MovimentoInteligente();
        }
    }

    void MovimentoInteligente()
    {
        float distancia =
            Vector2.Distance(
                transform.position,
                jogador.position
            );

        if (distancia <= distanciaMinimaDoPlayer)
            return;

        float direcao =
            jogador.position.x >
            transform.position.x
            ? 1f
            : -1f;

        Vector3 pos =
            transform.position;

        pos.x +=
            direcao *
            velocidade *
            Time.deltaTime;

        pos.x =
            Mathf.Clamp(
                pos.x,
                pontoEsquerda.position.x,
                pontoDireita.position.x
            );

        transform.position = pos;
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

        Vector2 inicio = transform.position;

        float direcao =
            jogador.position.x >
            transform.position.x
            ? 1f
            : -1f;

        float distanciaPulo = 2f;

        Vector2 destino =
            new Vector2(
                inicio.x +
                (direcao * distanciaPulo),
                yInicial
            );

        destino.x =
            Mathf.Clamp(
                destino.x,
                pontoEsquerda.position.x,
                pontoDireita.position.x
            );

        float tempo = 0f;

        while (tempo < tempoPulo)
        {
            float t =
                tempo / tempoPulo;

            float altura =
                Mathf.Sin(
                    t * Mathf.PI
                ) *
                alturaPulo;

            Vector2 pos =
                Vector2.Lerp(
                    inicio,
                    destino,
                    t
                );

            pos.y =
                yInicial +
                altura;

            transform.position = pos;

            tempo += Time.deltaTime;

            yield return null;
        }

        transform.position =
            new Vector3(
                destino.x,
                yInicial,
                transform.position.z
            );

        pulando = false;
    }

    // =====================================
    // SPIKES
    // =====================================

    IEnumerator RotinaSpikes()
    {
        while (!morto)
        {
            yield return new WaitForSeconds(3.5f);

            bool teto = false;

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

            Physics2D.IgnoreCollision(
    spike.GetComponent<Collider2D>(),
    GetComponent<Collider2D>());

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

        if (
            fonte != null &&
            fonte.CompareTag("Spike")
        )
        {
            return;
        }

        vidaAtual -= dano;

        Debug.Log(
            "Boss tomou dano. Vida: " +
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