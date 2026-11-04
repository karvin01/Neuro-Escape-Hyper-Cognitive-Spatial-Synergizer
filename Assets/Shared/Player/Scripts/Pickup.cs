using UnityEngine;

namespace NeuroEscape
{
    /// Put on an object in the world (with a collider). Press E to put its item in the hotbar.
    public class Pickup : Interactable
    {
        public ItemData item;

        public override string Prompt
        {
            get
            {
                if (item == null || Inventory.Instance == null) return "";
                return Inventory.Instance.IsFull
                    ? "Hotbar full"
                    : "Press E to pick up " + item.itemName;
            }
        }

        public override void Interact()
        {
            if (item == null || Inventory.Instance == null) return;
            if (Inventory.Instance.Add(item))
            {
                PlaySound(item.pickupSound);
                Destroy(gameObject);
            }
        }

        /// Plays a sound at the player's head (full volume, no matter where the item was).
        public static void PlaySound(AudioClip clip, float volume = 0.4f)
        {
            if (clip == null) return;
            Camera cam = Camera.main;
            AudioSource.PlayClipAtPoint(clip, cam != null ? cam.transform.position : Vector3.zero, volume);
        }
    }
}
