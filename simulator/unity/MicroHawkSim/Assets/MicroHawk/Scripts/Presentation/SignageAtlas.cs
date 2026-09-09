using UnityEngine;

namespace MicroHawk.Presentation
{
    /// <summary>Keeps locally authored depth-tested text bound to Unity's dynamic font atlas.</summary>
    [ExecuteAlways, RequireComponent(typeof(TextMesh))]
    public sealed class SignageAtlas : MonoBehaviour
    {
        private void OnEnable() { Font.textureRebuilt += Refresh; Refresh(GetComponent<TextMesh>().font); }
        private void OnDisable() => Font.textureRebuilt -= Refresh;
        private void Refresh(Font font)
        {
            var text=GetComponent<TextMesh>();
            var renderer=GetComponent<MeshRenderer>();
            if (font && text.font == font && renderer.sharedMaterial && font.material)
                renderer.sharedMaterial.mainTexture=font.material.mainTexture;
        }
    }
}
