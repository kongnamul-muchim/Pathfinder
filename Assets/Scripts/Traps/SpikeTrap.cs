using UnityEngine;

namespace Pathfinder.Traps
{
    /// <summary>
    /// 가시 함정 - 플레이어 닿으면 즉시 사망
    /// PlayerController의 OnTriggerEnter2D에서 "Trap" 태그로 감지
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SpikeTrap : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("충돌 시 이펙트 (선택사항)")]
        [SerializeField] private ParticleSystem _hitEffect;
        
        [Header("Audio")]
        [Tooltip("충돌 시 사운드 (선택사항)")]
        [SerializeField] private AudioClip _hitSound;
        
        private AudioSource _audioSource;
        
        private void Awake()
        {
            // Collider 확인
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogError($"[{nameof(SpikeTrap)}] Collider2D가 필요합니다!");
                enabled = false;
                return;
            }
            
            // IsTrigger 활성화
            collider.isTrigger = true;
            
            // 태그 설정
            if (!CompareTag("Trap"))
            {
                tag = "Trap";
            }
            
            // AudioSource 설정
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && _hitSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 플레이어 확인
            if (!other.CompareTag("Player")) return;
            
            // 이펙트 재생
            if (_hitEffect != null)
            {
                Instantiate(_hitEffect, other.transform.position, Quaternion.identity);
            }
            
            // 사운드 재생
            if (_audioSource != null && _hitSound != null)
            {
                _audioSource.PlayOneShot(_hitSound);
            }
            
            // PlayerController는 자동으로 Die() 호출
            // (PlayerController.OnTriggerEnter2D에서 "Trap" 태그로 처리)
        }
    }
}
