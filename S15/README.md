# S15 flowing landscape

Open this folder as a separate Unity 6.6 project, then open `Assets/S15/S15_Landscape.unity`.

- The S14 fire bowl keeps its coal, lighting, and smoke particle system. The smoke uses velocity and noise plus color and size over lifetime.
- `Assets/S15/S15_SG_Flow.shadergraph` samples two textures. Each UV is tiled and offset by Time multiplied by its Flow vector, then the two samples are blended. Emission makes the movement readable in the dark pavilion.
- The S02 table top keeps the practice flow, now toned down. The assignment's separate flow is clearly visible in `Water_Basin_Floor/Water_Flow_Plane` on the pavilion floor. `FireBowl/Lava_Flow_Quad` remains an additional example.
- The water uses [GGBotNet's CC0 Y2K Water Texture](https://opengameart.org/content/y2k-water-texture), processed through RenderHub's seamless texture generator. Source and processed textures are in `Assets/ThirdParty/Textures`; the Pavilion package remains in `Assets/ThirdParty/Pavilion`.
- Shader Graph texture sample nodes use Default color sampling, rather than Normal sampling, so the water texture's colors appear correctly.

Use `S15 > Capture ShotCamera Game view PNG` for the still image and `S15 > Record 40-second Game view walk` for the video. Both are saved under this project's `Recordings` folder and are excluded from GitHub.
