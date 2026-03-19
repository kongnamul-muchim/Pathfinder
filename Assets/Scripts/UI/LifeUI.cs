using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Pathfinder.Player;

namespace Pathfinder.UI
{
    public class LifeUI : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private Text _lifeText;
        
        [Header("Settings")]
        [SerializeField] private string _heartChar = "\u2665";
        [SerializeField] private int _baseLives = 3;
        
        private IAbilityManager _abilityManager;
        
        private void Start()
        {
            _abilityManager = FindFirstObjectByType<AbilityManager>();
            
            if (_abilityManager != null)
            {
                _abilityManager.OnLivesChanged += UpdateLifeDisplay;
            }
            
            UpdateLifeDisplay(_abilityManager?.GetLives() ?? _baseLives);
        }
        
        private void OnDestroy()
        {
            if (_abilityManager != null)
            {
                _abilityManager.OnLivesChanged -= UpdateLifeDisplay;
            }
        }
        
        private void UpdateLifeDisplay(int lives)
        {
            if (_lifeText == null) return;
            
            var sb = new StringBuilder(lives);
            for (int i = 0; i < lives; i++)
            {
                sb.Append(_heartChar);
            }
            
            _lifeText.text = sb.ToString();
        }
    }
}