using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FlipScreenRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class BlitSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
        public Material blitMaterial = null;
    }

    public BlitSettings settings = new BlitSettings();
    SimpleBlitPass blitPass;

    public override void Create()
    {
        blitPass = new SimpleBlitPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!isActive || settings.blitMaterial == null)
        {
            return;
        }
        
        if (renderingData.cameraData.renderType == CameraRenderType.Base)
        {
            blitPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(blitPass);
        }
    }

    public class SimpleBlitPass : ScriptableRenderPass
    {
        private Material blitMaterial;
        private RenderTargetIdentifier source;
        private RenderTargetHandle temporaryColorTexture;

        public SimpleBlitPass(BlitSettings settings)
        {
            this.blitMaterial = settings.blitMaterial;
            this.renderPassEvent = settings.renderPassEvent;
            temporaryColorTexture.Init("_TemporaryColorTexture");
        }

        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(temporaryColorTexture.id, cameraTextureDescriptor);
            ConfigureTarget(temporaryColorTexture.Identifier());
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SimpleBlitPass");

            // Blit to temporary render texture
            Blit(cmd, source, temporaryColorTexture.Identifier(), blitMaterial);
            // Blit back to camera target
            Blit(cmd, temporaryColorTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(temporaryColorTexture.id);
        }
    }
}