using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeCameraGuard : MonoBehaviour
{
    const float GuardDuration = 6f;
    float guardUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameConfig.GameplaySceneName)
        {
            return;
        }

        GameObject guardObject = new GameObject(nameof(RuntimeCameraGuard));
        guardObject.AddComponent<RuntimeCameraGuard>();
    }

    void Awake()
    {
        if (GameObject.FindGameObjectWithTag("Player") == null)
        {
            SceneBootstrapper.BuildPrototypeScene();
        }

        guardUntil = Time.unscaledTime + GuardDuration;
        EnsureCamera();
    }

    void Update()
    {
        if (Time.unscaledTime > guardUntil)
        {
            Destroy(gameObject);
            return;
        }

        EnsureCamera();
    }

    void EnsureCamera()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Camera preferred = null;

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null)
            {
                continue;
            }

            // Normalize every camera so Display 1 can render.
            cam.targetDisplay = 0;

            if (!cam.enabled && cam.CompareTag("MainCamera"))
            {
                cam.enabled = true;
            }

            if (!cam.gameObject.activeInHierarchy && cam.CompareTag("MainCamera"))
            {
                cam.gameObject.SetActive(true);
            }

            if (preferred == null && cam.enabled && cam.gameObject.activeInHierarchy)
            {
                preferred = cam;
            }
        }

        if (preferred != null)
        {
            return;
        }

        // Fallback camera if none is rendering.
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera fallback = cameraObject.AddComponent<Camera>();
        fallback.clearFlags = CameraClearFlags.Skybox;
        fallback.fieldOfView = 70f;
        fallback.nearClipPlane = 0.1f;
        fallback.farClipPlane = 2000f;
        fallback.targetDisplay = 0;
        cameraObject.AddComponent<AudioListener>();

        cameraObject.transform.position = new Vector3(0f, 2.2f, -12f);
        cameraObject.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
    }
}
