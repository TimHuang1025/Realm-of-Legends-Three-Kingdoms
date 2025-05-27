using System.Collections.Generic;
using UnityEngine;

/// <summary>存放 UI 元素 vh 规则的配置文件。</summary>
[CreateAssetMenu(fileName = "VhSizerConfig", menuName = "UI/Vh Sizer Config")]
public class VhSizerConfig : ScriptableObject
{
    [System.Serializable]
    public class Rule
    {
        public enum QueryType { ByName, ByClass }
        public QueryType queryType = QueryType.ByName;
        public string    queryValue;

        [Header("尺寸 vh (0 = ignore)")]
        public float widthVh  = 0;
        public float heightVh = 0;
        public float fontVh   = 0;

        [Header("是否缩放?")]
        public bool applyScale = true;     // ← 逐元素开关，默认开启
    }

    public List<Rule> rules = new();
}
