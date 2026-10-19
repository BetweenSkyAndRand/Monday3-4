using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Editor
{
    /// <summary>
    /// ポーズメニューに音量調整スライダーとエフェクトON/OFFトグルを自動追加するエディタツール。
    /// Tools/Game Setup/08. Add Settings to Pause Menu から実行する。
    /// </summary>
    public static class PauseMenuSettingsSetup
    {
        [MenuItem("Tools/6. Fix Tools/Settings/01. Add Settings to Pause Menu")]
        public static void AddSettingsToPauseMenu()
        {
            // 現在のシーンを取得
            Scene scene = EditorSceneManager.GetActiveScene();

            // PauseMenuCanvas を探す
            var canvas = GameObject.Find("PauseMenuCanvas");
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "PauseMenuCanvas が見つかりません。GameScene を開いて、先に Tools/Game Setup/07. Add Pause Menu to GameScene を実行してください。", "OK");
                return;
            }

            // PauseMenuPanel を探す
            var pauseMenuPanel = FindChildRecursive(canvas.transform, "PauseMenuPanel");
        if (pauseMenuPanel == null)
        {
            EditorUtility.DisplayDialog("Error", "PauseMenuPanel が見つかりません。先に Tools/Game Setup/07. Add Pause Menu to GameScene を実行してください。", "OK");
            return;
        }

        // 既に SettingsGroup が存在するか確認
        var existingSettingsGroup = pauseMenuPanel.Find("SettingsGroup");
        if (existingSettingsGroup != null)
        {
            if (!EditorUtility.DisplayDialog("Confirm", "SettingsGroup は既に存在します。再作成しますか？", "再作成", "キャンセル"))
            {
                return;
            }
            Object.DestroyImmediate(existingSettingsGroup.gameObject);
        }

            // SettingsGroup を作成
            var settingsGroup = CreateSettingsGroup(pauseMenuPanel);

            // PauseSettingsView コンポーネントを追加または取得（PauseMenuCanvasにアタッチ）
            var pauseSettingsView = canvas.GetComponent<Game.Pause.PauseSettingsView>();
            if (pauseSettingsView == null)
            {
                pauseSettingsView = canvas.AddComponent<Game.Pause.PauseSettingsView>();
            }

            // GameSettings アセットをロード
            var gameSettings = Resources.Load<Game.Core.GameSettings>("GameSettings");
        if (gameSettings != null)
        {
            // Reflection で settings フィールドに設定
            var settingsField = typeof(Game.Pause.PauseSettingsView).GetField("settings",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (settingsField != null)
            {
                settingsField.SetValue(pauseSettingsView, gameSettings);
            }
        }

        // スライダーとトグルへの参照を設定
        AssignReferencesToPauseSettingsView(pauseSettingsView, settingsGroup);

        // シーンを保存
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Complete",
            "ポーズメニューに設定UIを追加しました。\n\n" +
            "次のステップ:\n" +
            "1. Assets/Game/Core/Resources/ フォルダに GameSettings.asset を作成\n" +
            "2. PauseMenuPanel の Inspector で Settings に GameSettings.asset を設定",
            "OK");
    }

    private static GameObject CreateSettingsGroup(Transform parent)
    {
        var settingsGroup = new GameObject("SettingsGroup");
        settingsGroup.transform.SetParent(parent, false);

        var rectTransform = settingsGroup.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(600, 500);
        rectTransform.anchoredPosition = new Vector2(0, -50);

        // 音量設定セクション
        CreateVolumeSection(settingsGroup.transform);

        // エフェクト設定セクション
        CreateEffectSection(settingsGroup.transform);

        return settingsGroup;
    }

    private static void CreateVolumeSection(Transform parent)
    {
        var yPos = 200f;

        // セクションタイトル
        CreateLabel(parent, "VolumeSectionTitle", "音量設定", 28, new Vector2(0, yPos));
        yPos -= 50;

        // マスター音量
        CreateLabel(parent, "MasterVolumeLabel", "マスター音量", 20, new Vector2(-200, yPos));
        CreateSlider(parent, "MasterVolumeSlider", new Vector2(0, yPos - 30));
        yPos -= 80;

        // BGM音量
        CreateLabel(parent, "BgmVolumeLabel", "BGM音量", 20, new Vector2(-200, yPos));
        CreateSlider(parent, "BgmVolumeSlider", new Vector2(0, yPos - 30));
        yPos -= 80;

        // SE音量
        CreateLabel(parent, "SeVolumeLabel", "SE音量", 20, new Vector2(-200, yPos));
        CreateSlider(parent, "SeVolumeSlider", new Vector2(0, yPos - 30));
    }

    private static void CreateEffectSection(Transform parent)
    {
        var yPos = -120f;

        // セクションタイトル
        CreateLabel(parent, "EffectSectionTitle", "エフェクト設定", 28, new Vector2(0, yPos));
        yPos -= 50;

        // エフェクト表示トグル
        CreateToggle(parent, "VisualEffectsToggle", "エフェクト表示", new Vector2(0, yPos));
        yPos -= 50;

        // 画面エフェクトトグル
        CreateToggle(parent, "ScreenEffectsToggle", "画面エフェクト", new Vector2(0, yPos));
    }

    private static GameObject CreateLabel(Transform parent, string name, string text, int fontSize, Vector2 position)
    {
        var labelGo = new GameObject(name);
        labelGo.transform.SetParent(parent, false);

        var rectTransform = labelGo.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(400, 40);
        rectTransform.anchoredPosition = position;

        var textComponent = labelGo.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.color = Color.white;

        return labelGo;
    }

    private static GameObject CreateSlider(Transform parent, string name, Vector2 position)
    {
        var sliderGo = new GameObject(name);
        sliderGo.transform.SetParent(parent, false);

        var rectTransform = sliderGo.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(400, 20);
        rectTransform.anchoredPosition = position;

        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // Background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Fill Area
        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-10, -10);

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        var fillImage = fillGo.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.6f, 1f, 1f);

        slider.fillRect = fillRect;

        // Handle Slide Area
        var handleAreaGo = new GameObject("Handle Slide Area");
        handleAreaGo.transform.SetParent(sliderGo.transform, false);
        var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-10, 0);

        // Handle
        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        var handleRect = handleGo.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(20, 20);
        var handleImage = handleGo.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.handleRect = handleRect;

        return sliderGo;
    }

    private static GameObject CreateToggle(Transform parent, string name, string labelText, Vector2 position)
    {
        var toggleGo = new GameObject(name);
        toggleGo.transform.SetParent(parent, false);

        var rectTransform = toggleGo.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(400, 40);
        rectTransform.anchoredPosition = position;

        var toggle = toggleGo.AddComponent<Toggle>();

        // Background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(toggleGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.sizeDelta = new Vector2(40, 40);
        bgRect.anchoredPosition = new Vector2(20, 0);
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        toggle.targetGraphic = bgImage;

        // Checkmark
        var checkmarkGo = new GameObject("Checkmark");
        checkmarkGo.transform.SetParent(bgGo.transform, false);
        var checkmarkRect = checkmarkGo.AddComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.sizeDelta = new Vector2(-10, -10);
        var checkmarkImage = checkmarkGo.AddComponent<Image>();
        checkmarkImage.color = new Color(0.3f, 0.6f, 1f, 1f);

        toggle.graphic = checkmarkImage;

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(toggleGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(50, 0);
        labelRect.offsetMax = new Vector2(0, 0);
        var textComponent = labelGo.AddComponent<TextMeshProUGUI>();
        textComponent.text = labelText;
        textComponent.fontSize = 20;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.color = Color.white;

        return toggleGo;
    }

    private static void AssignReferencesToPauseSettingsView(Game.Pause.PauseSettingsView view, GameObject settingsGroup)
    {
        var masterVolumeSlider = settingsGroup.transform.Find("MasterVolumeSlider")?.GetComponent<Slider>();
        var bgmVolumeSlider = settingsGroup.transform.Find("BgmVolumeSlider")?.GetComponent<Slider>();
        var seVolumeSlider = settingsGroup.transform.Find("SeVolumeSlider")?.GetComponent<Slider>();
        var visualEffectsToggle = settingsGroup.transform.Find("VisualEffectsToggle")?.GetComponent<Toggle>();
        var screenEffectsToggle = settingsGroup.transform.Find("ScreenEffectsToggle")?.GetComponent<Toggle>();

        var type = typeof(Game.Pause.PauseSettingsView);

        SetFieldValue(view, type, "masterVolumeSlider", masterVolumeSlider);
        SetFieldValue(view, type, "bgmVolumeSlider", bgmVolumeSlider);
        SetFieldValue(view, type, "seVolumeSlider", seVolumeSlider);
        SetFieldValue(view, type, "visualEffectsToggle", visualEffectsToggle);
        SetFieldValue(view, type, "screenEffectsToggle", screenEffectsToggle);
    }

    private static void SetFieldValue(object target, System.Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
            Debug.Log($"[PauseMenuSettingsSetup] {fieldName} を設定しました。");
        }
        else
        {
            Debug.LogWarning($"[PauseMenuSettingsSetup] フィールド {fieldName} が見つかりませんでした。");
        }
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name)
            {
                return child;
            }

            var found = FindChildRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}
}



