using UnityEngine;

[DisallowMultipleComponent]
public class DashWall : MonoBehaviour
{
    [Header("REFERÊNCIAS")]
    [Tooltip("Player que pode atravessar a parede durante o Dash.")]
    [SerializeField] private PlayerController player;

    [Header("PAREDE")]
    [Tooltip("Deixe vazio para usar automaticamente todos os Collider2D deste objeto e dos filhos.")]
    [SerializeField] private Collider2D[] collidersParede;

    [Header("COMPORTAMENTO")]
    [Tooltip("Durante o Dash a parede vira Trigger.")]
    [SerializeField] private bool usarTriggerDuranteDash = true;

    [Tooltip("Se o Dash terminar com o Player dentro da parede, mantém a passagem até ele sair.")]
    [SerializeField] private bool esperarPlayerSairDaParede = true;

    [Header("DEBUG")]
    [SerializeField] private bool mostrarLogs = false;

    private bool dashAtivo;
    private bool aguardandoPlayerSair;

    // Guarda o estado original de cada Collider.
    private bool[] estadoOriginalTrigger;

    private void Awake()
    {
        EncontrarColliders();

        SalvarEstadoOriginal();

        EncontrarPlayer();
    }

    private void Start()
    {
        if (player == null)
        {
            EncontrarPlayer();
        }

        // A parede começa no estado normal.
        RestaurarEstadoOriginal();
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            EncontrarPlayer();

            if (player == null)
                return;
        }

        bool estaDashing = player.IsDashing;

        // ---------------------------------------------------------
        // PLAYER COMEÇOU O DASH
        // ---------------------------------------------------------

        if (estaDashing && !dashAtivo)
        {
            dashAtivo = true;
            aguardandoPlayerSair = false;

            AtivarPassagem();

            if (mostrarLogs)
                Debug.Log("[DashWall] Dash detectado. Parede atravessável.");
        }

        // ---------------------------------------------------------
        // PLAYER TERMINOU O DASH
        // ---------------------------------------------------------

        if (!estaDashing && dashAtivo)
        {
            dashAtivo = false;

            if (esperarPlayerSairDaParede && PlayerEstaDentroDaParede())
            {
                // O Player terminou o Dash dentro da parede.
                // Mantemos a passagem temporariamente para ele sair.
                aguardandoPlayerSair = true;

                if (mostrarLogs)
                    Debug.Log("[DashWall] Player ainda está dentro da parede. Aguardando sair.");
            }
            else
            {
                RestaurarEstadoOriginal();

                if (mostrarLogs)
                    Debug.Log("[DashWall] Dash terminou. Parede bloqueando novamente.");
            }
        }

        // ---------------------------------------------------------
        // PLAYER ESTAVA DENTRO DA PAREDE
        // ---------------------------------------------------------

        if (aguardandoPlayerSair)
        {
            if (!PlayerEstaDentroDaParede())
            {
                aguardandoPlayerSair = false;

                RestaurarEstadoOriginal();

                if (mostrarLogs)
                    Debug.Log("[DashWall] Player saiu da parede. Colisão restaurada.");
            }
        }
    }

    // =============================================================
    // ENCONTRAR COLLIDERS
    // =============================================================

    private void EncontrarColliders()
    {
        if (collidersParede != null && collidersParede.Length > 0)
            return;

        collidersParede = GetComponentsInChildren<Collider2D>();

        if (collidersParede == null || collidersParede.Length == 0)
        {
            Debug.LogError(
                "[DashWall] Nenhum Collider2D encontrado na parede.",
                this
            );

            return;
        }
    }

    // =============================================================
    // ENCONTRAR PLAYER
    // =============================================================

    private void EncontrarPlayer()
    {
        GameObject objetoPlayer = GameObject.FindGameObjectWithTag("Player");

        if (objetoPlayer == null)
        {
            if (mostrarLogs)
                Debug.LogWarning("[DashWall] Não encontrei nenhum objeto com a Tag 'Player'.");

            return;
        }

        player = objetoPlayer.GetComponent<PlayerController>();

        if (player == null)
            player = objetoPlayer.GetComponentInChildren<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning(
                "[DashWall] Encontrei o objeto Player, mas não encontrei PlayerController.",
                objetoPlayer
            );
        }
    }

    // =============================================================
    // SALVAR ESTADO ORIGINAL
    // =============================================================

    private void SalvarEstadoOriginal()
    {
        if (collidersParede == null)
            return;

        estadoOriginalTrigger = new bool[collidersParede.Length];

        for (int i = 0; i < collidersParede.Length; i++)
        {
            if (collidersParede[i] != null)
                estadoOriginalTrigger[i] = collidersParede[i].isTrigger;
        }
    }

    // =============================================================
    // ATIVAR PASSAGEM
    // =============================================================

    private void AtivarPassagem()
    {
        if (collidersParede == null)
            return;

        for (int i = 0; i < collidersParede.Length; i++)
        {
            if (collidersParede[i] == null)
                continue;

            if (usarTriggerDuranteDash)
            {
                collidersParede[i].isTrigger = true;
            }
        }
    }

    // =============================================================
    // RESTAURAR PAREDE
    // =============================================================

    private void RestaurarEstadoOriginal()
    {
        if (collidersParede == null)
            return;

        for (int i = 0; i < collidersParede.Length; i++)
        {
            if (collidersParede[i] == null)
                continue;

            if (estadoOriginalTrigger != null &&
                i < estadoOriginalTrigger.Length)
            {
                collidersParede[i].isTrigger =
                    estadoOriginalTrigger[i];
            }
            else
            {
                collidersParede[i].isTrigger = false;
            }
        }
    }

    // =============================================================
    // VERIFICAR SE PLAYER ESTÁ DENTRO DA PAREDE
    // =============================================================

    private bool PlayerEstaDentroDaParede()
    {
        if (player == null)
            return false;

        Collider2D[] collidersPlayer =
            player.GetComponentsInChildren<Collider2D>();

        if (collidersPlayer == null || collidersPlayer.Length == 0)
            return false;

        for (int i = 0; i < collidersParede.Length; i++)
        {
            Collider2D parede = collidersParede[i];

            if (parede == null)
                continue;

            for (int j = 0; j < collidersPlayer.Length; j++)
            {
                Collider2D playerCollider = collidersPlayer[j];

                if (playerCollider == null)
                    continue;

                if (parede.bounds.Intersects(playerCollider.bounds))
                    return true;
            }
        }

        return false;
    }

    // =============================================================
    // GARANTIR ESTADO CORRETO AO DESABILITAR
    // =============================================================

    private void OnDisable()
    {
        if (collidersParede != null)
            RestaurarEstadoOriginal();

        dashAtivo = false;
        aguardandoPlayerSair = false;
    }

    private void OnDestroy()
    {
        if (collidersParede != null)
            RestaurarEstadoOriginal();
    }
}