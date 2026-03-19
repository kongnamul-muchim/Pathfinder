using System.Collections.Generic;
using UnityEngine;

namespace Pathfinder.World
{
    public class ParallaxBackground : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private List<ParallaxLayer> _layers = new();
        
        [Header("Settings")]
        [Tooltip("자동으로 자식 레이어 찾기")]
        [SerializeField] private bool _autoFindLayers = true;
        
        private Transform _camera;
        
        private void Start()
        {
            _camera = Camera.main?.transform;
            
            if (_autoFindLayers)
            {
                FindLayers();
            }
        }
        
        private void FindLayers()
        {
            _layers.Clear();
            
            var foundLayers = GetComponentsInChildren<ParallaxLayer>();
            foreach (var layer in foundLayers)
            {
                if (!_layers.Contains(layer))
                {
                    _layers.Add(layer);
                }
            }
        }
        
        public void AddLayer(ParallaxLayer layer)
        {
            if (!_layers.Contains(layer))
            {
                _layers.Add(layer);
            }
        }
        
        public void RemoveLayer(ParallaxLayer layer)
        {
            _layers.Remove(layer);
        }
        
        public List<ParallaxLayer> GetLayers()
        {
            return _layers;
        }
    }
}