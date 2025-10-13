using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Main hotspot data structure
[System.Serializable]
public class HotspotData
{
    public string hotspotID;
    public string title;
    public string shortDescription;
    [TextArea(3, 5)]
    public string detailedInfo; // Fallback if Gemini API fails
    public Vector3 worldPosition;
    public Sprite iconSprite;
    public AudioClip audioClip; // Optional audio guide
    public bool isActive = true;
}

// Individual hotspot behavior
public class Hotspot : MonoBehaviour
{
    [Header("Hotspot References")]
    public HotspotData data;
    public Canvas hotspotCanvas;
    public Button hotspotButton;
    public Image iconImage;
    public Text titleText;
    public Animator pulseAnimator; // Optional pulsing animation
    
    [Header("Visual Settings")]
    public float maxViewDistance = 50f;
    public bool scaleWithDistance = true;
    public float minScale = 0.5f;
    public float maxScale = 1.5f;
    
    private Camera arCamera;
    private HotspotManager manager;
    private bool isVisible = true;

    void Start()
    {
        arCamera = Camera.main;
        manager = FindObjectOfType<HotspotManager>();
        
        SetupHotspot();
    }

    void SetupHotspot()
    {
        if (data == null) return;

        // Setup UI elements
        if (titleText != null)
            titleText.text = data.title;
            
        if (iconImage != null && data.iconSprite != null)
            iconImage.sprite = data.iconSprite;
            
        // Setup button click
        if (hotspotButton != null)
            hotspotButton.onClick.AddListener(OnHotspotClicked);
            
        // Make canvas face camera
        if (hotspotCanvas != null)
            hotspotCanvas.worldCamera = arCamera;
    }

    void Update()
    {
        if (arCamera == null || !data.isActive) return;

        UpdateVisibility();
        UpdateScale();
        FaceCamera();
    }

    void UpdateVisibility()
    {
        float distance = Vector3.Distance(transform.position, arCamera.transform.position);
        bool shouldBeVisible = distance <= maxViewDistance;
        
        if (shouldBeVisible != isVisible)
        {
            isVisible = shouldBeVisible;
            gameObject.SetActive(isVisible);
        }
    }

    void UpdateScale()
    {
        if (!scaleWithDistance || arCamera == null) return;
        
        float distance = Vector3.Distance(transform.position, arCamera.transform.position);
        float normalizedDistance = Mathf.Clamp01(distance / maxViewDistance);
        float scale = Mathf.Lerp(maxScale, minScale, normalizedDistance);
        
        transform.localScale = Vector3.one * scale;
    }

    void FaceCamera()
    {
        if (arCamera != null && hotspotCanvas != null)
        {
            Vector3 lookDirection = arCamera.transform.position - transform.position;
            lookDirection.y = 0; // Keep upright
            if (lookDirection != Vector3.zero)
            {
                hotspotCanvas.transform.rotation = Quaternion.LookRotation(-lookDirection);
            }
        }
    }

    void OnHotspotClicked()
    {
        if (manager != null)
            manager.OnHotspotSelected(data);
    }

    // Public method to update hotspot data
    public void UpdateHotspotData(HotspotData newData)
    {
        data = newData;
        SetupHotspot();
    }
}

// Main hotspot management system
public class HotspotManager : MonoBehaviour
{
    [Header("Hotspot Settings")]
    public List<HotspotData> hotspotDatabase;
    public GameObject hotspotPrefab;
    public Transform hotspotParent;
    
    [Header("UI References")]
    public GameObject infoPanel;
    public Text infoTitle;
    public Text infoDescription;
    public Button closeButton;
    public Button moreInfoButton; // Triggers Gemini API call
    public ScrollRect infoScrollView;
    public AudioSource audioSource;
    
    [Header("AR Placement")]
    public bool useARPlacement = true;
    public ARRaycastManager raycastManager;
    
    private List<GameObject> spawnedHotspots = new List<GameObject>();
    private HotspotData currentSelectedHotspot;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Events for your team to hook into
    public System.Action<string> OnRequestGeminiInfo; // Pass hotspot ID to Gemini API
    public System.Action<HotspotData> OnHotspotInteracted; // Analytics/tracking

    void Start()
    {
        SetupUI();
        
        if (!useARPlacement)
        {
            // Spawn all hotspots at predefined positions
            SpawnAllHotspots();
        }
    }

    void SetupUI()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInfoPanel);
            
        if (moreInfoButton != null)
            moreInfoButton.onClick.AddListener(RequestDetailedInfo);
            
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    void SpawnAllHotspots()
    {
        foreach (HotspotData data in hotspotDatabase)
        {
            if (data.isActive)
                CreateHotspot(data, data.worldPosition);
        }
    }

    // Method for AR placement of hotspots
    public void PlaceHotspotAtTouch(Vector2 screenPosition, int hotspotIndex)
    {
        if (!useARPlacement || raycastManager == null) return;
        if (hotspotIndex >= hotspotDatabase.Count) return;

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 position = hits[0].pose.position;
            CreateHotspot(hotspotDatabase[hotspotIndex], position);
        }
    }

    GameObject CreateHotspot(HotspotData data, Vector3 position)
    {
        if (hotspotPrefab == null) return null;

        GameObject hotspotObj = Instantiate(hotspotPrefab, position, Quaternion.identity);
        
        if (hotspotParent != null)
            hotspotObj.transform.SetParent(hotspotParent);

        Hotspot hotspotComponent = hotspotObj.GetComponent<Hotspot>();
        if (hotspotComponent != null)
        {
            hotspotComponent.data = data;
        }

        spawnedHotspots.Add(hotspotObj);
        return hotspotObj;
    }

    // Called when a hotspot is clicked
    public void OnHotspotSelected(HotspotData hotspotData)
    {
        currentSelectedHotspot = hotspotData;
        ShowInfoPanel(hotspotData);
        
        // Trigger event for analytics/tracking
        OnHotspotInteracted?.Invoke(hotspotData);
        
        // Play audio if available
        if (audioSource != null && hotspotData.audioClip != null)
        {
            audioSource.clip = hotspotData.audioClip;
            audioSource.Play();
        }
    }

    void ShowInfoPanel(HotspotData data)
    {
        if (infoPanel == null) return;

        if (infoTitle != null)
            infoTitle.text = data.title;
            
        if (infoDescription != null)
            infoDescription.text = data.shortDescription;

        infoPanel.SetActive(true);
    }

    void CloseInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
            
        if (audioSource != null)
            audioSource.Stop();
    }

    // This method triggers the Gemini API call
    void RequestDetailedInfo()
    {
        if (currentSelectedHotspot == null) return;
        
        // Trigger event for your team's Gemini API integration
        OnRequestGeminiInfo?.Invoke(currentSelectedHotspot.hotspotID);
        
        // Show loading state
        if (moreInfoButton != null)
        {
            Text buttonText = moreInfoButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = "Loading...";
        }
    }

    // Method for your team to call when Gemini API returns data
    public void UpdateHotspotInfoFromGemini(string hotspotID, string geminiResponse)
    {
        // Update the info panel with Gemini's response
        if (currentSelectedHotspot != null && currentSelectedHotspot.hotspotID == hotspotID)
        {
            if (infoDescription != null)
                infoDescription.text = geminiResponse;
                
            // Reset button text
            if (moreInfoButton != null)
            {
                Text buttonText = moreInfoButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = "More Details";
            }
        }
    }

    // Utility methods for your team
    public void ShowHotspot(string hotspotID)
    {
        foreach (GameObject hotspot in spawnedHotspots)
        {
            Hotspot component = hotspot.GetComponent<Hotspot>();
            if (component != null && component.data.hotspotID == hotspotID)
            {
                component.data.isActive = true;
                hotspot.SetActive(true);
            }
        }
    }

    public void HideHotspot(string hotspotID)
    {
        foreach (GameObject hotspot in spawnedHotspots)
        {
            Hotspot component = hotspot.GetComponent<Hotspot>();
            if (component != null && component.data.hotspotID == hotspotID)
            {
                component.data.isActive = false;
                hotspot.SetActive(false);
            }
        }
    }

    public List<HotspotData> GetActiveHotspots()
    {
        List<HotspotData> activeHotspots = new List<HotspotData>();
        foreach (HotspotData data in hotspotDatabase)
        {
            if (data.isActive)
                activeHotspots.Add(data);
        }
        return activeHotspots;
    }

    public HotspotData GetHotspotByID(string hotspotID)
    {
        return hotspotDatabase.Find(h => h.hotspotID == hotspotID);
    }
}