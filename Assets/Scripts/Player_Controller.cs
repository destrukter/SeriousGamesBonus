using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    [SerializeField] GameObject ballPrefab;

    [SerializeField] Transform drawPileParent;
    [SerializeField] Transform handPileParent;
    [SerializeField] Transform playPileParent;
    [SerializeField] Transform discardPileParent;
    [SerializeField] Transform playOrigin;

    List<Ball> drawPile = new List<Ball>();
    List<Ball> handPile = new List<Ball>();
    List<Ball> playPile = new List<Ball>();
    List<Ball> discardPile = new List<Ball>();

    float pileStackOffset = 0.02f;

    [SerializeField] int num_draw = 3;
    [SerializeField] int hand_size = 5;
    [SerializeField] int play_size = 3;
    [SerializeField] int start_size = 20;
    [SerializeField] float playSpawnDelaySeconds = 0.15f;
    [SerializeField] float pileRandomOffsetXZ = 0.01f;
    [SerializeField] TMP_Text roundScoreText;

    int totalPoints = 0;
    int roundPoints = 0;
    int roundRedPoints = 0;
    int roundBluePoints = 0;
    int roundGreenPoints = 0;
    bool isSpawningPlayBalls = false;
    int pendingStopEvents = 0;
    bool awaitingRoundCompletion = false;
    Coroutine waitForRoundCompletionRoutine;
    Events subscribedEvents;

    public enum PlayState { drawBalls, selectBalls, playBalls, postRound }
    PlayState play_state = PlayState.postRound;

    private void Start()
    {
        subscribedEvents = ResolveEventsInstance();
        if (subscribedEvents != null)
        {
            subscribedEvents.OnBallClicked += OnBallClicked;
            subscribedEvents.OnStopTriggered += OnStopTriggered;
            subscribedEvents.OnPointsAwarded += OnPointsAwarded;
        }
        else
        {
            Debug.LogWarning("Player_Controller could not find an Events instance during Start.");
        }

        GenerateStartingDeck(start_size);
        StartRound();
    }

    private void OnDestroy()
    {
        if (subscribedEvents != null)
        {
            subscribedEvents.OnBallClicked -= OnBallClicked;
            subscribedEvents.OnStopTriggered -= OnStopTriggered;
            subscribedEvents.OnPointsAwarded -= OnPointsAwarded;
        }
    }

    void Update()
    {
        if (play_state == PlayState.selectBalls && IsConfirmPressed())
        {
            PlayBalls();
        }
    }

    bool IsConfirmPressed()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
    }

    void DrawBalls()
    {
        if (play_state != PlayState.drawBalls)
            return;

        int needed = Mathf.Max(0, hand_size - handPile.Count);
        int drawCount = Mathf.Min(num_draw, needed);

        if (drawCount <= 0)
        {
            play_state = PlayState.selectBalls;
            UpdatePileCounters();
            return;
        }

        if (drawPile.Count < drawCount)
        {
            RefillDrawPileFromDiscard();
        }

        drawCount = Mathf.Min(drawCount, drawPile.Count);
        if (drawCount <= 0)
        {
            play_state = PlayState.selectBalls;
            UpdatePileCounters();
            return;
        }

        System.Random rng = new System.Random();
        List<Ball> drawnBalls = drawPile
            .OrderBy(x => rng.Next())
            .Take(drawCount)
            .ToList();

        foreach (var ball in drawnBalls)
        {
            drawPile.Remove(ball);
            handPile.Add(ball);
            MoveBallToPile(ball, handPileParent);
            ball.ToggleHand();
        }

        play_state = PlayState.selectBalls;
        UpdatePileCounters();
    }

    private void OnBallClicked(Ball ball)
    {
        if (play_state != PlayState.selectBalls)
            return;

        if (handPile.Contains(ball))
        {
            if (playPile.Count >= play_size)
                return;

            handPile.Remove(ball);
            playPile.Add(ball);
            MoveBallToPile(ball, playPileParent);
            ball.ToggleHand();
        }
        else if (playPile.Contains(ball))
        {
            playPile.Remove(ball);
            handPile.Add(ball);
            MoveBallToPile(ball, handPileParent);
            ball.ToggleHand();
        }

        UpdatePileCounters();
    }

    void GenerateStartingDeck(int amount)
    {
        drawPile.Clear();
        handPile.Clear();
        playPile.Clear();
        discardPile.Clear();

        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(ballPrefab);
            Ball ball = obj.GetComponent<Ball>();
            ball.Initialize();

            drawPile.Add(ball);
            MoveBallToPile(ball, drawPileParent);
        }

        UpdatePileCounters();
    }

    public void SelectBalls()
    {
        play_state = PlayState.selectBalls;
        UpdatePileCounters();
    }

    public void PlayBalls()
    {
        if (play_state != PlayState.selectBalls)
            return;

        play_state = PlayState.playBalls;
        pendingStopEvents = playPile.Count + 1; // one stop event per play ball + one for roulette
        awaitingRoundCompletion = pendingStopEvents > 0;
        TriggerPlayEvent();

        if (playPile.Count == 0)
        {
            TryCompleteRoundFromStopEvents();
            UpdatePileCounters();
            return;
        }

        StartCoroutine(SpawnPlayBallsRoutine());
    }

    private void TriggerPlayEvent()
    {
        Events eventsInstance = ResolveEventsInstance();
        if (eventsInstance == null)
        {
            Debug.LogWarning("Play trigger requested, but no Events instance is available.");
            return;
        }

        eventsInstance.PlayTriggered();
    }

    private Events ResolveEventsInstance()
    {
        if (Events.current != null)
            return Events.current;

        return FindFirstObjectByType<Events>();
    }

    private IEnumerator SpawnPlayBallsRoutine()
    {
        isSpawningPlayBalls = true;
        float spawnDelay = Mathf.Max(0f, playSpawnDelaySeconds);

        for (int i = playPile.Count - 1; i >= 0; i--)
        {
            Ball ball = playPile[i];
            SpawnBallForPlay(ball);

            if (spawnDelay > 0f && i > 0)
            {
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        isSpawningPlayBalls = false;
    }

    private void OnStopTriggered()
    {
        if (play_state != PlayState.playBalls || !awaitingRoundCompletion)
            return;

        pendingStopEvents = Mathf.Max(0, pendingStopEvents - 1);
        TryCompleteRoundFromStopEvents();
    }

    private void TryCompleteRoundFromStopEvents()
    {
        if (play_state != PlayState.playBalls || !awaitingRoundCompletion || pendingStopEvents > 0)
            return;

        awaitingRoundCompletion = false;
        if (waitForRoundCompletionRoutine != null)
        {
            StopCoroutine(waitForRoundCompletionRoutine);
        }
        waitForRoundCompletionRoutine = StartCoroutine(FinishRoundRoutine());
    }

    private IEnumerator FinishRoundRoutine()
    {
        TriggerGameEndEvent();
        yield return null; // wait for slot point calculation listeners
        PostRound();
        waitForRoundCompletionRoutine = null;
    }

    void PostRound()
    {
        if (play_state != PlayState.playBalls)
            return;
        play_state = PlayState.postRound;

        for (int i = playPile.Count - 1; i >= 0; i--)
        {
            Ball ball = playPile[i];
            playPile.RemoveAt(i);
            discardPile.Add(ball);
            MoveBallToPile(ball, discardPileParent);
            if (ball.InHand)
            {
                ball.ToggleHand();
            }
            totalPoints += ball.GetPoints();
        }

        //roundPoints += ball.GetPoints();
        /*for (int i = handPile.Count - 1; i >= 0; i--)
        {
            Ball ball = handPile[i];
            handPile.RemoveAt(i);
            discardPile.Add(ball);
            MoveBallToPile(ball, discardPileParent);

            if (ball.InHand)
            {
                ball.ToggleHand();
            }
        }*/
        UpdatePileCounters();
        StartRound();
    }

    private void TriggerGameEndEvent()
    {
        Events eventsInstance = ResolveEventsInstance();
        if (eventsInstance == null)
        {
            Debug.LogWarning("Game end trigger requested, but no Events instance is available.");
            return;
        }

        eventsInstance.GameEndTriggered();
    }

    public void StartRound()
    {
        if (play_state != PlayState.postRound)
            return;

        ResetRoundPoints();
        play_state = PlayState.drawBalls;
        DrawBalls();
    }

    private void ResetRoundPoints()
    {
        roundPoints = 0;
        roundRedPoints = 0;
        roundBluePoints = 0;
        roundGreenPoints = 0;
        UpdateRoundScoreText();
    }

    private void OnPointsAwarded(int points, string color)
    {
        if (points <= 0)
            return;

        roundPoints += points;

        switch (color)
        {
            case "Red":
                roundRedPoints += points;
                break;
            case "Green":
                roundGreenPoints += points;
                break;
            default:
                roundBluePoints += points;
                break;
        }

        UpdateRoundScoreText();
    }

    private void UpdateRoundScoreText()
    {
        if (roundScoreText == null)
            return;

        roundScoreText.text = $"Round Score: {roundPoints}\nR: {roundRedPoints}  B: {roundBluePoints}  G: {roundGreenPoints}";
    }

    private void SpawnBallForPlay(Ball ball)
    {
        if (ball == null || playOrigin == null)
            return;

        ball.transform.SetParent(playOrigin.parent);
        ball.transform.position = playOrigin.position;
        ball.transform.rotation = playOrigin.rotation;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            float randomYaw = Random.Range(-25f, 25f);   // left/right
            float randomPitch = Random.Range(-15f, 15f); // up/down

            Quaternion yawRotation = Quaternion.AngleAxis(randomYaw, playOrigin.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(randomPitch, playOrigin.right);

            Vector3 direction = yawRotation * pitchRotation * playOrigin.forward;

            rb.linearVelocity = direction.normalized * ball.GetLaunchVelocity();
        }

        ball.MarkAsInPlay();
    }

    private void MoveBallToPile(Ball ball, Transform parent)
    {
        if (ball == null || parent == null)
            return;

        ball.transform.SetParent(parent);

        int stackIndex = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == ball.transform)
                continue;

            if (child.GetComponent<Ball>() != null)
            {
                stackIndex++;
            }
        }

        float stackOffset = parent.position.y * (pileStackOffset * stackIndex) + 5f;
        float randomOffsetX = Random.Range(-pileRandomOffsetXZ, pileRandomOffsetXZ);
        float randomOffsetZ = Random.Range(-pileRandomOffsetXZ, pileRandomOffsetXZ);
        ball.transform.position = new Vector3(parent.position.x + randomOffsetX, stackOffset, parent.position.z + randomOffsetZ);
        //ball.transform.position = new Vector3(parent.position.x, stackOffset, parent.position.z);

        ball.transform.rotation = parent.rotation;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void RefillDrawPileFromDiscard()
    {
        if (discardPile.Count == 0)
            return;

        System.Random rng = new System.Random();
        List<Ball> shuffled = discardPile.OrderBy(x => rng.Next()).ToList();

        discardPile.Clear();
        for (int i = 0; i < shuffled.Count; i++)
        {
            Ball ball = shuffled[i];
            drawPile.Add(ball);
            MoveBallToPile(ball, drawPileParent);
        }

        UpdatePileCounters();
    }

    private void UpdatePileCounters()
    {
        UpdatePileCounter(drawPileParent, drawPile.Count);
        UpdatePileCounter(discardPileParent, discardPile.Count);
        UpdatePileCounter(handPileParent, handPile.Count, hand_size);
        UpdatePileCounter(playPileParent, playPile.Count, play_size);
    }

    private void UpdatePileCounter(Transform pileRoot, int count, int? max = null)
    {
        if (pileRoot == null)
            return;

        Transform numTransform = pileRoot.Find("Num");
        if (numTransform == null)
            return;

        TMP_Text label = numTransform.GetComponent<TMP_Text>();
        if (label == null)
            return;

        label.text = max.HasValue ? $"{count}/{max.Value}" : count.ToString();
    }

    public PlayState GetPlayState()
    {
        return play_state;
    }
}
