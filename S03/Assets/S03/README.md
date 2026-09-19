# S03 Custom Polygon Mesh

강의 자료: https://github.com/Vivi827/EWHA_CG_2026Q3

강의의 `S03_CustomPolygonMesh_Square.cs`는 참고 가능한 템플릿으로 확인했으며, 이 과제에서는 별 모양을 직접 구현한 `S03_CustomPolygonMesh_Star.cs`를 사용했습니다.

## 구현 내용

- 오각별의 외곽 정점 10개를 `Vector3[]` 배열로 직접 계산했습니다.
- 중심 정점 1개와 외곽 정점을 연결하는 Triangle Fan 방식으로 삼각형 10개를 구성했습니다.
- 외곽 정점에는 청록색 마커를 배치해 정점의 위치를 화면에서 확인할 수 있게 했습니다.
- Unity 기본 도형을 별 모양으로 스케일한 것이 아니라 `Mesh.vertices`와 `Mesh.triangles`로 생성한 커스텀 메시입니다.

## 주요 파일

- `S03_CustomPolygonMesh_Star.cs`: 정점과 삼각형을 생성하는 C# 스크립트
- `Meshes/S03_10VertexStar.asset`: 완성된 커스텀 메시 에셋
- `Scenes/S03_CustomPolygon_Star.unity`: 제출용 완성 씬
- `Screenshots/S03_01_SceneView.png`: Unity Scene 뷰
- `Screenshots/S03_02_GameView.png`: Unity Game 뷰

## 과제 요구사항 확인

- 모든 관련 에셋은 `Assets/S03` 아래에 정리했습니다.
- 다각형의 외곽 정점은 10개로, 최소 5개 조건을 충족합니다.
- Scene 뷰와 Game 뷰 스크린샷을 각각 포함했습니다.
- S03 제작 과정은 기능 구현, 장면 구성, 증빙 추가 등 3회 이상의 커밋으로 나누어 기록했습니다.
