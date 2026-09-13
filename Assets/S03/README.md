# S03 Custom Polygon Mesh

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
