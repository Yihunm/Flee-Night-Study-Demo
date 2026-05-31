using UnityEngine;

namespace FleeNightStudy
{
    public class TextbookPickup : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (GameStateManager.Instance == null) return;

            GameStateManager.Instance.RegisterTextbookPickup();
            GameplayAudioManager.Instance?.PlayPickupTextbook();
            Destroy(gameObject);
        }
    }
}
