using UnityEngine;
using UnityEngine.Rendering;

namespace XiaosuPet
{
    /// <summary>
    /// Runtime-generated orange disc. No imported art, no textures, no extra packages.
    /// Disc diameter is half of the 200 DIP window height (100 DIP), centered.
    /// </summary>
    public sealed class PetVisual : MonoBehaviour
    {
        private const int Segments = 96;
        private const float DiscRadiusWorld = 0.25f;
        private const float FlashSeconds = 0.18f;
        private const float HopSeconds = 0.14f;
        private const float HopHeightWorld = 0.02f; // 4 DIP at 200 DIP = 1 world unit

        private MeshRenderer _renderer;
        private Material _material;
        private Color _baseColor;
        private float _flashTimer;
        private float _hopTimer;
        private Vector3 _baseLocalPosition;

        private void Awake()
        {
            _baseColor = PetConfig.PetColor;
            _baseLocalPosition = transform.localPosition;

            var filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = BuildDiscMesh(DiscRadiusWorld, Segments);

            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            var shader = Resources.Load<Shader>("Shaders/PetUnlitAlpha");
            if (shader == null)
            {
                shader = Shader.Find("Xiaosu/PetUnlitAlpha");
            }
            if (shader == null)
            {
                Debug.LogError("[Xiaosu] PetUnlitAlpha shader not found.");
                shader = Shader.Find("Unlit/Color");
            }

            _material = new Material(shader);
            _material.SetColor("_Color", _baseColor);
            _renderer.material = _material;
        }

        public void Flash()
        {
            _flashTimer = FlashSeconds;
            _hopTimer = HopSeconds;
        }

        public void Tick(float deltaTime)
        {
            if (_material == null) return;

            if (_flashTimer > 0f)
            {
                _flashTimer -= deltaTime;
                float t = Mathf.Clamp01(_flashTimer / FlashSeconds);
                _material.SetColor("_Color", Color.Lerp(_baseColor, PetConfig.PetFlashColor, t));
            }
            else
            {
                _material.SetColor("_Color", _baseColor);
            }

            if (_hopTimer > 0f)
            {
                _hopTimer -= deltaTime;
                float t = 1f - Mathf.Clamp01(_hopTimer / HopSeconds);
                float y = Mathf.Sin(Mathf.PI * t) * HopHeightWorld;
                transform.localPosition = _baseLocalPosition + new Vector3(0f, y, 0f);
            }
            else
            {
                transform.localPosition = _baseLocalPosition;
            }
        }

        private static Mesh BuildDiscMesh(float radius, int segments)
        {
            var vertices = new Vector3[segments + 2];
            var colors = new Color32[segments + 2];
            var triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            colors[0] = new Color32(255, 255, 255, 255);

            for (int i = 0; i <= segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                colors[i + 1] = new Color32(255, 255, 255, 255);
            }

            for (int i = 0; i < segments; i++)
            {
                int idx = i * 3;
                triangles[idx] = 0;
                triangles[idx + 1] = i + 1;
                triangles[idx + 2] = i + 2;
            }

            var mesh = new Mesh { name = "PetDisc" };
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
