using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build WebGL do reactor breach para a pasta de webgame.
///
/// Linha de comandos:
///   Unity.exe -quit -batchmode -nographics -projectPath "&lt;projeto&gt;" ^
///             -executeMethod WebGLBuild.Build -logFile build_webgl.log
///
/// Editor: menu Assets &gt; Build WebGL &gt; Build to webgame folder
/// </summary>
public static class WebGLBuild
{
    /// <summary>Pasta de destino. Sobreponha com -webglOutputPath &lt;pasta&gt;.</summary>
    private const string DefaultOutputFolder = @"C:\Users\User\webgame";

    /// <summary>
    /// Cenas obrigatorias, por ordem. O primeiro indice tem de ser o arranque,
    /// porque o browser abre sempre a cena 0.
    /// </summary>
    private static readonly string[] RequiredScenes =
    {
        "Assets/TitleScreen.unity",
        "Assets/MainMenu.unity",
        "Assets/gameonline.unity",
        "Assets/lobbyonline.unity",
        "Assets/scene2.unity",
    };

    [MenuItem("Assets/Build WebGL/Build to webgame folder")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void Build()
    {
        string outputFolder = DefaultOutputFolder;
        string overridePath = ReadCommandLineArg("-webglOutputPath");
        if (!string.IsNullOrWhiteSpace(overridePath))
            outputFolder = overridePath;

        Build(outputFolder);
    }

    public static void Build(string outputFolder)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
            outputFolder = DefaultOutputFolder;

        ConfigurePlayerSettings();
        string[] scenes = EnsureScenesEnabled();

        string locationPathName = Path.GetFullPath(outputFolder);
        string parent = Path.GetDirectoryName(locationPathName);
        if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
            Directory.CreateDirectory(parent);

        Debug.Log("[WebGLBuild] Saida: " + locationPathName);
        Debug.Log("[WebGLBuild] Cenas: " + string.Join(", ", scenes));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPathName,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            string dataMb = TotalMb(locationPathName, ".data").ToString("0.0");
            string wasmMb = TotalMb(locationPathName, ".wasm").ToString("0.0");
            Debug.Log(string.Format(
                "[WebGLBuild] OK em {0:0.0}s | data={1} MB | wasm={2} MB",
                summary.totalTime.TotalSeconds, dataMb, wasmMb));

            CreateZip(locationPathName);

            // Sai com codigo 0 de forma explicita. Sem isto o Unity espera 300s
            // pelas operacoes assincronas pendentes (o plugin HackaTime mantem um
            // heartbeat vivo) e acaba a devolver codigo 1 apesar do build ser
            // um sucesso, o que faz o CI/linha de comandos reportar falha falsa.
            if (Application.isBatchMode)
                EditorApplication.Exit(0);

            return;
        }

        Debug.LogError(string.Format(
            "[WebGLBuild] FALHOU: {0} | {1} erros, {2} avisos",
            summary.result, summary.totalErrors, summary.totalWarnings));

        foreach (BuildStep step in report.steps)
        {
            foreach (BuildStepMessage message in step.messages)
            {
                if (message.type == LogType.Error || message.type == LogType.Exception)
                    Debug.LogError("[WebGLBuild] " + message.content);
            }
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }

    /// <summary>
    /// Garante que todas as cenas obrigatorias estao activas e por ordem.
    /// Uma cena desactivada no Inspector desaparece em silencio do build e o
    /// LoadScene correspondente falha em runtime com "Scene not found".
    /// </summary>
    private static string[] EnsureScenesEnabled()
    {
        var scenes = EditorBuildSettings.scenes.ToList();

        foreach (string required in RequiredScenes)
        {
            EditorBuildSettingsScene match = scenes.FirstOrDefault(s => s.path == required);
            if (match != null)
            {
                if (!match.enabled)
                {
                    match.enabled = true;
                    scenes[scenes.IndexOf(match)] = match;
                }
                continue;
            }

            scenes.Add(new EditorBuildSettingsScene(required, true));
            Debug.LogWarning("[WebGLBuild] Cena adicionada ao build: " + required);
        }

        scenes = scenes
            .OrderBy(s =>
            {
                int index = Array.IndexOf(RequiredScenes, s.path);
                return index >= 0 ? index : int.MaxValue;
            })
            .ToList();

        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();

        return scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
    }

    /// <summary>
    /// Definicoes de WebGL que evitam o freeze e o crash da tab.
    ///
    /// initialMemorySize e o mais importante. Com 32 MB (valor que estava no projecto)
    /// o heap do WASM tinha de crescer muitas vezes, e cada crescimento e um realloc
    /// com copia integral que bloqueia a main thread durante hundreds de milissegundos.
    /// Era a causa dos "congela" no browser. 512 MB cobre o arranque sem nenhum
    /// crescimento.
    ///
    /// maximumMemorySize fica em 2048 MB porque o WebGL corre em WASM32: pedir mais
    /// esgotaria o espaco de enderecos de 32 bits e matava a tab com
    /// "RuntimeError: memory access out of bounds".
    /// </summary>
    private static void ConfigurePlayerSettings()
    {
        // PlayerSettings.WebGL e uma classe estatica aninhada, por isso os membros
        // acedem-se directamente.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;

        // Nenhuma excepcao no release: menos peso, e uma excepcao deixa de abortar
        // o modulo WASM inteiro, que era o que aparecia como reset do browser.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

        PlayerSettings.WebGL.initialMemorySize = 512;
        PlayerSettings.WebGL.maximumMemorySize = 2048;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.memoryGeometricGrowthCap = 96;

        // memoryGeometricGrowthStep nao tem setter publico; fica no
        // ProjectSettings.asset (webGLMemoryGeometricGrowthStep).
        Debug.Log(string.Format(
            "[WebGLBuild] WebGL: compressao={0} initialMemory={1}MB maxMemory={2}MB exceptions={3}",
            PlayerSettings.WebGL.compressionFormat,
            PlayerSettings.WebGL.initialMemorySize,
            PlayerSettings.WebGL.maximumMemorySize,
            PlayerSettings.WebGL.exceptionSupport));
    }

    /// <summary>
    /// Regera o index.zip com o conteudo do build. Sem isto o zip ficava na
    /// versao anterior e era distribuido conteudo desatualizado.
    ///
    /// As entradas usam sempre "/" porque o spec de zip o exige: com "\" muitos
    /// servidores web devolvem 404 aos ficheiros dentro do zip.
    /// </summary>
    private static void CreateZip(string buildFolder)
    {
        string zipPath = Path.Combine(buildFolder, "index.zip");

        try
        {
            var files = Directory
                .EnumerateFiles(buildFolder, "*", SearchOption.AllDirectories)
                .Where(f => !string.Equals(
                    Path.GetFullPath(f), Path.GetFullPath(zipPath), StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using (FileStream stream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    string entryName = file
                        .Substring(buildFolder.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace('\\', '/');

                    ZipArchiveEntry entry = archive.CreateEntry(
                        entryName, System.IO.Compression.CompressionLevel.Optimal);
                    using (Stream entryStream = entry.Open())
                    using (FileStream source = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        source.CopyTo(entryStream);
                    }
                }
            }

            Debug.Log(string.Format(
                "[WebGLBuild] index.zip gerado com {0} ficheiros ({1:0.0} MB)",
                files.Count, new FileInfo(zipPath).Length / (1024f * 1024f)));
        }
        catch (Exception e)
        {
            // O zip e uma convenience: falhar aqui nao deve invalidar o build.
            Debug.LogWarning("[WebGLBuild] Nao foi possivel gerar o index.zip: " + e.Message);
        }
    }

    private static float TotalMb(string folder, string extension)
    {
        try
        {
            string[] matches = Directory.GetFiles(folder, "*" + extension + "*", SearchOption.AllDirectories);
            return matches.Sum(f => new FileInfo(f).Length) / (1024f * 1024f);
        }
        catch
        {
            return 0f;
        }
    }

    private static string ReadCommandLineArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}
