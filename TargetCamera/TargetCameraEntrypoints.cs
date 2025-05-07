
using Sandbox.Common;
using Sandbox.ModAPI;
using SETargetCamera;
using VRage.Game.Components;
using VRage.Utils;

[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
public class TargetCameraEntrypoints : MySessionComponentBase
{
    public override void LoadData()
    {
        base.LoadData();
        // This runs when the session is loading (before it's "Ready")
        MyLog.Default.WriteLine("TargetCameraEntrypoints: LoadData called.");
    }

    protected override void UnloadData()
    {
        base.UnloadData();
        // This runs when the session is closing
        TargetCamera.WorldUnload();
        MyLog.Default.WriteLine("TargetCameraEntrypoints: UnloadData called.");
    }

    public override void BeforeStart()
    {
        base.BeforeStart();
        // This runs when the session is "about to be ready"
        TargetCamera.WorldLoad();
        MyLog.Default.WriteLine("TargetCameraEntrypoints: BeforeStart called.");
    }

    public override void UpdateBeforeSimulation()
    {
        TargetCamera.Update();
        // Runs every tick before simulation step
    }

    public override void UpdateAfterSimulation()
    {
        
        // Runs every tick after simulation step
    }
}