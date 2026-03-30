using UnityEngine;

/// <summary>
/// Interface chuẩn chung cho tất cả các object có thể nhận sát thương trong game.
/// (Enemy, Boss, Thùng gỗ, hoặc chính Player nếu muốn).
/// SRP: Định nghĩa hợp đồng gây sát thương thuần túy, không phụ thuộc vào netcode rườm rà.
/// </summary>
public interface IDamageableEnemy
{
    /// <summary>
    /// Kích hoạt sát thương lên thực thể này.
    /// </summary>
    /// <param name="damage">Lượng sát thương cơ bản (đã tính toán chỉ số vũ khí)</param>
    /// <param name="hitPoint">Điểm va chạm (dùng cho việc phát VFX máu/tia lửa ngay mép chém)</param>
    /// <param name="hitNormal">Góc chém/pháp tuyến bề mặt (hỗ trợ tạo hướng máu văng Máu)</param>
    /// <param name="instigatorClientId">ID của Player gây sát thương (Mục đích tính điểm hoặc Aggro AI)</param>
    void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, ulong instigatorClientId);
}
