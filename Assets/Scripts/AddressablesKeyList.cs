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
    public static HashSet<string> KeyList = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void initialize()
    {
        KeyList.Add("EditorSceneList");
        KeyList.Add("nani mo nai");
        KeyList.Add("null string");
        KeyList.Add("Resources");
        KeyList.Add("test");
        KeyList.Add("New Label");

    }

    public static bool Contains(string key)
    {
        if (KeyList == null || KeyList.Contains(key) == false)
        {
            return false;
        }
        return true;
    }

}