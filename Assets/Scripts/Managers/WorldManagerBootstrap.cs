using UnityEngine;

/// <summary>
/// Ensures WorldManager exists in the scene.
/// Place this on any scene that needs WorldManager to function.
/// Creates a WorldManager if one doesn't already exist.
/// </summary>
public class WorldManagerBootstrap : MonoBehaviour
{
    [Header("Prefab (Optional)")]
    [SerializeField] private GameObject worldManagerPrefab;

    private void Awake()
    {
        // Check if WorldManager already exists
        if (WorldManager.Instance != null)
        {
            // WorldManager exists, destroy this bootstrap
            Destroy(gameObject);
            return;
        }

        // Create WorldManager
        if (worldManagerPrefab != null)
        {
            Instantiate(worldManagerPrefab);
        }
        else
        {
            // Create a simple WorldManager GameObject
            var managerObject = new GameObject("WorldManager");
            managerObject.AddComponent<WorldManager>();
        }

        // Destroy this bootstrap object
        Destroy(gameObject);
    }
}
