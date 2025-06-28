using System;
using System.Linq;
using System.Reflection;
using Sandbox.Game.Entities;
using VRage.Utils;
using VRageMath;
// Vector3D

// MyCubeGrid (adjust namespaces as needed)

namespace SETargetCamera;

public static class WeaponCoreInterop
{
    private static Assembly _wcAssembly;
    private static Type _sessionType;
    private static object _sessionInstance;

    private static FieldInfo _playerDummyTargetsField;
    private static FieldInfo _playerIdField;
    private static FieldInfo _tickField;

    private static MethodInfo _playerDummyTargetsTryGetValueMethod;

    private static Type _paintedTargetType;
    private static MethodInfo _updateMethod;

    private static bool _initialized = false;

    /// <summary>
    /// Call once to initialize and cache reflection info
    /// </summary>
    public static bool Initialize()
    {
        if (_initialized) return true;

        _wcAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.FullName.Contains("sbm_CoreSystems"));

        if (_wcAssembly == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] WeaponCore assembly not found");
            return false;
        }

        _sessionType = _wcAssembly.GetType("CoreSystems.Session");
        if (_sessionType == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] Session type not found");
            return false;
        }

        var instanceField = _sessionType.GetField("I", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (instanceField == null)
        {
            MyLog.Default.WriteLine("[WCInterop] Session.I field not found");
            return false;
        }
        _sessionInstance = instanceField.GetValue(null);;
        if (_sessionInstance == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] Session.I instance is null");
            return false;
        }

        _playerDummyTargetsField = _sessionType.GetField("PlayerDummyTargets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (_playerDummyTargetsField == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PlayerDummyTargets property not found");
            return false;
        }

        _playerIdField = _sessionType.GetField("PlayerId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (_playerIdField == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PlayerId property not found");
            return false;
        }

        _tickField = _sessionType.GetField("Tick", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (_tickField == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] Tick property not found");
            return false;
        }

        var playerDummyTargetsInstance = _playerDummyTargetsField.GetValue(_sessionInstance);
        if (playerDummyTargetsInstance == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PlayerDummyTargets instance is null");
            return false;
        }

        var dictType = playerDummyTargetsInstance.GetType();
        _playerDummyTargetsTryGetValueMethod = dictType.GetMethod("TryGetValue");
        if (_playerDummyTargetsTryGetValueMethod == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] TryGetValue method not found on PlayerDummyTargets");
            return false;
        }

        _initialized = true;
        MyLog.Default.WriteLine("[TargetCamera.WcInterop] Initialization successful");
        return true;
    }

    /// <summary>
    /// Call to update the PaintedTarget with given position and grid
    /// </summary>
    public static bool UpdatePaintedTarget(Vector3D position, MyCubeGrid grid)
    {
        if (!_initialized)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] Not initialized. Call Initialize() first.");
            return false;
        }

        var playerId = _playerIdField.GetValue(_sessionInstance);
        if (playerId == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PlayerId is null");
            return false;
        }

        var playerDummyTargets = _playerDummyTargetsField.GetValue(_sessionInstance);
        if (playerDummyTargets == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PlayerDummyTargets is null");
            return false;
        }

        object boxedValue = null;
        var parameters = new object[] { playerId, boxedValue };
        bool found = (bool)_playerDummyTargetsTryGetValueMethod.Invoke(playerDummyTargets, parameters);
        if (!found)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PaintedTarget not found for playerId");
            return false;
        }

        var paintedTargetInstance = parameters[1];
        if (paintedTargetInstance == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PaintedTarget instance is null");
            return false;
        }

        // First: get the PaintedTarget field from FakeTargets instance
        var paintedTargetField = paintedTargetInstance.GetType().GetField("PaintedTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (paintedTargetField == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PaintedTarget field not found on FakeTargets instance.");
            return false;
        }

// Get the actual FakeTarget instance
        var actualPaintedTarget = paintedTargetField.GetValue(paintedTargetInstance);
        if (actualPaintedTarget == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PaintedTarget value is null.");
            return false;
        }
        
        if (_updateMethod == null)
        {
            _paintedTargetType = actualPaintedTarget.GetType();
            var methods = _paintedTargetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                MyLog.Default.WriteLine($"Method found: {method.Name} Params: {method.GetParameters().Length}");
            }

            _updateMethod = _paintedTargetType.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_updateMethod == null)
            {
                MyLog.Default.WriteLine("[TargetCamera.WcInterop] Update method not found on FakeTarget.");
                return false;
            }
        }
        
        var tick = (uint)_tickField.GetValue(_sessionInstance);
        
        try
        {
            _updateMethod.Invoke(actualPaintedTarget, new object[] { position, (uint)tick, grid, 0L });
            return true;
        }
        catch (Exception ex)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] Exception calling Update: " + ex);
            return false;
        }

    }
    /// <summary>
    /// Gets the LocalPosition of the PaintedTarget
    /// </summary>
    public static Vector3D? GetPaintedTargetLocalPosition()
    {
        if (!_initialized)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] Not initialized. Call Initialize() first.");
            return null;
        }

        var playerId = _playerIdField.GetValue(_sessionInstance);
        if (playerId == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PlayerId is null");
            return null;
        }

        var playerDummyTargets = _playerDummyTargetsField.GetValue(_sessionInstance);
        if (playerDummyTargets == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PlayerDummyTargets is null");
            return null;
        }

        object boxedValue = null;
        var parameters = new object[] { playerId, boxedValue };
        bool found = (bool)_playerDummyTargetsTryGetValueMethod.Invoke(playerDummyTargets, parameters);
        if (!found)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PaintedTarget not found for playerId");
            return null;
        }

        var paintedTargetInstance = parameters[1];
        if (paintedTargetInstance == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PaintedTarget instance is null");
            return null;
        }

        var paintedTargetField = paintedTargetInstance.GetType().GetField("PaintedTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (paintedTargetField == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PaintedTarget field not found on FakeTargets instance.");
            return null;
        }

        var actualPaintedTarget = paintedTargetField.GetValue(paintedTargetInstance);
        if (actualPaintedTarget == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] PaintedTarget value is null.");
            return null;
        }

        var localPosField = actualPaintedTarget.GetType().GetField("LocalPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (localPosField == null)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] LocalPosition field not found on FakeTarget.");
            return null;
        }

        try
        {
            var localPosValue = (Vector3D)localPosField.GetValue(actualPaintedTarget);
            return localPosValue;
        }
        catch (Exception ex)
        {
            MyLog.Default.WriteLine("[TargetCamera.WcInterop] Exception reading LocalPosition: " + ex);
            return null;
        }
    }

}