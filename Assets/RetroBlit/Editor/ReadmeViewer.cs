using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// View for ReadMe asset
/// </summary>
[CustomEditor(typeof(Readme))]
[InitializeOnLoad]
public class ReadmeViewer : Editor
{
    private bool mInitialized;
    private GUIStyle mLinkStyle;
    private GUIStyle mTitleStyle;
    private GUIStyle mHeadingStyle;
    private GUIStyle mParagraphStyle;
    private GUIStyle mBodyStyle;
    private GUIStyle mIconStyle;
    private Texture2D mCoverArtTexture;

    /// <summary>
    /// Display ReadMe contents
    /// </summary>
    public override void OnInspectorGUI()
    {
        Init();

        int spacing = 8;

        // Title
        GUILayout.Space(spacing);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("RetroBlit Game Framework", mTitleStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical(mBodyStyle);

        GUILayout.Label(
            "Welcome to RetroBlit, your new framework for creating amazing retro pixel games the way " +
            "your grandma used to make them!",
            mParagraphStyle);

        GUILayout.Space(spacing);
        GUILayout.Space(spacing);
        GUILayout.Label("What now?", mHeadingStyle);
        GUILayout.Space(spacing);
        GUILayout.Label(
            "First check out some of the demo games that ship with RetroBlit: ",
            mParagraphStyle);

        if (LinkLabel(new GUIContent("Super Flag Run")))
        {
            OpenAndPlayScene("Assets/RetroBlit/Scenes/SuperFlagRun.unity");
        }

        if (LinkLabel(new GUIContent("Retro Dungeoneer")))
        {
            OpenAndPlayScene("Assets/RetroBlit/Scenes/RetroDungeoneer.unity");
        }

        if (LinkLabel(new GUIContent("Brick Bust")))
        {
            OpenAndPlayScene("Assets/RetroBlit/Scenes/BrickBust.unity");
        }

        if (LinkLabel(new GUIContent("Old Days")))
        {
            OpenAndPlayScene("Assets/RetroBlit/Scenes/OldDays.unity");
        }

        GUILayout.Space(spacing);

        GUILayout.Label(
            "The above demos have their scenes in the RetroBlit/Scenes folder, " +
            "their scripts are in RetroBlit/Scripts/Demos folder, and finally their resources are in " +
            "RetroBlit/Resources/Demos folder.",
            mParagraphStyle);

        GUILayout.Space(spacing);

        GUILayout.Label(
            "Next check out the Demo Reel to get a quick preview of the RetroBlit features:",
            mParagraphStyle);

        if (LinkLabel(new GUIContent("Demo Reel")))
        {
            OpenAndPlayScene("Assets/RetroBlit/Scenes/DemoReel.unity");
        }

        GUILayout.Space(spacing);

        GUILayout.Label(
            "Want to get a feel for RetroBlit performance? Try the stress test:",
            mParagraphStyle);

        if (LinkLabel(new GUIContent("Stress Test")))
        {
            OpenAndPlayScene("Assets/RetroBlit/Scenes/StressTest.unity");
        }

        GUILayout.Space(spacing);

        GUILayout.Label(
            "Finally, jump right in with \"My Game\", a bare-bones project that you can use as a starting point for your own amazing game!",
            mParagraphStyle);

        if (LinkLabel(new GUIContent("My Game")))
        {
            OpenAndPlayScene("Assets/RetroBlit/Scenes/MyGame.unity");
        }

        GUILayout.Space(spacing);
        GUILayout.Space(spacing);
        GUILayout.Label("Documentation", mHeadingStyle);
        GUILayout.Space(spacing);
        GUILayout.Label(
            "RetroBlit comes with extensive documentation, which you can find zipped up in the RetroBlit/Docs folder. " +
            "To avoid burdening the Unity Asset Manager with the documentation files please unzip it outside of your Unity Project!",
            mParagraphStyle);

        GUILayout.Space(spacing);
        GUILayout.Space(spacing);
        GUILayout.Label("Thank you!", mHeadingStyle);
        GUILayout.Space(spacing);
        GUILayout.Label(
            "Thank you so much for your interest in RetroBlit! Once you had a chance to play around with it please " +
            "consider further supporting RetroBlit by leaving a review on the asset store:",
            mParagraphStyle);

        if (LinkLabel(new GUIContent("Unity Asset Store: RetroBlit")))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/slug/102064");
        }

        GUILayout.Space(spacing);
        GUILayout.Space(spacing);
        GUILayout.Label("Support", mHeadingStyle);
        GUILayout.Space(spacing);
        GUILayout.Label(
            "Finally, if you're having issues, or would like to provide feedback then please email support at: ",
            mParagraphStyle);

        if (LinkLabel(new GUIContent("pixeltrollgames@gmail.com")))
        {
            Application.OpenURL("mailto:pixeltrollgames@gmail.com");
        }

        GUILayout.EndVertical();
    }

    /// <summary>
    /// Display ReadMe header image
    /// </summary>
    protected override void OnHeaderGUI()
    {
        Init();

        if (mCoverArtTexture == null)
        {
            return;
        }

        var iconWidth = EditorGUIUtility.currentViewWidth;
        var iconHeight = iconWidth / 2.0f;

        GUI.backgroundColor = Color.black;
        GUILayout.Label(mCoverArtTexture, mIconStyle, GUILayout.Width(iconWidth), GUILayout.Height(iconHeight));
    }

    private static void SelectReadMe()
    {
        var ids = AssetDatabase.FindAssets("Readme t:Readme");
        if (ids.Length == 1)
        {
            var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

            Selection.objects = new UnityEngine.Object[] { readmeObject };
        }
    }

    private static void PackageImportEnded(string packageName)
    {
        SelectReadMe();
    }

    private void Init()
    {
        if (mInitialized)
        {
            return;
        }

        mParagraphStyle = new GUIStyle(EditorStyles.label);
        mParagraphStyle.wordWrap = true;
        mParagraphStyle.fontSize = 14;

        mTitleStyle = new GUIStyle(mParagraphStyle);
        mTitleStyle.fontSize = 26;
        mTitleStyle.alignment = TextAnchor.UpperCenter;

        mHeadingStyle = new GUIStyle(mParagraphStyle);
        mHeadingStyle.fontSize = 18;

        mLinkStyle = new GUIStyle(mParagraphStyle);
        mLinkStyle.wordWrap = false;

        // Match selection color which works nicely for both light and dark skins
        mLinkStyle.normal.textColor = new Color(0.0f, 0x78 / 255.0f, 0xda / 255.0f, 1.0f);
        mLinkStyle.stretchWidth = false;

        mBodyStyle = new GUIStyle();
        mBodyStyle.padding = new RectOffset(0, 16, 0, 0);

        mIconStyle = new GUIStyle(GUI.skin.label);
        mIconStyle.alignment = TextAnchor.MiddleCenter;
        mIconStyle.border = new RectOffset(0, 0, 0, 0);
        mIconStyle.contentOffset = new Vector2(0, 0);
        mIconStyle.padding = new RectOffset(0, 0, 0, 0);
        mIconStyle.margin = new RectOffset(0, 0, 0, 0);

        mCoverArtTexture = (Texture2D)EditorGUIUtility.Load("Assets/RetroBlit/Internal/Textures/RetroBlit-ignore/ReadmeArt.png");

        mInitialized = true;
    }

    private void OpenAndPlayScene(string sceneName)
    {
        if (EditorApplication.isPlaying)
        {
            // If already playing a scene then switch scenes
            EditorSceneManager.LoadScene(sceneName);
        }
        else
        {
            // If not already playing then open first then play
            EditorSceneManager.OpenScene(sceneName);

            // Play
#if UNITY_2019_1_OR_NEWER
            EditorApplication.EnterPlaymode();
#else
            EditorApplication.ExecuteMenuItem("Edit/Play");
#endif
        }
    }

    private bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
    {
        var position = GUILayoutUtility.GetRect(label, mLinkStyle, options);

        Handles.BeginGUI();
        Handles.color = mLinkStyle.normal.textColor;
        Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
        Handles.color = Color.white;
        Handles.EndGUI();

        EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

        return GUI.Button(position, label, mLinkStyle);
    }

    /// <summary>
    /// Class for registering AssetDatabase callbacks
    /// </summary>
    [InitializeOnLoad]
    private class RetroBlitSpritePackProcessorStartup
    {
        /// <summary>
        /// Constructor
        /// </summary>
        static RetroBlitSpritePackProcessorStartup()
        {
            AssetDatabase.importPackageCompleted += PackageImportEnded;

            PackageImportEnded(string.Empty);
        }
    }
}
