using UnityEngine;

[CreateAssetMenu(menuName = "Balls/Ball Data")]
public class BallData : ScriptableObject
{
    public string ballName;
    public int points;
    public float weight;
    public float size;
    public float launchVelocity = 6f;
}