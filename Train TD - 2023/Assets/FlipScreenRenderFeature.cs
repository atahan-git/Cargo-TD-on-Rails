using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FlipScreenRenderFeature : ScriptableRendererFeature
{

	public Shader m_Shader;
	public float m_Intensity;

	Material m_Material;

	ColorBlitPass m_RenderPass;

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (renderingData.cameraData.cameraType == CameraType.Game)
		{
			// Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
			// ensures that the opaque texture is available to the Render Pass.
			m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
			m_RenderPass.SetIntensity(m_Intensity);
			renderer.EnqueuePass(m_RenderPass);
		}
	}

	public override void Create()
	{
		m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
		m_RenderPass = new ColorBlitPass(m_Material);
	}

	protected override void Dispose(bool disposing)
	{
		CoreUtils.Destroy(m_Material);
	}


	class ColorBlitPass  : ScriptableRenderPass {
		ProfilingSampler m_ProfilingSampler = new ProfilingSampler("ColorBlit");
		Material m_Material;
		float m_Intensity;

		public ColorBlitPass (Material material)
		{
			m_Material = material;
			renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
		}

		public void SetIntensity(float intensity)
		{
			m_Intensity = intensity;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var camera = renderingData.cameraData.camera;
			if (camera.cameraType != CameraType.Game)
				return;

			if (m_Material == null)
				return;

			CommandBuffer cmd = CommandBufferPool.Get();
			using (new ProfilingScope(cmd, m_ProfilingSampler))
			{
				m_Material.SetFloat("_Intensity", m_Intensity);

				//The RenderingUtils.fullscreenMesh argument specifies that the mesh to draw is a quad.
				cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
			}
			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();

			CommandBufferPool.Release(cmd);
		}
	}
}
