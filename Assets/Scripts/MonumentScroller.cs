using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MonumentScroller : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Scroll Settings")]
    public RectTransform contentPanel;
    public float itemWidth = 200f;
    public float snapSpeed = 10f;
    public float snapThreshold = 50f;
    
    [Header("Monument Items")]
    public List<MonumentScrollItem> monumentItems = new List<MonumentScrollItem>();
    
    [Header("References")]
    public ARObjectManager arObjectManager;
    
    private int currentIndex = 0;
    private bool isDragging = false;
    private Vector2 lastPanelPosition;
    private float targetPosition;
    private ScrollRect scrollRect;
    
    // Events
    public System.Action<int> OnMonumentSelected;

    void Start()
    {
        InitializeScroller();
    }

    void InitializeScroller()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect component not found on MonumentScroller!");
            return;
        }

        // Setup content panel
        if (contentPanel != null)
        {
            // Calculate total width needed
            float totalWidth = monumentItems.Count * itemWidth;
            contentPanel.sizeDelta = new Vector2(totalWidth, contentPanel.sizeDelta.y);
            
            // Position items
            for (int i = 0; i < monumentItems.Count; i++)
            {
                if (monumentItems[i] != null)
                {
                    RectTransform itemRect = monumentItems[i].GetComponent<RectTransform>();
                    if (itemRect != null)
                    {
                        itemRect.anchoredPosition = new Vector2(i * itemWidth, 0);
                        
                        // Setup the item
                        monumentItems[i].Initialize(i, this);
                    }
                }
            }
        }

        // Set initial position
        SnapToItem(currentIndex, true);
        UpdateSelection();
    }

    void Update()
    {
        if (!isDragging && scrollRect != null)
        {
            // Smooth snap to target position
            Vector2 currentPos = contentPanel.anchoredPosition;
            Vector2 targetPos = new Vector2(targetPosition, currentPos.y);
            
            if (Vector2.Distance(currentPos, targetPos) > 0.1f)
            {
                contentPanel.anchoredPosition = Vector2.Lerp(currentPos, targetPos, snapSpeed * Time.deltaTime);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastPanelPosition = contentPanel.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Let ScrollRect handle the dragging
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // Calculate which item we should snap to
        float currentXPos = contentPanel.anchoredPosition.x;
        int closestIndex = Mathf.RoundToInt(-currentXPos / itemWidth);
        
        // Clamp to valid range
        closestIndex = Mathf.Clamp(closestIndex, 0, monumentItems.Count - 1);
        
        // Check if we moved enough to change selection
        Vector2 dragDelta = contentPanel.anchoredPosition - lastPanelPosition;
        if (Mathf.Abs(dragDelta.x) > snapThreshold)
        {
            if (dragDelta.x > snapThreshold && currentIndex > 0)
            {
                closestIndex = currentIndex - 1;
            }
            else if (dragDelta.x < -snapThreshold && currentIndex < monumentItems.Count - 1)
            {
                closestIndex = currentIndex + 1;
            }
        }
        
        SelectMonument(closestIndex);
    }

    public void SelectMonument(int index)
    {
        if (index < 0 || index >= monumentItems.Count) return;
        
        currentIndex = index;
        SnapToItem(currentIndex);
        UpdateSelection();
        
        // Update AR Object Manager
        if (arObjectManager != null)
        {
            arObjectManager.currentIndex = currentIndex;
        }
        
        // Invoke event
        OnMonumentSelected?.Invoke(currentIndex);
    }

    public void NextMonument()
    {
        int nextIndex = (currentIndex + 1) % monumentItems.Count;
        SelectMonument(nextIndex);
    }

    public void PreviousMonument()
    {
        int prevIndex = currentIndex - 1;
        if (prevIndex < 0) prevIndex = monumentItems.Count - 1;
        SelectMonument(prevIndex);
    }

    void SnapToItem(int index, bool immediate = false)
    {
        if (index < 0 || index >= monumentItems.Count) return;
        
        targetPosition = -index * itemWidth;
        
        if (immediate)
        {
            contentPanel.anchoredPosition = new Vector2(targetPosition, contentPanel.anchoredPosition.y);
        }
    }

    void UpdateSelection()
    {
        for (int i = 0; i < monumentItems.Count; i++)
        {
            if (monumentItems[i] != null)
            {
                monumentItems[i].SetSelected(i == currentIndex);
            }
        }
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    public MonumentScrollItem GetCurrentMonument()
    {
        if (currentIndex >= 0 && currentIndex < monumentItems.Count)
        {
            return monumentItems[currentIndex];
        }
        return null;
    }

    // Method to add monuments dynamically
    public void AddMonument(MonumentScrollItem monument)
    {
        monumentItems.Add(monument);
        monument.transform.SetParent(contentPanel);
        
        // Reinitialize positions
        InitializeScroller();
    }
}