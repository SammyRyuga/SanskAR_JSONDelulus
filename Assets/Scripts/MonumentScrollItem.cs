using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MonumentScrollItem : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image monumentImage;
    public Text monumentName;
    public GameObject selectionIndicator;
    public Button selectButton;
    
    [Header("Monument Data")]
    public GameObject monumentPrefab;
    public string displayName;
    public Sprite monumentSprite;
    
    [Header("Visual Effects")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1f);
    
    private int itemIndex;
    private MonumentScroller parentScroller;
    private bool isSelected = false;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Setup button if it exists
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnItemClicked);
        }
    }

    public void Initialize(int index, MonumentScroller scroller)
    {
        itemIndex = index;
        parentScroller = scroller;
        
        // Set up the visual elements
        if (monumentImage != null && monumentSprite != null)
        {
            monumentImage.sprite = monumentSprite;
        }
        
        if (monumentName != null)
        {
            monumentName.text = displayName;
        }
        
        // Initial state
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // Update visual indicators
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }
        
        // Update colors
        if (monumentImage != null)
        {
            monumentImage.color = selected ? selectedColor : normalColor;
        }
        
        // Update scale with smooth animation
        StartCoroutine(AnimateScale(selected ? selectedScale : normalScale));
        
        // Update alpha for non-selected items
        if (canvasGroup != null)
        {
            canvasGroup.alpha = selected ? 1f : 0.7f;
        }
    }

    private System.Collections.IEnumerator AnimateScale(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnItemClicked();
    }

    void OnItemClicked()
    {
        if (parentScroller != null)
        {
            parentScroller.SelectMonument(itemIndex);
        }
    }

    // Getters
    public GameObject GetMonumentPrefab()
    {
        return monumentPrefab;
    }

    public string GetDisplayName()
    {
        return displayName;
    }

    public int GetIndex()
    {
        return itemIndex;
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    // Method to update monument data
    public void SetMonumentData(GameObject prefab, string name, Sprite sprite)
    {
        monumentPrefab = prefab;
        displayName = name;
        monumentSprite = sprite;
        
        // Update UI if already initialized
        if (monumentImage != null && monumentSprite != null)
        {
            monumentImage.sprite = monumentSprite;
        }
        
        if (monumentName != null)
        {
            monumentName.text = displayName;
        }
    }
}