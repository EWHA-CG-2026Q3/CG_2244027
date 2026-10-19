# S11 — Virtual Camera

Unity 6.6.0f1 독립 프로젝트입니다. Unity Hub에서 이 `S11` 폴더를 열고 `Assets/S11/Scenes`에서 씬을 선택하세요.

- `S11_VirtualCamera_Finish`: 공·큐브·다이아몬드·피라미드를 대상으로, `(2, 0, 0)` / `(0, 90, 0)` 가상 카메라와 동일하게 설정한 Orthographic Main Camera를 비교합니다. Size 2, Near 0.3, Far 20입니다.
- `S11_DiamondView`: S10 씬을 별도 파일로 복사해 보존하고 다이아몬드와 달만 캔버스에 투영합니다. S10 원본 씬은 수정하지 않았습니다.
- 두 씬에서 `VirtualCamera Canvas`의 선은 3D 메시의 정점을 수동 View 행렬로 변환한 결과입니다. Play 중에도 다이아몬드와 달의 움직임에 따라 갱신됩니다.
- `S11 > Verify both assignment scenes`는 수동 행렬곱, 카메라 일치, 타깃, 캔버스 선, 움직이는 샘플 시점의 투영 일치를 검사합니다.

직접 화면을 확인할 때는 Game 뷰에서 캔버스의 밝은 선이 실제 오브젝트의 모서리와 겹치는지 확인하세요. 다이아몬드 씬에서는 Play를 눌러 본체와 달이 움직이는 동안에도 선이 따라가는지 확인할 수 있습니다.
