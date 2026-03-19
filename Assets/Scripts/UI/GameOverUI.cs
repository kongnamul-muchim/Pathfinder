using UnityEngine;
using UnityEngine.SceneManagement;
using Pathfinder.Core;

namespace Pathfinder.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _gameOverPanel;
        
        private ISaveManager _saveManager;
        
        private void Awake()
        {
            _saveManager = FindFirstObjectByType<SaveManager>();
            SetPanelActive(false);
        }
        
        public void Show()
        {
            Time.timeScale = 0f;
            
            if (_gameOverPanel == null)
            {
                Debug.LogError("[GameOverUI] _gameOverPanel is null! Assign the GameOver panel GameObject in Inspector.");
                return;
            }
            
            SetPanelActive(true);
            Debug.Log("[GameOverUI] GameOver panel activated");
        }
        
        private void SetPanelActive(bool active)
        {
            if (_gameOverPanel == null) return;
            
            _gameOverPanel.SetActive(active);
            
            foreach (Transform child in _gameOverPanel.transform)
            {
                child.gameObject.SetActive(active);
            }
        }
        
        public void OnRestartClick()
        {
            Time.timeScale = 1f;
            
            if (_saveManager != null)
                _saveManager.ClearSave();
            
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}