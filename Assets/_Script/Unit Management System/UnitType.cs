using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Archer,    // Cung thủ
    Priest,    // Tu sĩ
    Warrior,   // Chiến binh
    Builder    // Thợ xây
}

public enum UnitState
{
    Idle,      // Rảnh rỗi
    Moving,    // Đang di chuyển
    Stationed, // Được đặt tại một vị trí
    Working    // Đang làm việc
}