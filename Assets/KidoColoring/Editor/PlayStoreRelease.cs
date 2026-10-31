using System;
using System.Collections.Generic;
using System.IO;
using Kido.Coloring;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Kido.Editor
{
    public static class PlayStoreRelease
    {
        public const string PackageId="com.kidoworld.coloring";
        public const string Scene="Assets/KidoColoring/Scenes/KidoGame.unity";

        [MenuItem("Kido Coloring/5. Apply non-secret Play Store settings")]
        public static void ApplyNonSecretSettings()
        {
            PlayerSettings.companyName="KidoWorld";
            PlayerSettings.productName="KidoWorld Coloring for Kids";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,PackageId);
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("Applied non-secret Play Store settings. Signing keys, privacy URL and billing credentials were not changed.");
        }

        [MenuItem("Kido Coloring/6. Validate Play Store release")]
        public static void ValidateMenu()
        {
            var failures=PlayStoreReleaseValidator.Failures();
            if(failures.Count==0)Debug.Log("KidoWorld Play Store release validation passed.");
            else Debug.LogError("Play Store release is blocked:\n• "+string.Join("\n• ",failures));
        }

        [MenuItem("Kido Coloring/7. Build signed Android App Bundle")]
        public static void BuildBundle()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
                throw new BuildFailedException("Android Build Support with SDK/NDK and OpenJDK is required.");
            ApplyNonSecretSettings();
            var failures=PlayStoreReleaseValidator.Failures();
            if(failures.Count>0)throw new BuildFailedException("Play Store release is blocked:\n• "+string.Join("\n• ",failures));
            string target=EditorUtility.SaveFilePanel("Signed Play Store bundle","","KidoWorld-Coloring-v"+PlayerSettings.bundleVersion,"aab");
            if(string.IsNullOrEmpty(target))return;
            EditorUserBuildSettings.buildAppBundle=true;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=target,target=BuildTarget.Android,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("KidoWorld App Bundle failed. Inspect the Editor console.");
            Debug.Log("Signed Play Store App Bundle: "+target);
        }
    }

    public static class PlayStoreReleaseValidator
    {
        const string SettingsPath="Assets/KidoColoring/Resources/Kido/settings.json";
        public static List<string> Failures()
        {
            var failures=new List<string>();
            GameSettings settings=null;
            try{settings=JsonUtility.FromJson<GameSettings>(File.ReadAllText(SettingsPath));}
            catch(Exception e){failures.Add("Game settings cannot be read: "+e.Message);}
            if(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)!=PlayStoreRelease.PackageId)
                failures.Add("Android package ID must be "+PlayStoreRelease.PackageId+".");
            if(PlayerSettings.companyName!="KidoWorld")failures.Add("Company name must be KidoWorld.");
            if((int)PlayerSettings.Android.targetSdkVersion<36)failures.Add("Android target API must be 36 or newer.");
            if((PlayerSettings.Android.targetArchitectures&AndroidArchitecture.ARM64)==0)failures.Add("ARM64 architecture must be enabled.");
            if(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)!=ScriptingImplementation.IL2CPP)failures.Add("Android scripting backend must be IL2CPP.");
            if(!PlayerSettings.Android.useCustomKeystore||string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName)||string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
                failures.Add("Configure a private upload keystore and key alias locally; never commit the key or passwords.");
            if(settings!=null)
            {
                if(settings.previewAllCanvases)failures.Add("Preview-all mode must be disabled.");
                if(settings.freeCanvasIds==null||settings.freeCanvasIds.Length!=9)failures.Add("Exactly nine free canvases are required for the approved launch model.");
                if(settings.adsEnabled)failures.Add("Version one must remain advertising-free.");
                if(string.IsNullOrWhiteSpace(settings.googleProductId))failures.Add("Google Play one-time product ID is missing.");
                if(!string.Equals(settings.billingProvider,"unity_iap",StringComparison.OrdinalIgnoreCase))failures.Add("A verified Google Play Billing provider is not connected. Install and validate Unity IAP before release.");
                if(!Uri.TryCreate(settings.privacyPolicyUrl,UriKind.Absolute,out var privacy)||privacy.Scheme!=Uri.UriSchemeHttps)failures.Add("Publish the child privacy policy at an HTTPS URL and add it to settings.json.");
            }
            if(PlayerSettings.Android.bundleVersionCode<1)failures.Add("Android version code must be at least 1.");
            if(string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))failures.Add("Application version name is missing.");
            return failures;
        }
    }

    public sealed class KidoPlayStoreReleaseGuard:IPreprocessBuildWithReport
    {
        public int callbackOrder=>10;
        public void OnPreprocessBuild(BuildReport report)
        {
            if((report.summary.options&BuildOptions.Development)!=0||report.summary.platform!=BuildTarget.Android)return;
            var failures=PlayStoreReleaseValidator.Failures();
            if(failures.Count>0)throw new BuildFailedException("KidoWorld production release blocked:\n• "+string.Join("\n• ",failures));
        }
    }
}
