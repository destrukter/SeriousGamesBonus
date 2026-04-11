using UnityEngine;
using TMPro;

public class Tile : MonoBehaviour
{
    public enum TileColor
    {
        Red,
        Black,
        Green
    }

    [SerializeField] int value;
    [SerializeField] float angle;
    [SerializeField] TileColor tileColor;
    [SerializeField] TMP_Text valueText;

    public int Value => value;
    public float Angle => angle;
    public TileColor Color => tileColor;

    private void Awake()
    {
        if (valueText == null)
        {
            valueText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void Configure(int assignedValue, float assignedAngle, TileColor assignedColor)
    {
        value = assignedValue;
        angle = assignedAngle;
        tileColor = assignedColor;

        if (valueText == null)
        {
            valueText = GetComponentInChildren<TMP_Text>(true);
        }

        if (valueText != null)
        {
            valueText.text = value.ToString();
        }
    }
}
