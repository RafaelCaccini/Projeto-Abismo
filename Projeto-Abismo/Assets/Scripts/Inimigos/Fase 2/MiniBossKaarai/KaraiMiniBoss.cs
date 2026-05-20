using UnityEngine;
using System.Collections;

public class KaraiBoss : MonoBehaviour
{
    // =========================================
    // REFERÊNCIAS
    // =========================================

    [Header("ROOTS")]
    [SerializeField] private Transform visual;

    [SerializeField] private Transform spawnRoot;

    [SerializeField] private Transform dashRoot;

    [SerializeField] private Rigidbody2D rb;

    // =========================================
    // PREFABS
    // =========================================

    [Header("PREFABS")]
    [SerializeField] private GameObject passaroPrefab;

    [SerializeField] private GameObject fireballPrefab;

    [SerializeField] private GameObject flameRainPrefab;

    // =========================================
    // DASH
    // =========================================

    [Header("DASH")]
    [SerializeField] private GameObject hitboxDash;

    [SerializeField] private float velocidadeDash = 30f;

    [SerializeField] private float tempoDash = 0.4f;

    // =========================================
    // VIDA
    // =========================================

    [Header("VIDA")]
    [SerializeField] private int vidaMaxima = 50;

    private int vidaAtual;

    // =========================================
    // ATAQUES
    // =========================================

    [Header("TEMPOS")]
    [SerializeField] private float cooldownAtaque = 2f;

    [SerializeField] private float cooldownFase2 = 1f;

    [SerializeField] private float intervaloChuva = 2f;

    // =========================================
    // CHUVA
    // =========================================

    [Header("CHUVA")]
    [SerializeField] private Transform chuvaLeft;

    [SerializeField] private Transform chuvaRight;

    // =========================================
    // CONTROLE
    // =========================================

    private Transform player;

    private bool olhandoDireita = true;

    private bool atacando;

    private bool morto;

    private bool fase2;

    // =========================================
    // SPAWN POINTS
    // =========================================

    private Transform birdCenter;

    private Transform birdUp;

    private Transform birdDown;

    private Transform fireballSpawn;

    private Transform dashLeft;

    private Transform dashRight;

    // =========================================
    // START
    // =========================================

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CriarEstruturaAutomatica();
    }

    private void Start()
    {
        vidaAtual = vidaMaxima;

        EncontrarPlayer();

        if (hitboxDash != null)
            hitboxDash.SetActive(false);

        StartCoroutine(RotinaBoss());

        StartCoroutine(ControlarFase2());

        Debug.Log("🔥 KARAI INICIADO");
    }

    // =========================================
    // UPDATE
    // =========================================

    private void Update()
    {
        if (morto)
            return;

        if (player == null)
        {
            EncontrarPlayer();
            return;
        }

        if (!atacando)
        {
            VirarPlayer();
        }
    }

    // =========================================
    // AUTO SETUP
    // =========================================

    void CriarEstruturaAutomatica()
    {
        // SPAWN ROOT
        if (spawnRoot == null)
        {
            GameObject s =
                new GameObject("SpawnPoints");

            s.transform.SetParent(transform);

            s.transform.localPosition =
                Vector3.zero;

            spawnRoot = s.transform;
        }

        // DASH ROOT
        if (dashRoot == null)
        {
            GameObject d =
                new GameObject("DashPoints");

            d.transform.SetParent(transform);

            d.transform.localPosition =
                Vector3.zero;

            dashRoot = d.transform;
        }

        // BIRDS
        birdCenter =
            CriarPonto(
                "BirdCenter",
                spawnRoot,
                new Vector3(2f, 0f)
            );

        birdUp =
            CriarPonto(
                "BirdUp",
                spawnRoot,
                new Vector3(2f, 2f)
            );

        birdDown =
            CriarPonto(
                "BirdDown",
                spawnRoot,
                new Vector3(2f, -2f)
            );

        // FIREBALL
        fireballSpawn =
            CriarPonto(
                "FireballSpawn",
                spawnRoot,
                new Vector3(2f, 0f)
            );

        // DASH
        dashLeft =
            CriarPonto(
                "DashLeft",
                dashRoot,
                new Vector3(-8f, 0f)
            );

        dashRight =
            CriarPonto(
                "DashRight",
                dashRoot,
                new Vector3(8f, 0f)
            );
    }

    Transform CriarPonto(
        string nome,
        Transform pai,
        Vector3 pos
    )
    {
        Transform t =
            pai.Find(nome);

        if (t != null)
            return t;

        GameObject g =
            new GameObject(nome);

        g.transform.SetParent(pai);

        g.transform.localPosition = pos;

        return g.transform;
    }

    // =========================================
    // PLAYER
    // =========================================

    void EncontrarPlayer()
    {
        PlayerController pc =
            FindFirstObjectByType<PlayerController>();

        if (pc != null)
            player = pc.transform;
    }

    // =========================================
    // FLIP
    // =========================================

    void VirarPlayer()
    {
        if (player.position.x > transform.position.x)
        {
            if (!olhandoDireita)
                Flip();
        }
        else
        {
            if (olhandoDireita)
                Flip();
        }
    }

    void Flip()
    {
        olhandoDireita = !olhandoDireita;

        Vector3 escala =
            visual.localScale;

        escala.x *= -1;

        visual.localScale =
            escala;
    }

    // =========================================
    // ROTINA
    // =========================================

    IEnumerator RotinaBoss()
    {
        yield return new WaitForSeconds(2f);

        while (!morto)
        {
            int atk =
                Random.Range(0, 3);

            switch (atk)
            {
                case 0:
                    yield return AtaqueDash();
                    break;

                case 1:
                    yield return AtaquePassaros();
                    break;

                case 2:
                    yield return AtaqueFireballs();
                    break;
            }

            yield return new WaitForSeconds(
                fase2
                ? cooldownFase2
                : cooldownAtaque
            );
        }
    }

    // =========================================
    // DASH
    // =========================================

    IEnumerator AtaqueDash()
    {
        atacando = true;

        Transform alvo =
            olhandoDireita
            ? dashRight
            : dashLeft;

        if (hitboxDash != null)
            hitboxDash.SetActive(true);

        float t = 0f;

        while (t < tempoDash)
        {
            t += Time.deltaTime;

            transform.position =
                Vector2.MoveTowards(
                    transform.position,
                    alvo.position,
                    velocidadeDash * Time.deltaTime
                );

            yield return null;
        }

        if (hitboxDash != null)
            hitboxDash.SetActive(false);

        Flip();

        atacando = false;
    }

    // =========================================
    // PASSAROS
    // =========================================

    IEnumerator AtaquePassaros()
    {
        atacando = true;

        Instantiate(
            passaroPrefab,
            birdCenter.position,
            Quaternion.identity
        );

        Instantiate(
            passaroPrefab,
            birdUp.position,
            Quaternion.identity
        );

        Instantiate(
            passaroPrefab,
            birdDown.position,
            Quaternion.identity
        );

        yield return new WaitForSeconds(1f);

        atacando = false;
    }

    // =========================================
    // FIREBALLS
    // =========================================

    IEnumerator AtaqueFireballs()
    {
        atacando = true;

        for (int i = 0; i < 5; i++)
        {
            Instantiate(
                fireballPrefab,
                fireballSpawn.position,
                Quaternion.identity
            );

            yield return new WaitForSeconds(0.3f);
        }

        atacando = false;
    }

    // =========================================
    // FASE 2
    // =========================================

    IEnumerator ControlarFase2()
    {
        while (!morto)
        {
            if (
                !fase2 &&
                vidaAtual <= vidaMaxima / 2
            )
            {
                fase2 = true;

                Debug.Log("🔥 FASE 2");
            }

            if (fase2)
            {
                SpawnarChuva();
            }

            yield return new WaitForSeconds(
                intervaloChuva
            );
        }
    }

    // =========================================
    // CHUVA
    // =========================================

    void SpawnarChuva()
    {
        if (
            flameRainPrefab == null ||
            chuvaLeft == null ||
            chuvaRight == null
        )
            return;

        float x =
            Random.Range(
                chuvaLeft.position.x,
                chuvaRight.position.x
            );

        Vector3 pos =
            new Vector3(
                x,
                chuvaLeft.position.y,
                0f
            );

        Instantiate(
            flameRainPrefab,
            pos,
            Quaternion.identity
        );
    }

    // =========================================
    // DANO
    // =========================================

    public void TakeDamage(
        int amount,
        GameObject source
    )
    {
        if (morto)
            return;

        vidaAtual -= amount;

        Debug.Log(
            "💥 KARAI VIDA: "
            + vidaAtual
        );

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    // =========================================
    // MORTE
    // =========================================

    void Morrer()
    {
        morto = true;

        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;

        Debug.Log("☠️ KARAI MORREU");

        Destroy(gameObject);
    }
}