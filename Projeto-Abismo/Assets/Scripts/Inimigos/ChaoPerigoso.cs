using UnityEngine;
using System.Collections;

public class ChaoPerigoso : MonoBehaviour
{
    [Header("Dano")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float intervaloDano = 1f;

    private bool podeDarDano = true;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        bool estaEmCima = collision.transform.position.y > transform.position.y + 0.1f;

        Debug.Log($"[Chao] Em cima (posição): {estaEmCima}");

        if (!estaEmCima || !podeDarDano) return;

        PlayerController pc = collision.gameObject.GetComponentInParent<PlayerController>();

        if (pc == null) return;

        Debug.Log("Luz ativa? " + pc.LuzAtiva);

        if (!pc.LuzAtiva)
        {
            Debug.Log("💥 DANO APLICADO (DIRETO)");

            pc.TakeDamage(dano, gameObject);

            StartCoroutine(CooldownDano());
        }
    }

    IEnumerator CooldownDano()
    {
        podeDarDano = false;
        yield return new WaitForSeconds(intervaloDano);
        podeDarDano = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}