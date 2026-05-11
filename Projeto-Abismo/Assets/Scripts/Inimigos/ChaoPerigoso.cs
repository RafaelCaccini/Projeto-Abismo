using UnityEngine;
using System.Collections;

public class ChaoPerigoso : MonoBehaviour
{
    [Header("Dano")]
    [SerializeField] private int dano = 1;

    [SerializeField] private float intervaloDano = 1f;

    [Header("Estado")]
    [SerializeField] private bool iluminado = false;

    private bool podeDarDano = true;

    // ---------------- DANO ----------------
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Só player
        if (!collision.gameObject.CompareTag("Player"))
            return;

        // Se iluminado, não dá dano
        if (iluminado)
            return;

        // Cooldown
        if (!podeDarDano)
            return;

        // Verifica se está pisando em cima
        bool estaEmCima =
            collision.transform.position.y >
            transform.position.y + 0.1f;

        if (!estaEmCima)
            return;

        PlayerController pc =
            collision.gameObject.GetComponentInParent<PlayerController>();

        if (pc == null)
            return;

        Debug.Log("💥 DANO APLICADO");

        pc.TakeDamage(dano, gameObject);

        StartCoroutine(CooldownDano());
    }

    IEnumerator CooldownDano()
    {
        podeDarDano = false;

        yield return new WaitForSeconds(intervaloDano);

        podeDarDano = true;
    }

    // ---------------- LUZ ----------------
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LuzLampiao"))
        {
            iluminado = true;

            Debug.Log("✨ Espinhos desativados");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("LuzLampiao"))
        {
            iluminado = false;

            Debug.Log("⚠️ Espinhos ativados");
        }
    }

    // ---------------- DEBUG ----------------
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            Gizmos.color = iluminado
                ? Color.green
                : Color.red;

            Gizmos.DrawWireCube(
                col.bounds.center,
                col.bounds.size
            );
        }
    }
}