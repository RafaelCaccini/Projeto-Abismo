using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MiniBossController : MonoBehaviour
{
    [Header("=== Configurações Gerais ===")]
    public float maxHealth = 1200f;
    public float currentHealth;
    public Transform player;

    [Header("=== Distâncias ===")]
    public float shortRangeThreshold = 8f;

    [Header("=== Timers ===")]
    public float idleTime = 0.6f;
    public float telegraphTime = 0.75f;
    public float recoverTime = 1.3f;
    public float globalAttackCooldown = 0.5f;

    [Header("=== Fase 2 ===")]
    public float phase2HealthThreshold = 0.5f;
    private bool phase2Triggered = false;
    public float phase2Duration = 7.5f;
    public float stunDuration = 3.2f;
    public int projectilesPerBurst = 5;
    public float projectileFireRate = 0.6f;

    [Header("=== Movimentação ===")]
    public float jumpForce = 19f;
    public float horizontalSpeed = 9f;
    public float maxFallSpeed = -25f;

    [Header("=== Limites da Arena (Spawn) ===")]
    public bool useArenaBounds = false;           // Ative só se quiser limitar
    public float arenaLeftBound = -20f;
    public float arenaRightBound = 20f;

    [Header("=== Tags para Colisão ===")]
    public string groundTag = "Ground";
    public string ceilingTag = "Ceiling";
    public string wallTag = "Wall";

    [Header("=== Referências ===")]
    public GameObject spikePrefab;
    public Transform[] spikeSpawnPoints;
    public Transform ceilingSpawnPoint;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;

    private enum BossState { Idle, ChooseAttack, Attack, Recover, Stunned, Phase2 }
    private BossState currentState = BossState.Idle;

    private float currentTimer = 0f;
    private float lastAttackTime = -999f;
    private int lastAttackIndex = -1;
    private bool isInvulnerable = false;
    private float phase2Timer = 0f;

    private Rigidbody2D rb;
    private Coroutine currentAttackRoutine;
    private Dictionary<string, Collider2D> hitboxCache = new Dictionary<string, Collider2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;

        if (rb != null)
            rb.gravityScale = 4f;

        // IMPORTANTE: Garante que ele spawne exatamente onde você colocou
        transform.position = new Vector2(transform.position.x, transform.position.y);
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        ClampVelocity();
        if (useArenaBounds) ClampPosition();     // Só ativa se você quiser

        CheckPhaseTransition();

        switch (currentState)
        {
            case BossState.Idle: State_Idle(); break;
            case BossState.ChooseAttack: State_ChooseAttack(); break;
            case BossState.Recover: State_Recover(); break;
            case BossState.Stunned: State_Stunned(); break;
            case BossState.Phase2: State_Phase2(); break;
        }
    }

    private void ClampVelocity()
    {
        if (rb.linearVelocity.y < maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }

    private void ClampPosition()
    {
        float x = Mathf.Clamp(transform.position.x, arenaLeftBound, arenaRightBound);
        transform.position = new Vector2(x, transform.position.y);
    }

    #region ESTADOS (mantido igual, só corrigi pequenas coisas)
    private void State_Idle()
    {
        currentTimer += Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > shortRangeThreshold + 2f)
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dir * 3.5f, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (currentTimer >= idleTime)
        {
            currentTimer = 0f;
            ChangeState(BossState.ChooseAttack);
        }
    }

    private void State_ChooseAttack()
    {
        if (Time.time - lastAttackTime < globalAttackCooldown)
        {
            ChangeState(BossState.Idle);
            return;
        }

        int attackIndex = ChooseAttackByDistance();
        if (attackIndex == lastAttackIndex && Random.value < 0.6f)
            attackIndex = ChooseAttackByDistance();

        lastAttackIndex = attackIndex;
        lastAttackTime = Time.time;

        StartAttack(attackIndex);
    }

    private void State_Recover()
    {
        currentTimer += Time.deltaTime;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (currentTimer >= recoverTime)
            ChangeState(BossState.Idle);
    }

    private void State_Stunned()
    {
        currentTimer += Time.deltaTime;
        rb.linearVelocity = Vector2.zero;

        if (currentTimer >= stunDuration)
            ChangeState(BossState.Idle);
    }

    private void State_Phase2()
    {
        phase2Timer += Time.deltaTime;
        isInvulnerable = true;
        rb.linearVelocity = Vector2.zero;

        if (Mathf.Repeat(phase2Timer, projectileFireRate) < Time.deltaTime)
            ShootDefensiveProjectiles();

        if (phase2Timer >= phase2Duration)
        {
            isInvulnerable = false;
            phase2Timer = 0f;
            ChangeState(BossState.Stunned);
        }
    }
    #endregion

    private void ChangeState(BossState newState)
    {
        currentState = newState;
        currentTimer = 0f;
    }

    private int ChooseAttackByDistance()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= shortRangeThreshold)
            return (Random.value < 0.6f) ? 1 : 3;
        else
            return (Random.value < 0.7f) ? 2 : 4;
    }

    private void StartAttack(int index)
    {
        ChangeState(BossState.Attack);
        if (currentAttackRoutine != null)
            StopCoroutine(currentAttackRoutine);

        rb.linearVelocity = Vector2.zero;

        switch (index)
        {
            case 1: currentAttackRoutine = StartCoroutine(Attack_SocoSalto()); break;
            case 2: currentAttackRoutine = StartCoroutine(Attack_QuicadasDiagonal()); break;
            case 3: currentAttackRoutine = StartCoroutine(Attack_PisaoEspinhos()); break;
            case 4: currentAttackRoutine = StartCoroutine(Attack_EspinhosTeto()); break;
        }
    }

    private void CheckPhaseTransition()
    {
        if (!phase2Triggered && currentHealth / maxHealth <= phase2HealthThreshold)
        {
            phase2Triggered = true;
            EnterPhase2();
        }
    }

    private void EnterPhase2()
    {
        if (currentAttackRoutine != null)
            StopCoroutine(currentAttackRoutine);
        ChangeState(BossState.Phase2);
    }

    #region ATAQUES
    private IEnumerator Attack_SocoSalto()
    {
        yield return new WaitForSeconds(telegraphTime);
        EnableHitbox("SocoHitbox", true);
        yield return new WaitForSeconds(0.25f);
        EnableHitbox("SocoHitbox", false);

        float dir = Mathf.Sign(player.position.x - transform.position.x) * -1f;
        rb.linearVelocity = new Vector2(dir * horizontalSpeed * 1.6f, jumpForce);

        yield return new WaitForSeconds(1.1f);
        ChangeState(BossState.Recover);
    }

    private IEnumerator Attack_QuicadasDiagonal()
    {
        yield return new WaitForSeconds(telegraphTime * 0.7f);

        int bounces = Random.Range(3, 6);
        Vector2 dir = (Random.value < 0.5f) ? new Vector2(1, 1) : new Vector2(-1, 1);
        dir.Normalize();

        EnableHitbox("BodyHitbox", true);

        for (int i = 0; i < bounces; i++)
        {
            rb.linearVelocity = dir * 20f;
            yield return new WaitForSeconds(0.45f);   // tempo aproximado de cada quicada

            if (IsTouchingWall())
                dir.x = -dir.x;
            else if (IsTouchingCeiling() || IsTouchingGround())
                dir.y = -dir.y;
        }

        EnableHitbox("BodyHitbox", false);

        float finalDir = Mathf.Sign(player.position.x - transform.position.x) * -1f;
        rb.linearVelocity = new Vector2(finalDir * 7f, jumpForce * 0.85f);

        yield return new WaitForSeconds(0.9f);
        ChangeState(BossState.Recover);
    }

    // Ataques 3 e 4 mantidos iguais ao seu código
    private IEnumerator Attack_PisaoEspinhos()
    {
        yield return new WaitForSeconds(telegraphTime);
        EnableHitbox("SlamHitbox", true);
        yield return new WaitForSeconds(0.25f);
        EnableHitbox("SlamHitbox", false);

        yield return StartCoroutine(SpawnSpikeWave(-1));
        yield return StartCoroutine(SpawnSpikeWave(1));
        yield return new WaitForSeconds(0.8f);
        ChangeState(BossState.Recover);
    }

    private IEnumerator Attack_EspinhosTeto()
    {
        yield return new WaitForSeconds(telegraphTime);
        yield return StartCoroutine(SpawnDescendingSpikes());
        ChangeState(BossState.Recover);
    }
    #endregion

    #region DETECÇÃO COM TAGS (Corrigido)
    private bool IsTouchingCeiling()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, 2.5f);
        return hit.collider != null && hit.collider.CompareTag(ceilingTag);
    }

    private bool IsTouchingGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 2.5f);
        return hit.collider != null && hit.collider.CompareTag(groundTag);
    }

    private bool IsTouchingWall()
    {
        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, Vector2.right, 2.2f);
        RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, Vector2.left, 2.2f);

        return (hitRight.collider != null && hitRight.collider.CompareTag(wallTag)) ||
               (hitLeft.collider != null && hitLeft.collider.CompareTag(wallTag));
    }
    #endregion

    #region UTILITÁRIOS
    private void ShootDefensiveProjectiles()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null) return;

        float step = 360f / projectilesPerBurst;
        for (int i = 0; i < projectilesPerBurst; i++)
        {
            float angle = i * step;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
            if (prb != null) prb.linearVelocity = dir * 10f;
        }
    }

    private IEnumerator SpawnSpikeWave(int direction)
    {
        if (spikeSpawnPoints == null || spikePrefab == null) yield break;

        int start = direction > 0 ? 0 : spikeSpawnPoints.Length - 1;
        int step = direction > 0 ? 1 : -1;
        int end = direction > 0 ? spikeSpawnPoints.Length : -1;

        for (int i = start; i != end; i += step)
        {
            if (i >= 0 && i < spikeSpawnPoints.Length)
                Instantiate(spikePrefab, spikeSpawnPoints[i].position, Quaternion.identity);

            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator SpawnDescendingSpikes()
    {
        if (ceilingSpawnPoint == null || spikePrefab == null) yield break;

        for (int i = 0; i < 6; i++)
        {
            Vector2 pos = new Vector2(ceilingSpawnPoint.position.x + Random.Range(-6.5f, 6.5f), ceilingSpawnPoint.position.y);
            Instantiate(spikePrefab, pos, Quaternion.Euler(0, 0, 180));
            yield return new WaitForSeconds(0.18f);
        }
    }

    public void EnableHitbox(string name, bool enable)
    {
        if (!hitboxCache.ContainsKey(name))
        {
            Transform t = transform.Find(name);
            if (t != null)
                hitboxCache[name] = t.GetComponent<Collider2D>();
        }
        if (hitboxCache.TryGetValue(name, out Collider2D col))
            col.enabled = enable;
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log("Mini Boss derrotado!");
        enabled = false;
    }
    #endregion
}