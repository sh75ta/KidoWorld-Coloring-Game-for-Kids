using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Kido.Coloring;

namespace Kido.Editor
{
    public sealed class KidoTextureImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/KidoColoring/Resources/Kido/",StringComparison.Ordinal))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
        }
    }
    public static class KidoSetup
    {
        const string Scene="Assets/KidoColoring/Scenes/KidoGame.unity";
        [MenuItem("Kido Coloring/1. Verify Editor and modules")]
        public static void Verify()
        {
            Debug.Log("Actual Unity Editor: "+Application.unityVersion+"\nExecutable: "+EditorApplication.applicationPath+
                "\nAndroid build support: "+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android)+
                "\niOS build support: "+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS));
            if(!Application.unityVersion.StartsWith("6000.3.",StringComparison.Ordinal))Debug.LogWarning("This source targets Unity 6.3. Verify compatibility before proceeding.");
        }
        [MenuItem("Kido Coloring/2. Configure and open game")]
        public static void Configure()
        {
            Verify();
            PlayerSettings.productName="Kido Coloring";PlayerSettings.companyName="KidoColoringPrototype";
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.example.kidocoloring");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.example.kidocoloring");
            // This project deliberately uses the built-in input backend, with no Input System package.
            var playerAssets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if(playerAssets.Length>0)
            {
                var so=new SerializedObject(playerAssets[0]);var input=so.FindProperty("activeInputHandler");
                if(input!=null){input.intValue=0;so.ApplyModifiedPropertiesWithoutUndo();}
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            EditorSettings.serializationMode=SerializationMode.ForceText;
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/KidoColoring/Resources/Kido"}))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid),ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Scene);
            Debug.Log("Kido source configured. Press Play. Mobile builds and device behavior must still be tested.");
        }
        [MenuItem("Kido Coloring/3. Build Android development APK")]
        public static void AndroidBuild()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new BuildFailedException("Install Android Build Support with SDK/NDK and OpenJDK through Unity Hub.");
            string target=EditorUtility.SaveFilePanel("Android development build","","KidoColoring","apk");if(string.IsNullOrEmpty(target))return;
            Build(target,BuildTarget.Android);
        }
        [MenuItem("Kido Coloring/4. Export iOS development project")]
        public static void IosBuild()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS))throw new BuildFailedException("Install iOS Build Support. Xcode/macOS and signing are also required to build/install the exported project.");
            string target=EditorUtility.SaveFolderPanel("iOS Xcode export","","");if(string.IsNullOrEmpty(target))return;
            Build(target,BuildTarget.iOS);
        }
        static void Build(string path,BuildTarget target)
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=path,target=target,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Kido build failed. Inspect the Editor console.");
            Debug.Log("Development output: "+path+". This is not a production release.");
        }
    }
    public sealed class KidoReleaseGuard:IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report)
        {
            if((report.summary.options&BuildOptions.Development)==0)
                throw new BuildFailedException("Kido is a first-play prototype. Live purchase/restore, eligible ad integration, final access/pricing, child-privacy review, and device validation are unfinished. Production builds are intentionally blocked. Complete those integrations and replace this guard with explicit release validation.");
        }
    }
}
