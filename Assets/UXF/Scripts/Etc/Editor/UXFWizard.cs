using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UXF.EditorUtils
{

    [InitializeOnLoad]
    public class UXFWizard : EditorWindow
    {
		public Texture2D uxfIcon;
        public static bool forceShow = false;
#if UNITY_2018_3_OR_NEWER
        ApiCompatibilityLevel targetApiLevel = ApiCompatibilityLevel.NET_4_6;
#else
        ApiCompatibilityLevel targetApiLevel = ApiCompatibilityLevel.NET_2_0;
#endif
        static string settingsKey { get { return PlayerSettings.productName + ":uxf_seen_wizard"; } }

        static string version;

        static UXFWizard()
        {
#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += OnProjectChanged;
#else
            EditorApplication.projectWindowChanged += OnProjectChanged;
#endif
        }


        [MenuItem("UXF/Show setup wizard")]
        static void Init()
        {
            var window = (UXFWizard) EditorWindow.GetWindow(typeof(UXFWizard), false, "UXF Wizard");
            window.minSize = new Vector2(300, 501);
			window.titleContent = new GUIContent("UXF Wizard");
            window.Show();

            
            if (File.Exists("Assets/UXF/VERSION.txt"))
            {
                version = File.ReadAllText("Assets/UXF/VERSION.txt");
            }
            else
            {
                version = "unknown";
            }
        }

        static void OnProjectChanged()
        {
            bool seen;

            if (EditorPrefs.HasKey(settingsKey))
            {
                seen = EditorPrefs.GetBool(settingsKey);
            }
            else
            {
                seen = false;
            }

            if (forceShow | !seen)
            {
                Init();
                EditorPrefs.SetBool(settingsKey, true);
            }
        }

        public void OnGUI()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.wordWrap = true;

            var rect = GUILayoutUtility.GetRect(Screen.width, 128, GUI.skin.box);
            if (uxfIcon)
                GUI.DrawTexture(rect, uxfIcon, ScaleMode.ScaleToFit);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("UXF: Unity Experiment Framework", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version " + version, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            GUILayout.Label("Help and info", EditorStyles.boldLabel);

            GUILayout.Label("The GitHub page contains the most up-to-date information & release.", labelStyle);
			if (GUILayout.Button("Visit GitHub"))
				Application.OpenURL("https://github.com/immersivecognition/unity-experiment-framework/");

            EditorGUILayout.Space();
            GUILayout.Label("The GitHub Wiki contains documentation and in-depth explanations of concepts.", labelStyle);
            if (GUILayout.Button("Visit Wiki"))
                Application.OpenURL("https://github.com/immersivecognition/unity-experiment-framework/wiki");


            EditorGUILayout.Separator();

            GUILayout.Label("Examples", EditorStyles.boldLabel);

            GUILayout.Label("Check your Assets > UXF > Examples folder", labelStyle);

            EditorGUILayout.Separator();

            GUILayout.Label("Cite UXF", EditorStyles.boldLabel);

            if (GUILayout.Button("DOI Link"))
                Application.OpenURL("https://doi.org/10.3758/s13428-019-01242-0");

            EditorGUILayout.Separator();

            GUILayout.Label("Compatibility", EditorStyles.boldLabel);

            bool compatible = PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone) == targetApiLevel;

            if (compatible)
            {
                EditorGUILayout.HelpBox("API Compatibility Level is set correctly", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("API Compatibility Level should be set to .NET 2.0 (Older versions of Unity) or .NET 4.x (Unity 2018.3+), expect errors on building", MessageType.Warning);
                if (GUILayout.Button("Fix"))
                {
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, targetApiLevel);
                }
            }


            EditorGUILayout.Separator();

            GUILayout.Label("WebGL", EditorStyles.boldLabel);

            if (PlayerSettings.WebGL.template == "PROJECT:UXF WebGL")
            {
                EditorGUILayout.HelpBox("UXF WebGL template is set correctly.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("UXF WebGL template is not selected as the WebGL Template in Player Settings.", MessageType.Warning);
                if (GUILayout.Button("Fix"))
                {
                    PlayerSettings.WebGL.template = "PROJECT:UXF WebGL";
                }
            }



            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox("To show this window again go to UXF -> Show setup wizard in the menubar.", MessageType.None);

        }

    }

}