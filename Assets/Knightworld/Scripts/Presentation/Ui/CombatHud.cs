using System.Collections.Generic;
using Knightworld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Knightworld.Presentation
{
    public sealed class CombatHud : MonoBehaviour
    {
        private CombatSession _session;
        private Text _initiative;
        private Text _unit;
        private Text _log;
        private Text _tooltip;
        private Text _outcome;
        private GameObject _outcomePanel;
        private readonly Queue<string> _lines = new Queue<string>();

        public static CombatHud Create(CombatSession session)
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("CombatHud");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            var hud = canvasGo.AddComponent<CombatHud>();
            hud._session = session;
            hud.Build();
            session.LogGenerated += hud.AddLog;
            session.TurnStarted += _ => hud.Refresh();
            session.AttackResolved += _ => hud.Refresh();
            session.UnitMoved += (_, __) => hud.Refresh();
            session.UnitDied += _ => hud.Refresh();
            session.CombatEnded += hud.ShowOutcome;
            hud.Refresh();
            return hud;
        }

        public void SetTooltip(string text)
        {
            if (_tooltip == null)
                return;
            _tooltip.text = text ?? "";
        }

        public void Refresh()
        {
            if (_session == null)
                return;
            if (_initiative != null)
                _initiative.text = BuildInitiative();
            if (_unit != null)
                _unit.text = BuildUnit();
            if (_log != null)
                _log.text = string.Join("\n", _lines);
        }

        private void AddLog(string line)
        {
            _lines.Enqueue(line);
            while (_lines.Count > 6)
                _lines.Dequeue();
            Refresh();
        }

        private void Build()
        {
            _initiative = CreateText("Initiative", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), 18, TextAnchor.UpperCenter, new Vector2(1400, 80));
            _unit = CreateText("Unit", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24, 24), 20, TextAnchor.LowerLeft, new Vector2(640, 220));
            _log = CreateText("Log", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 24), 16, TextAnchor.LowerCenter, new Vector2(900, 180));
            _tooltip = CreateText("Tooltip", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), 22, TextAnchor.MiddleCenter, new Vector2(800, 60));
            CreateButton("End Turn", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40, 40), new Vector2(220, 56), () =>
            {
                if (_session.Outcome == CombatOutcome.Ongoing && _session.ActiveUnit != null && _session.ActiveUnit.Team == Team.Player)
                    _session.EndTurn();
            });

            _outcomePanel = new GameObject("Outcome");
            _outcomePanel.transform.SetParent(transform, false);
            var panelRect = _outcomePanel.AddComponent<RectTransform>();
            Stretch(panelRect);
            var image = _outcomePanel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            _outcome = CreateTextOn(_outcomePanel.transform, "Outcome", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 56), 42, TextAnchor.MiddleCenter, new Vector2(800, 120));
            CreateButtonOn(_outcomePanel.transform, "Retry", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-130, -50), new Vector2(220, 56), () =>
            {
                SceneManager.LoadScene(OverworldController.BattleScene);
            });
            CreateButtonOn(_outcomePanel.transform, "World Map", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130, -50), new Vector2(220, 56), () =>
            {
                SceneManager.LoadScene(OverworldController.SceneName);
            });
            _outcomePanel.SetActive(false);
        }

        private void ShowOutcome(CombatOutcome outcome)
        {
            if (outcome == CombatOutcome.PlayerVictory)
                CampaignState.RecordVictory();
            _outcomePanel.SetActive(true);
            _outcome.text = outcome == CombatOutcome.PlayerVictory ? "Victory" : "Defeat";
            Refresh();
        }

        private string BuildInitiative()
        {
            var parts = new List<string>();
            var living = _session.Initiative.LivingOrder(_session.Units);
            foreach (var id in living)
            {
                var unit = _session.GetUnit(id);
                if (unit == null)
                    continue;
                string mark = unit.Id == _session.Initiative.CurrentUnitId ? "▶ " : "";
                parts.Add($"{mark}{unit.Name}");
            }

            return $"Round {_session.Initiative.Round}    {string.Join("   →   ", parts)}";
        }

        private string BuildUnit()
        {
            var unit = _session.ActiveUnit;
            if (unit == null)
                return "";
            string team = unit.Team == Team.Player ? "Knight" : "Enemy";
            return $"{unit.Name}  ({unit.ClassName}, {team})\n" +
                   $"HP {unit.Hp}/{unit.MaxHp}    AC {unit.ArmorClass}\n" +
                   $"Action: {(unit.HasAction ? "Ready" : "Used")}    Bonus: {(unit.HasBonusAction ? "Ready" : "Used")}    Reaction: {(unit.HasReaction ? "Ready" : "Used")}\n" +
                   $"Movement: {unit.RemainingMovementFeet} ft\n" +
                   $"{unit.AttackName}  +{unit.AttackBonus}  {unit.Damage}  ({unit.AttackRangeFeet} ft)\n" +
                   "Click a highlighted tile to move. Click an enemy to attack. Space or End Turn finishes.";
        }

        private Text CreateText(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, int size, TextAnchor align, Vector2 sizeDelta)
        {
            return CreateTextOn(transform, name, anchorMin, anchorMax, anchored, size, align, sizeDelta);
        }

        private Text CreateTextOn(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, int size, TextAnchor align, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            if (anchorMin == new Vector2(0.5f, 1f))
                rect.pivot = new Vector2(0.5f, 1f);
            if (anchorMin == new Vector2(0.5f, 0f))
                rect.pivot = new Vector2(0.5f, 0f);
            if (anchorMin == new Vector2(1f, 0f))
                rect.pivot = new Vector2(1f, 0f);
            if (anchorMin == new Vector2(0.5f, 0.5f))
                rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = sizeDelta;
            var text = go.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private void CreateButton(string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            CreateButtonOn(transform, label, anchorMin, anchorMax, anchored, size, click);
        }

        private void CreateButtonOn(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == new Vector2(0.5f, 0.5f) ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 0f);
            if (anchorMin == new Vector2(0.5f, 0.5f))
                rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(click);
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            Stretch(textRect);
            var text = textGo.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font UiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
