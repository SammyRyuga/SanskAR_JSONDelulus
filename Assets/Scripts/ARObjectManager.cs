using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

public class ARObjectManager : MonoBehaviour
{
    [Header("Prefabs to Spawn")]
    public List<GameObject> objectPrefabs;
    public int currentIndex = 0;

    [Header("Spawn Settings")]
    public bool deleteOldWhenSpawningNew = true;

    [Header("Zoom & Rotation Settings")]
    public float zoomSpeed = 0.5f;
    public float rotationSpeed = 2f;
    public float minScale = 0.1f;
    public float maxScale = 5.0f;

    [Header("Monument Scroller Integration")]
    public MonumentScroller monumentScroller;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private GameObject currentActiveObject;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Touch gesture tracking
    private bool isTwoFingerGesture = false;
    private float initialPinchDistance = 0f;
    private Vector3 initialScale = Vector3.one;

    void Start()
    {
        // Find AR components
        raycastManager = FindObjectOfType<ARRaycastManager>();
        planeManager = FindObjectOfType<ARPlaneManager>();

        if (raycastManager == null)
            Debug.LogError("ARRaycastManager not found!");
        if (planeManager == null)
            Debug.LogError("ARPlaneManager not found!");

        // Enable plane detection
        if (planeManager != null)
            planeManager.enabled = true;

        // Setup monument scroller integration
        SetupScrollerIntegration();
    }

    void SetupScrollerIntegration()
    {
        if (monumentScroller != null)
        {
            // Subscribe to scroller selection events
            monumentScroller.OnMonumentSelected += OnMonumentSelectedFromScroller;
            
            // Sync initial state
            monumentScroller.SelectMonument(currentIndex);
        }
    }

    void OnMonumentSelectedFromScroller(int selectedIndex)
    {
        // Update current index when scroller selection changes
        currentIndex = selectedIndex;
        Debug.Log($"Monument selected from scroller: {GetCurrentPrefabName()}");
    }

    void Update()
    {
        HandleTouchGestures();
    }

    void HandleTouchGestures()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null || currentActiveObject == null) return;

        int touchCount = 0;
        for (int i = 0; i < touchscreen.touches.Count; i++)
        {
            if (touchscreen.touches[i].press.isPressed)
                touchCount++;
        }

        if (touchCount == 2)
        {
            HandleTwoFingerGestures();
        }
        else
        {
            isTwoFingerGesture = false;
        }
    }

    void HandleTwoFingerGestures()
    {
        var touchscreen = Touchscreen.current;
        var touch0 = touchscreen.touches[0];
        var touch1 = touchscreen.touches[1];

        if (!touch0.press.isPressed || !touch1.press.isPressed) return;

        Vector2 touch0Pos = touch0.position.ReadValue();
        Vector2 touch1Pos = touch1.position.ReadValue();

        float currentPinchDistance = Vector2.Distance(touch0Pos, touch1Pos);

        if (!isTwoFingerGesture)
        {
            // Start of two-finger gesture
            isTwoFingerGesture = true;
            initialPinchDistance = currentPinchDistance;
            initialScale = currentActiveObject.transform.localScale;
        }
        else
        {
            // Continue two-finger gesture
            // Pinch to zoom
            float scaleMultiplier = currentPinchDistance / initialPinchDistance;
            Vector3 newScale = initialScale * scaleMultiplier;
            newScale = Vector3.Max(Vector3.one * minScale, Vector3.Min(Vector3.one * maxScale, newScale));
            currentActiveObject.transform.localScale = newScale;

            // Rotation based on finger movement
            Vector2 touch0Delta = touch0.delta.ReadValue();
            Vector2 touch1Delta = touch1.delta.ReadValue();
            
            // Average the delta movement for smoother rotation
            Vector2 avgDelta = (touch0Delta + touch1Delta) * 0.5f;
            float rotationAmount = avgDelta.x * rotationSpeed * Time.deltaTime;
            
            currentActiveObject.transform.Rotate(Vector3.up, rotationAmount, Space.World);
        }
    }

    // UI BUTTON METHODS

    public void SpawnObject()
    {
        // Spawn at screen center
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        TrySpawnObject(screenCenter);
    }

    public void NextPrefab()
    {
        if (objectPrefabs != null && objectPrefabs.Count > 0)
        {
            currentIndex = (currentIndex + 1) % objectPrefabs.Count;
            
            // Update scroller if available
            if (monumentScroller != null)
            {
                monumentScroller.SelectMonument(currentIndex);
            }
        }
    }

    public void PreviousPrefab()
    {
        if (objectPrefabs != null && objectPrefabs.Count > 0)
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = objectPrefabs.Count - 1;
            
            // Update scroller if available
            if (monumentScroller != null)
            {
                monumentScroller.SelectMonument(currentIndex);
            }
        }
    }

    public void DeleteCurrentObject()
    {
        if (currentActiveObject != null)
        {
            spawnedObjects.Remove(currentActiveObject);
            Destroy(currentActiveObject);
            currentActiveObject = null;
        }
    }

    public void ZoomIn()
    {
        if (currentActiveObject != null)
        {
            Vector3 newScale = currentActiveObject.transform.localScale * (1f + zoomSpeed);
            newScale = Vector3.Min(newScale, Vector3.one * maxScale);
            currentActiveObject.transform.localScale = newScale;
        }
    }

    public void ZoomOut()
    {
        if (currentActiveObject != null)
        {
            Vector3 newScale = currentActiveObject.transform.localScale * (1f - zoomSpeed);
            newScale = Vector3.Max(newScale, Vector3.one * minScale);
            currentActiveObject.transform.localScale = newScale;
        }
    }

    public void RotateLeft()
    {
        if (currentActiveObject != null)
        {
            currentActiveObject.transform.Rotate(Vector3.up, -45f, Space.World);
        }
    }

    public void RotateRight()
    {
        if (currentActiveObject != null)
        {
            currentActiveObject.transform.Rotate(Vector3.up, 45f, Space.World);
        }
    }

    // CORE SPAWNING METHOD
    void TrySpawnObject(Vector2 screenPosition)
    {
        if (objectPrefabs == null || objectPrefabs.Count == 0) return;
        if (currentIndex >= objectPrefabs.Count) return;
        if (raycastManager == null) return;

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            // Delete old object if setting is enabled
            if (deleteOldWhenSpawningNew && currentActiveObject != null)
            {
                spawnedObjects.Remove(currentActiveObject);
                Destroy(currentActiveObject);
            }

            // Spawn new object
            GameObject prefabToSpawn = objectPrefabs[currentIndex];
            if (prefabToSpawn != null)
            {
                GameObject newObj = Instantiate(prefabToSpawn, hitPose.position, hitPose.rotation);
                spawnedObjects.Add(newObj);
                currentActiveObject = newObj;

                // Make object face camera
                Camera arCamera = Camera.main;
                if (arCamera != null)
                {
                    Vector3 lookDirection = arCamera.transform.position - newObj.transform.position;
                    lookDirection.y = 0;
                    if (lookDirection != Vector3.zero)
                    {
                        newObj.transform.rotation = Quaternion.LookRotation(-lookDirection);
                    }
                }
            }
        }
    }

    // UTILITY METHODS FOR UI
    public string GetCurrentPrefabName()
    {
        if (objectPrefabs != null && currentIndex < objectPrefabs.Count && objectPrefabs[currentIndex] != null)
            return objectPrefabs[currentIndex].name;
        return "None";
    }

    public int GetSpawnedCount()
    {
        return spawnedObjects.Count;
    }

    public bool HasActiveObject()
    {
        return currentActiveObject != null;
    }

    public int GetCurrentPrefabIndex()
    {
        return currentIndex + 1; // +1 for display (1-based instead of 0-based)
    }

    public int GetTotalPrefabCount()
    {
        return objectPrefabs?.Count ?? 0;
    }

    // NEW METHODS FOR SCROLLER INTEGRATION
    public GameObject GetCurrentPrefab()
    {
        if (objectPrefabs != null && currentIndex < objectPrefabs.Count)
            return objectPrefabs[currentIndex];
        return null;
    }

    public void SetCurrentIndex(int index)
    {
        if (index >= 0 && index < objectPrefabs.Count)
        {
            currentIndex = index;
            
            // Update scroller if available
            if (monumentScroller != null)
            {
                monumentScroller.SelectMonument(currentIndex);
            }
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (monumentScroller != null)
        {
            monumentScroller.OnMonumentSelected -= OnMonumentSelectedFromScroller;
        }
    }
}