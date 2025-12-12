using SharpDX.Mathematics.Interop;
using System.Threading;
using VRage.Render.Scene;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Scene;
using VRage.Render11.Scene.Components;
using VRage.Utils;
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

    // controlled character head is hidden in 1st person view so that the player isn't looking at the insides of the character's face
    // so we 'fix' it for camera views by forcibly making it visible again
    // for some reason not returning the visibility to its original state is fine.
    // also if we do set the vis state back it can make the character head invisible when switching back to 3rd person view
    // we provide a toggle for the fix in the settings in case it breaks modded characters or something
    private static void FixFirstPersonCharacterHead()
    {
        // FPV state is changed on simulation thread but we want to read it from render thread so we use a boxed struct set from Plugin.Update()
        var firstPersonInfo = Volatile.Read(ref Plugin.FirstPersonCharacter); // null if not in FPV
        if (firstPersonInfo != null
            && MyIDTracker<MyActor>.FindByID(firstPersonInfo.BoxedValue.CharacterActorId) is MyActor actor
            && actor.GetRenderable() is MyRenderableComponent renderable)
        {
            string[] disabledMaterials = firstPersonInfo.BoxedValue.MaterialsDisabledInFirst;
            var lods = renderable.Lods;
            var mesh = renderable.Mesh;
            MyRenderableProxyFlags flagsToAdd = MyProxiesFactory.GetRenderableProxyFlags(RenderFlags.Visible);
            MyRenderableProxyFlags flagsToRemove = MyProxiesFactory.GetRenderableProxyFlags(0);
            foreach (string materialName in disabledMaterials)
            {
                MyStringId material = MyStringId.GetOrCompute(materialName);
                for (int j = 0; j < lods.Length; j++)
                {
                    MyRenderLod myRenderLod = lods[j];
                    for (int k = 0; k < myRenderLod.RenderableProxies.Length; k++)
                    {
                        MyRenderableProxy myRenderableProxy = myRenderLod.RenderableProxies[k];
                        if (MyMeshes.GetMeshPart(mesh, j, myRenderableProxy.PartIndex).Info.Material.Info.Name == material)
                        {
                            myRenderableProxy.Flags |= flagsToAdd;
                            myRenderableProxy.Flags &= ~flagsToRemove;
                        }
                    }
                }
            }
        }
    }

    // all profiler calls removed since they don't do anything in the release build of the game
    public static void Draw(IRtvBindable renderTarget)
    {
        IsDrawing = true;

        if (Plugin.Settings.HeadFix)
        {
            FixFirstPersonCharacterHead();
        }

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
