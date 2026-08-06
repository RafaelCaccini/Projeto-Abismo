using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int playerLife;
    public int maxLife = 10;

    [Header("Checkpoint")]
    [SerializeField] private Vector3 checkpointPosition;
    [SerializeField] private bool hasCheckpoint = false;

    public Vector3 CheckpointPosition =>
        checkpointPosition;

    public bool HasCheckpoint =>
        hasCheckpoint;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerLife = maxLife;

        checkpointPosition = Vector3.zero;
        hasCheckpoint = false;

        // =====================================
        // SUBSCRIBE: reposiciona player no checkpoint
        // toda vez que uma cena carrega (inclui respawn)
        // =====================================
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // =====================================
    // RESPAWN VIA sceneLoaded
    // =====================================
    // Este callback garante que o checkpoint seja
    // respeitado EM QUALQUER cena, mesmo que não
    // exista PlayerSceneLoader nela.
    // =====================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        // Try to apply scene-specific abilities regardless of checkpoint state.
        StartCoroutine(ApplySceneAbilitiesWhenPlayerAvailable());

        // If we have a checkpoint, reposition player as before
        if (!hasCheckpoint)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            player.transform.position = checkpointPosition;

            Debug.Log(
                "[GameManager] Player reposicionado " +
                "no checkpoint: " +
                checkpointPosition
            );
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] Player não encontrado " +
                "no sceneLoaded. PlayerSceneLoader " +
                "deve posicionar no Start()."
            );
        }
    }

    private System.Collections.IEnumerator ApplySceneAbilitiesWhenPlayerAvailable()
    {
        var sceneAbilities = Object.FindObjectOfType<ScenePlayerAbilities>();
        if (sceneAbilities == null)
            yield break;

        // Try for a few frames to find the Player in case it's instantiated in Start()
        const int maxAttempts = 10;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var pa = player.GetComponent<PlayerAbilities>();
                if (pa != null)
                {
                    pa.ReplaceAbilities(sceneAbilities.Abilities);
                    Debug.Log("[GameManager] Applied ScenePlayerAbilities to Player");
                }
                else
                {
                    Debug.LogWarning("[GameManager] PlayerAbilities not found on Player while applying ScenePlayerAbilities.");
                }
                yield break;
            }

            attempts++;
            yield return null; // wait a frame
        }

        Debug.LogWarning("[GameManager] Could not find Player to apply ScenePlayerAbilities after scene load.");
    }

    // =====================================
    // CHECKPOINT
    // =====================================

    public void SetCheckpoint(
        Vector3 position
    )
    {
        checkpointPosition = position;
        hasCheckpoint = true;
    }

    public void ClearCheckpoint()
    {
        hasCheckpoint = false;
        checkpointPosition = Vector3.zero;
    }
}