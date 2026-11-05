
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class WallTextureVariation : MonoBehaviour
{
    [Header("Texture Settings")]
    public string textureProperty = "_BaseMap";

    [Header("Random Offset Range")]
    public Vector2 offsetX = new Vector2(0f, 10f);
    public Vector2 offsetY = new Vector2(0f, 10f);

    private Renderer wallRenderer;
    private MaterialPropertyBlock propertyBlock;

    private int textureSTProperty;
    private int randomSeed;

    private void OnEnable()
    {
        Initialize();
        ApplyTextureVariation();
    }

    private void OnValidate()
    {
        Initialize();
        ApplyTextureVariation();
    }

    private void Update()
    {
        // Keep texture tiling synchronized with the original material.
        ApplyTextureVariation();
    }

    private void Initialize()
    {
        if (wallRenderer == null)
            wallRenderer = GetComponent<Renderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        textureSTProperty = Shader.PropertyToID(
            textureProperty + "_ST"
        );

        // Stable random offset for this wall.
        randomSeed = gameObject.GetInstanceID();
    }

    private void ApplyTextureVariation()
    {
        if (wallRenderer == null)
            return;

        Material material = wallRenderer.sharedMaterial;

        if (material == null ||
            !material.HasProperty(textureProperty))
        {
            return;
        }

        // Read tiling directly from the shared original material.
        Vector2 tiling = material.GetTextureScale(textureProperty);

        // Generate a stable random offset for this wall.
        Random.InitState(randomSeed);

        float x = Random.Range(offsetX.x, offsetX.y);
        float y = Random.Range(offsetY.x, offsetY.y);

        Vector4 textureST = new Vector4(
            tiling.x,
            tiling.y,
            x,
            y
        );

        wallRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetVector(
            textureSTProperty,
            textureST
        );

        wallRenderer.SetPropertyBlock(propertyBlock);
    }
}
