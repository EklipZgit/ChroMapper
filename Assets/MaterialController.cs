using UnityEngine;

public class MaterialController : MonoBehaviour
{
	[SerializeField]
	private Material _material;

	[Space]
	[SerializeField]
	private Renderer[] _renderers;

	public Material material => _material;

	protected void OnValidate()
	{
		Renderer[] renderers = _renderers;
		for (int i = 0; i < renderers.Length; i++)
		{
			_ = renderers[i];
		}
	}
}
