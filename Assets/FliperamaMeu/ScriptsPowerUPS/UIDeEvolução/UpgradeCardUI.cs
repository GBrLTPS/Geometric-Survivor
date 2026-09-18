using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("Elementos de UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage;
    public Image borderHighlight; // A borda que acende quando selecionado

    [Header("Cores de Destaque")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedColor = new Color(1f, 0.85f, 0f, 1f); // Amarelo/Dourado

    public UpgradeData currentUpgrade { get; private set; }

    public void Setup(UpgradeData data)
    {
        currentUpgrade = data;

        if (titleText != null) titleText.text = data.upgradeName;
        if (descriptionText != null) descriptionText.text = data.description;
        
        if (iconImage != null)
        {
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (borderHighlight != null)
        {
            borderHighlight.color = isSelected ? selectedColor : normalColor;
        }

        // Leve aumento de escala ao focar no card
        transform.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;
    }
}