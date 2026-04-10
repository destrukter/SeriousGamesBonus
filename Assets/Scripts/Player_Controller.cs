using UnityEngine;
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

    [SerializeField] int num_draw = 3;
    [SerializeField] int hand_size = 5;
    [SerializeField] int play_size = 3;

    int totalPoints = 0;

    public enum PlayState { drawBalls, selectBalls, playBalls, postRound }
    PlayState play_state = PlayState.drawBalls;

    private void Start()
    {
        if (Events.current != null)
        {
            Events.current.OnBallClicked += OnBallClicked;
        }

        GenerateStartingDeck(10);
        StartRound();
    }

    private void OnDestroy()
    {
        if (Events.current != null)
        {
            Events.current.OnBallClicked -= OnBallClicked;
        }
    }

    void Update()
    {
        if (play_state == PlayState.selectBalls && IsConfirmPressed())
        {
            PlayBalls();
            PostRound();
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

        if (playPile.Count == 0)
        {
            UpdatePileCounters();
            return;
        }

        int roundPoints = 0;
        for (int i = playPile.Count - 1; i >= 0; i--)
        {
            Ball ball = playPile[i];
            roundPoints += ball.GetPoints();

            SpawnBallForPlay(ball);

            playPile.RemoveAt(i);
            discardPile.Add(ball);
            MoveBallToPile(ball, discardPileParent);

            if (ball.InHand)
            {
                ball.ToggleHand();
            }
        }

        totalPoints += roundPoints;
        Debug.Log($"Round points: {roundPoints}. Total points: {totalPoints}");

        UpdatePileCounters();
    }

    void PostRound()
    {
        if (play_state != PlayState.playBalls)
            return;

        for (int i = handPile.Count - 1; i >= 0; i--)
        {
            Ball ball = handPile[i];
            handPile.RemoveAt(i);
            discardPile.Add(ball);
            MoveBallToPile(ball, discardPileParent);

            if (ball.InHand)
            {
                ball.ToggleHand();
            }
        }

        play_state = PlayState.postRound;
        UpdatePileCounters();
    }

    public void StartRound()
    {
        if (play_state != PlayState.postRound && play_state != PlayState.drawBalls)
            return;

        play_state = PlayState.drawBalls;
        DrawBalls();
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
            float randomAngle = Random.Range(-25f, 25f);
            Vector3 direction = Quaternion.AngleAxis(randomAngle, playOrigin.up) * playOrigin.forward;
            rb.velocity = direction.normalized * ball.GetLaunchVelocity();
        }
    }

    private void MoveBallToPile(Ball ball, Transform parent)
    {
        if (ball == null || parent == null)
            return;

        ball.transform.SetParent(parent);
        ball.transform.position = parent.position;
        ball.transform.rotation = parent.rotation;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
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
