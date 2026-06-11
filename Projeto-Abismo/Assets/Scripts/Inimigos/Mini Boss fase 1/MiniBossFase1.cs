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
    [SerializeField] private Animator animator;

    [Header("Vida")]
    public int vidaMaxima = 20;

    private int vidaAtual;

    private bool lutaComecou;
    private bool morto;

    // =====================================
    // CONTROLE DE ESTADO
    // =====================================
    // Apenas UMA ação especial (pulo, spikes ou projetil)
    // pode acontecer por vez.
    private bool ocupado;
    private bool pulando;

    private float yInicial;
    private Rigidbody2D rb;

    [Header("Tempos de ataque")]
    public float intervaloPulo = 2f;
    public float intervaloSpikes = 4f;
    public float intervaloProjetil = 6f;

    [Header("Projetil")]
    public GameObject prefabProjetil;
    public Transform pontoTiro;

    public float intervaloEntreTiros = 0.3f;
    public float velocidadeProjetil = 8f;

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

        if (!ocupado)
        {
            MovimentoInteligente();
        }
    }

    // =====================================
    // MOVIMENTO
    // =====================================
    // Persegue o player agressivamente, encurralando-o
    // contra os limites da arena.

    void MovimentoInteligente()
    {
        float distancia =
            Vector2.Distance(
                transform.position,
                jogador.position
            );

        // Zona mínima: praticamente colado no player
        if (distancia <= 0.3f)
            return;

        float direcao =
            jogador.position.x >
            transform.position.x
            ? 1f
            : -1f;

        Vector3 pos =
            transform.position;

        float minX = Mathf.Min(pontoEsquerda.position.x, pontoDireita.position.x);
        float maxX = Mathf.Max(pontoEsquerda.position.x, pontoDireita.position.x);

        // Velocidade agressiva enquanto o player está dentro
        // do alcance de detecção (perseguição "sem piedade")
        float velAtual =
            distancia <= alcanceDeteccao
            ? velocidadePerseguicao
            : velocidade;

        float novoX =
            pos.x +
            direcao *
            velAtual *
            Time.deltaTime;

        novoX =
            Mathf.Clamp(
                novoX,
                minX,
                maxX
            );

        pos.x = novoX;

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
            StartCoroutine(RotinaProjetil());

            Debug.Log("LUTA INICIADA");
        }
    }

    // =====================================
    // PULO
    // =====================================

    IEnumerator RotinaPulo()
    {
        while (!morto)
        {
            yield return new WaitForSeconds(intervaloPulo);

            // Só pula se não estiver fazendo outra ação
            if (ocupado)
                continue;

            yield return Pular();
        }
    }

    IEnumerator Pular()
    {
        ocupado = true;
        pulando = true;

        Vector2 inicio = transform.position;

        float distanciaPlayer =
            jogador.position.x -
            transform.position.x;

        distanciaPlayer =
            Mathf.Clamp(
                distanciaPlayer,
                -3f,
                3f
            );

        float minX = Mathf.Min(pontoEsquerda.position.x, pontoDireita.position.x);
        float maxX = Mathf.Max(pontoEsquerda.position.x, pontoDireita.position.x);

        Vector2 destino =
            new Vector2(
                inicio.x + distanciaPlayer,
                yInicial
            );

        destino.x =
            Mathf.Clamp(
                destino.x,
                minX,
                maxX
            );

        float tempo = 0f;

        while (tempo < tempoPulo)
        {
            float t = tempo / tempoPulo;

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

            pos.y = yInicial + altura;

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
        ocupado = false;
    }

    // =====================================
    // SPIKES
    // =====================================

    IEnumerator RotinaSpikes()
    {
        while (!morto)
        {
            yield return new WaitForSeconds(intervaloSpikes);

            // Só pode usar espinhos se estiver no chão e livre
            if (ocupado || pulando)
                continue;

            ocupado = true;

            if (animator != null)
                animator.SetTrigger("Pisao");

            yield return new WaitForSeconds(0.6f);

            // Verifica de novo: se pulou durante a animação, cancela
            if (!pulando)
            {
                SpawnSpikes(false);
            }

            ocupado = false;
        }
    }

    // =====================================
    // PROJETIL
    // =====================================

    IEnumerator RotinaProjetil()
    {
        while (!morto)
        {
            yield return new WaitForSeconds(intervaloProjetil);

            if (ocupado || pulando)
                continue;

            ocupado = true;

            if (animator != null)
                animator.SetTrigger("Atirar");

            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < 3; i++)
            {
                if (pulando || morto)
                    break;

                AtirarProjetil();

                yield return new WaitForSeconds(intervaloEntreTiros);
            }

            ocupado = false;
        }
    }

    void AtirarProjetil()
    {
        if (prefabProjetil == null)
            return;

        if (pontoTiro == null)
            return;

        if (jogador == null)
            return;

        Vector2 direcao =
            (jogador.position -
            pontoTiro.position).normalized;

        GameObject proj =
            Instantiate(
                prefabProjetil,
                pontoTiro.position,
                Quaternion.identity
            );

        Rigidbody2D rbProj =
            proj.GetComponent<Rigidbody2D>();

        if (rbProj != null)
        {
            rbProj.linearVelocity =
                direcao *
                velocidadeProjetil;
        }

        float angulo =
            Mathf.Atan2(
                direcao.y,
                direcao.x
            ) * Mathf.Rad2Deg;

        proj.transform.rotation =
            Quaternion.Euler(
                0,
                0,
                angulo
            );

        Destroy(proj, 5f);
    }

    // =====================================
    // SPAWN SPIKES
    // =====================================

    void SpawnSpikes(bool teto)
    {
        Transform inicio =
            teto ? inicioTeto : inicioChao;

        Transform fim =
            teto ? fimTeto : fimChao;

        if (inicio == null || fim == null || prefabSpike == null)
            return;

        Collider2D colliderBoss = GetComponent<Collider2D>();

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

            Collider2D colliderSpike =
                spike.GetComponent<Collider2D>();

            if (colliderSpike != null && colliderBoss != null)
            {
                Physics2D.IgnoreCollision(
                    colliderSpike,
                    colliderBoss
                );
            }

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