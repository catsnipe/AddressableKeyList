using System.Collections.Generic;
using UnityEngine;

///<summary>
/// AddressableAsset Key-Const
///</summary>
public class AddressableConst
{
    public const string EditorSceneList = "EditorSceneList";
    public const string nani_mo_nai     = "nani mo nai";
    public const string null_string     = "null string";
    public const string Resources       = "Resources";
    public const string test            = "test";
    public const string New_Label       = "New Label";

}

///<summary>
/// AddressableAsset Key-List
///</summary>
public partial class AddressablesKey
{
    static HashSet<string> keyList = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void initialize()
    {
        keyList.Add("EditorSceneList");
        keyList.Add("nani mo nai");
        keyList.Add("null string");
        keyList.Add("Resources");
        keyList.Add("test");
        keyList.Add("New Label");

    }

    public static bool Contains(string key)
    {
        if (keyList == null || keyList.Contains(key) == false)
        {
            return false;
        }
        return true;
    }

}