using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class IOSPostProcessBuild
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            CreatePodfile(buildPath);
        }
    }

    private static void CreatePodfile(string buildPath)
    {
        string podfilePath = Path.Combine(buildPath, "Podfile");
        
        string podfileContent = @"
platform :ios, '11.0'

target 'Unity-iPhone' do
  pod 'FirebaseCore'
  pod 'FirebaseAuth'
  pod 'FirebaseFirestore'
  # Aggiungi altri pod Firebase che ti servono
end

post_install do |installer|
  installer.pods_project.targets.each do |target|
    target.build_configurations.each do |config|
      config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '11.0'
    end
  end
end
";
        
        File.WriteAllText(podfilePath, podfileContent);
        Debug.Log("Podfile creato in: " + podfilePath);
    }
}