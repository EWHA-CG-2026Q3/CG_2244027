# S04 Custom Diamond Mesh

강의 자료: https://github.com/Vivi827/EWHA_CG_2026Q3

이 폴더에는 6개 정점과 8개 삼각형으로 직접 구성하는 다이아몬드 과제의 코드, 장면, 재질, 메시 및 스크린샷을 정리합니다.

## 구현 기준

- 정육면체의 0, 1, 5, 4번 위치를 다이아몬드의 허리 정점 4개로 사용했습니다.
- 위와 아래에 새 꼭짓점을 하나씩 추가하여 총 6개 정점을 구성했습니다.
- 위쪽 4면과 아래쪽 4면의 인덱스를 바깥쪽 winding order로 직접 입력했습니다.

## 주요 파일

- `S04_CustomDiamondMesh.cs`: 정점 6개와 삼각형 8개의 인덱스를 직접 정의한 코드
- `Meshes/S04_Diamond_6V_8T.asset`: 완성된 다이아몬드 메시
- `Scenes/S04_CustomDiamond.unity`: 제출용 완성 장면
- `Screenshots/S04_01_SceneView.png`: 정점과 격자를 포함한 Scene 뷰
- `Screenshots/S04_02_GameView.png`: 완성된 Game 뷰
- `Screenshots/S04_03_SceneAndGame.png`: Scene 뷰와 Game 뷰를 함께 배치한 제출용 이미지

## 과제 요구사항 확인

- 관련 파일은 모두 `Assets/S04` 아래에 정리했습니다.
- 총 6개 정점과 8개 삼각형을 사용했습니다.
- 모든 삼각형 인덱스는 외부에서 보이는 winding order로 직접 작성했습니다.
- 프로젝트 생성, 메시 구현, 장면 구성, 증빙 추가를 서로 다른 커밋으로 기록했습니다.
