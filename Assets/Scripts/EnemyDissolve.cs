using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class EnemyDissolve : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public VisualEffect VFXGraph;
    public float dissolveRate = 0.0125f;
    public float refreshRate = 0.025f;

    private Material[] skinnedMaterials;
    private bool isDissolving = false;

    private void Awake()
    {
        if (skinnedMesh != null)
        {
            skinnedMaterials = skinnedMesh.materials;
        }
        else
        {
            Debug.LogError("Skinned Mesh is not assigned!", this);
        }

        if (VFXGraph != null)
        {
            VFXGraph.Stop();
            VFXGraph.gameObject.SetActive(false);
        }
    }

    public void StartDissolve()
    {
        if (!isDissolving)
        {
            StartCoroutine(DissolveCo());
        }
    }

    private IEnumerator DissolveCo()
    {
        if (skinnedMaterials == null || skinnedMaterials.Length == 0)
        {
            Debug.LogWarning(
                "No materials found on the SkinnedMeshRenderer.", this);
            yield break;
        }

        if (dissolveRate <= 0f || refreshRate <= 0f)
        {
            Debug.LogError(
                "Dissolve Rate and Refresh Rate must be greater than zero.",
                this);
            yield break;
        }

        isDissolving = true;

        if (VFXGraph != null)
        {
            VFXGraph.gameObject.SetActive(true);
            VFXGraph.Reinit();
            VFXGraph.Play();
        }

        float counter = 0f;

        while (counter < 1f)
        {
            counter = Mathf.Min(counter + dissolveRate, 1f);

            foreach (Material material in skinnedMaterials)
            {
                if (material != null &&
                    material.HasProperty("_DissolveAmount"))
                {
                    material.SetFloat("_DissolveAmount", counter);
                }
            }

            yield return new WaitForSeconds(refreshRate);
        }
    }
}