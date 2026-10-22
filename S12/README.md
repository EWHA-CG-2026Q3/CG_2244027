# S12 — Perspective Virtual Camera

This is an independent Unity 6.6 project. Open `Assets/S12/Scenes/S12_VirtualCamera_Finish.unity` for the two-ball camera demonstration or `Assets/S12/Scenes/S12_StillLife.unity` for the submission scene.

`S12_VirtualCamera_Finish` builds its own positive-z perspective projection matrix, transforms target mesh vertices into camera space, and draws their projected edges on a screen-space UI canvas. The demo scene uses `(2, 0, 0)`, `(0, 90, 0)`, FOV `90`, near `0.3`, far `20`, and `Ball` plus `Ball_Far`. The Editor verification checks canvas/Game viewport agreement again at FOV `60`, then restores FOV `90`.

The still-life scene instantiates the copied S02 table prefab and adds three handmade meshes (star, six-vertex diamond, pyramid). Its camera uses FOV `45` for a closer composition. The Main Camera is a zeroed child of VirtualCamera. In Play mode, `S12_CameraDolly` moves the parent forwards and backwards while the UI overlay follows.

To reproduce the aspect experiment, temporarily replace `scale / aspect` with `scale / 1f` at `BuildProjectionMatrix` in `Assets/S12/S12_VirtualCamera_Finish.cs`, take a Game-view screenshot, and then restore the original expression. The canvas drawing becomes horizontally wider than the actual Game-view objects because the horizontal scaling no longer accounts for a wide viewport.

Submission media belong in `Recordings/` and are intentionally Git-ignored. Upload them and the short written explanation to CyberCampus separately; the Unity project is pushed to GitHub.
