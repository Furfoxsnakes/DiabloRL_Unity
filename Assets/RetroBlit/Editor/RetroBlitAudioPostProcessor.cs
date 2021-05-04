using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom default settings for audio imports
/// </summary>
public class RetroBlitAudioPostProcessor : AssetPostprocessor
{
    /// <summary>
    /// Called when audio clip is processed by the asset importer, this gives us a chance to adjust the defaults
    /// </summary>
    /// <param name="audioClip">Audio clip</param>
    public void OnPostprocessAudio(AudioClip audioClip)
    {
        if (assetPath.Contains("RetroBlit-ignore"))
        {
            return;
        }

        AudioImporter importer = assetImporter as AudioImporter;
        importer.preloadAudioData = true;
        AudioImporterSampleSettings iss = new AudioImporterSampleSettings();
        iss.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;

        bool normalize;

        // Assume clips longer than 10 seconds should stream (they are probably music)
        if (audioClip.length > 10)
        {
            importer.forceToMono = false;
            importer.preloadAudioData = false;
            iss.loadType = AudioClipLoadType.Streaming;
            iss.compressionFormat = AudioCompressionFormat.Vorbis;
            iss.quality = 0.7f;
            iss.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            normalize = true;
        }
        else
        {
            importer.forceToMono = false;
            importer.preloadAudioData = true;
            iss.loadType = AudioClipLoadType.DecompressOnLoad;
            iss.compressionFormat = AudioCompressionFormat.Vorbis;
            iss.quality = 0.7f;
            iss.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            normalize = true;
        }

        importer.defaultSampleSettings = iss;

        // Have to use serialized object to get at Normalize
        SerializedObject so = new SerializedObject(importer);
        var normalizeProp = so.FindProperty("m_Normalize");
        if (normalizeProp != null)
        {
            normalizeProp.boolValue = normalize;
            so.ApplyModifiedProperties();
        }
    }
}
