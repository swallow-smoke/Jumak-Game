using System;
using System.Collections.Generic;
using _001_Scripts._002_Controller;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    public sealed class KeyBindingSettingsPanel : MonoBehaviour
    {
        private static readonly PlayerControlAction[] Actions =
        {
            PlayerControlAction.MoveUp,
            PlayerControlAction.MoveDown,
            PlayerControlAction.MoveLeft,
            PlayerControlAction.MoveRight,
            PlayerControlAction.Interact,
            PlayerControlAction.SelectUp,
            PlayerControlAction.SelectDown,
            PlayerControlAction.Dash,
            PlayerControlAction.Drop
        };

        private readonly Dictionary<(PlayerControlProfile, PlayerControlAction), Text> bindingLabels = new();

        private GameObject settingsPanel;
        private GameObject panelRoot;
        private Text statusText;
        private Font font;
        private bool capturing;
        private PlayerControlProfile captureProfile;
        private PlayerControlAction captureAction;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Initialize(GameObject sourceSettingsPanel)
        {
            settingsPanel = sourceSettingsPanel;
            if (settingsPanel == null || panelRoot != null)
            {
                return;
            }

            font = settingsPanel.GetComponentInChildren<Text>(true)?.font
                   ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildOpenButton();
            BuildPanel();
            panelRoot.SetActive(false);
        }

        public bool HandleKeyboardInput()
        {
            if (!IsOpen)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return true;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (capturing)
                {
                    CancelCapture("키 변경을 취소했습니다.");
                }
                else
                {
                    Close(true);
                }

                return true;
            }

            if (!capturing)
            {
                return true;
            }

            foreach (KeyControl keyControl in keyboard.allKeys)
            {
                if (!keyControl.wasPressedThisFrame)
                {
                    continue;
                }

                Key key = keyControl.keyCode;
                if (PlayerControlBindings.TrySet(captureProfile, captureAction, key, out string error))
                {
                    capturing = false;
                    statusText.text = $"{PlayerControlBindings.GetActionLabel(captureAction)}: " +
                                      $"{PlayerControlBindings.GetKeyLabel(key)} 적용 완료";
                    RefreshLabels();
                }
                else
                {
                    statusText.text = error;
                }

                return true;
            }

            return true;
        }

        public void CloseImmediate()
        {
            capturing = false;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void BuildOpenButton()
        {
            Button button = CreateButton(settingsPanel.transform, "ControlsButton", "조작키 변경", new Color32(191, 132, 48, 255));
            SetRect(button.GetComponent<RectTransform>(), Vector2.one, Vector2.one, new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(190f, 54f));
            button.onClick.AddListener(Open);
        }

        private void BuildPanel()
        {
            panelRoot = new GameObject("KeyBindingPanel", typeof(RectTransform), typeof(Image), typeof(Shadow));
            panelRoot.transform.SetParent(settingsPanel.transform.parent, false);
            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            SetRect(rootRect, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(980f, 760f));
            panelRoot.GetComponent<Image>().color = new Color32(249, 235, 202, 255);
            Shadow shadow = panelRoot.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(8f, -8f);

            Text title = CreateText(panelRoot.transform, "Title", "조작키 설정", 38, FontStyle.Bold,
                new Color32(82, 43, 25, 255));
            SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(-80f, 62f));

            BuildProfileColumn(PlayerControlProfile.Hall, -245f);
            BuildProfileColumn(PlayerControlProfile.Kitchen, 245f);

            statusText = CreateText(panelRoot.transform, "Status", "변경할 키를 누른 뒤 원하는 키를 입력하세요.", 20,
                FontStyle.Normal, new Color32(104, 76, 54, 255));
            SetRect(statusText.rectTransform, new Vector2(0.2f, 0f), new Vector2(0.8f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 28f), new Vector2(0f, 44f));

            Button closeButton = CreateButton(panelRoot.transform, "Close", "완료", new Color32(205, 150, 56, 255));
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(150f, 54f));
            closeButton.onClick.AddListener(() => Close(true));
        }

        private void BuildProfileColumn(PlayerControlProfile profile, float centerX)
        {
            Text header = CreateText(panelRoot.transform, $"{profile}_Header",
                PlayerControlBindings.GetProfileLabel(profile), 27, FontStyle.Bold, new Color32(133, 70, 35, 255));
            SetRect(header.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(centerX, 245f), new Vector2(420f, 48f));

            for (int i = 0; i < Actions.Length; i++)
            {
                PlayerControlAction action = Actions[i];
                float y = 198f - i * 46f;
                Text actionLabel = CreateText(panelRoot.transform, $"{profile}_{action}_Label",
                    PlayerControlBindings.GetActionLabel(action), 19, FontStyle.Normal, new Color32(75, 57, 43, 255));
                actionLabel.alignment = TextAnchor.MiddleLeft;
                SetRect(actionLabel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(centerX - 105f, y), new Vector2(205f, 44f));

                Button bindingButton = CreateButton(panelRoot.transform, $"{profile}_{action}_Button",
                    PlayerControlBindings.GetKeyLabel(PlayerControlBindings.Get(profile, action)),
                    new Color32(116, 86, 58, 255));
                SetRect(bindingButton.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f,
                    Vector2.one * 0.5f, new Vector2(centerX + 115f, y), new Vector2(190f, 42f));
                bindingLabels[(profile, action)] = bindingButton.GetComponentInChildren<Text>();
                bindingButton.onClick.AddListener(() => BeginCapture(profile, action));
            }

            Button resetButton = CreateButton(panelRoot.transform, $"{profile}_Reset", "기본값 복원",
                new Color32(151, 107, 68, 255));
            SetRect(resetButton.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.one * 0.5f, new Vector2(centerX, -240f), new Vector2(210f, 48f));
            resetButton.onClick.AddListener(() =>
            {
                PlayerControlBindings.Reset(profile);
                statusText.text = $"{PlayerControlBindings.GetProfileLabel(profile)} 기본값을 복원했습니다.";
                RefreshLabels();
            });
        }

        private void Open()
        {
            settingsPanel.SetActive(false);
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            capturing = false;
            statusText.text = "변경할 키를 누른 뒤 원하는 키를 입력하세요.";
            RefreshLabels();
        }

        private void Close(bool restoreSettings)
        {
            capturing = false;
            panelRoot.SetActive(false);
            if (restoreSettings && settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void BeginCapture(PlayerControlProfile profile, PlayerControlAction action)
        {
            captureProfile = profile;
            captureAction = action;
            capturing = true;
            statusText.text = $"{PlayerControlBindings.GetProfileLabel(profile)} · " +
                              $"{PlayerControlBindings.GetActionLabel(action)}에 사용할 키를 누르세요.  (ESC 취소)";
        }

        private void CancelCapture(string message)
        {
            capturing = false;
            statusText.text = message;
        }

        private void RefreshLabels()
        {
            foreach (KeyValuePair<(PlayerControlProfile, PlayerControlAction), Text> entry in bindingLabels)
            {
                entry.Value.text = PlayerControlBindings.GetKeyLabel(
                    PlayerControlBindings.Get(entry.Key.Item1, entry.Key.Item2));
            }
        }

        private Button CreateButton(Transform parent, string objectName, string label, Color color)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(root.transform, "Label", label, 20, FontStyle.Bold, new Color32(255, 244, 216, 255));
            Stretch(text.rectTransform, 6f);
            return button;
        }

        private Text CreateText(Transform parent, string objectName, string value, int size, FontStyle style, Color color)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = -Vector2.one * padding;
        }
    }
}
