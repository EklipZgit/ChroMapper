using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem;

// A fixture without a namespace wraps the entire test assembly.
[SetUpFixture]
public class TestRunCleanup
{
    private Mouse batchMouse;
    private Keyboard batchKeyboard;

    [OneTimeSetUp]
    public void InitializeBatchDevices()
    {
        // Headless hosts can expose no devices, but mapper components read mouse and keyboard state during scene loading.
        if (!InputSystem.devices.Any(device => device is Mouse))
            batchMouse = InputSystem.AddDevice<Mouse>();

        if (!InputSystem.devices.Any(device => device is Keyboard))
            batchKeyboard = InputSystem.AddDevice<Keyboard>();
    }

    [OneTimeTearDown]
    public void DeleteGeneratedTestInstallation()
    {
        if (batchMouse != null && batchMouse.added)
            InputSystem.RemoveDevice(batchMouse);

        if (batchKeyboard != null && batchKeyboard.added)
            InputSystem.RemoveDevice(batchKeyboard);

        var temporaryRoot = PathUtils.Combine(Path.GetFullPath(Path.GetTempPath()), "ChroMapper Tests") + "/";
        var installation = PathUtils.Combine(Path.GetFullPath(Settings.TestRunnerInstallationDirectory));
        if (!installation.StartsWith(temporaryRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("The generated test installation must remain inside its temporary directory.");

        if (Directory.Exists(installation))
            Directory.Delete(installation, true);
    }
}
