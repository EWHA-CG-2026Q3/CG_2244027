# S13 정자 야간 장면

이 폴더 자체가 독립된 Unity 프로젝트입니다. Unity Hub에서 `S13_Main` 폴더를 프로젝트로 추가한 뒤 `Assets/S13/S13_Landscape.unity`를 여세요. 기존 노을 장면은 `S13_SunsetPavilion.unity`에 남겨 두었고, 외부 Pavilion 원본 에셋은 `Assets/ThirdParty/Pavilion`에 있습니다.

정자 안의 `S02_MyTable_InPavilion`은 S02에서 만든 기본 도형 탁자 프리팹입니다. 탁자 위에는 서로 다른 URP Lit 재질을 사용하는 기본 도형 3개와 S03·S04·S10에서 만든 커스텀 메시 3개가 있습니다. `SceneWalker`에 붙은 Main Camera로 Play 모드에서 WASD 이동과 마우스 시점 회전을 할 수 있습니다. 게임 화면을 클릭하면 마우스가 고정되고 Esc를 누르면 해제됩니다.

Unity 메뉴 `S13 Main > Preview ShotCamera`는 제출용 내부 사진 구도를 보여 줍니다. 다시 걷기 화면으로 돌아오려면 `Preview Walker Camera`를 누르세요. `Record 40-second Game view walk`는 야외에서 정자 안으로 이동하는 40초 영상을 프로젝트의 `Recordings` 폴더에 생성합니다. `Recordings`는 GitHub 업로드에서 제외되므로 영상과 스크린샷은 사이버캠퍼스에 별도로 제출해야 합니다.
