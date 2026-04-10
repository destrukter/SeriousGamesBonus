using UnityEngine;
using System.Collections;

public class Roulette_Controller : MonoBehaviour
{
    [SerializeField] float spinDurationSeconds = 2.8f;
    [SerializeField] float spinForceMin = 260f;
    [SerializeField] float spinForceMax = 420f;

    [SerializeField] GameObject roulettePart1;
    [SerializeField] GameObject roulettePart2;
    //Rigidbody rb1;
    //Rigidbody rb2;

    Coroutine spinRoutine;

    private void OnEnable()
    {
        if (Events.current != null)
        {
            Events.current.OnPlayTriggered += OnPlayTriggered;
        }
        //rb1 = roulettePart1.GetComponent<Rigidbody>();
        //rb2 = roulettePart2.GetComponent<Rigidbody>();
    }

    private void OnDisable()
    {
        if (Events.current != null)
        {
            Events.current.OnPlayTriggered -= OnPlayTriggered;
        }
    }

    private void OnPlayTriggered()
    {
        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
        }

        float minForce = Mathf.Min(spinForceMin, spinForceMax);
        float maxForce = Mathf.Max(spinForceMin, spinForceMax);
        float spinForce = Random.Range(minForce, maxForce);
        spinRoutine = StartCoroutine(SpinRoutine(spinForce));
    }

    private IEnumerator SpinRoutine(float spinForce)
    {
        float duration = Mathf.Max(0.1f, spinDurationSeconds);
        float randomDirection = Random.value > 0.5f ? 1f : -1f;
        float startingSpeed = Mathf.Abs(spinForce) * randomDirection;

        float elapsed = 0f;
        float accumulatedDegrees = 0f;
        Quaternion startRotation = roulettePart1.transform.localRotation;
        Quaternion startRotation2 = roulettePart2.transform.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentSpeed = Mathf.Lerp(startingSpeed, 0f, t);
            accumulatedDegrees += currentSpeed * Time.deltaTime;
            roulettePart1.transform.localRotation = startRotation * Quaternion.Euler(0f, accumulatedDegrees, 0f);
            roulettePart2.transform.localRotation = startRotation * Quaternion.Euler(0f, accumulatedDegrees, 0f);
            yield return null;
        }

        spinRoutine = null;
    }
}
