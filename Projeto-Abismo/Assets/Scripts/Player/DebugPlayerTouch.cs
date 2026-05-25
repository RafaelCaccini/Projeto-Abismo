using UnityEngine;

public class DebugPlayerTouch : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private bool usarRaycast = true;

    [SerializeField] private float rayDistance = 2f;

    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb == null)
            Debug.LogError("❌ PLAYER SEM Rigidbody2D");

        if (col == null)
            Debug.LogError("❌ PLAYER SEM Collider2D");
    }

    private void Update()
    {
        if (usarRaycast)
            FazerRaycast();
    }

    // =====================================
    // COLLISION
    // =====================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        MostrarInfos(collision.gameObject, "COLLISION ENTER");
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        MostrarInfos(collision.gameObject, "COLLISION STAY");
    }

    // =====================================
    // TRIGGER
    // =====================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        MostrarInfos(other.gameObject, "TRIGGER ENTER");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        MostrarInfos(other.gameObject, "TRIGGER STAY");
    }

    // =====================================
    // RAYCAST
    // =====================================

    private void FazerRaycast()
    {
        Vector2 origem = col.bounds.center;

        RaycastHit2D hit =
            Physics2D.Raycast(
                origem,
                Vector2.down,
                rayDistance
            );

        Debug.DrawRay(
            origem,
            Vector2.down * rayDistance,
            Color.green
        );

        if (hit.collider != null)
        {
            MostrarInfos(
                hit.collider.gameObject,
                "RAYCAST"
            );
        }
    }

    // =====================================
    // DEBUG
    // =====================================

    private void MostrarInfos(
        GameObject obj,
        string tipo
    )
    {
        Debug.Log(
            "\n======================" +
            "\nTIPO: " + tipo +
            "\nNOME: " + obj.name +
            "\nTAG: " + obj.tag +
            "\nLAYER: " + LayerMask.LayerToName(obj.layer) +
            "\nHIERARQUIA: " + GetHierarchyPath(obj.transform) +
            "\nPOSIÇÃO: " + obj.transform.position +
            "\n======================"
        );
    }

    private string GetHierarchyPath(Transform current)
    {
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}