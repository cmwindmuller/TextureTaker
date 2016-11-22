using UnityEngine;
using UnityEditor;
using System.IO;

/**
TextureTaker (TTaker)

Author: Colin Windmuller

Version: 1.0
Last Updated: 11/22/2016

About: TTaker is an Editor script that automates importing Textures and creating a Material.

Assumptions:
    0) TTaker.cs is located in Assets/Editor folder.
    1) Textures have suffixes for what they do: {Albedo: 'a', Metal/Spec: 'm', Normal: 'n', Height: 'h', Occlusion: 'ao', Emissive: 'e'}
    2) Texture Suffixes are prefixed with a special character, '_' by default, can be changed in the options panel
    3) Textures for 1 Material exist in 1 Folder. TTaker will use this folder name for the Material name.
    4) TTaker only uses the Standard Shader.

Future Development: Better importing, that can handle multiple Materials at once, changing assumption #3.
    Better Options Menu, to support other Shaders as well as custom texture suffixes, changing assumptions #1 and #4.
**/

public class TTaker : EditorWindow
{
    private static string ShaderName = "Standard";
    private static string DirectoryPath = "Assets";
    private static string DirectoryName = "Materials";
    private static string MaterialType = ".mat";

    public static string PrefixToken = "_";
    private const char BSLASH = '\\';
    private const char FSLASH = '/';
    private const char DOT = '.';
    private static string[] fileTypes = { "PSD", "TIFF", "JPG", "TGA", "PNG", "GIF", "BMP" };
    //public static string 

    //  returns a/b
    private static string AddPath(string a, string b)
    {
        return a + FSLASH + b;
    }

    private static int LastSlash(string a)
    {
        int i = a.LastIndexOf(FSLASH);
        int j = a.LastIndexOf(BSLASH);

        return Mathf.Max( i, j );
    }

    void OnGUI()
    {

        GUILayout.Label( "Name Settings", EditorStyles.boldLabel );
        PrefixToken = EditorGUILayout.TextField( "Prefix Token", PrefixToken );

    }

    //  Creates Material Asset using textures in a given folder
    [MenuItem( "Tools/TTaker/Import" )]
    private static void TakeTextures ()
    {
        //File Browser prompt and files collection
        string path = EditorUtility.OpenFolderPanel( "Select Texture Folder", "", "" );
        string[] files = Directory.GetFiles( path );

        string rootPath = DirectoryPath;
        string exportFolderName = DirectoryName;
        string exportPath = AddPath( rootPath, exportFolderName );
        
        // Does the export folder exist? If not, make one
        if ( !AssetDatabase.IsValidFolder( exportPath ) )
            AssetDatabase.CreateFolder( rootPath, exportFolderName );

        // Parse for the material name, check for CONFLICTS, use this for the .mat
        string materialName = path.Substring( LastSlash( path ) + 1 );
        Material material = new Material(Shader.Find( ShaderName ) );

        string[] lookFor = new string[] { exportPath };
        string[] assets = AssetDatabase.FindAssets(materialName + " " + "t:material",lookFor);
        if ( assets.Length > 0 )
        {
            Debug.Log( materialName + ".mat Already Exists. Import cancelled." );
            return;
        }

        // For each file, see if it is an image and if it has the appropriate format (xx_ao.psd, eg)
        foreach ( string file in files )
        {
            // File extension is after the '.' (.jpg, eg)
            int lastDot = file.LastIndexOf( DOT );
            
            // Is this an accepted Image file type? ignores case
            if ( ArrayUtility.Contains<string>( fileTypes, file.Substring( lastDot + 1 ).ToUpper() ) )
            {
                // Find the special Prefix Token ( xx_h.jpg -> '_', eg)
                int prefixIndex = file.LastIndexOf( PrefixToken[0] );

                // Does this image file contain the prefix token? Then let's try to import it
                if ( prefixIndex > 0 )
                {
                    string textureType = file.Substring( ++prefixIndex , lastDot - prefixIndex );
                    string newFile = file.Substring( LastSlash( file ) + 1 );

                    string newPath = AddPath( exportPath, newFile );

                    try
                    {
                        File.Copy( file, newPath ); //maybe change to AssetDatabase function
                        AssetDatabase.Refresh(); //IMPORTANT
                        Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath( newPath, typeof(Texture2D) );

                        // Attaches the Texture to the Material, using file suffix
                        switch ( textureType )
                        {
                            case "d":
                            case "c":
                            case "a":
                                material.SetTexture( "_MainTex", texture );
                                break;
                            case "s":
                            case "m":
                                material.SetTexture( "_MetallicGlossMap", texture );
                                break;
                            case "b":
                            case "n":
                                material.SetTexture( "_BumpMap", texture );
                                break;
                            case "h":
                                material.SetTexture( "_ParallaxMap", texture );
                                break;
                            case "ao":
                                material.SetTexture( "_OcclusionMap", texture );
                                break;
                            case "e":
                                material.SetTexture( "_EmissionMap", texture );
                                break;
                            default:
                                Debug.Log( "Unkown suffix: " + textureType );
                                break;
                        }
                    }
                    catch
                    {
                        Debug.Log( "Could not import: " + file );
                    }
                }
            }
        }
        //Finally, save the Material as an Asset
        AssetDatabase.CreateAsset( material, exportPath + FSLASH + materialName + MaterialType );
    }

    [MenuItem( "Tools/TTaker/Options" )]
    private static void SetOptions ()
    {
        EditorWindow.GetWindow( typeof( TTaker ) );
    }

}
