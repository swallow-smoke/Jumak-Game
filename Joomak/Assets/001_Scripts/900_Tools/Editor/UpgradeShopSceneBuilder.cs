using System.Collections.Generic;
using _001_Scripts._001_Manager;
using _001_Scripts._004_UI._000_Panels;
using _001_Scripts._005_Data.Upgrade;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts._900_Tools.Editor
{
    // 그래픽 에셋 없이 기본 UGUI 도형만 사용해 Upgrade 씬의 카테고리 진입 화면을 만든다.
    public static class UpgradeShopSceneBuilder
    {
        private const string ScenePath = "Assets/000_Scenes/Upgrade.unity";
        private static readonly Color PaperWhite = Hex("F7F4EC");
        private static readonly Color InkBlack = Hex("111111");
        private static readonly Color SoftBlack = Hex("242424");
        private static readonly Color CommonYellow = Hex("E8B93F");
        private static readonly Color HallRed = Hex("C84B47");
        private static readonly Color KitchenBlue = Hex("3E6FAE");

        [MenuItem("Joomak/Upgrade/Build Category Entry Scene")]
        public static void Build()
        {
            // 상단 '전' 표시가 플레이 모드에서도 실제 RunState를 읽을 수 있게 한다.
            UpgradeCatalogBuilder.Build();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteRootIfPresent("Upgrade Shop UI");
            DeleteRootIfPresent("EventSystem");

            ConfigureCamera();

            GameObject canvasObject = new("Upgrade Shop UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            UpgradeShopEntryView view = canvasObject.AddComponent<UpgradeShopEntryView>();
            ShopController shop = canvasObject.AddComponent<ShopController>();
            CommonUpgradePanel commonPanel = canvasObject.AddComponent<CommonUpgradePanel>();
            HallUpgradePanel hallPanel = canvasObject.AddComponent<HallUpgradePanel>();
            KitchenUpgradePanel kitchenPanel = canvasObject.AddComponent<KitchenUpgradePanel>();
            List<Text> texts = new();

            CreateStretchImage(canvasObject.transform, "Background", PaperWhite);
            CreateTopRule(canvasObject.transform);

            Text title = CreateText(canvasObject.transform, "Title", "업그레이드 상점", 48, InkBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(760f, 70f));

            Text caption = CreateText(canvasObject.transform, "Caption", "영업 종료 · 다음 라운드를 준비하세요", 21, SoftBlack, FontStyle.Normal, TextAnchor.MiddleCenter, texts);
            SetRect(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(760f, 40f));

            Text moneyText = CreateStatusBlock(canvasObject.transform, "Money Status", "전  0", new Vector2(-165f, -145f), CommonYellow, texts);
            Text reputationText = CreateStatusBlock(canvasObject.transform, "Reputation Status", "명성  20 / 100", new Vector2(165f, -145f), HallRed, texts);

            GameObject entryPage = CreateRectObject("Category Entry Page", canvasObject.transform);
            Stretch(entryPage.GetComponent<RectTransform>(), 0f);

            GameObject commonPage = CreateRectObject("Common Upgrade Page", canvasObject.transform);
            Stretch(commonPage.GetComponent<RectTransform>(), 0f);

            GameObject hallPage = CreateRectObject("Hall Upgrade Page", canvasObject.transform);
            Stretch(hallPage.GetComponent<RectTransform>(), 0f);

            GameObject kitchenPage = CreateRectObject("Kitchen Upgrade Page", canvasObject.transform);
            Stretch(kitchenPage.GetComponent<RectTransform>(), 0f);

            Text guide = CreateText(entryPage.transform, "Guide", "업그레이드 분류를 선택하세요", 25, InkBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(guide.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -218f), new Vector2(700f, 46f));

            Button common = CreateCategoryCard(entryPage.transform, "Common Category", "共", "공용 업그레이드", "두 플레이어에게\n함께 적용되는 강화", CommonYellow, -420f, texts);
            Button hall = CreateCategoryCard(entryPage.transform, "Hall Category", "客", "홀 업그레이드", "접객과 홀 운영을\n보조하는 강화", HallRed, 0f, texts);
            Button kitchen = CreateCategoryCard(entryPage.transform, "Kitchen Category", "食", "주방 업그레이드", "조리와 주방 운영을\n보조하는 강화", KitchenBlue, 420f, texts);

            Button commonBackButton = CreateBackButton(commonPage.transform, CommonYellow, texts);
            Text commonTitle = CreateText(commonPage.transform, "Common Title", "공용 업그레이드", 42, InkBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(commonTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(720f, 58f));

            Text commonCaption = CreateText(commonPage.transform, "Common Caption", "황색 · 두 플레이어 공통 적용", 21, SoftBlack, FontStyle.Normal, TextAnchor.MiddleCenter, texts);
            SetRect(commonCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(720f, 38f));

            UpgradeCardParts dashCard = CreateUpgradeCard(
                commonPage.transform, "Dash Upgrade", "疾", "대쉬", "짧은 거리를 빠르게 돌진\n쿨타임 5초", CommonYellow, -500f, texts);
            UpgradeCardParts speedCard = CreateUpgradeCard(
                commonPage.transform, "Move Speed Upgrade", "速", "이동속도 증가", "단계마다 이동속도 +10%\n최대 3단계", CommonYellow, 0f, texts);
            UpgradeCardParts reputationCard = CreateUpgradeCard(
                commonPage.transform, "Reputation Upgrade", "名", "명성 회복", "명성 +10 회복\n최대 100", CommonYellow, 500f, texts);

            Text feedbackText = CreateText(commonPage.transform, "Feedback", "황색 공용 강화는 두 플레이어에게 함께 적용됩니다.", 20, SoftBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(feedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1120f, 42f));

            Button hallBackButton = CreateBackButton(hallPage.transform, HallRed, texts);
            Text hallTitle = CreateText(hallPage.transform, "Hall Title", "홀 업그레이드", 42, InkBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(hallTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(720f, 58f));

            Text hallCaption = CreateText(hallPage.transform, "Hall Caption", "적색 · 접객과 홀 운영 강화", 21, SoftBlack, FontStyle.Normal, TextAnchor.MiddleCenter, texts);
            SetRect(hallCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(720f, 38f));

            UpgradeCardParts patienceCard = CreateUpgradeCard(
                hallPage.transform, "Patience Upgrade", "忍", "손님 인내심", "단계마다 제한시간 +10초\n최대 3단계", HallRed, -500f, texts);
            UpgradeCardParts broomCard = CreateUpgradeCard(
                hallPage.transform, "Iron Broom Upgrade", "掃", "철제 손잡이 빗자루", "손놈 5→3회\n청소 3→2회", HallRed, 0f, texts);
            UpgradeCardParts tableCard = CreateUpgradeCard(
                hallPage.transform, "Table Upgrade", "床", "테이블 추가", "구매마다 테이블 +1\n최대 4회 · 총 6개", HallRed, 500f, texts);

            Text hallFeedbackText = CreateText(hallPage.transform, "Feedback", "적색 홀 강화는 접객과 홀 운영에 적용됩니다.", 20, SoftBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(hallFeedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1120f, 42f));

            Button kitchenBackButton = CreateBackButton(kitchenPage.transform, KitchenBlue, texts);
            Text kitchenTitle = CreateText(kitchenPage.transform, "Kitchen Title", "주방 업그레이드", 42, InkBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(kitchenTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(720f, 58f));

            Text kitchenCaption = CreateText(kitchenPage.transform, "Kitchen Caption", "청색 · 조리와 요리 판매 강화", 21, SoftBlack, FontStyle.Normal, TextAnchor.MiddleCenter, texts);
            SetRect(kitchenCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(720f, 38f));

            UpgradeCardParts cookTimeCard = CreateUpgradeCard(
                kitchenPage.transform, "Cook Time Upgrade", "火", "조리시간 감소", "단계마다 조리시간 -10%\n최대 3단계", KitchenBlue, -500f, texts);
            UpgradeCardParts failureDelayCard = CreateUpgradeCard(
                kitchenPage.transform, "Failure Delay Upgrade", "延", "실패시간 지연", "완성 요리 실패까지 +10%\n최대 3단계", KitchenBlue, 0f, texts);
            UpgradeCardParts premiumDishCard = CreateUpgradeCard(
                kitchenPage.transform, "Premium Dish Upgrade", "膳", "고급 요리", "요리 판매 시\n명성 +1 추가", KitchenBlue, 500f, texts);

            Text kitchenFeedbackText = CreateText(kitchenPage.transform, "Feedback", "청색 주방 강화는 조리와 요리 판매에 적용됩니다.", 20, SoftBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(kitchenFeedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1120f, 42f));

            UnityEventTools.AddPersistentListener(common.onClick, commonPanel.Open);
            UnityEventTools.AddPersistentListener(hall.onClick, hallPanel.Open);
            UnityEventTools.AddPersistentListener(kitchen.onClick, kitchenPanel.Open);
            UnityEventTools.AddPersistentListener(commonBackButton.onClick, commonPanel.Back);
            UnityEventTools.AddPersistentListener(dashCard.Button.onClick, commonPanel.BuyDash);
            UnityEventTools.AddPersistentListener(speedCard.Button.onClick, commonPanel.BuyMoveSpeed);
            UnityEventTools.AddPersistentListener(reputationCard.Button.onClick, commonPanel.BuyReputation);
            UnityEventTools.AddPersistentListener(hallBackButton.onClick, hallPanel.Back);
            UnityEventTools.AddPersistentListener(patienceCard.Button.onClick, hallPanel.BuyPatience);
            UnityEventTools.AddPersistentListener(broomCard.Button.onClick, hallPanel.BuyIronBroom);
            UnityEventTools.AddPersistentListener(tableCard.Button.onClick, hallPanel.BuyTable);
            UnityEventTools.AddPersistentListener(kitchenBackButton.onClick, kitchenPanel.Back);
            UnityEventTools.AddPersistentListener(cookTimeCard.Button.onClick, kitchenPanel.BuyCookTime);
            UnityEventTools.AddPersistentListener(failureDelayCard.Button.onClick, kitchenPanel.BuyFailureDelay);
            UnityEventTools.AddPersistentListener(premiumDishCard.Button.onClick, kitchenPanel.BuyPremiumDish);

            UpgradeCatalog catalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>("Assets/002_Resources/001_Datas/UpgradeCatalog.asset");
            SerializedObject serializedShop = new(shop);
            serializedShop.FindProperty("catalog").objectReferenceValue = catalog;
            serializedShop.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedCommon = new(commonPanel);
            serializedCommon.FindProperty("categoryEntryPage").objectReferenceValue = entryPage;
            serializedCommon.FindProperty("commonPage").objectReferenceValue = commonPage;
            serializedCommon.FindProperty("shop").objectReferenceValue = shop;
            serializedCommon.FindProperty("dashButton").objectReferenceValue = dashCard.Button;
            serializedCommon.FindProperty("dashStateText").objectReferenceValue = dashCard.StateText;
            serializedCommon.FindProperty("dashPriceText").objectReferenceValue = dashCard.PriceText;
            serializedCommon.FindProperty("moveSpeedButton").objectReferenceValue = speedCard.Button;
            serializedCommon.FindProperty("moveSpeedStateText").objectReferenceValue = speedCard.StateText;
            serializedCommon.FindProperty("moveSpeedPriceText").objectReferenceValue = speedCard.PriceText;
            serializedCommon.FindProperty("reputationButton").objectReferenceValue = reputationCard.Button;
            serializedCommon.FindProperty("reputationStateText").objectReferenceValue = reputationCard.StateText;
            serializedCommon.FindProperty("reputationPriceText").objectReferenceValue = reputationCard.PriceText;
            serializedCommon.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedCommon.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedHall = new(hallPanel);
            serializedHall.FindProperty("categoryEntryPage").objectReferenceValue = entryPage;
            serializedHall.FindProperty("hallPage").objectReferenceValue = hallPage;
            serializedHall.FindProperty("shop").objectReferenceValue = shop;
            serializedHall.FindProperty("patienceButton").objectReferenceValue = patienceCard.Button;
            serializedHall.FindProperty("patienceStateText").objectReferenceValue = patienceCard.StateText;
            serializedHall.FindProperty("patiencePriceText").objectReferenceValue = patienceCard.PriceText;
            serializedHall.FindProperty("broomButton").objectReferenceValue = broomCard.Button;
            serializedHall.FindProperty("broomStateText").objectReferenceValue = broomCard.StateText;
            serializedHall.FindProperty("broomPriceText").objectReferenceValue = broomCard.PriceText;
            serializedHall.FindProperty("tableButton").objectReferenceValue = tableCard.Button;
            serializedHall.FindProperty("tableStateText").objectReferenceValue = tableCard.StateText;
            serializedHall.FindProperty("tablePriceText").objectReferenceValue = tableCard.PriceText;
            serializedHall.FindProperty("feedbackText").objectReferenceValue = hallFeedbackText;
            serializedHall.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedKitchen = new(kitchenPanel);
            serializedKitchen.FindProperty("categoryEntryPage").objectReferenceValue = entryPage;
            serializedKitchen.FindProperty("kitchenPage").objectReferenceValue = kitchenPage;
            serializedKitchen.FindProperty("shop").objectReferenceValue = shop;
            serializedKitchen.FindProperty("cookTimeButton").objectReferenceValue = cookTimeCard.Button;
            serializedKitchen.FindProperty("cookTimeStateText").objectReferenceValue = cookTimeCard.StateText;
            serializedKitchen.FindProperty("cookTimePriceText").objectReferenceValue = cookTimeCard.PriceText;
            serializedKitchen.FindProperty("failureDelayButton").objectReferenceValue = failureDelayCard.Button;
            serializedKitchen.FindProperty("failureDelayStateText").objectReferenceValue = failureDelayCard.StateText;
            serializedKitchen.FindProperty("failureDelayPriceText").objectReferenceValue = failureDelayCard.PriceText;
            serializedKitchen.FindProperty("premiumDishButton").objectReferenceValue = premiumDishCard.Button;
            serializedKitchen.FindProperty("premiumDishStateText").objectReferenceValue = premiumDishCard.StateText;
            serializedKitchen.FindProperty("premiumDishPriceText").objectReferenceValue = premiumDishCard.PriceText;
            serializedKitchen.FindProperty("feedbackText").objectReferenceValue = kitchenFeedbackText;
            serializedKitchen.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedView = new(view);
            serializedView.FindProperty("moneyText").objectReferenceValue = moneyText;
            serializedView.FindProperty("reputationText").objectReferenceValue = reputationText;
            SerializedProperty textArray = serializedView.FindProperty("sceneTexts");
            textArray.arraySize = texts.Count;
            for (int index = 0; index < texts.Count; index++)
            {
                textArray.GetArrayElementAtIndex(index).objectReferenceValue = texts[index];
            }
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            commonPage.SetActive(false);
            hallPage.SetActive(false);
            kitchenPage.SetActive(false);

            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = canvasObject;
            Debug.Log($"[UpgradeShopSceneBuilder] 카테고리 + 공용/홀/주방 업그레이드 화면 생성 완료: {ScenePath}");
        }

        private readonly struct UpgradeCardParts
        {
            public UpgradeCardParts(Button button, Text stateText, Text priceText)
            {
                Button = button;
                StateText = stateText;
                PriceText = priceText;
            }

            public Button Button { get; }
            public Text StateText { get; }
            public Text PriceText { get; }
        }

        private static UpgradeCardParts CreateUpgradeCard(
            Transform parent,
            string name,
            string symbol,
            string title,
            string description,
            Color accentColor,
            float x,
            List<Text> texts)
        {
            GameObject card = CreateRectObject(name, parent);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = InkBlack;
            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -120f), new Vector2(430f, 540f));

            GameObject accent = CreateRectObject("Accent", card.transform);
            accent.AddComponent<Image>().color = accentColor;
            SetRect(accent.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 14f));

            Text symbolText = CreateText(card.transform, "Symbol", symbol, 64, accentColor, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(symbolText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(150f, 88f));

            Text titleText = CreateText(card.transform, "Title", title, 31, accentColor, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(380f, 55f));

            Text descriptionText = CreateText(card.transform, "Description", description, 21, PaperWhite, FontStyle.Normal, TextAnchor.UpperCenter, texts);
            descriptionText.lineSpacing = 1.2f;
            SetRect(descriptionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(360f, 85f));

            Text stateText = CreateText(card.transform, "State", "미구매", 19, accentColor, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(stateText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(380f, 58f));

            GameObject purchaseObject = CreateRectObject("Purchase Button", card.transform);
            Image purchaseImage = purchaseObject.AddComponent<Image>();
            purchaseImage.color = accentColor;
            Button purchaseButton = purchaseObject.AddComponent<Button>();
            ConfigureButtonColors(purchaseButton);
            SetRect(purchaseObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 43f), new Vector2(300f, 64f));

            Text priceText = CreateText(purchaseObject.transform, "Price", "0전", 22, InkBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            Stretch(priceText.rectTransform, 8f);
            return new UpgradeCardParts(purchaseButton, stateText, priceText);
        }

        private static Button CreateBackButton(Transform parent, Color accentColor, List<Text> texts)
        {
            GameObject buttonObject = CreateRectObject("Back Button", parent);
            buttonObject.AddComponent<Image>().color = InkBlack;
            Button button = buttonObject.AddComponent<Button>();
            ConfigureButtonColors(button);
            SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -218f), new Vector2(170f, 54f));

            GameObject accent = CreateRectObject("Accent", buttonObject.transform);
            accent.AddComponent<Image>().color = accentColor;
            SetRect(accent.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(7f, 0f));

            Text text = CreateText(buttonObject.transform, "Text", "← 돌아가기", 20, PaperWhite, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        private static void ConfigureButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.76f, 0.76f, 0.76f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.72f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static Button CreateCategoryCard(
            Transform parent,
            string name,
            string symbol,
            string title,
            string description,
            Color accent,
            float x,
            List<Text> texts)
        {
            GameObject card = CreateRectObject(name, parent);
            Image image = card.AddComponent<Image>();
            image.color = InkBlack;

            Button button = card.AddComponent<Button>();
            ConfigureButtonColors(button);

            SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -108f), new Vector2(360f, 590f));

            GameObject accentBar = CreateRectObject("Accent", card.transform);
            Image accentImage = accentBar.AddComponent<Image>();
            accentImage.color = accent;
            SetRect(accentBar.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 14f));

            Text symbolText = CreateText(card.transform, "Symbol", symbol, 82, accent, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(symbolText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(190f, 110f));

            GameObject divider = CreateRectObject("Divider", card.transform);
            Image dividerImage = divider.AddComponent<Image>();
            dividerImage.color = new Color(accent.r, accent.g, accent.b, 0.9f);
            SetRect(divider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(240f, 3f));

            Text titleText = CreateText(card.transform, "Category Title", title, 32, accent, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -224f), new Vector2(310f, 70f));

            Text descriptionText = CreateText(card.transform, "Description", description, 21, PaperWhite, FontStyle.Normal, TextAnchor.UpperCenter, texts);
            descriptionText.lineSpacing = 1.25f;
            SetRect(descriptionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -315f), new Vector2(300f, 95f));

            GameObject entryBox = CreateRectObject("Entry Label", card.transform);
            Image entryImage = entryBox.AddComponent<Image>();
            entryImage.color = accent;
            SetRect(entryBox.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(250f, 58f));

            Text entryText = CreateText(entryBox.transform, "Text", "카테고리 진입", 21, InkBlack, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            Stretch(entryText.rectTransform, 8f);

            return button;
        }

        private static Text CreateStatusBlock(Transform parent, string name, string value, Vector2 position, Color accent, List<Text> texts)
        {
            GameObject block = CreateRectObject(name, parent);
            Image image = block.AddComponent<Image>();
            image.color = InkBlack;
            SetRect(block.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(300f, 66f));

            GameObject accentLine = CreateRectObject("Accent", block.transform);
            Image accentImage = accentLine.AddComponent<Image>();
            accentImage.color = accent;
            SetRect(accentLine.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 0f));

            Text text = CreateText(block.transform, "Value", value, 26, PaperWhite, FontStyle.Bold, TextAnchor.MiddleCenter, texts);
            Stretch(text.rectTransform, 10f);
            return text;
        }

        private static void CreateTopRule(Transform parent)
        {
            GameObject rule = CreateRectObject("Top Ink Rule", parent);
            Image image = rule.AddComponent<Image>();
            image.color = InkBlack;
            SetRect(rule.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Color color,
            FontStyle style,
            TextAnchor alignment,
            List<Text> texts)
        {
            GameObject gameObject = CreateRectObject(name, parent);
            Text text = gameObject.AddComponent<Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            texts.Add(text);
            return text;
        }

        private static void CreateStretchImage(Transform parent, string name, Color color)
        {
            GameObject gameObject = CreateRectObject(name, parent);
            gameObject.transform.SetAsFirstSibling();
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            Stretch(gameObject.GetComponent<RectTransform>(), 0f);
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetAsLastSibling();
        }

        private static void ConfigureCamera()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.backgroundColor = PaperWhite;
            }
        }

        private static void DeleteRootIfPresent(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
            {
                Object.DestroyImmediate(found);
            }
        }

        private static GameObject CreateRectObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color color);
            return color;
        }
    }
}
