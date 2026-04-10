using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    [SerializeField] GameObject ballPrefab;

    [SerializeField] Transform drawPileParent;
    [SerializeField] Transform handPileParent;
    [SerializeField] Transform playPileParent;
    [SerializeField] Transform discardPileParent;

    //List<Joker> jokers = new List<Joker>();

    List<Ball> drawPile = new List<Ball>();
    List<Ball> handPile = new List<Ball>();
    List<Ball> playPile = new List<Ball>();
    List<Ball> discardPile = new List<Ball>();

    Roulette_Controller roulette;

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
        //handle state changes and next button click
        //when enter is clicked
        //also handle shop?
        if (IsConfirmPressed())
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
            MoveBallToPile(ball, handPileParent, handPile.Count - 1);
            ball.ToggleHand();
        }

        play_state = PlayState.selectBalls;
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
            MoveBallToPile(ball, playPileParent, playPile.Count - 1);
            ball.ToggleHand();
        }
        else if (playPile.Contains(ball))
        {
            playPile.Remove(ball);
            handPile.Add(ball);
            MoveBallToPile(ball, handPileParent, handPile.Count - 1);
            ball.ToggleHand();
        }
    }

    void GenerateStartingDeck(int amount)
    {
        drawPile.Clear();
        handPile.Clear();
        playPile.Clear();
        discardPile.Clear();

        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(ballPrefab, drawPileParent);
            Ball ball = obj.GetComponent<Ball>();

            ball.Initialize();

            drawPile.Add(ball);
            MoveBallToPile(ball, drawPileParent, i);
        }
    }

    public void SelectBalls()
    {
        play_state = PlayState.selectBalls;
    }

    public void PlayBalls()
    {
        if (play_state != PlayState.selectBalls && play_state != PlayState.playBalls)
            return;

        play_state = PlayState.playBalls;
        if (playPile.Count == 0)
            return;

        int roundPoints = 0;
        for (int i = playPile.Count - 1; i >= 0; i--)
        {
            Ball ball = playPile[i];
            roundPoints += ball.GetPoints();

            playPile.RemoveAt(i);
            discardPile.Add(ball);
            MoveBallToPile(ball, discardPileParent, discardPile.Count - 1);
            if (ball.InHand)
            {
                ball.ToggleHand();
            }
        }

        totalPoints += roundPoints;
        Debug.Log($"Round points: {roundPoints}. Total points: {totalPoints}");
        play_state = PlayState.postRound;
    }

    void PostRound()
    {
        if (play_state != PlayState.postRound)
            return;

        for (int i = handPile.Count - 1; i >= 0; i--)
        {

            Ball ball = handPile[i];
            handPile.RemoveAt(i);
            discardPile.Add(ball);
            MoveBallToPile(ball, discardPileParent, discardPile.Count - 1);
            if (ball.InHand)
            {
                ball.ToggleHand();
            }
        }

        StartRound();
    }

    public void StartRound()
    {
        play_state = PlayState.drawBalls;
        DrawBalls();
    }

    private void MoveBallToPile(Ball ball, Transform parent, int index)
    {
        if (ball == null || parent == null)
            return;

        ball.transform.SetParent(parent);
        ball.transform.localPosition = new Vector3(0f, 0f, index * 0.25f);
        ball.transform.localRotation = Quaternion.identity;
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
            MoveBallToPile(ball, drawPileParent, drawPile.Count - 1);
        }
    }

    public PlayState GetPlayState()
    {
        return play_state;
    }

    public void NextPlayState()
    {
        play_state = play_state++;
    }
}