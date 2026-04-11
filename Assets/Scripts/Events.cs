using System;
using UnityEngine;

public class Events : MonoBehaviour
{
    public static Events current;

    private void Awake()
    {
        if (current == null)
        {
            current = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public event Action<Ball> OnBallClicked;
    public event Action OnPlayTriggered;
    public event Action OnStopTriggered;
    public event Action OnGameEndTriggered;
    public event Action<int, string> OnPointsAwarded;

    public void BallClicked(Ball ball)
    {
        OnBallClicked?.Invoke(ball);
    }

    public void PlayTriggered()
    {
        Debug.Log("Play button triggered, invoking OnPlayTriggered event.");
        OnPlayTriggered?.Invoke();
    }

    public void StopTriggered()
    {
        OnStopTriggered?.Invoke();
    }

    public void GameEndTriggered()
    {
        OnGameEndTriggered?.Invoke();
    }

    public void PointsAwarded(int amount, string color)
    {
        OnPointsAwarded?.Invoke(amount, color);
    }
}
