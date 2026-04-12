using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private BallData data;

    protected string runtimeName;
    protected int runtimePoints;
    protected float runtimeWeight;
    protected float runtimeSize;
    protected float runtimeLaunchVelocity;

    private bool inHand = false;
    private bool isInPlay;
    private bool stopEventSent;
    private float stillTime;
    private Rigidbody rb;

    private const float StopSpeedThreshold = 0.05f;
    private const float StopAngularSpeedThreshold = 0.05f;
    private const float StopConfirmTime = 0.25f;

    public BallData Data => data;
    public bool InHand => inHand;
    public bool IsInPlay => isInPlay;

    public virtual void Initialize()
    {
        if (data != null)
        {
            runtimeName = string.IsNullOrWhiteSpace(data.ballName) ? "Standard" : data.ballName;
            runtimeSize = data.size <= 0f ? 1f : data.size;
            runtimeWeight = data.weight <= 0f ? 1f : data.weight;
            runtimePoints = data.points;
            runtimeLaunchVelocity = data.launchVelocity <= 0f ? 6f : data.launchVelocity;
        }
        else
        {
            runtimeName = "Standard";
            runtimeSize = 0.1f;
            runtimeWeight = 0.01f;
            runtimePoints = 1;
            runtimeLaunchVelocity = 50f;
        }

        inHand = false;
        isInPlay = false;
        stopEventSent = false;
        stillTime = 0f;
        rb = GetComponent<Rigidbody>();

        OnBallInitialized();
    }

    protected virtual void OnBallInitialized()
    {
    }

    private void OnMouseDown()
    {
        if (Events.current != null)
        {
            Events.current.BallClicked(this);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Ball otherBall = collision.gameObject.GetComponent<Ball>();
        if (otherBall != null)
        {
            OnCollidedWithBall(otherBall, collision);
        }
    }

    protected virtual void OnCollidedWithBall(Ball otherBall, Collision collision)
    {
    }

    public void ToggleHand()
    {
        inHand = !inHand;
    }

    public void MarkAsInPlay()
    {
        isInPlay = true;
        stopEventSent = false;
        stillTime = 0f;
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (!isInPlay || stopEventSent || rb == null)
            return;

        bool isSlowEnough =
            rb.linearVelocity.sqrMagnitude <= StopSpeedThreshold * StopSpeedThreshold &&
            rb.angularVelocity.sqrMagnitude <= StopAngularSpeedThreshold * StopAngularSpeedThreshold;

        if (!isSlowEnough)
        {
            stillTime = 0f;
            return;
        }

        stillTime += Time.deltaTime;
        if (stillTime < StopConfirmTime)
            return;

        stopEventSent = true;
        isInPlay = false;
        if (Events.current != null)
        {
            Events.current.StopTriggered();
        }
    }

    public virtual int GetPoints()
    {
        return runtimePoints;
    }

    public float GetWeight()
    {
        return runtimeWeight;
    }

    public float GetSize()
    {
        return runtimeSize;
    }

    public float GetLaunchVelocity()
    {
        return runtimeLaunchVelocity;
    }
}
