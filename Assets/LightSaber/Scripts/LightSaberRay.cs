using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class LightSaberRay : NetworkBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private GameObject onHitParticleEffectPrefab;
    [SerializeField] private float rayOffset = 0.25f;
    [SerializeField] private float rayMaxDistance = 0.6f;
    [SerializeField] private float rayRadius = 0.025f;
    [SerializeField] private LayerMask includedLayers;
    
    private bool collisionStarted;
    private LightSaberAudioManager lightSaberAudioManager;
    
    [SerializeField]
    private bool collisionAllowed;

    public UnityEvent<Vector3> OnLightSaberRayHit = new();
    
    private void Start()
    {
        lightSaberAudioManager = GetComponent<LightSaberAudioManager>();
        grabbable.WhenPointerEventRaised += GrabbableOnWhenPointerEventRaised;
    }

    private void GrabbableOnWhenPointerEventRaised(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type == PointerEventType.Select)
        {
            collisionAllowed = true;
        }
        else if (pointerEvent.Type == PointerEventType.Unselect)
        {
            collisionAllowed = false;
        }
    }

    private void Update()
    {
        if (!collisionAllowed) return;

        if (Physics.SphereCast(transform.position + (transform.forward * rayOffset), rayRadius,
                transform.forward, out RaycastHit hitInfo, rayMaxDistance, includedLayers))
        {
            if (!collisionStarted)
            {
                SpawnLightSaberEffectServerRpc(hitInfo.point);
                OnLightSaberRayHit.Invoke(hitInfo.point);
            }
            collisionStarted = true;
        }
        else
            collisionStarted = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position + (transform.forward * rayOffset), rayRadius);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnLightSaberEffectServerRpc(Vector3 position)
    {
        // Change the material on the host
        var instance = Instantiate(onHitParticleEffectPrefab, position, Quaternion.identity);
        var instanceNetworkObject = instance.GetComponent<NetworkObject>();
        instanceNetworkObject.Spawn();
        
        PlaySparkSoundClientRpc();
    }
    
    [ClientRpc]
    private void PlaySparkSoundClientRpc()
    {
        lightSaberAudioManager.PlayLightSaberSparkSound();
    }
}
