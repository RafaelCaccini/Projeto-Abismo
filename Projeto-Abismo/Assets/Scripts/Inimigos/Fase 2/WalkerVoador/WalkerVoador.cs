using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class WalkerVoador : MonoBehaviour, IDamageable
{
    // =====================================
    // REFERÊNCIAS
    // =====================================

    [Header("REFERÊNCIAS")]
    [Tooltip("SpriteRenderer que será flipado horizontalmente. Se nulo, busca automaticamente no filho.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Ponto de origem do raycast que detecta paredes à frente.")]
    [SerializeField] private Transform wallCheck;

    // =====================================
    // MOVIMENTO
    // =====================================

    [Header("MOVIMENTO")]
    [SerializeField] private float velocidade = 3f;

    // =====================================
    // PAREDE
    // =====================================

    [Header("PAREDE")]
    [Tooltip("LayerMask para detecção por layer (ex: wallLayer).")]
    [SerializeField] private LayerMask paredeLayer;

    [Tooltip("Tag usada como fallback quando a LayerMask não bate. Deixe vazio para desativar.")]
    [SerializeField] private string wallTag = "Wall";

    [SerializeField] private float distanciaParede = 0.6f;

    // =====================================
    // DANO
    // =====================================

    [Header("DANO")]
    [SerializeField] private int dano = 1;

    [SerializeField] private float cooldownDano = 1f;

    // =====================================
    // VIDA
    // =====================================

    [Header("VIDA")]
    [SerializeField] private int vida = 5;

    [SerializeField] private bool podeMorrer = true;

    // =====================================
    // DEBUG
    // =====================================

    [Header("DEBUG")]
    [SerializeField] private bool mostrarRaycast = true;

    [Tooltip("Loga informações completas sobre visibilidade do sprite para diagnosticar inimigos invisíveis.")]
    [SerializeField] private bool debugInvisibilidade = true;

    [Tooltip("Se true, corrige automaticamente a posição local do visual quando está muito distante do pai.")]
    [SerializeField] private bool corrigirPosicaoVisual = true;

    [Tooltip("Distância máxima permitida entre o WalkerVoador e seu visual antes de considerar um problema.")]
    [SerializeField] private float distanciaMaxVisual = 2f;

    // =====================================
    // COMPONENTES
    // =====================================

    private Rigidbody2D rb;

    private Collider2D col;

    private Transform visualTransform;

    // =====================================
    // CONTROLE
    // =====================================

    private int direcao = 1;

    private float ultimoDano;

    private bool morto;

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        col = GetComponent<Collider2D>();

        spriteRenderer =
            spriteRenderer != null
            ? spriteRenderer
            : GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            visualTransform = spriteRenderer.transform;

        if (
            wallCheck == null
        )
        {
            wallCheck = new GameObject(
                "WallCheck"
            ).transform;

            wallCheck.SetParent(
                transform,
                worldPositionStays: false
            );

            wallCheck.localPosition =
                new Vector3(0.72f, 0f, 0f);
        }

        // =====================================
        // SEGURANÇA RB
        // =====================================

        if (rb != null)
        {
            rb.gravityScale = 0f;

            rb.freezeRotation = true;

            rb.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            rb.bodyType =
                RigidbodyType2D.Dynamic;
        }

        Debug.Log(
            $"[WalkerVoador] Iniciado na posição: "
            + $"{transform.position} | Direção inicial: {direcao}"
        );
    }

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        ValidarVisibilidade();
    }

    // =====================================
    // VALIDAÇÃO DE VISIBILIDADE
    // =====================================

    private void ValidarVisibilidade()
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning(
                $"[WalkerVoador] ⚠️ SpriteRenderer NÃO ENCONTRADO! " +
                $"O inimigo está INVISÍVEL na posição {transform.position}. " +
                $"Verifique se há um SpriteRenderer na hierarquia de filhos."
            );
            return;
        }

        // =====================================
        // VERIFICA OFFSET DO VISUAL
        // =====================================

        Vector3 offsetLocal =
            visualTransform.localPosition;

        float distanciaVisual =
            Vector3.Distance(
                transform.position,
                visualTransform.position
            );

        if (
            distanciaVisual >
            distanciaMaxVisual
        )
        {
            Debug.LogWarning(
                $"[WalkerVoador] ⚠️ PROBLEMA CRÍTICO DE VISIBILIDADE!\n" +
                $"  Posição do WalkerVoador: {transform.position}\n" +
                $"  Posição do Visual (sprite): {visualTransform.position}\n" +
                $"  Offset local: {offsetLocal}\n" +
                $"  Distância entre os dois: {distanciaVisual:F2} unidades\n" +
                $"  → O sprite está RENDERIZADO LONGE do collider!\n" +
                $"  → Isso faz o inimigo parecer INVISÍVEL.\n" +
                $"  → O player colide com o collider aqui: {transform.position}\n" +
                $"  → Mas o sprite é desenhado aqui: {visualTransform.position}\n" +
                $"  → Corrigindo posição do visual..."
            );

            if (corrigirPosicaoVisual)
            {
                visualTransform.localPosition =
                    Vector3.zero;

                Debug.Log(
                    $"[WalkerVoador] ✅ Posição visual corrigida " +
                    $"para (0, 0, 0) relativo ao pai."
                );
            }
            else
            {
                Debug.LogWarning(
                    $"  Ative 'corrigirPosicaoVisual' para corrigir " +
                    $"automaticamente na próxima inicialização."
                );
            }
        }

        // =====================================
        // DEBUG COMPLETO DE RENDERIZAÇÃO
        // =====================================

        if (debugInvisibilidade)
        {
            Debug.Log(
                $"[WalkerVoador] Status de visibilidade:\n" +
                $"  Sprite: {spriteRenderer.sprite?.name ?? "NULL"}\n" +
                $"  Cor: {spriteRenderer.color}\n" +
                $"  Sorting Layer: {spriteRenderer.sortingLayerName}\n" +
                $"  Sorting Order: {spriteRenderer.sortingOrder}\n" +
                $"  FlipX: {spriteRenderer.flipX}\n" +
                $"  Visual pos (local): {offsetLocal}\n" +
                $"  Visual pos (world): {visualTransform.position}\n" +
                $"  Distance to parent: {distanciaVisual:F2}\n" +
                $"  Collider bounds: {col?.bounds}\n" +
                $"  Parent pos (world): {transform.position}"
            );
        }
    }

    // =====================================
    // FIXED UPDATE
    // =====================================

    private void FixedUpdate()
    {
        if (morto)
            return;

        Mover();
    }

    // =====================================
    // MOVIMENTO
    // =====================================

    private void Mover()
    {
        if (
            rb == null
            || wallCheck == null
        )
            return;

        Vector2 origem =
            wallCheck.position;

        Vector2 direcaoRay =
            Vector2.right * direcao;

        // =====================================
        // DETECÇÃO DE PAREDE (LAYER + TAG)
        // =====================================

        RaycastHit2D hit =
            Physics2D.Raycast(
                origem,
                direcaoRay,
                distanciaParede,
                paredeLayer
            );

        // Se a LayerMask não bate, tenta por tag
        if (
            hit.collider == null
            && !string.IsNullOrEmpty(wallTag)
        )
        {
            RaycastHit2D tagHit =
                Physics2D.Raycast(
                    origem,
                    direcaoRay,
                    distanciaParede
                );

            if (
                tagHit.collider != null
                && tagHit.collider.CompareTag(wallTag)
            )
            {
                hit = tagHit;
            }
        }

        // =====================================
        // DEBUG
        // =====================================

        if (mostrarRaycast)
        {
            Debug.DrawRay(
                origem,
                direcaoRay
                * distanciaParede,
                hit.collider != null
                    ? Color.green
                    : Color.red
            );
        }

        // =====================================
        // PAREDE ENCONTRADA
        // =====================================

        if (hit.collider != null)
        {
            Debug.Log(
                $"[WalkerVoador] 🧱 Parede detectada " +
                $"(layer={hit.collider.gameObject.layer}, " +
                $"tag={hit.collider.tag}, " +
                $"obj={hit.collider.name}) " +
                $"na posição {transform.position}"
            );

            Virar();

            return;
        }

        // =====================================
        // MOVIMENTO
        // =====================================

        rb.linearVelocity =
            new Vector2(
                direcao * velocidade,
                0f
            );
    }

    // =====================================
    // VIRAR
    // =====================================

    private void Virar()
    {
        direcao *= -1;

        Debug.Log(
            $"[WalkerVoador] 🔄 Virou direção: "
            + $"{direcao} " +
            $"| Posição: {transform.position}"
        );

        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            direcao < 0;
    }

    // =====================================
    // DANO PLAYER
    // =====================================

    private void OnCollisionStay2D(
        Collision2D other
    )
    {
        if (morto)
            return;

        if (
            !other.gameObject.CompareTag(
                "Player"
            )
        )
            return;

        if (
            Time.time <
            ultimoDano + cooldownDano
        )
            return;

        PlayerController player =
            other.gameObject.GetComponent<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning(
                $"[WalkerVoador] ❌ PlayerController não encontrado " +
                $"no objeto {other.gameObject.name}. " +
                $"Tentando GetComponentInParent..."
            );

            player =
                other.gameObject
                .GetComponentInParent<PlayerController>();

            if (player == null)
            {
                Debug.LogWarning(
                    $"[WalkerVoador] ❌ PlayerController " +
                    $"realmente não encontrado. " +
                    $"Posição do WalkerVoador: {transform.position}"
                );
                return;
            }
        }

        player.TakeDamage(
            dano,
            gameObject
        );

        ultimoDano = Time.time;

        Debug.Log(
            $"[WalkerVoador] 💥 CAUSEI DANO ao player! " +
            $"(dano={dano}, posição={transform.position}, " +
            $"velocidade={rb.linearVelocity})"
        );
    }

    // =====================================
    // TOMAR DANO
    // =====================================

    public void TakeDamage(
        int amount,
        GameObject source
    )
    {
        if (morto)
            return;

        Debug.Log(
            $"[WalkerVoador] 💥 Recebeu {amount} de dano " +
            $"de {source.name} | Vida: {vida - amount}"
        );

        // =====================================
        // IMORTAL
        // =====================================

        if (!podeMorrer)
        {
            Debug.Log(
                "🛡️ WalkerVoador imortal"
            );

            return;
        }

        vida -= amount;

        if (vida <= 0)
        {
            Morrer();
        }
    }

    // =====================================
    // MORRER
    // =====================================

    private void Morrer()
    {
        if (morto)
            return;

        morto = true;

        Debug.Log(
            $"[WalkerVoador] ☠️ Morreu na posição " +
            $"{transform.position}"
        );

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (col != null)
            col.enabled = false;

        Destroy(gameObject, 0.05f);
    }

    // =====================================
    // GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        // =====================================
        // RAIZ DO ENEMY
        // =====================================

        if (col != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireCube(
                col.bounds.center,
                col.bounds.size
            );

            Gizmos.color = new Color(
                1f, 0f, 1f, 0.1f
            );

            Gizmos.DrawCube(
                col.bounds.center,
                col.bounds.size
            );
        }

        // =====================================
        // WALL CHECK
        // =====================================

        if (wallCheck == null)
            return;

        Vector2 origem =
            wallCheck.position;

        Vector2 direcaoRay =
            Vector2.right * direcao;

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            origem,
            origem
            + direcaoRay
            * distanciaParede
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawSphere(
            origem,
            0.05f
        );

        // =====================================
        // VISUAL (SPRITE)
        // =====================================

        if (visualTransform != null)
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawWireSphere(
                visualTransform.position,
                0.1f
            );

            float distancia =
                Vector3.Distance(
                    transform.position,
                    visualTransform.position
                );

            if (distancia > 0.01f)
            {
                Gizmos.color = Color.magenta;

                Gizmos.DrawLine(
                    transform.position,
                    visualTransform.position
                );
            }
        }

        // =====================================
        // ALERTA VISUAL
        // =====================================

        if (
            visualTransform != null
            && col != null
        )
        {
            float distancia =
                Vector3.Distance(
                    transform.position,
                    visualTransform.position
                );

            if (
                distancia >
                distanciaMaxVisual
            )
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position,
                    "⚠️ SPRITE DESALINHADO!"
                );
#endif
            }
        }
    }
}
