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
        // 테이블은 씬에 미리 다 놓여 있고 앞에서부터 열린다. 잠긴 테이블은 아래 조회에 안 잡힌다.
        IReadOnlyList<TableSnapshot> GetTables();
        int GetFreeSeatCount();
        int GetTableCount();
        int GetMaxTableCount();

        // 상점의 '테이블 추가' 업그레이드용. 상한에 닿았으면 false.
        bool TryUnlockTable();

        // --- Reputation ---
        int GetReputation();
        int GetMaxReputation();
        void UpdateReputation(int delta, string reason);
    }
}
