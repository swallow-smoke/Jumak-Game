using UnityEngine;

namespace _001_Scripts._005_Data.GameData
{
    [System.Serializable]
    public class GameDatas
    {
        // [SerializeField]가 없으면 private 필드는 직렬화되지 않아 인스펙터에서 보이지도, 저장되지도 않는다.
        [SerializeField, Min(0)] private int money;
        [SerializeField, Min(0)] private int population;

        public int Money => money;
        public int Population => population;

        // 전(錢)은 음수가 될 수 없다. 상점에서 살 수 없는 물건은 애초에 구매가 막혀야 한다.
        public int AddMoney(int delta)
        {
            money = Mathf.Max(0, money + delta);
            return money;
        }

        public bool CanAfford(int cost) => cost >= 0 && money >= cost;

        public int AddPopulation(int delta)
        {
            population = Mathf.Max(0, population + delta);
            return population;
        }
    }
}
