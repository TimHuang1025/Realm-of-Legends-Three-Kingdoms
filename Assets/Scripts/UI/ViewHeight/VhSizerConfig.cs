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

        public float widthVh  = 0;
        public float heightVh = 0;
        public float fontVh   = 0;

        [Header("可选开关")]
        public bool  applyScale  = true;   // 之前的
        public bool  aspectWidth = false;  // ← 新：按长宽比调宽度
    }

    public List<Rule> rules = new();
}
