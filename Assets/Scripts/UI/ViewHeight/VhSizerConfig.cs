using System.Collections.Generic;
using UnityEngine;

/// <summary>存放 UI 元素 vh / vw 规则的配置文件。</summary>
[CreateAssetMenu(fileName = "VhSizerConfig", menuName = "UI/Vh Sizer Config")]
public class VhSizerConfig : ScriptableObject
{
    [System.Serializable]
    public class Rule
    {
        public enum QueryType { ByName, ByClass }
        public QueryType queryType = QueryType.ByName;
        public string    queryValue;

        // 主要尺寸 (vh)
        public float widthVh  = 0;
        public float heightVh = 0;
        public float fontVh   = 0;

        [Header("可选开关")]
        public bool  applyScale  = true;   // 受全局缩放影响
        public bool  aspectWidth = false;  // 按长宽比额外调宽

        /* ---------- 新增 ---------- */
        [Header("+vw 叠加 (选填)")]
        public bool  addWidthVw = false;   // 打勾才生效
        public float widthVw    = 0;       // 例如 40 (=40 vw)
    }

    public List<Rule> rules = new();
}
