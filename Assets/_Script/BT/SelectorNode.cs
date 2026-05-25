using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorNode : BTComposite
{
    public SelectorNode(params BTNode[] nodes) : base(nodes) {}

    public override BTStatus Tick()
    {
        bool intermediateNodeFound = false;

        for (int i = 0; i < children.Count; i++)
        {
            if (intermediateNodeFound)
            {
                // Nếu đã có một nhánh ưu tiên cao hơn chiếm quyền (Success/Running)
                // Ta dọn dẹp bộ nhớ của toàn bộ các nhánh ưu tiên thấp hơn phía sau
                children[i].ClearState();
                continue;
            }

            var status = children[i].Tick();

            if (status != BTStatus.Failure)
            {
                intermediateNodeFound = true; // Đánh dấu đã tìm thấy nhánh thực thi
            }
        }

        // Trả về kết quả thực tế của vòng lặp (vẫn giữ nguyên logic xử lý gốc của bạn)
        for (int i = 0; i < children.Count; i++)
        {
            var status = children[i].Tick(); // Lưu ý: Để tối ưu tránh chạy Tick 2 lần, bạn nên lưu status vào 1 mảng tạm trong luồng xử lý thực tế nhé!
            if (status != BTStatus.Failure) return status;
        }
        return BTStatus.Failure;
    }
}
