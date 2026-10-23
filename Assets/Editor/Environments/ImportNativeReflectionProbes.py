"""Import native packed probes, lightmaps and renderer atlas transforms without regenerating mip levels.

Requires Python 3.12+, UnityPy 1.25.2, numpy and texture2ddecoder.
Run outside Unity, then run NativeEnvironmentProbeParityTest through Test-ChroMapper.ps1.
"""
import argparse
import gzip
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import struct
import uuid

import numpy as np
import texture2ddecoder
import UnityPy


def asset_guid(path):
    metadata = Path(str(path) + ".meta")
    if metadata.exists():
        return re.search(r"^guid: (\w+)", metadata.read_text(), re.MULTILINE)[1]
    guid = uuid.uuid4().hex
    metadata.write_text("fileFormatVersion: 2\nguid: " + guid + "\n")
    return guid


def write_changed(path, content):
    if not path.exists() or path.read_bytes() != content:
        path.write_bytes(content)


def write_probe(path, payload):
    # Reimporting unchanged native data should not recompress the whole lighting archive.
    if path.exists() and gzip.decompress(path.read_bytes()) == payload:
        return
    write_changed(path, gzip.compress(payload, mtime=0))


def rgb_mips(texture):
    raw = texture.get_image_data()
    if texture.m_TextureFormat == 3:
        return raw
    decoders = {10: (texture2ddecoder.decode_bc1, 8), 25: (texture2ddecoder.decode_bc7, 16)}
    decode, block_bytes = decoders[texture.m_TextureFormat]
    offset = 0
    chunks = []
    for face in range(6):
        for mip in range(texture.m_MipCount):
            size = max(1, texture.m_Width >> mip)
            count = ((size + 3) // 4) ** 2 * block_bytes
            pixels = np.frombuffer(decode(raw[offset:offset + count], size, size), dtype=np.uint8)
            chunks.append(pixels.reshape(-1, 4)[:, [2, 1, 0]].tobytes())
            offset += count
    if offset != len(raw):
        raise ValueError("Unexpected packed cubemap byte count: " + texture.m_Name)
    return b"".join(chunks)


def data_asset(name, script_guid, texture_guids, field_prefix="ReflectionProbeCubemap", texture_id=-98131583685500669):
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:
  {field_prefix}1: {{fileID: {texture_id}, guid: {texture_guids[0]}, type: 3}}
  {field_prefix}2: {{fileID: {texture_id}, guid: {texture_guids[1]}, type: 3}}
""".encode()


def native_renderer_atlases(environment):
    renderers = {}
    for obj in environment.objects:
        if obj.type.name != "MeshRenderer":
            continue
        renderer = obj.read()
        if renderer.m_LightmapIndex >= 65534:
            continue
        game_object = renderer.m_GameObject.read()
        transform = next(component.component.read() for component in game_object.m_Component
                         if component.component.type.name == "Transform")
        names = [game_object.m_Name]
        while transform.m_Father.path_id:
            transform = transform.m_Father.read()
            names.insert(0, transform.m_GameObject.read().m_Name)
        path = "/".join(names)
        if path in renderers:
            raise ValueError("Ambiguous native renderer hierarchy: " + path)
        offset = renderer.m_LightmapTilingOffset
        renderers[path] = {"lightmapIndex": renderer.m_LightmapIndex,
                           "lightmapScaleOffset": [offset.x, offset.y, offset.z, offset.w]}
    return renderers


def import_lightmaps(environment, bundle, scenes, assets, destination):
    textures = sorted([(obj.path_id, obj.read()) for obj in environment.objects
                       if obj.type.name == "Texture2D" and (obj.peek_name() or "").startswith("Lightmap_")],
                      key=lambda pair: pair[1].m_Name)
    if not textures:
        return []
    expected_names = ["Lightmap_0-0_comp_light", "Lightmap_1-0_comp_light"]
    if [texture.m_Name for _, texture in textures] != expected_names:
        raise ValueError("Resolve native lightmap channel bindings: " + bundle.name)
    names_by_id = {path_id: texture.m_Name for path_id, texture in textures}
    data_object = next(obj for obj in environment.objects
                       if obj.type.name == "MonoBehaviour" and obj.peek_name() == "LightmapData")
    fields = data_object.read_typetree()
    if [names_by_id[fields[key]["m_PathID"]] for key in ["_lightmap1", "_lightmap2"]] != expected_names:
        raise ValueError("Native lightmap channel order differs: " + bundle.name)
    texture_guids = []
    records = []
    prefix = bundle.name.split("_scenes")[0]
    for index, (path_id, texture) in enumerate(textures):
        if texture.m_TextureFormat != 3:
            raise ValueError("Resolve native lightmap format before importing: " + texture.m_Name)
        sampler = texture.m_TextureSettings
        if (sampler.m_FilterMode, sampler.m_WrapU, sampler.m_WrapV, sampler.m_Aniso, sampler.m_MipBias) != (2, 1, 1, 2, 0):
            raise ValueError("Resolve native lightmap sampler before importing: " + texture.m_Name)
        path = destination / f"{prefix}-Lightmap{index + 1}.cmlightmap"
        header = struct.pack("<IHHB?", 0x314c4d43, texture.m_Width, texture.m_Height,
                             texture.m_MipCount, texture.m_ColorSpace == 1)
        write_probe(path, header + texture.get_image_data())
        texture_guids.append(asset_guid(path))
        records.append({"asset": path.relative_to(assets.parent).as_posix(),
                        "nativeName": texture.m_Name, "pathId": path_id,
                        "sourceBundle": bundle.name,
                        "sourceBundleSha256": hashlib.sha256(bundle.read_bytes()).hexdigest(),
                        "width": texture.m_Width, "height": texture.m_Height,
                        "mips": texture.m_MipCount, "format": texture.m_TextureFormat,
                        "srgb": texture.m_ColorSpace == 1,
                        "sampler": {"filterMode": sampler.m_FilterMode, "wrapU": sampler.m_WrapU,
                                    "wrapV": sampler.m_WrapV, "anisotropy": sampler.m_Aniso,
                                    "mipBias": sampler.m_MipBias},
                        "sha256": hashlib.sha256(texture.get_image_data()).hexdigest()})
    script_guid = asset_guid(assets / "__Scripts/Environments/Components/LightmapDataSO.cs")
    native_atlases = native_renderer_atlases(environment)
    for scene in scenes:
        name = scene.stem + "LightmapData"
        data_path = destination / (name + ".asset")
        content = data_asset(name, script_guid, texture_guids, "Lightmap", 7506314328700112305)
        write_changed(data_path, content)
        guid = asset_guid(data_path)
        export_path = assets / "__Scenes/Environments/Data" / (scene.stem + ".json")
        export_bytes = export_path.read_bytes()
        export = json.loads(export_bytes)
        newline = "\r\n" if b"\r\n" in export_bytes else "\n"
        ending = newline if export_bytes.endswith(b"\n") else ""
        for item in export["objects"]:
            path = re.sub(r"\.\[\d+\]", "/", item["id"]).split("/", 1)[-1]
            if path in native_atlases and "MeshRenderer" in item["components"]:
                item["components"]["MeshRenderer"][0].update(native_atlases[path])
        write_changed(export_path, (json.dumps(export, indent=2, ensure_ascii=False).replace("\n", newline) + ending).encode())
        blocks = re.split(rb"(?=^--- !u!)", scene.read_bytes(), flags=re.MULTILINE)
        game_objects = {}
        for block in blocks:
            marker = re.search(rb"  ChromaID: ([^\r\n]+)", block)
            if marker:
                game_id = re.search(rb"  m_GameObject: \{fileID: (\d+)\}", block)[1]
                path = re.sub(r"\.\[\d+\]", "/", marker[1].decode()).split("/", 1)[-1]
                game_objects[game_id] = path
        atlas_script = asset_guid(assets / "__Scripts/Environments/Components/BakedLightmapAtlas.cs")
        for index, block in enumerate(blocks):
            if b"Main::BakedLightDataLoader" in block:
                blocks[index] = re.sub(rb"  LightmapData: [^\r\n]*",
                                       f"  LightmapData: {{fileID: 11400000, guid: {guid}, type: 2}}".encode(), block)
        for game_id, path in game_objects.items():
            if path not in native_atlases:
                continue
            row = native_atlases[path]
            component_id = str(int(hashlib.sha256((scene.stem + path + "BakedLightmapAtlas").encode()).hexdigest()[:15], 16))
            reference = ("  - component: {fileID: " + component_id + "}").encode()
            for index, block in enumerate(blocks):
                if block.startswith(b"--- !u!1 &" + game_id + b"\n") or block.startswith(b"--- !u!1 &" + game_id + b"\r\n"):
                    if reference not in block:
                        ending = b"\r\n" if b"\r\n" in block else b"\n"
                        blocks[index] = block.replace(b"  m_Component:" + ending, b"  m_Component:" + ending + reference + ending)
            offset = ", ".join(f"{key}: {value:.9g}" for key, value in zip("xyzw", row["lightmapScaleOffset"]))
            component = f"""--- !u!114 &{component_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_id.decode()}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {atlas_script}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Main::BakedLightmapAtlas
  Index: {row['lightmapIndex']}
  ScaleOffset: {{{offset}}}
""".encode()
            existing_index = next((index for index, block in enumerate(blocks)
                                   if block.startswith(b"--- !u!114 &" + component_id.encode() + b"\n")), None)
            if existing_index is None:
                blocks.append(component)
            else:
                blocks[existing_index] = component
        write_changed(scene, b"".join(blocks))
        records.append({"environment": scene.stem, "rendererAtlases": native_atlases})
    return records


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-data", required=True, type=Path)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--game-version", required=True)
    args = parser.parse_args()
    assets = args.project / "Assets"
    destination = assets / "_Graphics/Textures/Environments/BakedLighting"
    destination.mkdir(parents=True, exist_ok=True)
    asset_guid(destination)
    script_guid = asset_guid(assets / "__Scripts/Environments/Components/ReflectionProbeDataSO.cs")
    sources = []
    lightmap_sources = []
    bundles = args.game_data / "StreamingAssets/aa/StandaloneWindows64"
    for bundle in sorted(bundles.glob("*environment_scenes*.bundle")):
        environment = UnityPy.load(str(bundle))
        scene_names = [PurePosixPath(name.replace("\\", "/")).stem for name in environment.container
                       if name.lower().endswith(".unity")]
        scenes = [assets / "__Scenes/Environments" / (name + ".unity") for name in scene_names]
        scenes = [scene for scene in scenes if scene.exists()
                  and b"Main::BakedReflectionProbe" in scene.read_bytes()]
        if not scenes:
            continue
        lightmap_sources.extend(import_lightmaps(environment, bundle, scenes, assets, destination))
        cubemaps = sorted([(obj.path_id, obj.read()) for obj in environment.objects
                           if obj.type.name == "Cubemap"], key=lambda item: item[1].m_Name)
        if len(cubemaps) != 2 or [texture.m_Name for _, texture in cubemaps] != ["ReflectionProbe_0-0", "ReflectionProbe_1-0"]:
            raise ValueError("Resolve native probe channel bindings before importing: " + bundle.name)
        texture_names = {path_id: texture.m_Name for path_id, texture in cubemaps}
        for obj in environment.objects:
            if obj.type.name == "MonoBehaviour" and obj.peek_name() == "ReflectionProbeData":
                fields = obj.read_typetree()
                bindings = [texture_names[fields[key]["m_PathID"]]
                            for key in ["_reflectionProbeCubemap1", "_reflectionProbeCubemap2"]]
                if bindings != ["ReflectionProbe_0-0", "ReflectionProbe_1-0"]:
                    raise ValueError("Resolve native probe channel bindings before importing: " + bundle.name)
        texture_guids = []
        texture_records = []
        prefix = bundle.name.split("_scenes")[0]
        for index, (path_id, texture) in enumerate(cubemaps):
            if prefix == "gagaenvironment":
                texture_path = assets / "_Graphics/Textures/Environments/Gaga" / f"GagaReflectionProbe{index + 1}.cmprobe"
            else:
                texture_path = destination / f"{prefix}-Probe{index + 1}.cmprobe"
            header = struct.pack("<IHB?", 0x31524d43, texture.m_Width, texture.m_MipCount, texture.m_ColorSpace == 1)
            write_probe(texture_path, header + rgb_mips(texture))
            texture_guids.append(asset_guid(texture_path))
            texture_records.append({"name": texture.m_Name, "pathId": path_id,
                                    "width": texture.m_Width, "mips": texture.m_MipCount,
                                    "format": texture.m_TextureFormat,
                                    "sha256": hashlib.sha256(texture.get_image_data()).hexdigest()})
        for scene in scenes:
            name = scene.stem + "ReflectionProbeData"
            data_path = destination / (name + ".asset")
            write_changed(data_path, data_asset(name, script_guid, texture_guids))
            data_guid = asset_guid(data_path)
            blocks = re.split(rb"(?=^--- !u!)", scene.read_bytes(), flags=re.MULTILINE)
            for index, block in enumerate(blocks):
                if b"Main::BakedReflectionProbe" in block:
                    replacement = f"  ReflectionProbeData: {{fileID: 11400000, guid: {data_guid}, type: 2}}".encode()
                    blocks[index] = re.sub(rb"  ReflectionProbeData: [^\r\n]*", replacement, block)
            # Gaga already has a native pair and retains its existing data asset identity.
            if scene.stem != "GagaEnvironment":
                write_changed(scene, b"".join(blocks))
            sources.append({"environment": scene.stem, "sourceBundle": bundle.name,
                            "sourceBundleSha256": hashlib.sha256(bundle.read_bytes()).hexdigest(),
                            "textures": texture_records})
    report = destination / "NativeProbeSources.json"
    write_changed(report, json.dumps({"gameVersion": args.game_version, "reader": "UnityPy 1.25.2",
                  "format": "CMR1 gzip RGB24, original face/mip order; BC1/BC7 decoded without mip regeneration",
                  "environments": sorted(sources, key=lambda source: source["environment"])}, indent=2).encode())
    asset_guid(report)
    lightmap_report = destination / "NativeLightmapSources.json"
    write_changed(lightmap_report, json.dumps(lightmap_sources, indent=2).encode())
    asset_guid(lightmap_report)
    print(f"Imported native probe data for {len(sources)} environment scenes.")


if __name__ == "__main__":
    main()
