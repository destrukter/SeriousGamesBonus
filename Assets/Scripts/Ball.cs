using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private BallData data;

    private string runtimeName;
    private int runtimePoints;
    private float runtimeWeight;
    private float runtimeSize;
    private float runtimeLaunchVelocity;

    private bool inHand = false;

    public BallData Data => data;
    public bool InHand => inHand;

    public void Initialize()
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
            runtimeSize = 1f;
            runtimeWeight = 1f;
            runtimePoints = 1;
            runtimeLaunchVelocity = 6f;
        }

        inHand = false;
    }

    private void OnMouseDown()
    {
        if (Events.current != null)
        {
            Events.current.BallClicked(this);
        }
    }

    public void ToggleHand()
    {
        inHand = !inHand;
    }

    public int GetPoints()
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
