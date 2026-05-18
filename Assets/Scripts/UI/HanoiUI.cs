using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HanoiGame
{
    public class HanoiUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int handIndex;
        public Font uiFont;
        private Image[] _pegHighlights = new Image[3];
        public HanoiPuzzle Puzzle { get; private set; }
        public CardData CardData { get; private set; }

        private BattleManager _battle;
        private RectTransform _panelRt;
        private Text _infoText, _progressText;
        private Image _borderImage;
        private List<GameObject>[] _diskObjects = new List<GameObject>[3];
        private GameObject _targetMarker;
        private float _pw, _ph, _pegH, _pegBot, _diskH;
        private float[] _pegX;

        // Drag state
        private int _dragFromPeg = -1;
        private GameObject _draggedDisk;
        private RectTransform _draggedRt;
        private Vector2 _dragOffset;

        // Genshin element colors: Pyro, Cryo, Hydro, Electro, Anemo, Geo, Dendro, Gold(Omni)
        private static readonly Color[] DiskColors = {
            new(1f, 0.35f, 0.2f),   // 1 - Pyro 火红
            new(0.5f, 0.85f, 1f),    // 2 - Cryo 冰蓝
            new(0.3f, 0.5f, 1f),     // 3 - Hydro 水蓝
            new(0.7f, 0.3f, 1f),     // 4 - Electro 紫电
            new(0.35f, 0.85f, 0.7f), // 5 - Anemo 风绿
            new(0.85f, 0.7f, 0.3f),  // 6 - Geo 岩黄
            new(0.3f, 0.8f, 0.35f),  // 7 - Dendro 草绿
            new(1f, 0.84f, 0f)        // 8 - Omni 金色
        };

        // ── init ──
        public void Initialize(HanoiPuzzle puzzle, CardData cardData, int index, BattleManager battle)
        {
            // Destroy all old children so re-init on new turn works cleanly
            foreach (Transform child in transform) Destroy(child.gameObject);
            _diskObjects = new List<GameObject>[3];
            Puzzle = puzzle; CardData = cardData; handIndex = index; _battle = battle;
            _panelRt = GetComponent<RectTransform>();
            for (int p = 0; p < 3; p++) _diskObjects[p] = new();
            if (uiFont == null) uiFont = FontHelper.GetFont(12);
            CalcLayout();
            BuildBase();
            BuildDisks();
            UpdateLabels();
            ApplyElementTheme();
        }

        void ApplyElementTheme()
        {
            if (IsPreview) return;
            string elemName = CardData?.element.ToString().ToLower() ?? "default";
            var tex = Resources.Load<Texture2D>($"card_{elemName}");
            if (tex == null) tex = Resources.Load<Texture2D>("card_default");
            if (tex != null)
            {
                var img = GetComponent<Image>();
                if (img != null) { img.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), Vector2.one*0.5f); img.color = Color.white; }
            }
        }

        public void RefreshPuzzle(HanoiPuzzle puzzle, CardData cardData)
        {
            Puzzle = puzzle; CardData = cardData;
            ClearDisks(); BuildDisks(); UpdateLabels();
        }

        // ── layout ──
        void CalcLayout()
        {
            _pw = _panelRt.rect.width; _ph = _panelRt.rect.height;
            _pegBot = _ph * 0.08f;
            _pegH = _ph * 0.56f;
            _diskH = Mathf.Min(15f, _pegH / 9f);
            _pegX = new[] { _pw * 0.2f, _pw * 0.5f, _pw * 0.8f };
        }

        void BuildBase()
        {
            // Border for task card
            var bo = new GameObject("Border", typeof(Image));
            bo.transform.SetParent(transform, false);
            _borderImage = bo.GetComponent<Image>();
            _borderImage.color = Color.clear; _borderImage.raycastTarget = false;
            var br = bo.GetComponent<RectTransform>();
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.sizeDelta = Vector2.zero;

            // Pegs + platforms
            for (int p = 0; p < 3; p++)
            {
                float cx = _pegX[p];
                // Platform
                MakeRect($"Plat{p}", cx, _pegBot, _pw * 0.22f, 5f, new Color(0.55f, 0.5f, 0.45f));
                // Peg
                MakeRect($"Peg{p}", cx, _pegBot + _pegH * 0.5f, 7f, _pegH, new Color(0.55f, 0.5f, 0.45f));

                // Drag highlight overlay per peg
                var hl = MakeRect($"HL{p}", cx, _pegBot + _pegH * 0.5f, _pw * 0.22f, _pegH, Color.clear);
                _pegHighlights[p] = hl.GetComponent<Image>();
                _pegHighlights[p].raycastTarget = false;
            }

            // Target indicator (recreated each refresh)
            _targetMarker = MakeRect("Target", 0, _pegBot - 6f, _pw * 0.12f, 6f, new Color(1f, 0.84f, 0f, 0.9f));
            _targetMarker.GetComponent<Image>().raycastTarget = false;

            // Info text
            var ig = new GameObject("Info", typeof(Text));
            ig.transform.SetParent(transform, false);
            _infoText = ig.GetComponent<Text>();
            _infoText.font = uiFont; _infoText.fontSize = 10; _infoText.alignment = TextAnchor.UpperCenter;
            _infoText.color = new Color(1f, 0.9f, 0.7f); _infoText.raycastTarget = false;
            _infoText.rectTransform.anchorMin = _infoText.rectTransform.anchorMax = new Vector2(0.5f, 1);
            _infoText.rectTransform.pivot = new Vector2(0.5f, 1);
            _infoText.rectTransform.sizeDelta = new Vector2(_pw - 12, 40);
            _infoText.rectTransform.anchoredPosition = new Vector2(0, 0);

            var pg = new GameObject("Prog", typeof(Text));
            pg.transform.SetParent(transform, false);
            _progressText = pg.GetComponent<Text>();
            _progressText.font = uiFont; _progressText.fontSize = 9; _progressText.alignment = TextAnchor.LowerCenter;
            _progressText.color = new Color(1f, 0.84f, 0f); _progressText.raycastTarget = false;
            _progressText.rectTransform.anchorMin = _progressText.rectTransform.anchorMax = new Vector2(0.5f, 0);
            _progressText.rectTransform.pivot = new Vector2(0.5f, 0);
            _progressText.rectTransform.sizeDelta = new Vector2(_pw - 12, 16);
            _progressText.rectTransform.anchoredPosition = new Vector2(0, 2);
        }

        GameObject MakeRect(string name, float cx, float cy, float w, float h, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx - _pw * 0.5f, cy);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = raycast;
            return go;
        }

        float DiskW(int size) => 12f + size * 14f;

        // ── disks ──
        void ClearDisks()
        {
            for (int p = 0; p < 3; p++) { if (_diskObjects[p] == null) _diskObjects[p] = new(); foreach (var d in _diskObjects[p]) Destroy(d); _diskObjects[p].Clear(); if (_pegHighlights[p] != null) _pegHighlights[p].color = Color.clear; }
        }

        void BuildDisks()
        {
            if (Puzzle == null) return;
            for (int p = 0; p < 3; p++)
            {
                var peg = Puzzle.GetPeg(p);
                for (int i = 0; i < peg.Count; i++)
                {
                    int size = peg[i];
                    bool isTop = (i == peg.Count - 1);
                    float y = _pegBot + _diskH * i + _diskH * 0.5f;
                    var disk = MakeRect($"D{size}", _pegX[p], y, DiskW(size), _diskH,
                        DiskColors[Mathf.Clamp(size - 1, 0, DiskColors.Length - 1)], false); // never block raycasts
                    if (isTop) disk.AddComponent<DiskDragger>();
                    _diskObjects[p].Add(disk);
                }
            }
        }

        void UpdateLabels()
        {
            if (_infoText && CardData != null)
            {
                string elemTag = CardData.isTaskCard ? "" : $"【{CardData.element}】";
                string desc = CardData.effectDescription;
                // Shorten if too long
                if (desc.Length > 20) desc = desc.Substring(0, 18) + "..";
                _infoText.text = CardData.isTaskCard ? $"★ {desc}" : $"{elemTag} {desc}";
            }
            if (_progressText)
                _progressText.text = (CardData != null && CardData.isTaskCard) ? $"进度 {Puzzle?.stepsUsed ?? 0}/255" : "";
            if (_borderImage)
                _borderImage.color = (CardData != null && CardData.isTaskCard) ? new Color(1f, 0.84f, 0f, 0.2f) : Color.clear;

            // Move target marker
            if (_targetMarker != null && Puzzle != null)
            {
                var rt = _targetMarker.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(_pegX[Puzzle.targetPeg] - _pw * 0.5f, _pegBot - 6f);
                _targetMarker.SetActive(true);
            }
        }

        // ── drag ──
        bool IsPreview => handIndex == -1;
        bool IsTask => handIndex == -2;

        public void OnBeginDrag(PointerEventData e)
        {
            if (Puzzle == null) return;
            if (IsPreview) return; // preview mode: no drag
            if (!IsTask && (_battle == null || _battle.IsBattleEnded() || !_battle.IsPlayerTurn())) return;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRt, e.position, e.pressEventCamera, out local);
            local += new Vector2(_pw * 0.5f, 0);

            float bestDist = float.MaxValue;
            int bestPeg = -1;
            for (int p = 0; p < 3; p++)
            {
                float d = Mathf.Abs(local.x - _pegX[p]);
                if (d < _pw * 0.18f && d < bestDist && Puzzle.GetPeg(p).Count > 0)
                { bestDist = d; bestPeg = p; }
            }
            if (bestPeg < 0) return;
            if (!IsTask && !IsPreview && _battle.blockedPegTurns > 0 && _battle.blockedPegIndex == bestPeg) return;

            _dragFromPeg = bestPeg;
            var disks = _diskObjects[bestPeg];
            _draggedDisk = disks[disks.Count - 1];
            _draggedRt = _draggedDisk.GetComponent<RectTransform>();
            _draggedRt.SetAsLastSibling();
            SimpleAudio.Instance?.PlayClick();
        }

        public void OnDrag(PointerEventData e)
        {
            if (_draggedRt == null) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRt, e.position, e.pressEventCamera, out local);
            _draggedRt.anchoredPosition = new Vector2(local.x, local.y + _ph * 0.5f - _diskH * 0.5f);

            // Highlight valid/invalid target pegs
            int size = Puzzle.TopDisk(_dragFromPeg);
            for (int p = 0; p < 3; p++)
            {
                if (_pegHighlights[p] != null)
                {
                    bool valid = p != _dragFromPeg && Puzzle.IsValidMove(_dragFromPeg, p);
                    if (valid) _pegHighlights[p].color = new Color(0, 1, 0, 0.3f); // green glow
                    else if (p != _dragFromPeg) _pegHighlights[p].color = new Color(1, 0, 0, 0.2f); // red dim
                    else _pegHighlights[p].color = Color.clear;
                }
            }
        }

        public void OnEndDrag(PointerEventData e)
        {
            // Clear highlights
            for (int p = 0; p < 3; p++) if (_pegHighlights[p] != null) _pegHighlights[p].color = Color.clear;

            if (_draggedRt == null || _dragFromPeg < 0) { _dragFromPeg = -1; _draggedDisk = null; _draggedRt = null; return; }

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRt, e.position, e.pressEventCamera, out local);
            local += new Vector2(_pw * 0.5f, 0);

            int toPeg = -1;
            float bestDist = float.MaxValue;
            for (int p = 0; p < 3; p++)
            {
                float d = Mathf.Abs(local.x - _pegX[p]);
                if (d < _pw * 0.25f && d < bestDist) { bestDist = d; toPeg = p; }
            }

            if (toPeg >= 0 && toPeg != _dragFromPeg && Puzzle.IsValidMove(_dragFromPeg, toPeg))
            {
                bool canMove = IsTask ? _battle.UseTaskStep() : (IsPreview || _battle.UseStep());
                if (canMove)
                {
                    // Save for undo before making the move
                    if (!IsPreview) _battle.SaveForUndo(IsTask ? -1 : handIndex, Puzzle);
                    SimpleAudio.Instance?.PlayMove();
                    int from = _dragFromPeg;
                    int diskSize = Puzzle.TopDisk(from);
                    _diskObjects[from].RemoveAt(_diskObjects[from].Count - 1);
                    _diskObjects[toPeg].Add(_draggedDisk);
                    Puzzle.MoveDisk(from, toPeg);

                    float landY = _pegBot + _diskH * (_diskObjects[toPeg].Count - 1) + _diskH * 0.5f;
                    _draggedRt.anchoredPosition = new Vector2(_pegX[toPeg] - _pw * 0.5f, landY);
                    Destroy(_draggedDisk.GetComponent<DiskDragger>());

                    if (Puzzle.GetPeg(toPeg).Count > 0)
                    {
                        int topIdx = Puzzle.GetPeg(toPeg).Count - 1;
                        var oldDisk = _diskObjects[toPeg][topIdx];
                        if (oldDisk != _draggedDisk) Destroy(oldDisk.GetComponent<DiskDragger>());
                    }
                    UpdateLabels();

                    if (Puzzle.IsComplete())
                    {
                        StartCoroutine(CompleteAnim());
                    }
                    else
                    {
                        for (int p = 0; p < 3; p++)
                            if (_diskObjects[p].Count > 0 && _diskObjects[p][^1].GetComponent<DiskDragger>() == null)
                                _diskObjects[p][^1].AddComponent<DiskDragger>();
                        if (!IsPreview) _battle.OnStateChanged?.Invoke();
                    }
                }
                else
                {
                    // No steps — snap back
                    SnapBack();
                }
            }
            else
            {
                // Invalid — snap back
                if (toPeg >= 0 && toPeg != _dragFromPeg) SimpleAudio.Instance?.PlayError();
                SnapBack();
            }

            _dragFromPeg = -1; _draggedDisk = null; _draggedRt = null;
        }

        void SnapBack()
        {
            if (_draggedRt == null) return;
            float y = _pegBot + _diskH * (_diskObjects[_dragFromPeg].Count - 1) + _diskH * 0.5f;
            StartCoroutine(SnapAnim(_draggedRt, new Vector2(_pegX[_dragFromPeg] - _pw * 0.5f, y)));
        }

        IEnumerator SnapAnim(RectTransform rt, Vector2 target)
        {
            Vector2 start = rt.anchoredPosition;
            float t = 0f, dur = 0.12f;
            while (t < dur) { t += Time.deltaTime; rt.anchoredPosition = Vector2.Lerp(start, target, t / dur); yield return null; }
            rt.anchoredPosition = target;
        }

        IEnumerator CompleteAnim()
        {
            var all = new List<GameObject>();
            for (int p = 0; p < 3; p++) all.AddRange(_diskObjects[p]);
            float t = 0f;
            while (t < 0.35f) { t += Time.deltaTime; foreach (var d in all) d.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.3f, t / 0.35f); yield return null; }
            for (int p = 0; p < 3; p++) { foreach (var d in _diskObjects[p]) Destroy(d); _diskObjects[p].Clear(); }
            if (IsPreview) { Puzzle.GenerateRandomState(); }
            else if (IsTask) _battle.OnTaskPuzzleCompleted();
            else _battle.OnPuzzleCompleted(handIndex);
            BuildDisks();
            UpdateLabels();
            if (!IsPreview) _battle.OnStateChanged?.Invoke();
        }

        // Marker component for top disk drag targeting
        class DiskDragger : MonoBehaviour { }
    }
}
