using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DamageVisualFeedback : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("Se vazio, procura automaticamente todos os SpriteRenderers neste objeto e nos filhos.")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("Feedback")]
    [SerializeField] private bool ativarFeedback = true;

    [SerializeField] private Color corDano = Color.red;

    [SerializeField] private float duracao = 0.2f;

    [SerializeField] private int quantidadePiscadas = 2;

    [SerializeField] private bool incluirFilhos = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private readonly List<Color> coresOriginais = new List<Color>();

    private Coroutine feedbackCoroutine;

    private void Awake()
    {
        BuscarSprites();
    }

    private void BuscarSprites()
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            return;
        }

        if (incluirFilhos)
        {
            spriteRenderers =
                GetComponentsInChildren<SpriteRenderer>(true);
        }
        else
        {
            SpriteRenderer sprite =
                GetComponent<SpriteRenderer>();

            if (sprite != null)
            {
                spriteRenderers =
                    new SpriteRenderer[] { sprite };
            }
        }

        if (debugLogs)
        {
            Debug.Log(
                "[DamageVisualFeedback] " +
                gameObject.name +
                " | Sprites encontrados: " +
                (spriteRenderers != null
                    ? spriteRenderers.Length
                    : 0)
            );
        }
    }

    // =====================================
    // MÉTODO PÚBLICO UNIVERSAL
    // QUALQUER PLAYER OU INIMIGO CHAMA ISSO
    // =====================================

    public void PlayDamageFeedback()
    {
        if (!ativarFeedback)
            return;

        BuscarSprites();

        if (spriteRenderers == null ||
            spriteRenderers.Length == 0)
        {
            Debug.LogWarning(
                "[DamageVisualFeedback] Nenhum SpriteRenderer encontrado em: "
                + gameObject.name
            );

            return;
        }

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        feedbackCoroutine =
            StartCoroutine(FeedbackRoutine());
    }

    // =====================================
    // ROTINA DO PISCAR
    // =====================================

    private IEnumerator FeedbackRoutine()
    {
        SalvarCoresOriginais();

        float tempoPorEstado =
            duracao /
            (quantidadePiscadas * 2f);

        for (int i = 0;
             i < quantidadePiscadas;
             i++)
        {
            AplicarCor(corDano);

            yield return new WaitForSeconds(
                tempoPorEstado
            );

            RestaurarCores();

            yield return new WaitForSeconds(
                tempoPorEstado
            );
        }

        RestaurarCores();

        feedbackCoroutine = null;
    }

    // =====================================
    // SALVAR CORES
    // =====================================

    private void SalvarCoresOriginais()
    {
        coresOriginais.Clear();

        foreach (SpriteRenderer sprite
                 in spriteRenderers)
        {
            if (sprite != null)
            {
                coresOriginais.Add(
                    sprite.color
                );
            }
            else
            {
                coresOriginais.Add(
                    Color.white
                );
            }
        }
    }

    // =====================================
    // APLICAR COR
    // =====================================

    private void AplicarCor(Color cor)
    {
        if (spriteRenderers == null)
            return;

        foreach (SpriteRenderer sprite
                 in spriteRenderers)
        {
            if (sprite != null)
            {
                sprite.color = cor;
            }
        }
    }

    // =====================================
    // RESTAURAR CORES
    // =====================================

    public void RestaurarCores()
    {
        if (spriteRenderers == null)
            return;

        for (int i = 0;
             i < spriteRenderers.Length;
             i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            if (i < coresOriginais.Count)
            {
                spriteRenderers[i].color =
                    coresOriginais[i];
            }
        }
    }

    private void OnDisable()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        RestaurarCores();
    }

    [ContextMenu("Testar Feedback de Dano")]
    private void TestarFeedbackDeDano()
    {
        PlayDamageFeedback();
    }
}