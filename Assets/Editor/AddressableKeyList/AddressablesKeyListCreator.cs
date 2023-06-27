using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine.AddressableAssets;

public class AddressablesKeyListCreator : EditorWindow
{
    /// <summary>
    /// Addressable Groups が保管されているディレクトリ
    /// </summary>
    public static string AASGROUP_DIRECTORY  =  "Assets/AddressableAssetsData/AssetGroups";
    /// <summary>
    /// 作成されるファイル名（ファイルパス）と初期パス
    /// </summary>
    public static string PREFS_OUTPUT_PATH   = $"{nameof(AddressablesKeyListCreator)}_OUTPUT_PATH";
    public static string INIT_OUTPUT_PATH    = $"Assets/Scripts/Const/AddressablesKeyList.cs";
    /// <summary>
    /// 作成されるファイル名（ファイルパス）
    /// </summary>
    static string TEMPLATE_FILENAME        = $"AddressableKeyListTemplate.txt";

    const string  TOOLMENU_NAME            =  "Tools/Addressable/Create AddressablesKeyList";
    const string  REGION_KEY               =  "//$$REGION KEY$$";
    const string  REGION_KEYDIC            =  "//$$REGION KEYDIC$$";

    static string outputPath;
    static string templatePath;

    static int    errorCount;
    
    /// <summary>
    /// ウィンドウを開きます
    /// </summary>
    [MenuItem(TOOLMENU_NAME)]
    static void MenuExec()
    {
        var window = GetWindow<AddressablesKeyListCreator>(true, $"{nameof(AddressablesKeyListCreator)}");
        window.init();
    }

    /// <summary>
    /// ウィンドウオープンの可否を取得します
    /// </summary>
    [MenuItem(TOOLMENU_NAME, true)]
    static bool CanCreate()
    {
        bool enable = !EditorApplication.isPlaying && !Application.isPlaying && !EditorApplication.isCompiling;
        if (enable == false)
        {
            Debug.Log($"{nameof(AddressablesKeyListCreator)}: cannot create. wait seconds.");
        }

        if (Directory.Exists(AASGROUP_DIRECTORY) == false)
        {
            Debug.LogError($"{nameof(AddressablesKeyListCreator)}: cannot find AASGroup.");
        }

        return enable;
    }

    /// <summary>
    /// init
    /// </summary>
    void init()
    {
        loadTemplate();
        loadPrefs();
    }

    /// <summary>
    /// GUI を表示する時に呼び出されます
    /// </summary>
    void OnGUI()
    {
        if (templatePath == null)
        {
            Close();
            return;
        }

        GUILayout.Space(20);
        outputPath        = EditorGUILayout.TextField("output path", outputPath);
        GUILayout.Space(20);

        if (GUILayout.Button("Create"))
        {
            savePrefs();
            Create(AASGROUP_DIRECTORY, outputPath);
            Close();
        }
    }
    
    /// <summary>
    /// create
    /// </summary>
    public static void Create(string aasGroupDirectory, string outputPath)
    {
        logStart();

        var addressDic = new SortedDictionary<string, string>();
        var labelDic   = new SortedDictionary<string, string>();
        var dic        = new Dictionary<string, string>();

        // 対象のディレクトリ以下のアセットを全て取得し、アドレスとラベルを記録
        foreach (var group in loadAll<AddressableAssetGroup>(aasGroupDirectory))
        {
            foreach (var entry in group.entries)
            {
                if (addressDic.ContainsKey(entry.address) == true)
                {
                    logError($"[{nameof(AddressablesKeyListCreator)}]: duplicate same address/label '{entry.address}'");
                }
                addressDic[entry.address] = entry.address;
                
                foreach (var label in entry.labels)
                {
                    if (labelDic.ContainsKey(label) == false)
                    {
                        labelDic[label] = label;
                    }
                }
            }
        }

        foreach (var pair in addressDic)
        {
            dic[pair.Key] = pair.Key;
        }
        foreach (var pair in labelDic)
        {
            if (dic.ContainsKey(pair.Key) == true)
            {
                logError($"[{nameof(AddressablesKeyListCreator)}]: duplicate same address/label '{pair.Key}'");
                continue;
            }
            dic[pair.Key] = pair.Key;
        }

        // ファイル書き出し
        outputFile(dic, outputPath);

        Debug.Log($"Saved AAS KeyList: {outputPath}");
    }

    /// <summary>
    /// load T asset
    /// </summary>
    static List<T> loadAll<T>(string directoryPath) where T : Object
    {
        List<T> assetList = new List<T>();

        string[] filePathArray = Directory.GetFiles (directoryPath, "*", SearchOption.AllDirectories);

        // 取得したファイルの中からアセットだけリストに追加
        foreach (string filePath in filePathArray)
        {
            
            T asset = AssetDatabase.LoadAssetAtPath<T>(filePath);
            if(asset != null)
            {
                assetList.Add(asset);
            }
        }

        return assetList;
    }

    /// <summary>
    /// キーリストファイルを作成
    /// </summary>
    static void outputFile(Dictionary<string, string> dic, string filename)
    {
        Dictionary<string, string> newDic  = new Dictionary<string, string>();
        Encoding                   sjisEnc = Encoding.GetEncoding("Shift_JIS");

        if (templatePath == null)
        {
            loadTemplate();
        }

        int length = 0;
        foreach (var pair in dic)
        {
            string newkey = changeInvalidChars(pair.Key);
            if (string.IsNullOrEmpty(newkey) == true)
            {
                logError($"[{nameof(AddressablesKeyListCreator)}]: illegal ascii name '{pair.Key}'");
                continue;
            }
            length = Mathf.Max(length, sjisEnc.GetByteCount(newkey));

            newDic.Add(newkey, pair.Value);
        }

        StringBuilder sb  = new StringBuilder();
        StringBuilder sb2 = new StringBuilder();

        int cnt = 0;
        try
        {
            foreach (var pair in newDic)
            {
                if (cancelableProgressBar(cnt++, newDic.Count, "") == true)
                {
                    break;
                }

                string key   = pair.Key + "".PadRight(length - sjisEnc.GetByteCount(pair.Key));
                string line  = $"    public const string {key} = \"{pair.Value}\";";
                sb.AppendLine(line);

                string line2 = $"        keyList.Add(\"{pair.Value}\");";
                sb2.AppendLine(line2);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string text = File.ReadAllText(templatePath, Encoding.UTF8);
        text = text.Replace(REGION_KEY, sb.ToString());
        text = text.Replace(REGION_KEYDIC, sb2.ToString());

        writeAllTextWithDirectory(filename, text, Encoding.UTF8);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        if (errorCount > 0)
        {
            EditorUtility.DisplayDialog(nameof(AddressablesKeyListCreator), $"completed. see console log.\r\n\r\n[ERROR] = {errorCount}", "ok");
        }
        else
        {
//            EditorUtility.DisplayDialog(nameof(AddressablesKeyListCreator), "completed.", "ok");
        }
    }

    /// <summary>
    /// テキストを書き出す。ディレクトリがなければ作成する
    /// </summary>
    static void writeAllTextWithDirectory(string path, string contents, Encoding encoding)
    {
        completeDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, contents, encoding);
    }

    /// <summary>
    /// コードテンプレート
    /// </summary>
    static void loadTemplate()
    {
        string   path  = Path.Combine(Application.dataPath, "Editor");
        string[] files = Directory.GetFiles(path, TEMPLATE_FILENAME, SearchOption.AllDirectories);

        if (files.Length != 1)
        {
            logError($"cannot find [{TEMPLATE_FILENAME}] in editor.");
        }

        templatePath = files[0];

    }
    /// <summary>
    /// 指定ディレクトリが存在しない場合、上から辿って作成する
    /// </summary>
    /// <param name="dir">指定ディレクトリ</param>
    /// <returns>true..作成した</returns>
    static bool completeDirectory(string dir)
    {
        if (string.IsNullOrEmpty(dir) == true)
        {
            return false;
        }

        if (Directory.Exists(dir) == false)
        {
            completeDirectory(Path.GetDirectoryName(dir));
            Directory.CreateDirectory(dir);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// キー名より、const 名に使えない文字を変換する
    /// </summary>
    static string changeInvalidChars(string key)
    {
        // 使用文字を限定
        key = Regex.Replace(key, "[ !\"#$%&\'()\\-=\\^~\\|\\[\\]{}@`:*;+/?.<>,]", "_");

        if (Regex.IsMatch(key, "^[0-9]{1}") == true)
        {
            // 数字始まりであれば先頭に _ を付加
            key = "_" + key;
        }

        return key;
    }
    
    /// <summary>
    /// prefs にディレクトリを保存
    /// </summary>
    static void savePrefs()
    {
        EditorPrefs.SetString(PREFS_OUTPUT_PATH, outputPath);
    }

    /// <summary>
    /// prefs からディレクトリを復帰
    /// </summary>
    static void loadPrefs()
    {
        outputPath = EditorPrefs.GetString(PREFS_OUTPUT_PATH, INIT_OUTPUT_PATH);
    }

    /// <summary>
    /// ログカウント開始
    /// </summary>
    static void logStart()
    {
        errorCount = 0;
    }
    
    /// <summary>
    /// エラーログ
    /// </summary>
    static void logError(string message)
    {
        Debug.LogError(message);
        errorCount++;
    }

    /// <summary>
    /// キャンセルつき進捗バー (index+1)/max %
    /// </summary>
    static bool cancelableProgressBar(int index, int max, string msg)
    {
        float	perc = (float)(index+1) / (float)max;
        
        bool result =
            EditorUtility.DisplayCancelableProgressBar(
                nameof(AddressablesKeyListCreator),
                perc.ToString("00.0%") + "　" + msg,
                perc
            );
        if (result == true)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog(nameof(AddressablesKeyListCreator), "cancel.", "ok");
            return true;
        }
        return false;
    }
 }
