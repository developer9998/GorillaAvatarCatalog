using System;
using UnityEngine;

namespace GorillaAvatarCatalog.Behaviours
{
    public class PushButton : MonoBehaviour
    {
        public event Action<bool> OnActivated;

        public Material PressedMaterial;

        public Material UnpressedMaterial;

        public MeshRenderer ButtonRenderer;

        public bool Activated;

        public float Debounce = 0.25f;

        private float touchTime;

        public void OnTriggerEnter(Collider collider)
        {
            if (Time.realtimeSinceStartup > touchTime && collider.TryGetComponent(out GorillaTriggerColliderHandIndicator component))
            {
                touchTime = Time.realtimeSinceStartup + Debounce;

                GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, component.isLeftHand, 0.05f);
                GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

                OnActivated?.Invoke(component.isLeftHand);
            }
        }

        public void UpdateColour()
        {
            if (ButtonRenderer is not Renderer buttonRenderer)
                return;

            buttonRenderer.material = Activated ? PressedMaterial : UnpressedMaterial;
        }
    }
}
