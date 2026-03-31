using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public Color normalColor = Color.white;
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.1f;

    [Header("Lines")]
    public float lineLength = 10f;
    public float lineThickness = 2f;
    public float gapSize = 4f;

    private Image[] lines = new Image[4];
    private Image dot;
    private float hitTimer = 0f;
    private bool isFlashing = false;

    void Awake()
    {
        BuildCrosshair();
    }

    void BuildCrosshair()
    {
        // Dot central
        GameObject dotObj = CreateLine("Dot");
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(lineThickness, lineThickness);
        dot = dotObj.GetComponent<Image>();

        // Top
        lines[0] = CreateLine("Top").GetComponent<Image>();
        RectTransform top = lines[0].rectTransform;
        top.sizeDelta = new Vector2(lineThickness, lineLength);
        top.anchoredPosition = new Vector2(0, gapSize + lineLength / 2f);

        // Bottom
        lines[1] = CreateLine("Bottom").GetComponent<Image>();
        RectTransform bottom = lines[1].rectTransform;
        bottom.sizeDelta = new Vector2(lineThickness, lineLength);
        bottom.anchoredPosition = new Vector2(0, -(gapSize + lineLength / 2f));

        // Left
        lines[2] = CreateLine("Left").GetComponent<Image>();
        RectTransform left = lines[2].rectTransform;
        left.sizeDelta = new Vector2(lineLength, lineThickness);
        left.anchoredPosition = new Vector2(-(gapSize + lineLength / 2f), 0);

        // Right
        lines[3] = CreateLine("Right").GetComponent<Image>();
        RectTransform right = lines[3].rectTransform;
        right.sizeDelta = new Vector2(lineLength, lineThickness);
        right.anchoredPosition = new Vector2(gapSize + lineLength / 2f, 0);

        SetColor(normalColor);
    }

    GameObject CreateLine(string lineName)
    {
        GameObject obj = new GameObject(lineName);
        obj.transform.SetParent(transform, false);
        obj.AddComponent<Image>();
        return obj;
    }

    void Update()
    {
        if (isFlashing)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                isFlashing = false;
                SetColor(normalColor);
            }
        }
    }

    public void FlashHit()
    {
        isFlashing = true;
        hitTimer = hitFlashDuration;
        SetColor(hitColor);
    }

    void SetColor(Color color)
    {
        if (dot != null) dot.color = color;
        foreach (Image line in lines)
            if (line != null) line.color = color;
    }
}
