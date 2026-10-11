# S10 — Bouncing Ball Animation

Unity 6.6.0f1 프로젝트입니다. Unity Hub에서 이 `S10` 폴더를 프로젝트로 열고 `Assets/S10/Scenes/S10_BouncingBallAnimation.unity`를 열어 Play를 누르세요.

- 청록색 공: 실제 부모–자식 Hierarchy로 이동·회전·달 궤도를 구성했습니다.
- 분홍색 다이아몬드: 부모 없는 오브젝트들의 Transform을 `S10_DiamondChain_Finish`가 순서대로 곱해 같은 동작을 만듭니다.
- 양쪽 달: 지름 0.05, 본체 중심에서 거리 0.15, y축 120프레임(60 FPS에서 2초)당 1회전입니다.
- `Assets/S10/Submission/S10_GameView_Moons.mp4`: 두 달이 여러 바퀴 도는 8초 분량의 Game 뷰 녹화본입니다. 사이버캠퍼스에는 이 파일을 별도로 업로드하세요.

Unity 상단 메뉴 `S10 > Verify assignment scene`에서 계층, Chain 순서, 달 크기·거리·회전 주기를 다시 검사할 수 있습니다.
