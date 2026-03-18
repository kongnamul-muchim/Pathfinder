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
            _saveManager = FindObjectOfType<SaveManager>();
            
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);
        }
        
        public void Show()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(true);
            
            Time.timeScale = 0f;
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