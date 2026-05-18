using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HanoiGame
{
    public class NodeHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string tooltipText;
        private Vector3 _origScale = Vector3.one;
        private float _animTimer;
        private bool _hovered;
        private static GameObject _tooltipInstance;
        private static Text _tooltipText;

        void Start() { _origScale = transform.localScale; }

        void Update()
        {
            float target = _hovered ? 1.3f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, _origScale * target, Time.deltaTime * 12f);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _hovered = true;
            if (!string.IsNullOrEmpty(tooltipText))
                ShowTooltip(tooltipText, transform.position);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hovered = false;
            HideTooltip();
        }

        void OnDisable() { if (_hovered) { _hovered = false; HideTooltip(); } }

        static void ShowTooltip(string text, Vector3 worldPos)
        {
            if (_tooltipInstance == null)
            {
                _tooltipInstance = new GameObject("Tooltip", typeof(RectTransform), typeof(Image), typeof(CanvasRenderer));
                _tooltipInstance.transform.SetParent(GameObject.Find("Canvas")?.transform, false);
                _tooltipInstance.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.9f);
                var rt = _tooltipInstance.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 50);

                var txtGo = new GameObject("txt", typeof(Text));
                txtGo.transform.SetParent(_tooltipInstance.transform, false);
                _tooltipText = txtGo.GetComponent<Text>();
                _tooltipText.fontSize = 14;
                _tooltipText.alignment = TextAnchor.MiddleCenter;
                _tooltipText.color = Color.white;
                _tooltipText.font = FontHelper.GetFont(14);
                _tooltipText.raycastTarget = false;
                _tooltipText.rectTransform.anchorMin = Vector2.zero;
                _tooltipText.rectTransform.anchorMax = Vector2.one;
                _tooltipText.rectTransform.sizeDelta = Vector2.zero;
            }
            _tooltipText.text = text;
            _tooltipInstance.transform.position = worldPos + new Vector3(0, 50, 0);
            _tooltipInstance.SetActive(true);
        }

        static void HideTooltip()
        {
            if (_tooltipInstance) _tooltipInstance.SetActive(false);
        }
    }
}
