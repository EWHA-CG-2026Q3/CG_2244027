# S06 픽셀 판정과 GL immediate mode

Unity Hub에서 `S06` 폴더를 독립 프로젝트로 열고, 아래 장면을 열어 Play를 누르세요.

- `Scenes/S06_PixelRasterizer.unity`: CPU에서 픽셀의 삼각형 내부 여부를 판정해 Texture2D에 색을 설정합니다.
- `Scenes/S06_GLImmediateMode.unity`: 카메라의 OnPostRender에서 GL.TRIANGLES로 삼각형을 그립니다.

강의 원본은 `Vivi827/EWHA_CG_2026Q3`의 `Assets/S06_SoftwareRasterizer/S06_SoftwareRasterizer_Finish.cs`입니다.
꼭짓점을 A(128,200), B(60,60), C(200,60)에서 A(29,50), B(222,77), C(148,226)으로 바꾸고, 주황색을 분홍색 RGB(1,0.27,0.4)로 변경했습니다.
GL 장면에서는 청록색 삼각형을 표시하며, GL.LoadOrtho로 정규화된 화면 좌표를 사용합니다.

`Screenshots`의 두 PNG는 Unity UI와 Game 뷰가 함께 보이는 실제 실행 화면입니다. `Answer.md`의 4문장과 함께 사이버캠퍼스에 별도 제출해야 합니다.
