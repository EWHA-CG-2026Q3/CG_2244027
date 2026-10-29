# S14 화로·숯·연기 정자 장면

이 폴더는 독립된 Unity 프로젝트입니다. Unity Hub에서 `S14`를 프로젝트로 추가하고 `Assets/S14/S14_Landscape.unity`를 여세요. S13의 정자 장면을 이어받되 원본 프로젝트는 변경하지 않았습니다. 외부 Pavilion 에셋은 `Assets/ThirdParty/Pavilion`에 있습니다.

정자 내부의 기본 도형 3개는 각각 다른 텍스처를 사용하는 URP Lit 재질을 가지며, S03·S04·S10의 커스텀 메시 3개는 텍스처 없는 각기 다른 URP Lit 재질을 사용합니다. S03 별은 발광합니다. 탁자 위 화로의 금속 몸체, 발광 숯, Point Light, 연기 Particle System과 Global Volume Bloom이 추가되었습니다. 숯 균열 및 연기 알파 텍스처는 이 프로젝트에서 직접 생성한 에셋입니다.

`SceneWalker`로 Play 모드에서 WASD 이동과 마우스 회전을 할 수 있습니다. Unity 메뉴 `S14 > Preview ShotCamera`로 정자 내부 스크린샷 구도를 볼 수 있고, `Preview Walker Camera`로 걷기 화면에 돌아옵니다. `Record 40-second Game view walk`는 SceneWalker가 야외에서 내부로 이동하는 40초 영상을 `Recordings/S14_GameView_40s_Walker.mp4`에 저장합니다. 스크린샷은 `Recordings/S14_GameView_CharcoalSmoke.png`입니다. `Recordings`는 GitHub 업로드에서 제외되므로 영상과 스크린샷은 사이버캠퍼스에 별도로 제출해야 합니다.
