# S15 flowing landscape

Open this folder as a separate Unity 6.6 project, then open `Assets/S15/S15_Landscape.unity`.

- The S14 fire bowl keeps its coal, lighting, and smoke particle system. The smoke uses velocity and noise plus color and size over lifetime.
- `Assets/S15/S15_SG_Flow.shadergraph` samples two textures. Each UV is tiled and offset by Time multiplied by its Flow vector, then the two samples are blended. Emission makes the movement readable in the dark pavilion.
- The S02 table top uses the two water textures with different flow directions. `FireBowl/Lava_Flow_Quad` uses the same Shader Graph with a seamless lava texture as the additional flow surface.
- Original textures and the RenderHub seamless results are in `Assets/ThirdParty/Textures`. The Pavilion package remains in `Assets/ThirdParty/Pavilion`.

Use `S15 > Capture ShotCamera Game view PNG` for the still image and `S15 > Record 40-second Game view walk` for the video. Both are saved under this project's `Recordings` folder and are excluded from GitHub.
