using UnityEngine;
using System.Collections;

public class BossFase02 : MonoBehaviour, IDamageable, IAtordoavel
{
    // =====================================
    // VIDA
    // =====================================

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 10;
    private int vidaAtual;

    // =====================================
    // MOVIMENTO
    // =====================================

    [Header("Movimento")]
    [SerializeField] private Transform pontoEsquerdo;
    [SerializeField] private Transform pontoDireito;
    [SerializeField] private float velocidadeVoo = 4f;
    [SerializeField] private float tempoParadaExtremidade = 1.5f;
    [SerializeField] private float alturaVoo = 5f;

    // =====================================
    // ATAQUE 01 — BOLA DE FOGO
    // =====================================

    [Header("Ataque 01 — Bola de Fogo")]
    [SerializeField] private GameObject bolaFogoPrefab;
    [SerializeField] private Transform pontoDisparo;
    [SerializeField] private float intervaloDisparo = 2f;
    [SerializeField] private int quantidadeBolas = 1;
    [SerializeField] private float velocidadeBola = 6f;
    [SerializeField] private float anguloSpread = 15f; // ângulo entre bolas se > 1

    // =====================================
    // ATAQUE 02 — GÊISERES
    // =====================================

    [Header("Ataque 02 — Gêiseres")]
    [SerializeField] private GeiserFogo[] geiseres;
    [SerializeField] private float intervaloGeiser = 3f;

    // ========================================
    // ATORDOAMENTO
    // ========================================

    [Header("Atordoamento")]
    [SerializeField] private float duracaoVulneravel = 3f;

    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("Referências")]
    [SerializeField] private Transform player;
    [SerializeField] private SpriteRenderer sr;

    // =====================================
    // ESTADO INTERNO
    // =====================================

    private enum EstadoBoss { Voando, Parado, Atordoado, Morto }
    private EstadoBoss estado = EstadoBoss.Voando;

    private Transform destinoAtual;
    private bool vulneravel = false;

    private Coroutine rotinaPrincipal;
    private Coroutine rotinaGeiser;
    private Coroutine rotinaDisparo;

    // =====================================
    // AWAKE / START
    // =====================================

    private void Awake()
    {
        vidaAtual = vidaMaxima;

        if (player == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        // Posiciona na altura de voo
        Vector3 pos = transform.position;
        pos.y = alturaVoo;
        transform.position = pos;
    }

    private void Start()
    {
        destinoAtual = pontoDireito;
        rotinaPrincipal = StartCoroutine(RotinaVoo());
        rotinaGeiser = StartCoroutine(RotinaGeiser());
        rotinaDisparo = StartCoroutine(RotinaDisparo());
    }

    // =====================================
    // ROTINA DE VOO
    // =====================================

    IEnumerator RotinaVoo()
    {
        while (estado != EstadoBoss.Morto)
        {
            if (estado == EstadoBoss.Atordoado)
            {
                yield return null;
                continue;
            }

            estado = EstadoBoss.Voando;

            // Vai até o destino
            while (Vector3.Distance(transform.position, DestinoPosicao()) > 0.1f)
            {
                if (estado == EstadoBoss.Atordoado || estado == EstadoBoss.Morto)
                    break;

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    DestinoPosicao(),
                    velocidadeVoo * Time.deltaTime
                );

                // Flip
                if (player != null)
                {
                    bool olhandoDireita = player.position.x > transform.position.x;
                    Vector3 scale = transform.localScale;
                    scale.x = olhandoDireita ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }

                yield return null;
            }

            if (estado == EstadoBoss.Atordoado || estado == EstadoBoss.Morto)
            {
                yield return null;
                continue;
            }

            // Para na extremidade
            estado = EstadoBoss.Parado;
            yield return new WaitForSeconds(tempoParadaExtremidade);

            // Troca destino
            destinoAtual = destinoAtual == pontoDireito ? pontoEsquerdo : pontoDireito;
        }
    }

    Vector3 DestinoPosicao()
    {
        return new Vector3(destinoAtual.position.x, alturaVoo, transform.position.z);
    }

    // =====================================
    // ROTINA DE DISPARO
    // =====================================

    IEnumerator RotinaDisparo()
    {
        while (estado != EstadoBoss.Morto)
        {
            yield return new WaitForSeconds(intervaloDisparo);

            if (estado == EstadoBoss.Atordoado || estado == EstadoBoss.Morto)
                continue;

            if (estado == EstadoBoss.Voando)
                Atirar();
        }
    }

    void Atirar()
    {
        if (bolaFogoPrefab == null || player == null) return;

        Vector3 origem = pontoDisparo != null ? pontoDisparo.position : transform.position;
        Vector2 direcaoBase = (player.position - origem).normalized;

        for (int i = 0; i < quantidadeBolas; i++)
        {
            float angulo = 0f;

            if (quantidadeBolas > 1)
            {
                float totalSpread = anguloSpread * (quantidadeBolas - 1);
                angulo = -totalSpread / 2f + anguloSpread * i;
            }

            Vector2 dir = RotacionarVetor(direcaoBase, angulo);

            GameObject bola = Instantiate(bolaFogoPrefab, origem, Quaternion.identity);
            Rigidbody2D rbBola = bola.GetComponent<Rigidbody2D>();

            if (rbBola != null)
                rbBola.linearVelocity = dir * velocidadeBola;
        }
    }

    Vector2 RotacionarVetor(Vector2 v, float graus)
    {
        float rad = graus * Mathf.Deg2Rad;
        return new Vector2(
            v.x * Mathf.Cos(rad) - v.y * Mathf.Sin(rad),
            v.x * Mathf.Sin(rad) + v.y * Mathf.Cos(rad)
        );
    }

    // =====================================
    // ROTINA DE GÊISER
    // =====================================

    IEnumerator RotinaGeiser()
    {
        while (estado != EstadoBoss.Morto)
        {
            yield return new WaitForSeconds(intervaloGeiser);

            if (estado == EstadoBoss.Atordoado || estado == EstadoBoss.Morto)
                continue;

            if (geiseres == null || geiseres.Length == 0) continue;

            // Ativa todos em sequência
            foreach (var geiser in geiseres)
            {
                if (geiser != null)
                    geiser.Ativar();

                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    // =====================================
    // ATORDOAMENTO (Runa 02)
    // =====================================

    public void Atordoar(float duracao)
    {
        if (estado == EstadoBoss.Morto) return;
        if (estado == EstadoBoss.Atordoado) return;

        StartCoroutine(RotinaAtordoamento(duracao));
    }

    IEnumerator RotinaAtordoamento(float duracao)
    {
        estado = EstadoBoss.Atordoado;
        vulneravel = true;

        // Cai
        float alturaCaida = 1.5f;
        Vector3 posicaoChao = new Vector3(
            transform.position.x,
            transform.position.y - alturaCaida,
            transform.position.z
        );

        float t = 0f;
        float tempoCaida = 0.4f;
        Vector3 posInicial = transform.position;

        while (t < tempoCaida)
        {
            transform.position = Vector3.Lerp(posInicial, posicaoChao, t / tempoCaida);
            t += Time.deltaTime;
            yield return null;
        }

        // Feedback visual — pisca
        if (sr != null)
            StartCoroutine(PiscarVulneravel(duracao));

        Debug.Log($"[Boss] Atordoado por {duracao}s — vulnerável!");

        yield return new WaitForSeconds(duracaoVulneravel);

        // Volta a voar
        vulneravel = false;
        estado = EstadoBoss.Voando;

        // Sobe de volta
        t = 0f;
        posInicial = transform.position;
        Vector3 posVoo = new Vector3(transform.position.x, alturaVoo, transform.position.z);

        while (t < tempoCaida)
        {
            transform.position = Vector3.Lerp(posInicial, posVoo, t / tempoCaida);
            t += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[Boss] Voltou a voar — invulnerável.");
    }

    IEnumerator PiscarVulneravel(float duracao)
    {
        float timer = 0f;
        float intervalo = 0.15f;

        while (timer < duracao)
        {
            if (sr != null) sr.color = Color.red;
            yield return new WaitForSeconds(intervalo);
            if (sr != null) sr.color = Color.white;
            yield return new WaitForSeconds(intervalo);
            timer += intervalo * 2f;
        }

        if (sr != null) sr.color = Color.white;
    }

    // =====================================
    // DANO
    // =====================================

    public void TakeDamage(int dano, GameObject fonte)
    {
        if (!vulneravel)
        {
            Debug.Log("[Boss] Invulnerável — dano ignorado.");
            return;
        }

        vidaAtual -= dano;
        Debug.Log($"[Boss] Tomou {dano} de dano | Vida: {vidaAtual}/{vidaMaxima}");

        if (vidaAtual <= 0)
            Morrer();
    }

    void Morrer()
    {
        estado = EstadoBoss.Morto;
        Debug.Log("[Boss] Morreu!");
        // Aqui você adiciona: animação de morte, loot, evento de cena etc.
        Destroy(gameObject, 1f);
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (pontoEsquerdo != null)
            Gizmos.DrawWireSphere(new Vector3(pontoEsquerdo.position.x, alturaVoo, 0f), 0.3f);
        if (pontoDireito != null)
            Gizmos.DrawWireSphere(new Vector3(pontoDireito.position.x, alturaVoo, 0f), 0.3f);

        Gizmos.color = Color.red;
        if (pontoEsquerdo != null && pontoDireito != null)
            Gizmos.DrawLine(
                new Vector3(pontoEsquerdo.position.x, alturaVoo, 0f),
                new Vector3(pontoDireito.position.x, alturaVoo, 0f)
            );
    }
}