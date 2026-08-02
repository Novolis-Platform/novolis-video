using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Novolis.Media.Capture.Windows;

/// <summary>
/// Preloads SIPSorcery's <c>vpxmd</c> native library from the app base directory
/// so DllImport succeeds even when the process working directory differs.
/// </summary>
public static class WindowsMediaNatives
{
    static int _vp8State; // 0 = unknown, 1 = loaded, -1 = failed
    static string? _vp8Error;

    /// <summary>True when <c>vpxmd</c> is loaded into the process.</summary>
    public static bool IsVp8Available => Volatile.Read(ref _vp8State) == 1;

    /// <summary>Last load failure message, if any.</summary>
    public static string? Vp8LoadError => _vp8Error;

    /// <summary>
    /// Loads managed SIPSorcery encoder/windows assemblies and <c>vpxmd.dll</c>
    /// before capture starts. Safe to call repeatedly.
    /// </summary>
    public static bool TryEnsureVp8Loaded(out string? error)
    {
        var state = Volatile.Read(ref _vp8State);
        if (state == 1)
        {
            error = null;
            return true;
        }

        if (state == -1)
        {
            error = _vp8Error;
            return false;
        }

        if (!TryLoadManagedDependency("SIPSorceryMedia.Encoders", out var managedError)
            || !TryLoadManagedDependency("SIPSorceryMedia.Windows", out managedError)
            || !TryLoadManagedDependency("SIPSorceryMedia.Abstractions", out managedError))
        {
            _vp8Error = managedError;
            Volatile.Write(ref _vp8State, -1);
            error = _vp8Error;
            return false;
        }

        try
        {
            TryInstallResolver();
        }
        catch (Exception ex)
        {
            _vp8Error = $"Failed to prepare VP8 native resolver: {ex.Message}";
            Volatile.Write(ref _vp8State, -1);
            error = _vp8Error;
            return false;
        }

        foreach (var candidate in CandidatePaths())
        {
            if (!File.Exists(candidate))
                continue;
            if (NativeLibrary.TryLoad(candidate, out _))
            {
                Volatile.Write(ref _vp8State, 1);
                _vp8Error = null;
                error = null;
                return true;
            }
        }

        if (NativeLibrary.TryLoad("vpxmd", out _) || NativeLibrary.TryLoad("vpxmd.dll", out _))
        {
            Volatile.Write(ref _vp8State, 1);
            _vp8Error = null;
            error = null;
            return true;
        }

        _vp8Error =
            "Native VP8 library 'vpxmd.dll' could not be loaded. " +
            "Ensure SIPSorceryMedia.Encoders natives are copied next to the app.";
        Volatile.Write(ref _vp8State, -1);
        error = _vp8Error;
        return false;
    }

    static bool TryLoadManagedDependency(string assemblyName, out string? error)
    {
        try
        {
            AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
            error = null;
            return true;
        }
        catch (Exception)
        {
            var path = Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll");
            if (File.Exists(path))
            {
                try
                {
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                    error = null;
                    return true;
                }
                catch (Exception ex)
                {
                    error = $"Could not load '{assemblyName}' from '{path}': {ex.Message}";
                    return false;
                }
            }

            error =
                $"Could not load '{assemblyName}' (missing next to app at '{AppContext.BaseDirectory}'). " +
                "Ensure SIPSorcery media PackageReferences copy to the app output.";
            return false;
        }
    }

    static IEnumerable<string> CandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "vpxmd.dll");
        yield return Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "vpxmd.dll");

        var asmDir = Path.GetDirectoryName(typeof(WindowsMediaNatives).Assembly.Location);
        if (!string.IsNullOrEmpty(asmDir))
        {
            yield return Path.Combine(asmDir, "vpxmd.dll");
            yield return Path.Combine(asmDir, "runtimes", "win-x64", "native", "vpxmd.dll");
        }
    }

    static void TryInstallResolver()
    {
        var encoders = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("SIPSorceryMedia.Encoders"));
        NativeLibrary.SetDllImportResolver(encoders, static (name, _, _) =>
        {
            if (!name.Equals("vpxmd", StringComparison.OrdinalIgnoreCase)
                && !name.Equals("vpxmd.dll", StringComparison.OrdinalIgnoreCase))
                return IntPtr.Zero;

            foreach (var candidate in CandidatePaths())
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
                    return handle;
            }

            return IntPtr.Zero;
        });
    }
}
