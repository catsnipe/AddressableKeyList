using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;

[InitializeOnLoad]
public static class AddressableKeyListOnBuildCompleted
{
    static AddressableKeyListOnBuildCompleted()
    {
        BuildScript.buildCompleted += OnBuildCompleted;
    }

    /// <summary>
    /// AAS Build 時に呼ばれる
    /// </summary>
    private static void OnBuildCompleted(AddressableAssetBuildResult result)
    {
        if (result.OutputPath.IndexOf("com.unity.addressables") < 0)
        {
            return;
        }

        Debug.Log( $"Duration: {result.Duration}sec." );
        Debug.Log( $"Error: {result.Error}" );

        var outputPath = EditorPrefs.GetString(AddressablesKeyListCreator.PREFS_OUTPUT_PATH, AddressablesKeyListCreator.INIT_OUTPUT_PATH);

        AddressablesKeyListCreator.Create(AddressablesKeyListCreator.AASGROUP_DIRECTORY, outputPath);
    }
}