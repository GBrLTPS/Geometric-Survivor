using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArcadeMenuButtonUI : MonoBehaviour
{
    [Header("Visual do Botão")]
    public Image buttonBackground;
    public TextMeshProUGUI buttonText;

    [Header("Cores de Destaque")]
    public Color normalColor = Color.gray;
    public Color selectedColor = Color.yellow;

    public void SetSelected(bool isSelected)
    {
        if (buttonBackground != null)
        {
            buttonBackground.color = isSelected ? selectedColor : normalColor;
        }

        // Efeito de leve destaque na escala
        transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
    }
}