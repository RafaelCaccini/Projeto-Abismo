using UnityEngine;
using System.Collections;

public class PlataformaQueCai : MonoBehaviour
{
    [Header("Tempo")]
    [SerializeField] private float tempoAntesDeCair = 0.5f;
    [SerializeField] private float tempoParaVoltar = 2f;

    [Header("Config")]
    [SerializeField] private bool respawn = true;

    private Collider2D col;
    private SpriteRenderer sr;
    private bool ativada;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (ativada) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            ativada = true;
            StartCoroutine(Cair());
        }
    }

    IEnumerator Cair()
    {
        yield return new WaitForSeconds(tempoAntesDeCair);

        // 🔥 desativa colisão (plataforma "cai")
        col.enabled = false;

        // opcional: esconde visualmente
        if (sr != null)
            sr.enabled = false;

        if (respawn)
        {
            yield return new WaitForSeconds(tempoParaVoltar);

            col.enabled = true;

            if (sr != null)
                sr.enabled = true;

            ativada = false;
        }
    }
}