using _001_Scripts._004_UI.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._900_Tools.Editor
{
    public static class PauseSettingsMenuBuilder
    {
        private const string FontPath = "Assets/002_Resources/005_Fonts/CookieRun Bold.otf";
        private static Font font;

        [MenuItem("Joomak/UI/Add Settings Menu _F8")]
        public static void Build()
        {
            Canvas canvas = FindRootCanvas();
            if (canvas == null)
            {
                Debug.LogError("[PauseSettingsMenuBuilder] 활성 씬에서 Canvas를 찾지 못했습니다.");
                return;
            }

            PauseSettingsMenu[] existingMenus = Object.FindObjectsByType<PauseSettingsMenu>(FindObjectsInactive.Include);
            if (existingMenus.Length > 0)
            {
                PauseSettingsMenu existingMenu = null;
                foreach (PauseSettingsMenu candidate in existingMenus)
                {
                    if (!candidate.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    if (existingMenu == null || candidate.transform.parent == canvas.transform)
                    {
                        existingMenu = candidate;
                    }
                }

                if (existingMenu == null)
                {
                    return;
                }

                foreach (PauseSettingsMenu duplicate in existingMenus)
                {
                    if (duplicate != existingMenu && duplicate.gameObject.scene.IsValid())
                    {
                        duplicate.gameObject.SetActive(false);
                        EditorUtility.SetDirty(duplicate.gameObject);
                    }
                }

                if (existingMenu.transform.parent != canvas.transform)
                {
                    Undo.SetTransformParent(existingMenu.transform, canvas.transform, "Move Settings Menu To Screen Canvas");
                    Stretch(existingMenu.GetComponent<RectTransform>());
                }

                existingMenu.gameObject.SetActive(true);
                EditorUtility.SetDirty(existingMenu);
                Selection.activeGameObject = existingMenu.gameObject;
                Debug.Log("[PauseSettingsMenuBuilder] 설정 메뉴를 화면 UI Canvas에 정리했습니다.");
                return;
            }

            font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            GameObject root = CreateUiObject("PauseSettingsMenu", canvas.transform, typeof(PauseSettingsMenu));
            Undo.RegisterCreatedObjectUndo(root, "Add Settings Menu");
            Stretch(root.GetComponent<RectTransform>());

            GameObject dimmer = CreateUiObject("DimmedBackground", root.transform, typeof(Image));
            Stretch(dimmer.GetComponent<RectTransform>());
            dimmer.GetComponent<Image>().color = new Color(0.08f, 0.04f, 0.025f, 0.74f);

            GameObject panel = CreateUiObject("SettingsPanel", root.transform, typeof(Image), typeof(Shadow), typeof(Outline));
            SetCentered(panel.GetComponent<RectTransform>(), new Vector2(660f, 720f));
            panel.GetComponent<Image>().color = Hex("FFF3D6");
            Shadow shadow = panel.GetComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.035f, 0.015f, 0.7f);
            shadow.effectDistance = new Vector2(8f, -8f);
            Outline outline = panel.GetComponent<Outline>();
            outline.effectColor = Hex("7D4328");
            outline.effectDistance = new Vector2(3f, -3f);

            CreateText(panel.transform, "Title", "설정", 38, Hex("6C3522"), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(400f, 52f));
            CreateText(panel.transform, "Subtitle", "ESC를 다시 누르면 게임으로 돌아갑니다", 18, Hex("8C6A55"), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(500f, 32f));
            CreateDivider(panel.transform, -108f);

            CreateSectionTitle(panel.transform, "소리", -135f);
            Slider master = CreateSliderRow(panel.transform, "전체 음량", -188f, out Text masterValue);
            Slider bgm = CreateSliderRow(panel.transform, "배경 음악", -248f, out Text bgmValue);
            Slider sfx = CreateSliderRow(panel.transform, "효과음", -308f, out Text sfxValue);

            CreateDivider(panel.transform, -348f);
            CreateSectionTitle(panel.transform, "화면", -378f);
            Toggle fullscreen = CreateToggleRow(panel.transform, "전체 화면", -428f);
            Dropdown resolution = CreateResolutionDropdown(panel.transform, -486f);

            GameObject controlsBox = CreateUiObject("ControlsGuide", panel.transform, typeof(Image));
            RectTransform controlsRect = controlsBox.GetComponent<RectTransform>();
            SetTop(controlsRect, new Vector2(0.5f, 1f), new Vector2(0f, -555f), new Vector2(570f, 54f));
            controlsBox.GetComponent<Image>().color = new Color(0.55f, 0.34f, 0.22f, 0.1f);
            CreateText(controlsBox.transform, "GuideText", "이동  WASD / 방향키     상호작용  플레이어 지정 키     대시  SHIFT", 16,
                Hex("75513D"), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 42f));

            Button resume = CreateButton(panel.transform, "ResumeButton", "계속하기", new Vector2(-205f, -655f), new Vector2(170f, 48f), Hex("C87938"));
            Button restart = CreateButton(panel.transform, "RestartButton", "다시 시작", new Vector2(0f, -655f), new Vector2(170f, 48f), Hex("9D6B43"));
            Button quit = CreateButton(panel.transform, "QuitButton", "게임 종료", new Vector2(205f, -655f), new Vector2(170f, 48f), Hex("784536"));

            SerializedObject menuData = new(root.GetComponent<PauseSettingsMenu>());
            menuData.FindProperty("dimmedBackground").objectReferenceValue = dimmer;
            menuData.FindProperty("settingsPanel").objectReferenceValue = panel;
            menuData.FindProperty("masterVolumeSlider").objectReferenceValue = master;
            menuData.FindProperty("bgmVolumeSlider").objectReferenceValue = bgm;
            menuData.FindProperty("sfxVolumeSlider").objectReferenceValue = sfx;
            menuData.FindProperty("masterVolumeValue").objectReferenceValue = masterValue;
            menuData.FindProperty("bgmVolumeValue").objectReferenceValue = bgmValue;
            menuData.FindProperty("sfxVolumeValue").objectReferenceValue = sfxValue;
            menuData.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
            menuData.FindProperty("resolutionDropdown").objectReferenceValue = resolution;
            menuData.FindProperty("resumeButton").objectReferenceValue = resume;
            menuData.FindProperty("restartButton").objectReferenceValue = restart;
            menuData.FindProperty("quitButton").objectReferenceValue = quit;
            menuData.ApplyModifiedPropertiesWithoutUndo();

            dimmer.SetActive(false);
            panel.SetActive(false);
            EditorUtility.SetDirty(root);
            Selection.activeGameObject = root;
            Debug.Log("[PauseSettingsMenuBuilder] ESC 설정 메뉴를 Canvas에 추가했습니다.");
        }

        private static Slider CreateSliderRow(Transform parent, string labelText, float y, out Text valueText)
        {
            CreateText(parent, labelText.Replace(" ", string.Empty) + "Label", labelText, 20, Hex("5B3929"), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(105f, y), new Vector2(140f, 42f));

            GameObject sliderObject = CreateUiObject(labelText.Replace(" ", string.Empty) + "Slider", parent, typeof(Slider));
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            SetTop(sliderRect, new Vector2(0f, 1f), new Vector2(345f, y), new Vector2(330f, 30f));

            GameObject background = CreateUiObject("Background", sliderObject.transform, typeof(Image));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 12f);
            background.GetComponent<Image>().color = Hex("D8C4A1");

            GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
            Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(6f, 9f), new Vector2(-6f, -9f));
            GameObject fill = CreateUiObject("Fill", fillArea.transform, typeof(Image));
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = Hex("D6813F");

            GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(7f, 0f), new Vector2(-7f, 0f));
            GameObject handle = CreateUiObject("Handle", handleArea.transform, typeof(Image), typeof(Outline));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 24f);
            handle.GetComponent<Image>().color = Hex("FFF9E9");
            handle.GetComponent<Outline>().effectColor = Hex("A65D31");

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 1f;

            valueText = CreateText(parent, labelText.Replace(" ", string.Empty) + "Value", "100%", 18, Hex("76513E"), TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(-52f, y), new Vector2(60f, 40f));
            return slider;
        }

        private static Toggle CreateToggleRow(Transform parent, string labelText, float y)
        {
            CreateText(parent, "FullscreenLabel", labelText, 20, Hex("5B3929"), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(105f, y), new Vector2(200f, 42f));

            GameObject toggleObject = CreateUiObject("FullscreenToggle", parent, typeof(Toggle));
            SetTop(toggleObject.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-75f, y), new Vector2(48f, 30f));
            GameObject background = CreateUiObject("Background", toggleObject.transform, typeof(Image), typeof(Outline));
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = Hex("E2CEAA");
            background.GetComponent<Outline>().effectColor = Hex("A37555");
            GameObject checkmark = CreateUiObject("Checkmark", background.transform, typeof(Image));
            Stretch(checkmark.GetComponent<RectTransform>(), new Vector2(6f, 6f), new Vector2(-6f, -6f));
            checkmark.GetComponent<Image>().color = Hex("C96E36");

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            return toggle;
        }

        private static Dropdown CreateResolutionDropdown(Transform parent, float y)
        {
            CreateText(parent, "ResolutionLabel", "해상도", 20, Hex("5B3929"), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(105f, y), new Vector2(150f, 42f));

            GameObject dropdownObject = CreateUiObject("ResolutionDropdown", parent, typeof(Image), typeof(Dropdown), typeof(Outline));
            SetTop(dropdownObject.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-190f, y), new Vector2(250f, 42f));
            dropdownObject.GetComponent<Image>().color = Hex("F7E7C6");
            dropdownObject.GetComponent<Outline>().effectColor = Hex("A37555");

            Text caption = CreateText(dropdownObject.transform, "Label", "1920 x 1080", 18, Hex("5B3929"), TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0.5f), new Vector2(-8f, 0f), new Vector2(190f, 38f));
            Text arrow = CreateText(dropdownObject.transform, "Arrow", "▼", 16, Hex("8E5533"), TextAnchor.MiddleCenter,
                new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(34f, 38f));
            arrow.raycastTarget = false;

            GameObject template = CreateUiObject("Template", dropdownObject.transform, typeof(Image), typeof(ScrollRect));
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);
            templateRect.sizeDelta = new Vector2(0f, 180f);
            template.GetComponent<Image>().color = Hex("FFF5DF");

            GameObject viewport = CreateUiObject("Viewport", template.transform, typeof(Image), typeof(Mask));
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            GameObject content = CreateUiObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 36f);

            GameObject item = CreateUiObject("Item", content.transform, typeof(Toggle));
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 36f);
            GameObject itemBackground = CreateUiObject("Item Background", item.transform, typeof(Image));
            Stretch(itemBackground.GetComponent<RectTransform>());
            itemBackground.GetComponent<Image>().color = new Color(0.79f, 0.43f, 0.2f, 0.24f);
            Text itemLabel = CreateText(item.transform, "Item Label", "1920 x 1080", 17, Hex("5B3929"), TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(210f, 34f));
            Toggle itemToggle = item.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBackground.GetComponent<Image>();
            itemToggle.graphic = itemBackground.GetComponent<Image>();

            ScrollRect scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;

            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.targetGraphic = dropdownObject.GetComponent<Image>();
            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            template.SetActive(false);
            return dropdown;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
        {
            GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(Shadow));
            SetTop(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), position, size);
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Shadow shadow = buttonObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0.18f, 0.07f, 0.02f, 0.42f);
            shadow.effectDistance = new Vector2(2f, -3f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
            button.colors = colors;
            CreateText(buttonObject.transform, "Label", label, 21, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(12f, 8f));
            return button;
        }

        private static void CreateSectionTitle(Transform parent, string text, float y)
        {
            CreateText(parent, text + "SectionTitle", text, 23, Hex("A6542B"), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(70f, y), new Vector2(200f, 38f));
        }

        private static void CreateDivider(Transform parent, float y)
        {
            GameObject divider = CreateUiObject("Divider", parent, typeof(Image));
            SetTop(divider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(570f, 2f));
            divider.GetComponent<Image>().color = new Color(0.49f, 0.27f, 0.17f, 0.24f);
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Color color,
            TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            GameObject textObject = CreateUiObject(name, parent, typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] extraComponents)
        {
            System.Type[] components = new System.Type[extraComponents.Length + 1];
            components[0] = typeof(RectTransform);
            extraComponents.CopyTo(components, 1);
            GameObject gameObject = new(name, components);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Canvas FindRootCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace && canvas.gameObject.scene.IsValid())
                {
                    return canvas;
                }
            }

            return null;
        }

        private static void SetCentered(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void SetTop(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2? minOffset = null, Vector2? maxOffset = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minOffset ?? Vector2.zero;
            rect.offsetMax = maxOffset ?? Vector2.zero;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString($"#{value}", out Color color) ? color : Color.white;
        }
    }
}
