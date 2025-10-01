// A script to upgrade texture quality on PC without increasing VRAM usage, by Jonnyawsom3.

using UnityEngine;
using UnityEditor;

public class TextureUpgrader : EditorWindow
{
    private DefaultAsset folder;

    [MenuItem("Tools/Jonny's Texture Upgrader")]
    public static void ShowWindow()
    {
        GetWindow<TextureUpgrader>("Jonny's Texture Upgrader");
    }

    private void OnGUI()
    {
        GUILayout.Label("Drag and drop Folder to Upgrade Textures", EditorStyles.boldLabel);
        folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", folder, typeof(DefaultAsset), false);

        if (GUILayout.Button("Upgrade Textures"))
        {
            if (folder == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder first.", "OK");
                return;
            }

            string path = AssetDatabase.GetAssetPath(folder);
            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorUtility.DisplayDialog("Error", "Selected asset is not a valid folder.", "OK");
                return;
            }

            ProcessTexturesInFolder(path);
        }
    }

    private void ProcessTexturesInFolder(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folderPath });
        int texturesUpgraded = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
                continue;

            bool changed = false;
            bool OverridePlatform = false;

            // Disable Crunch
            if (importer.crunchedCompression)
            {
                importer.crunchedCompression = false;
                changed = true;
            }

            // Set mipmap filter to Kaiser (Enable override in VRChat SDK settings)
            if (importer.mipmapFilter != TextureImporterMipFilter.KaiserFilter)
            {
                importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                changed = true;
            }

            // Check default texture settings
            TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings("Standalone");

            // Preserve max size
            platformSettings.maxTextureSize = importer.maxTextureSize;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            TextureImporterFormat defaultFormat = importer.GetAutomaticFormat("Standalone");

                // Tiny textures shouldn't be compressed
                if (importer.maxTextureSize <= 128 || (width <= 128 && height <= 128)) {
                  if (importer.textureCompression != TextureImporterCompression.Uncompressed) {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                    }
                  if (platformSettings.overridden) {
                    platformSettings.overridden = false;
                    importer.SetPlatformTextureSettings(platformSettings);
                    changed = true;
                    }

                    // Upgrade Normal Maps to BC5
                  } else if (importer.textureType == TextureImporterType.NormalMap) {
                if (platformSettings.format != TextureImporterFormat.BC5 || !platformSettings.overridden) {
                    platformSettings.format = TextureImporterFormat.BC5;
                    OverridePlatform = true;
                    changed = true;
                }
                // Set small BC1 textures to BC7
              } else if ((importer.maxTextureSize <= 512 || (width <= 512 && height <= 512)) &&
                  (defaultFormat == TextureImporterFormat.DXT1 ||
                  defaultFormat == TextureImporterFormat.DXT1Crunched) &&
                  (platformSettings.format != TextureImporterFormat.BC7 ||
                  platformSettings.compressionQuality != (int)TextureCompressionQuality.Best ||
                 !platformSettings.overridden))
                {
                    platformSettings.format = TextureImporterFormat.BC7;
                    platformSettings.compressionQuality = (int)TextureCompressionQuality.Best;
                    OverridePlatform = true;
                    changed = true;

                  // Upgrade transparent textures from BC3 to BC7
                } else if ((defaultFormat == TextureImporterFormat.DXT5 ||
                    defaultFormat == TextureImporterFormat.DXT5Crunched) &&
                    (platformSettings.format != TextureImporterFormat.BC7 ||
                    platformSettings.compressionQuality != (int)TextureCompressionQuality.Best ||
                   !platformSettings.overridden))
                {
                    platformSettings.format = TextureImporterFormat.BC7;
                    platformSettings.compressionQuality = (int)TextureCompressionQuality.Best;
                    OverridePlatform = true;
                    changed = true;
                }

            if (OverridePlatform)
            {
                platformSettings.overridden = true;
                importer.SetPlatformTextureSettings(platformSettings);
            }

            if (changed)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                texturesUpgraded++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"{texturesUpgraded} Textures upgraded");
        EditorUtility.DisplayDialog("Compression Kobold Done", $"{texturesUpgraded} Textures have been upgraded.", "OK");
    }
}
