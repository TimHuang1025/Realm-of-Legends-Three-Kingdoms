using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingEllipsis : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;   // 指向「載入中…」這個文字
    [SerializeField] float interval = 0.5f;   // 每幀間隔（秒）
    readonly string[] frames = { ".", "..", "...", };
    int index;

    void OnEnable()  => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        while (true)
        {
            label.text = $"加载中{frames[index]}";
            index = (index + 1) % frames.Length;   // 0→1→2→3→0…
            yield return new WaitForSeconds(interval);
        }
    }
}
