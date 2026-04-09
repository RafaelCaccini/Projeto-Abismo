using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requirePlayerLightOn = true;
    [SerializeField] private float activationDelay = 0.5f;
    [SerializeField] private float closeDuration = 2f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 1f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D hurtbox;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private HashSet<GameObject> playersInside = new HashSet<GameObject>();

    private bool isClosed = false;
    private bool isActivating = false;

    private Coroutine activationRoutine;
    private Coroutine damageRoutine;

    private void Awake()
    {
        if (hurtbox != null && !hurtbox.isTrigger)
            hurtbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playersInside.Add(other.gameObject);

        if (debugLogs) Debug.Log("[Trap] Player entrou");

        TryActivate();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playersInside.Remove(other.gameObject);

        if (debugLogs) Debug.Log("[Trap] Player saiu");

        if (playersInside.Count == 0)
            CancelActivation();
    }

    private void TryActivate()
    {
        if (isClosed || isActivating) return;

        if (requirePlayerLightOn && !AnyPlayerWithLight())
        {
            if (debugLogs) Debug.Log("[Trap] Luz não está ligada");
            return;
        }

        activationRoutine = StartCoroutine(ActivationCoroutine());
    }

    private IEnumerator ActivationCoroutine()
    {
        isActivating = true;

        yield return new WaitForSeconds(activationDelay);

        if (playersInside.Count == 0)
        {
            isActivating = false;
            yield break;
        }

        if (requirePlayerLightOn && !AnyPlayerWithLight())
        {
            isActivating = false;
            yield break;
        }

        Close();

        isActivating = false;
    }

    private void CancelActivation()
    {
        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }

        isActivating = false;
    }

    private void Close()
    {
        if (isClosed) return;

        isClosed = true;

        if (debugLogs) Debug.Log("[Trap] FECHOU");

        if (animator != null)
            animator.SetBool("Closed", true);

        if (hurtbox != null)
            hurtbox.enabled = true;

        if (damageRoutine != null)
            StopCoroutine(damageRoutine);

        damageRoutine = StartCoroutine(DamageRoutine());

        StartCoroutine(OpenAfterDelay());
    }

    private IEnumerator DamageRoutine()
    {
        while (isClosed)
        {
            // CRIA UMA CÓPIA SEGURA
            var playersSnapshot = new List<GameObject>(playersInside);

            foreach (var player in playersSnapshot)
            {
                if (player == null) continue;

                IDamageable dmg = player.GetComponent<IDamageable>();

                if (dmg != null)
                {
                    dmg.TakeDamage(damage, gameObject);
                    if (debugLogs) Debug.Log("[Trap] Dano aplicado");
                }

                if (debugLogs) Debug.Log("[Trap] Dano aplicado");
            }

            yield return new WaitForSeconds(damageInterval);
        }
    }

    private IEnumerator OpenAfterDelay()
    {
        yield return new WaitForSeconds(closeDuration);

        isClosed = false;

        if (animator != null)
            animator.SetBool("Closed", false);

        if (hurtbox != null)
            hurtbox.enabled = false;

        if (debugLogs) Debug.Log("[Trap] ABRIU");
    }

    private bool AnyPlayerWithLight()
    {
        foreach (var player in playersInside)
        {
            if (player == null) continue;

            var pc = player.GetComponent<PlayerController>();

            if (pc != null)
            {
                if (debugLogs)
                    Debug.Log($"[Trap] Luz do player: {pc.LuzAtiva}");

                if (pc.LuzAtiva)
                    return true;
            }
        }

        return false;
    }
}