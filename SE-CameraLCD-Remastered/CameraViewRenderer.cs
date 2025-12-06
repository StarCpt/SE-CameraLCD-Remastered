using SharpDX.Mathematics.Interop;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace CameraLCD;

public static class CameraViewRenderer
{
    public static bool IsDrawing { get; private set; }

    private static MyRenderContext RC => MyRender11.RC;
    private static ref MyRenderSettings Settings => ref MyRender11.Settings;
    private static ref MyPostprocessSettings Postprocess => ref MyRender11.Postprocess;
    private static MyRenderDebugOverrides DebugOverrides => MyRender11.DebugOverrides;
    private static Vector2I ResolutionI => MyRender11.ResolutionI;

    private static void PrepareGameScene()
    {
        //MyManagers.EnvironmentProbe.UpdateProbe();
        MyCommon.UpdateFrameConstants();
        MyCommon.VoxelMaterialsConstants.FeedGPU();
        //MyOffscreenRenderer.Render();
    }

    // all profiler calls removed since they don't do anything in the release build of the game
    public static void Draw(IRtvBindable renderTarget)
    {
        IsDrawing = true;

        PrepareGameScene();
        RC.ClearState();

        MyManagers.RenderScheduler.Init();
        MyManagers.RenderScheduler.Execute();
        MyManagers.RenderScheduler.Done();

        MyManagers.Ansel.MarkHdrBufferFinished(); // see if this can be removed

        RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

        if (Postprocess.EnableEyeAdaptation)
        {
            MyEyeAdaptation.Run(RC, MyGBuffer.Main.LBuffer, false, out _);
        }
        else
        {
            MyEyeAdaptation.ConstantExposure(RC);
        }

        IBorrowedRtvTexture rtvBloom;
        if (DebugOverrides.Postprocessing && DebugOverrides.Bloom && Postprocess.BloomEnabled)
        {
            rtvBloom = MyModernBloom.Run(RC, MyGBuffer.Main.LBuffer, MyGBuffer.Main.GBuffer2, MyGBuffer.Main.ResolvedDepthStencil.SrvDepth, MyEyeAdaptation.GetExposure());
        }
        else
        {
            rtvBloom = MyManagers.RwTexturesPool.BorrowRtv("bloom_EightScreenUavHDR", ResolutionI.X / 8, ResolutionI.Y / 8, MyGBuffer.LBufferFormat);
            RC.ClearRtv(rtvBloom, default);
        }

        bool enableTonemapping = Postprocess.EnableTonemapping && DebugOverrides.Postprocessing && DebugOverrides.Tonemapping;
        IBorrowedCustomTexture postprocessResult = MyToneMapping.Run(MyGBuffer.Main.LBuffer, MyEyeAdaptation.GetExposure(), rtvBloom, enableTonemapping, Postprocess.DirtTexture, MyRender11.FxaaEnabled);
        rtvBloom.Release();

        // disable block highlighting in camera feed
        //if (MyHighlight.HasHighlights && !MyManagers.Ansel.IsSessionRunning)
        //{
        //    MyHighlight.Run(RC, postprocessResult.Linear, null);
        //}

        if (Settings.DrawBillboards && Settings.DrawBillboardsLDR)
        {
            MyBillboardRenderer.RenderLDR(RC, MyGBuffer.Main.ResolvedDepthStencil.SrvDepth, postprocessResult.SRgb);
        }

        if (MyRender11.FxaaEnabled)
        {
            IBorrowedCustomTexture fxaaResult = MyManagers.RwTexturesPool.BorrowCustom("MyRender11.FXAA.Rgb8");
            MyFXAA.Run(RC, fxaaResult.Linear, postprocessResult.Linear);
            postprocessResult.Release();
            postprocessResult = fxaaResult;
        }

        // chromatic aberration and vignette are disabled in camera feed

        if (Settings.DrawBillboards && Settings.DrawBillboardsPostPP)
        {
            MyBillboardRenderer.RenderPostPP(RC, MyGBuffer.Main.ResolvedDepthStencil.SrvDepth, postprocessResult.SRgb);
        }

        RC.ClearRtv(renderTarget, new RawColor4(0, 0, 0, 0)); // don't remove, needed to ensure 0 alpha (TODO: use custom blend state to write 0 alpha)
        CopyReplaceNoAlpha(postprocessResult.SRgb, renderTarget);

        postprocessResult.Release();
        MyManagers.Cull.OnFrameEnd();

        IsDrawing = false;
    }

    private static void CopyReplaceNoAlpha(ISrvBindable source, IRtvBindable destination)
    {
        MyRender11.RC.SetBlendState(MyBlendStateManager.BlendReplaceNoAlphaChannel);

        MyRender11.RC.SetInputLayout(null);
        MyRender11.RC.PixelShader.Set(MyCopyToRT.CopyPs);

        MyRender11.RC.SetRtv(destination);
        MyRender11.RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
        MyRender11.RC.PixelShader.SetSrv(0, source);
        MyScreenPass.DrawFullscreenQuad(MyRender11.RC, new MyViewport(destination.Size));
    }

}
