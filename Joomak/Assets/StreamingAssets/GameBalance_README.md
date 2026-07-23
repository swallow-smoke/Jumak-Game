# GameBalance.json 편집 안내

`GameBalance.json`은 게임 시작 시 자동으로 읽힙니다. 에디터에서는 `Assets/StreamingAssets`, 빌드에서는 실행 파일의 `StreamingAssets` 폴더에 있습니다.

- 확률은 `0`부터 `1` 사이 값입니다. 예: `0.15` = 15%.
- 번들 등장 비중은 `delivery.bundleWeights`의 `weight`로 조절합니다. `0`이면 등장하지 않습니다.
- 한 번 배달되는 재료 종류 수는 `bundleTypesPerDelivery`, 실제 채워지는 재고 범위는 `minStockPerBundle`~`maxStockPerBundle`입니다.
- `reputation.deathEndingThreshold` 이하가 되면 사망 엔딩이 발생합니다. `deleteSaveOnDeath`로 사망 시 이어하기 저장 삭제 여부를 정할 수 있습니다.
- `componentOverrides`는 코드에 있는 숫자/불리언 필드를 추가로 덮어쓰는 범용 목록입니다.
  - `component`: C# 컴포넌트 클래스 이름
  - `field`: 해당 컴포넌트의 필드 이름
  - `value`: 적용할 값. 숫자도 JSON 문자열로 적습니다.
  - `objectPath`: 비워두면 해당 컴포넌트 전부에 적용합니다. 특정 오브젝트만 바꾸려면 Hierarchy 경로 또는 오브젝트 이름을 적습니다.
  - `scene`: 적용할 씬 이름. 비워두면 모든 씬에 적용합니다.

잘못된 필드명이나 값은 게임을 중단시키지 않고 Console에 `[Balance]` 경고로 표시됩니다.
