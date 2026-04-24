using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Adds a subtle scale-up on hover to menu buttons.
/// </summary>
public class MenuHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 _baseScale;
    private float _target = 1f;
    private float _current = 1f;
    const float Speed = 8f;

    void Awake()  { _baseScale = transform.localScale; }

    void Update()
    {
        _current = Mathf.Lerp(_current, _target, Time.unscaledDeltaTime * Speed);
        transform.localScale = _baseScale * _current;
    }

    public void OnPointerEnter(PointerEventData _) => _target = 1.03f;
    public void OnPointerExit(PointerEventData _)  => _target = 1f;
}
