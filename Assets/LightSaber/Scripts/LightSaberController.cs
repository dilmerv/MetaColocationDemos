using System.Collections;
using Oculus.Interaction;
using UnityEngine;
using Unity.Netcode;

public class LightSaberController : NetworkBehaviour
{
    [SerializeField] private float minimumSfxPlayThreshold = 0.5f;
    [SerializeField] private float lightSaberRayFadeDuration = 5.0f;
    [SerializeField] private MeshRenderer lightSaberRayRenderer;
    
    // light saber 
    [SerializeField] private GrabInteractable grabInteractable;

    private LightSaberAudioManager audioManager;
    private Material saberRayMaterial;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private Grabbable grabbable;
    
    void Start()
    {
        audioManager = GetComponent<LightSaberAudioManager>();
        grabbable = grabInteractable.PointableElement as Grabbable;
        // index 2 is the SaberRay material
        saberRayMaterial = lightSaberRayRenderer.materials[2];
        saberRayMaterial.SetColor(BaseColor, Color.clear);
        grabbable.WhenPointerEventRaised += GrabbableOnWhenPointerEventRaised;
    }

    private void GrabbableOnWhenPointerEventRaised(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type == PointerEventType.Select)
        {
            LightSaberEffectServerRpc(true);
        }
        else if (pointerEvent.Type == PointerEventType.Unselect)
        {
            LightSaberEffectServerRpc(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LightSaberEffectServerRpc(bool fadeIn)
    {
        // Change the material on the host
        StartCoroutine(fadeIn ? FadeInAlpha() : FadeOutAlpha());
        audioManager.PlayLightSaberStateSound(fadeIn);
        
        // Propagate the material change to all clients
        LightSaberEffectClientRpc(fadeIn);
    }

    [ClientRpc]
    private void LightSaberEffectClientRpc(bool fadeIn)
    {
        StartCoroutine(fadeIn ? FadeInAlpha() : FadeOutAlpha());
        audioManager.PlayLightSaberStateSound(fadeIn);
    }
    
    private IEnumerator FadeInAlpha()
    {
        float elapsedTime = 0.0f;
        Color baseColor = saberRayMaterial.color;
        while (baseColor.a < 1)
        {
            yield return new WaitForEndOfFrame(); // Wait until the end of the frame
            elapsedTime += Time.deltaTime;
            baseColor.a = Mathf.Clamp01(elapsedTime / lightSaberRayFadeDuration); // Scale the alpha based on elapsed time
            saberRayMaterial.SetColor(BaseColor, baseColor);
        }

        // Ensure the alpha is fully set to 1 at the end of the coroutine
        baseColor.a = 1;
        saberRayMaterial.SetColor(BaseColor, baseColor);
    }
    
    private IEnumerator FadeOutAlpha()
    {
        float elapsedTime = 0.0f;
        Color baseColor = saberRayMaterial.color;
        while (baseColor.a > 0)
        {
            yield return new WaitForEndOfFrame();
            elapsedTime += Time.deltaTime;
            baseColor.a = Mathf.Clamp01(1 - (elapsedTime / lightSaberRayFadeDuration));
            saberRayMaterial.SetColor(BaseColor, baseColor);
        }

        baseColor.a = 0;
        saberRayMaterial.SetColor(BaseColor, baseColor);
    }
    
    private void OnDestroy()
    {
        grabbable.WhenPointerEventRaised -= GrabbableOnWhenPointerEventRaised;
    }
}
