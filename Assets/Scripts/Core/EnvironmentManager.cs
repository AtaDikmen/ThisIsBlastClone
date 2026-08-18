using UnityEngine;

namespace Core
{
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("Main Menu Environment")]
        [SerializeField] private GameObject mainMenuEnvironmentRoot;
        [SerializeField] private Camera mainMenuCamera;

        [Header("Gameplay Environment")]
        [SerializeField] private GameObject gameplayEnvironmentRoot;
        [SerializeField] private Camera gameplayCamera;

        private void Awake()
        {
            ActivateMainMenuEnvironment();
        }

        public void ActivateMainMenuEnvironment()
        {
            if(gameplayEnvironmentRoot != null) gameplayEnvironmentRoot.SetActive(false);
            if(gameplayCamera != null) gameplayCamera.gameObject.SetActive(false);

            if(mainMenuEnvironmentRoot != null) mainMenuEnvironmentRoot.SetActive(true);
            if(mainMenuCamera != null) mainMenuCamera.gameObject.SetActive(true);
        }

        public void ActivateGameplayEnvironment()
        {
            if(mainMenuEnvironmentRoot != null) mainMenuEnvironmentRoot.SetActive(false);
            if(mainMenuCamera != null) mainMenuCamera.gameObject.SetActive(false);

            if(gameplayEnvironmentRoot != null) gameplayEnvironmentRoot.SetActive(true);
            if(gameplayCamera != null) gameplayCamera.gameObject.SetActive(true);
        }
    }
}
