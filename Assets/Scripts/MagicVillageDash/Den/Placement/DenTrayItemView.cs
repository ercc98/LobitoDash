using System;
using UnityEngine;
using UnityEngine.UI;
using MagicVillageDash.Collections;
using TMPro;

namespace MagicVillageDash.Den.Placement
{
    /// <summary>
    /// One cell in the bottom tray carousel: shows an owned-but-unplaced entry's icon and, when tapped,
    /// hands the entry back to the controller so its placement arrows light up. Bound/rebound by
    /// <see cref="DenTrayCarouselUI"/> as the player pages through the tray; an unbound cell goes empty.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DenTrayItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text textName;
        [Tooltip("Optional: shown when this cell has no item to display (tray smaller than the row).")]
        [SerializeField] private GameObject emptyState;

        private ModelCollectionEntry _entry;
        private Action<ModelCollectionEntry> _onSelect;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(Click);
        }

        public void Bind(ModelCollectionEntry entry, Action<ModelCollectionEntry> onSelect)
        {
            _entry = entry;
            _onSelect = onSelect;

            bool has = entry != null;
            if (icon != null)
            {
                icon.enabled = has;
                icon.sprite = has ? entry.icon : null;
            }
            if(textName != null) textName.text = has ? entry.title : "";
            if (emptyState != null) emptyState.SetActive(!has);
            if (button != null) button.interactable = has;
        }

        private void Click()
        {
            if (_entry != null) _onSelect?.Invoke(_entry);
        }
    }
}
