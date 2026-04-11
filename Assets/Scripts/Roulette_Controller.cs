using UnityEngine;
using System.Collections;

public class Roulette_Controller : MonoBehaviour
{
    [Header("Spin")]
    [SerializeField] float spinDurationSeconds = 70f;
    [SerializeField] float spinForceMin = 5000f;
    [SerializeField] float spinForceMax = 15000f;

    [SerializeField] GameObject roulettePart1;
    [SerializeField] GameObject roulettePart2;

    [Header("Tile Spawning")]
    [SerializeField] bool spawnTilesOnStart;
    [SerializeField] bool clearExistingTilesBeforeSpawning = true;
    [SerializeField] GameObject tilePrefab;
    [SerializeField] Transform tileParent;
    [SerializeField] float tileRadius = 0.25f;
    [SerializeField] float tileHeightOffset;

    static readonly float[] TileAngles =
    {
        0f,
        9.73f,
        19.46f,
        29.19f,
        38.92f,
        48.65f,
        58.38f,
        68.11f,
        77.84f,
        87.57f,
        97.30f,
        107.03f,
        116.76f,
        126.49f,
        136.22f,
        145.95f,
        155.68f,
        165.41f,
        175.14f,
        184.87f,
        194.60f,
        204.33f,
        214.06f,
        223.79f,
        233.52f,
        243.25f,
        252.98f,
        262.71f,
        272.44f,
        282.17f,
        291.90f,
        301.63f,
        311.36f,
        321.09f,
        330.82f,
        340.55f,
        350.28f,
        360f,
    };

    // European roulette wheel order + repeated 0 for the 360° position.
    static readonly int[] TileValues =
    {
        0,
        32,
        15,
        19,
        4,
        21,
        2,
        25,
        17,
        34,
        6,
        27,
        13,
        36,
        11,
        30,
        8,
        23,
        10,
        5,
        24,
        16,
        33,
        1,
        20,
        14,
        31,
        9,
        22,
        18,
        29,
        7,
        28,
        12,
        35,
        3,
        26,
        0,
    };

    Coroutine spinRoutine;
    Coroutine subscribeRoutine;
    bool isSubscribedToPlayEvent;

    private void Start()
    {
        if (spawnTilesOnStart)
        {
            SpawnRouletteTiles();
        }
        //tileParent = this.gameObject.transform;
    }

    private void OnEnable()
    {
        if (!TrySubscribeToPlayEvent())
        {
            subscribeRoutine = StartCoroutine(WaitForEventsAndSubscribe());
        }
    }

    private void OnDisable()
    {
        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        TryUnsubscribeFromPlayEvent();
    }

    [ContextMenu("Spawn Roulette Tiles")]
    public void SpawnRouletteTiles()
    {
        if (tilePrefab == null)
        {
            Debug.LogWarning("Tile prefab is not assigned on Roulette_Controller.");
            return;
        }

        Transform parent = tileParent != null ? tileParent : transform;

        if (clearExistingTilesBeforeSpawning)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        int spawnCount = Mathf.Min(TileAngles.Length, TileValues.Length);
        for (int i = 0; i < spawnCount; i++)
        {
            float angle = TileAngles[i];
            int value = TileValues[i];

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 localPosition = direction * tileHeightOffset;

            GameObject tileInstance = Instantiate(tilePrefab, parent);
            tileInstance.name = $"Tile_{i:00}_{value}";
            tileInstance.transform.localPosition = localPosition;
            tileInstance.transform.localRotation = Quaternion.Euler(0f, angle, 0f);

            Tile tile = tileInstance.GetComponent<Tile>();
            if (tile != null)
            {
                tile.Configure(value, angle, GetColorFromValue(value));
            }
        }
    }

    private IEnumerator WaitForEventsAndSubscribe()
    {
        while (!isSubscribedToPlayEvent)
        {
            if (TrySubscribeToPlayEvent())
            {
                subscribeRoutine = null;
                yield break;
            }

            yield return null;
        }
    }

    private bool TrySubscribeToPlayEvent()
    {
        if (isSubscribedToPlayEvent || Events.current == null)
            return false;

        Events.current.OnPlayTriggered += OnPlayTriggered;
        isSubscribedToPlayEvent = true;
        return true;
    }

    private void TryUnsubscribeFromPlayEvent()
    {
        if (!isSubscribedToPlayEvent)
            return;

        if (Events.current != null)
        {
            Events.current.OnPlayTriggered -= OnPlayTriggered;
        }

        isSubscribedToPlayEvent = false;
    }

    private void OnPlayTriggered()
    {
        if (roulettePart1 == null || roulettePart2 == null)
        {
            Debug.LogWarning("Roulette parts are not assigned on Roulette_Controller.");
            return;
        }
        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
        }
        Debug.Log("Roulette spin triggered with force range: " + spinForceMin + " to " + spinForceMax);

        float minForce = Mathf.Min(spinForceMin, spinForceMax);
        float maxForce = Mathf.Max(spinForceMin, spinForceMax);
        float spinForce = Random.Range(minForce, maxForce);
        spinRoutine = StartCoroutine(SpinRoutine(spinForce));
    }

    private IEnumerator SpinRoutine(float spinForce)
    {
        float duration = Mathf.Max(4f, spinDurationSeconds);

        float startingSpeed = Mathf.Abs(spinForce);

        float elapsed = 0f;
        float accumulatedDegrees = 0f;
        Quaternion startRotation1 = roulettePart1.transform.localRotation;
        Quaternion startRotation2 = roulettePart2.transform.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentSpeed = Mathf.Lerp(startingSpeed, 0f, t);
            accumulatedDegrees += currentSpeed * Time.deltaTime;
            roulettePart1.transform.localRotation = startRotation1 * Quaternion.Euler(0f, 0f, accumulatedDegrees);
            roulettePart2.transform.localRotation = startRotation1 * Quaternion.Euler(0f, 0f, accumulatedDegrees);
            yield return null;
        }

        spinRoutine = null;
        if (Events.current != null)
        {
            Events.current.StopTriggered();
        }
    }

    private static Tile.TileColor GetColorFromValue(int value)
    {
        if (value == 0)
            return Tile.TileColor.Green;

        switch (value)
        {
            case 1:
            case 3:
            case 5:
            case 7:
            case 9:
            case 12:
            case 14:
            case 16:
            case 18:
            case 19:
            case 21:
            case 23:
            case 25:
            case 27:
            case 30:
            case 32:
            case 34:
            case 36:
                return Tile.TileColor.Red;
            default:
                return Tile.TileColor.Black;
        }
    }
}
