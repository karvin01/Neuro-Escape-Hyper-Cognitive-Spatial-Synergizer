using UnityEngine;

namespace NeuroEscape
{
    /// A wall bracket for a torch.
    /// - With a torch in it: press E to take it. It goes into the hotbar as a Torch item (F takes it out).
    /// - Empty, while you have a torch in the hotbar: press E to place one torch into it (uses it up).
    /// Used by Alex_Torch_Wall_Pickable (starts with a torch) and Alex_Torch_Holder (starts empty).
    public class TorchPickup : Interactable
    {
        [Tooltip("The Torch item (Alex_Item_Torch).")]
        public ItemData torchItem;

        [Tooltip("The torch parts in the bracket (shown when it holds a torch).")]
        public GameObject[] hideWhenTaken;

        [Tooltip("Ticked = the bracket starts with a lit torch in it.")]
        public bool startsWithTorch = true;

        [Header("Sounds")]
        public AudioClip takeSound;
        public AudioClip placeSound;
        [Range(0f, 1f)] public float soundVolume = 0.5f;

        bool hasTorch;

        void Start()
        {
            hasTorch = startsWithTorch;
            ShowTorch(hasTorch);
        }

        public override string Prompt
        {
            get
            {
                Inventory inv = Inventory.Instance;
                if (inv == null || torchItem == null) return "";
                if (hasTorch) return inv.IsFull ? "Hotbar full" : "Press E to take the torch";
                return inv.Has(torchItem) ? "Press E to place a torch" : "";
            }
        }

        public override void Interact()
        {
            Inventory inv = Inventory.Instance;
            if (inv == null || torchItem == null) return;

            if (hasTorch)
            {
                if (inv.Add(torchItem))
                {
                    hasTorch = false;
                    ShowTorch(false);
                    PlaySound(takeSound);
                }
            }
            else if (inv.Has(torchItem))
            {
                inv.Remove(torchItem);
                hasTorch = true;
                ShowTorch(true);
                PlaySound(placeSound);
            }
        }

        void PlaySound(AudioClip clip)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, soundVolume);
        }

        void ShowTorch(bool show)
        {
            if (hideWhenTaken == null) return;
            foreach (GameObject g in hideWhenTaken)
                if (g != null) g.SetActive(show);
        }
    }
}
