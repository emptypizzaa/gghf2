using System.IO;
using UnityEditor;
using UnityEngine;

namespace PaperHeroes.EditorTools
{
    /// <summary>
    /// Resources/Story 폴더의 미디어 임포트 설정을 강제한다(비파괴, 임포트 시 1회 적용).
    /// AndroidBuilder.cs / WebGLBuilder.cs 와 같은 Assets/Editor 스크립트 전례를 따른다.
    /// - 텍스처: RawImage+Texture2D 경로 → Default·sRGB·mips off·clamp·max 2048
    /// - 오디오: bgm* → Streaming(긴 루프), 나머지 → DecompressOnLoad(짧은 음성), Vorbis·mono
    /// 재생 자체는 임포트 설정과 무관하게 동작하므로(Resources.Load 성공), 이 설정은 최적화 목적이다.
    /// </summary>
    public sealed class StoryAssetPostprocessor : AssetPostprocessor
    {
        const string Folder = "/Resources/Story/";

        static bool InStory(string path) => path.Replace('\\', '/').Contains(Folder);

        void OnPreprocessTexture()
        {
            if (!InStory(assetPath)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType        = TextureImporterType.Default; // RawImage + Texture2D 경로(Sprite 불필요)
            ti.sRGBTexture        = true;
            ti.mipmapEnabled      = false;                        // 스크린스페이스 UI는 mip 불필요(메모리·블러 방지)
            ti.wrapMode           = TextureWrapMode.Clamp;
            ti.maxTextureSize     = 2048;
            ti.textureCompression = TextureImporterCompression.Compressed;
        }

        void OnPreprocessAudio()
        {
            if (!InStory(assetPath)) return;
            var ai = (AudioImporter)assetImporter;
            bool isBgm = Path.GetFileNameWithoutExtension(assetPath)
                             .StartsWith("bgm", System.StringComparison.OrdinalIgnoreCase);

            var s = ai.defaultSampleSettings;
            s.loadType          = isBgm ? AudioClipLoadType.Streaming        // 긴 BGM은 스트리밍(RAM 최소)
                                        : AudioClipLoadType.DecompressOnLoad; // 짧은 음성은 즉시 재생
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality           = 0.7f;
            ai.defaultSampleSettings = s;
            ai.forceToMono      = true;                           // 모바일: 모노로 데이터 절반
            ai.loadInBackground = isBgm;
        }
    }
}
