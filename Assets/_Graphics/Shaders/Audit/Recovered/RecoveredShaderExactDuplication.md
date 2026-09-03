# Exact Duplication

## Scope decision

The evidence-based catalog contains 26 shaders. It is the authoritative recovered scope.

The behavior scan contains 29 candidates because it uses a broader evidence rule. The three extra candidates are adjacent and unverified for catalog membership:

- `GrabPassTexture1.shader` has binary evidence for a helper role.
- `Post Process/BlitBlendColor.shader` has binary evidence for a hidden replacement role.
- `Post Process/ChromaticAberration.shader` is a derived split from a combined bloom corpus. No standalone source export joins this split.

These three shaders do not become recovered catalog entries. Their behavior records remain available with an `adjacent_unverified` scope status.

## Result

The all-source scan found 4 normalized exact-function groups. No group contains a recovered catalog shader.

Equal hashes do not prove compatible inputs, outputs, render state, or caller contracts.

## 1. `4c6d9571c1a4a09768a2c3ab2870b9acdcc3bc683c7801e8b5369651e957f30b`

- Classification: `EXACT_SHARED`
- Member count: 2
- Members: Editor/UIWorld.shader::vert;Missing.shader::vert
- Operation hash: `012c1585e874a858ccc7a7c57d4e23f21ceab624cfb8d8854a056c00b5371a54`
- Recovered member count: 0
- Recovered members: none
- Evidence: Equal comment/whitespace-normalized token SHA-256 for the function body; caller contracts still require review.

## 2. `a63df18085ac2c4c0282c1e5a21de26a26e878458a78af67a3d10038b34b3e60`

- Classification: `EXACT_SHARED`
- Member count: 2
- Members: Font/TMP_SDF.shader::PixShader;Font/TMP_SDFTransparentColor.shader::PixShader
- Operation hash: `5190dfb3cd733e85be20761e8db94149dcbad48dbb85fcf018eb1370e1ceaf92`
- Recovered member count: 0
- Recovered members: none
- Evidence: Equal comment/whitespace-normalized token SHA-256 for the function body; caller contracts still require review.

## 3. `de89a9bb3cc9fad383e88fa885b22dbc81e34d4725b11dc87d272dcabf716ba7`

- Classification: `EXACT_SHARED`
- Member count: 2
- Members: Object/SelectionHighlight.shader::vert;ToonOutlineInstanced.shader::vert
- Operation hash: `e46d85b9dab6a69855cb49902ba9bc0f0652f8543b1d02b25d95f7c070d40f3c`
- Recovered member count: 0
- Recovered members: none
- Evidence: Equal comment/whitespace-normalized token SHA-256 for the function body; caller contracts still require review.

## 4. `e6b7a070c02811145dd21b0cd65da41fe0fb3b00cff7c1a8f35beb26a4447f79`

- Classification: `EXACT_SHARED`
- Member count: 2
- Members: Font/TMP_SDF.shader::VertShader;Font/TMP_SDFTransparentColor.shader::VertShader
- Operation hash: `d96489277ac578a56484972064f4a0fe7d80b7c92434109adbddd33a0072e12e`
- Recovered member count: 0
- Recovered members: none
- Evidence: Equal comment/whitespace-normalized token SHA-256 for the function body; caller contracts still require review.
