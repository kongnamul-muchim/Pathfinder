using UnityEngine;

namespace Pathfinder.Common
{
    public interface IInteractable
    {
        string GetInteractionText();
        bool CanInteract();
        void OnInteract();
        Transform GetPromptTransform();
    }
}