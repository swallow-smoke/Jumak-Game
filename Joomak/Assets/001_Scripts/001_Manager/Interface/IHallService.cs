using System;
using System.Collections.Generic;
using _001_Scripts._005_Data.Hall;

namespace _001_Scripts._001_Manager.Interface
{
    // 홀 시스템의 외부 공개 API.
    // UI / 상점 / 라운드 / 주방이 홀 내부(HallManager, Customer, DiningTable)를 직접 뒤지지 않고 여기만 쓴다.
    // 홀 내부끼리는(Customer, ServingCounter) HallManager를 직접 호출한다.
    public interface IHallService : IService
    {
        // --- Order ---
        IReadOnlyList<OrderSnapshot> GetOrders();
        bool TryGetOrder(Guid orderId, out OrderSnapshot order);
        int GetOrderCount(HallOrderStatus status);

        // --- Table / Seat ---
        IReadOnlyList<TableSnapshot> GetTables();
        int GetFreeSeatCount();

        // --- Reputation ---
        int GetReputation();
        int GetMaxReputation();
        void UpdateReputation(int delta, string reason);
    }
}
