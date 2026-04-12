using System.Collections;
using UnityEngine;

public class JackpotJellyBall : Ball
{
    [Header("Jelly Ball")]
    [SerializeField] private int firstHitBonusPoints = 5;
    [SerializeField] private float collisionBounceImpulse = 0.75f;
    [SerializeField] private float squishMultiplier = 1.2f;
    [SerializeField] private float squishDurationSeconds = 0.1f;

    private bool firstHitBonusUnlocked;
    private Coroutine squishRoutine;

    public override void Initialize()
    {
        base.Initialize();
        firstHitBonusUnlocked = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = Mathf.Max(0.005f, GetWeight() * 0.7f);
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.02f;
        }

        transform.localScale = Vector3.one * Mathf.Max(0.05f, GetSize() * 1.1f);
    }

    protected override void OnCollidedWithBall(Ball otherBall, Collision collision)
    {
        if (!firstHitBonusUnlocked)
        {
            firstHitBonusUnlocked = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 bounceDirection = collision.contacts.Length > 0
                ? -collision.contacts[0].normal
                : (transform.position - otherBall.transform.position).normalized;
            rb.AddForce(bounceDirection * collisionBounceImpulse, ForceMode.Impulse);
        }

        if (squishRoutine != null)
        {
            StopCoroutine(squishRoutine);
        }
        squishRoutine = StartCoroutine(SquishRoutine());
    }

    public override int GetPoints()
    {
        return base.GetPoints() + (firstHitBonusUnlocked ? firstHitBonusPoints : 0);
    }

    private IEnumerator SquishRoutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 squishScale = new Vector3(
            originalScale.x * squishMultiplier,
            originalScale.y * (2f - squishMultiplier),
            originalScale.z * squishMultiplier);

        float elapsed = 0f;
        while (elapsed < squishDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / squishDurationSeconds);
            transform.localScale = Vector3.Lerp(originalScale, squishScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < squishDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / squishDurationSeconds);
            transform.localScale = Vector3.Lerp(squishScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        squishRoutine = null;
    }
}

public class GremlinSplitterBall : Ball
{
    [Header("Gremlin Splitter")]
    [SerializeField] private Ball spawnedBallPrefab;
    [SerializeField] private int maxSplits = 2;
    [SerializeField] private float splitSpawnOffset = 0.04f;
    [SerializeField] private float splitImpulse = 1.2f;

    private int splitCount;

    public override void Initialize()
    {
        base.Initialize();
        splitCount = 0;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = Mathf.Max(0.01f, GetWeight() * 1.4f);
            rb.linearDamping = 0.01f;
            rb.angularDamping = 0.01f;
        }

        transform.localScale = Vector3.one * Mathf.Max(0.05f, GetSize() * 0.9f);
    }

    protected override void OnCollidedWithBall(Ball otherBall, Collision collision)
    {
        if (!IsInPlay || splitCount >= maxSplits || spawnedBallPrefab == null)
            return;

        splitCount++;

        Vector3 spawnNormal = collision.contacts.Length > 0
            ? collision.contacts[0].normal
            : (transform.position - otherBall.transform.position).normalized;

        Vector3 spawnPosition = transform.position + spawnNormal * splitSpawnOffset;
        Ball newBall = Instantiate(spawnedBallPrefab, spawnPosition, Random.rotation);
        newBall.Initialize();
        newBall.MarkAsInPlay();

        Rigidbody newBallRb = newBall.GetComponent<Rigidbody>();
        if (newBallRb != null)
        {
            Vector3 splitDirection = (spawnNormal + Random.insideUnitSphere * 0.2f).normalized;
            newBallRb.linearVelocity = splitDirection * Mathf.Max(0.25f, splitImpulse);
        }
    }
}
