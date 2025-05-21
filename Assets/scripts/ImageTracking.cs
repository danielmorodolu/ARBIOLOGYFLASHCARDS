using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracking : MonoBehaviour
{
    public GameObject[] prefabs; // Array of prefabs (animalCellPrefab, fungiCellPrefab, plantCellPrefab, bacterialCellPrefab)
    public AudioClip spawnSound; // Audio clip for model spawn
    private ARTrackedImageManager trackedImageManager;
    private GameObject[] spawnedObjects;
    private UIManager uiManager;
    private ARSession arSession;
    private string lastTrackedImageName = "";
    private AudioSource audioSource; // AudioSource for playing spawn sound
    private bool[] isAnimated; // Flag to track if each model has been animated
    private GameObject selectedModel; // Currently selected model for interaction
    private float initialPinchDistance = 0f; // For pinch-to-zoom
    private float initialScale = 0.19f; // Default scale of models
    private Vector2 lastTouchPosition; // For dragging

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        arSession = FindObjectOfType<ARSession>();
        spawnedObjects = new GameObject[prefabs.Length];
        isAnimated = new bool[prefabs.Length]; // Initialize animation flags

        // Initialize AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (spawnSound == null)
        {
            Debug.LogWarning("Spawn sound not assigned in ImageTracking!");
        }

        // Check for null prefabs
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == null)
            {
                Debug.LogError($"Prefab at index {i} is null! Please assign a prefab in the Inspector.");
            }
        }

        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager not found in the scene!");
        }
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void Update()
    {
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        // Select a model by tapping
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                selectedModel = hit.transform.gameObject;
                Debug.Log($"Selected model: {selectedModel.name}");
            }
            else
            {
                selectedModel = null;
            }
            lastTouchPosition = Input.GetTouch(0).position;
        }

        // Move the selected model by dragging with one finger
        if (Input.touchCount == 1 && selectedModel != null && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 currentTouchPosition = Input.GetTouch(0).position;
            Vector2 deltaPosition = (currentTouchPosition - lastTouchPosition) * 0.001f; // Adjust sensitivity
            Vector3 worldDelta = new Vector3(deltaPosition.x, 0, deltaPosition.y); // Move in XZ plane
            selectedModel.transform.position += worldDelta;
            lastTouchPosition = currentTouchPosition;
        }

        // Zoom in/out with pinch gesture (two fingers)
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                initialScale = selectedModel != null ? selectedModel.transform.localScale.x : 0.19f;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                if (selectedModel != null)
                {
                    float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                    float pinchScale = (currentPinchDistance / initialPinchDistance) * initialScale;
                    pinchScale = Mathf.Clamp(pinchScale, 0.1f, 0.5f); // Limit scale range
                    selectedModel.transform.localScale = Vector3.one * pinchScale;
                    Debug.Log($"Zooming model to scale: {pinchScale}");
                }
            }
        }
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            UpdateImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            int index = GetPrefabIndex(trackedImage.referenceImage.name);
            if (index >= 0 && spawnedObjects[index] != null)
            {
                if (spawnedObjects[index] == selectedModel) selectedModel = null; // Clear selection
                Destroy(spawnedObjects[index]);
                spawnedObjects[index] = null;
                isAnimated[index] = false; // Reset animation flag on removal
            }
        }
    }

    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        Debug.Log($"Tracked image name: {imageName}");
        int index = GetPrefabIndex(imageName);

        if (index < 0 || index >= prefabs.Length) return;

        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            lastTrackedImageName = imageName;
            Debug.Log($"Set lastTrackedImageName to: {lastTrackedImageName}");

            if (spawnedObjects[index] == null && !trackedImage.Equals(null))
            {
                if (prefabs[index] == null)
                {
                    Debug.LogError($"Cannot instantiate prefab for {imageName} at index {index}: Prefab is null.");
                    return;
                }

                Vector3 spawnPosition = trackedImage.transform.position + new Vector3(0, 0.01f, 0);
                Quaternion spawnRotation = trackedImage.transform.rotation;

                spawnedObjects[index] = Instantiate(prefabs[index], spawnPosition, spawnRotation);
                spawnedObjects[index].transform.localScale = new Vector3(0.19f, 0.19f, 0.19f);
                Debug.Log($"Instantiated prefab: {prefabs[index].name} at index {index}");

                // Add collider to model for raycasting (needed for touch selection)
                foreach (var renderer in spawnedObjects[index].GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.gameObject.GetComponent<Collider>())
                    {
                        renderer.gameObject.AddComponent<BoxCollider>();
                    }
                }

                // Play spawn sound and particles only if not animated before
                if (!isAnimated[index])
                {
                    if (audioSource != null && spawnSound != null)
                    {
                        audioSource.PlayOneShot(spawnSound);
                    }
                    GameObject particleObj = new GameObject("SpawnParticles");
                    particleObj.transform.SetParent(spawnedObjects[index].transform, false);
                    particleObj.transform.localPosition = Vector3.zero;
                    ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
                    SetupParticleSystem(particles);
                    particles.Play();

                    isAnimated[index] = true; // Mark as animated
                }

                Renderer modelRenderer = spawnedObjects[index].GetComponentInChildren<Renderer>();
                if (modelRenderer != null)
                {
                    Material[] materials = modelRenderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material newMaterial = new Material(Shader.Find("Standard"));
                        newMaterial.color = Color.white;
                        materials[i] = newMaterial;
                        if (materials[i].mainTexture != null)
                        {
                            Texture tex = materials[i].mainTexture;
                            if (tex.width > 512 || tex.height > 512)
                            {
                                Debug.LogWarning($"Texture on {prefabs[index].name} is {tex.width}x{tex.height}. Consider reducing to 512x512 or lower for better performance.");
                            }
                        }
                    }
                    modelRenderer.materials = materials;
                    Debug.Log($"Applied material color: white to {prefabs[index].name} with {materials.Length} materials");
                }
                else
                {
                    Debug.LogWarning($"No Renderer found in {prefabs[index].name}. Please add a MeshRenderer or SkinnedMeshRenderer.");
                }

                Debug.Log($"Spawned {prefabs[index].name} at position: {spawnedObjects[index].transform.position}, local position of child: {spawnedObjects[index].transform.GetChild(0).localPosition}");
            }
            else if (spawnedObjects[index] != null)
            {
                // Only update position if the model is not being manually moved
                if (spawnedObjects[index] != selectedModel)
                {
                    Vector3 updatedPosition = trackedImage.transform.position + new Vector3(0, 0.01f, 0);
                    Quaternion updatedRotation = trackedImage.transform.rotation;
                    spawnedObjects[index].transform.position = Vector3.Lerp(spawnedObjects[index].transform.position, updatedPosition, Time.deltaTime * 5f); // Smooth transition
                    spawnedObjects[index].transform.rotation = Quaternion.Slerp(spawnedObjects[index].transform.rotation, updatedRotation, Time.deltaTime * 5f);
                }
            }
        }
        else if (trackedImage.trackingState == TrackingState.Limited || trackedImage.trackingState == TrackingState.None)
        {
            if (spawnedObjects[index] != null)
            {
                if (spawnedObjects[index] == selectedModel) selectedModel = null; // Clear selection
                Destroy(spawnedObjects[index]);
                spawnedObjects[index] = null;
                isAnimated[index] = false; // Reset animation flag on removal
            }
            lastTrackedImageName = "";
            Debug.Log("Tracking lost, cleared lastTrackedImageName.");
        }
    }

    private void SetupParticleSystem(ParticleSystem ps)
    {
        var main = ps.main;
        main.startSize = 0.05f;
        main.startSpeed = 0.1f;
        main.startLifetime = 1f;
        main.startColor = new Color(1f, 1f, 0f, 1f); // Yellow sparkles
        main.maxParticles = 50;
        main.duration = 1f;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 20;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = Color.yellow;
    }

    private int GetPrefabIndex(string imageName)
    {
        return imageName switch
        {
            "animalcell.jpg" => 0,
            "fungicell.jpg" => 1,
            "plantcell.jpg" => 2,
            "bacterialcell.jpg" => 3,
            _ => -1
        };
    }

    public string GetLastTrackedImageName()
    {
        return lastTrackedImageName;
    }

    public void ClearSession()
    {
        Debug.Log("ClearSession method called from button click");
        foreach (var obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        for (int i = 0; i < spawnedObjects.Length; i++)
        {
            spawnedObjects[i] = null;
            isAnimated[i] = false; // Reset all animation flags
        }
        selectedModel = null; // Clear selection
        lastTrackedImageName = "";
        if (uiManager != null)
        {
            uiManager.ClearFacts();
        }

        if (arSession != null)
        {
            arSession.Reset();
        }
    }
}