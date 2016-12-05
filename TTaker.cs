using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

/// <summary>
/// Imports Textures to the Assets/Materials folder in bulk and creates Standard shader Materials.
/// Creates or updates Material assets with the imported textures.
/// Relies HEAVILY on file naming conventions of the images.
/// Expects file names as "materialName_x.png", where the 'x' suffix identifies the material slot as follows:
/// {Albedo:('a','c','d'), Metal/Spec: ('m','s'), Normal: 'n', Height: ('b','h'), Occlusion: 'ao', Emissive: 'e'}
/// </summary>
/// <author email="cwin627@gmail.com">Colin Windmuller</author>
public class TTaker : EditorWindow
{
    private const string SHADER_TYPE = "Standard";
    private const string ASSETS_DIRECTORY = "Assets";
    private const string MATERIAL_EXT = ".mat";
    private static string ExportFolder = "Materials";

    public static string SuffixToken = "_";
    public static bool ShouldOverwrite = false;
    private const char BSLASH = '\\';
    private const char FSLASH = '/';
    private const char DOT = '.';
    private static string[] fileTypes = { "PSD", "TIFF", "JPG", "TGA", "PNG", "GIF", "BMP" };
    
    /// <summary>
    /// Returns a + '/' + b
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static string AddPath(string a, string b)
    {
        return a + FSLASH + b;
    }
    /// <summary>
    /// Finds the last occuring slash, either '\' or '/'
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    private static int LastSlash(string a)
    {
        int i = a.LastIndexOf(FSLASH);
        int j = a.LastIndexOf(BSLASH);

        return Mathf.Max( i, j );
    }
    ///<summary>
    ///Matches a suffix letter with a Standard Shader parameter. ('b' -> "_BumpMap", eg)
    ///</summary>
    private static string SuffixToParameter(string suffix, string fileName)
    {
        suffix = suffix.ToLower();
        string propertyName = "";
        switch ( suffix )
        {
            case "d":
            case "c":
            case "a":
                propertyName = "_MainTex";
                break;
            case "s":
            case "m":
                propertyName = "_MetallicGlossMap";
                break;
            case "n":
                propertyName = "_BumpMap";
                break;
            case "h":
            case "b":
                propertyName = "_ParallaxMap";
                break;
            case "ao":
                propertyName = "_OcclusionMap";
                break;
            case "e":
                propertyName = "_EmissionMap";
                break;
            default:
                Debug.Log( "(TTaker) Unkown suffix: '" + suffix + "' in \"" + fileName + "\"" );
                break;
        }
        return propertyName;
    }
    /// <summary>
    /// Returns either a new empty Material, or the one that already exists.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Material MakeOrGetMaterial(string directory, string name, out bool isNewMaterial)
    {
        isNewMaterial = false;
        string[] searchFolders = new string[] { directory };
        string[] assets = AssetDatabase.FindAssets(name + " " + "t:material",searchFolders);
        if ( assets.Length > 0 )
        {
            return AssetDatabase.LoadAssetAtPath( AddPath( directory, name ) + MATERIAL_EXT, typeof( Material ) ) as Material;
        }
        isNewMaterial = true;
        return new Material( Shader.Find( SHADER_TYPE ) );
    }
    /// <summary>
    /// Optionally creates thew new Material Asset, does nothing if the asset already exists
    /// </summary>
    /// <param name="material"></param>
    /// <param name="assetPath"></param>
    /// <param name="isNewMaterial"></param>
    public static bool CreateNewMaterial(Material material, string assetPath, bool isNewMaterial)
    {
        if ( !isNewMaterial )
            return false;
        AssetDatabase.CreateAsset( material, assetPath + MATERIAL_EXT );
        return true;
    }
    public struct IOFilePaths
    {
        public string localDirectory;
        public string readDirectory;
        public string writeDirectory;
    }
    public struct ImgFile
    {
        public int lastDot;
        public int lastSlash;
        public int lastToken;
        public string name;
        public string fileName;
        public string suffix;
    }

    ///<summary>
    ///Main function that streamlines importing textures and creating related Materials in Assets/folderX.
    ///</summary>
    [MenuItem( "Tools/TextureTaker" )]
    private static void TakeTextures ()
    {
        string[] files = Directory.GetFiles(
                            EditorUtility.OpenFolderPanel( "Select Texture Folder", "", "" )
                         ).OrderBy( x => x).ToArray();

        if ( files.Length == 0 )
            return;

        IOFilePaths paths;
        paths.localDirectory = ASSETS_DIRECTORY;
        paths.writeDirectory = AddPath( paths.localDirectory, ExportFolder );

        if ( !AssetDatabase.IsValidFolder( paths.writeDirectory ) )
            AssetDatabase.CreateFolder( paths.localDirectory, ExportFolder );

        string currentMaterialName = "";
        Texture texture = null;
        Material material = null;
        bool isNewMaterial = false;
        ImgFile imgFile = new ImgFile();

        int totalTexturesImported = 0;
        int totalMaterialsCreated = 0;

        foreach ( string file in files )
        {
            imgFile.lastDot = file.LastIndexOf( DOT );

            if ( !ArrayUtility.Contains<string>( fileTypes, file.Substring( imgFile.lastDot + 1 ).ToUpper() ) )
                return;

            imgFile.lastSlash = LastSlash( file );
            imgFile.fileName = file.Substring( imgFile.lastSlash + 1, file.Length - imgFile.lastSlash - 1 );
            imgFile.lastToken = file.LastIndexOf( SuffixToken );
            if ( imgFile.lastToken < 0 )
                return;

            imgFile.name = file.Substring( imgFile.lastSlash + 1, imgFile.lastToken - imgFile.lastSlash - 1 );
            imgFile.suffix = file.Substring( imgFile.lastToken + 1, imgFile.lastDot - imgFile.lastToken - 1 );

            if ( imgFile.name != currentMaterialName )
            {
                if ( material != null )
                    if ( CreateNewMaterial( material, AddPath( paths.writeDirectory, currentMaterialName ), isNewMaterial ) )
                        totalMaterialsCreated++;
                material = MakeOrGetMaterial( paths.writeDirectory, imgFile.name, out isNewMaterial );
                currentMaterialName = imgFile.name;
            }

            string fileNewPath = AddPath( paths.writeDirectory, imgFile.fileName );
            try
            {
                File.Copy( file, fileNewPath, true );
                totalTexturesImported++;
            }
            catch { }
            AssetDatabase.Refresh(); //must refresh before this texture can be assigned
            texture = (Texture2D)AssetDatabase.LoadAssetAtPath( fileNewPath, typeof( Texture2D ) );

            material.SetTexture( SuffixToParameter( imgFile.suffix.ToString(), imgFile.fileName ), texture );
        }
        if ( CreateNewMaterial( material, AddPath( paths.writeDirectory, currentMaterialName ), isNewMaterial ) )
            totalMaterialsCreated++;
        Debug.Log( "(TTaker) " + totalTexturesImported + " Textures taken,  " + totalMaterialsCreated + " Materials created." );
    }
}