using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(LayoutGroup))]
public class LayoutOptimizer : MonoBehaviour
{
    private LayoutGroup layoutGroup;

    private void Awake()
    {
        layoutGroup = GetComponent<LayoutGroup>();
    }

    private void OnEnable()
    {
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            
            StartCoroutine(DisableLayoutAfterFrame());
        }
    }

    private IEnumerator DisableLayoutAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }
    }

    // (Tùy chọn) Hàm này dành cho trường hợp bạn có thêm/bớt nút bằng code khi menu đang mở
    // Bạn chỉ cần gọi hàm này, nó sẽ tự động bật lên xếp lại rồi tắt đi
    public void ForceRebuildLayout()
    {
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}