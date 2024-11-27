using System.Collections.Generic;
using Meta.XR.ImmersiveDebugger;
using Meta.XR.MRUtilityKit;
using Meta.XR.MultiplayerBlocks.Shared;
using Unity.Netcode;
using UnityEngine;

public class ColocatedAreaSetup : NetworkBehaviour
{
    [SerializeField] private ColocationController colocationController;
    [SerializeField] private GameObject spawnedNetworkObjectPrefab;
    [SerializeField] private Vector3 spawnedNetworkObjectOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private int spawnAmount = 2;

    [DebugMember(Category = "ServerInformation")]
    private bool isColocationReady;

    [DebugMember(Category = "ServerInformation")]
    private bool tableWasFound;
    
    void Start()
    {
        colocationController.ColocationReadyCallbacks.AddListener(() =>
        {
            if (IsHost)
            {
                FindLargestTable();
                isColocationReady = true;
            }
        });    
    }

    void FindLargestTable()
    {
        List<GameObject> targets = new();

        for (int i = 0; i < spawnAmount; i++)
        {
            var instance = Instantiate(spawnedNetworkObjectPrefab);
            var instancePhysics = instance.GetComponent<Rigidbody>();
            instancePhysics.isKinematic = true;
            var instanceNetworkObject = instance.GetComponent<NetworkObject>();
            instanceNetworkObject.Spawn();
            targets.Add(instance);
        }

        var largestTableSurface = MRUK.Instance?.GetCurrentRoom()?
            // TODO - change this later on to be passed in as an option
            .FindLargestSurface(MRUKAnchor.SceneLabels.TABLE);

        if (largestTableSurface != null)
        {
            if (targets.Count > 0)
            {
                var placement = largestTableSurface.transform.position + spawnedNetworkObjectOffset;
                foreach (var target in targets)
                {
                    target.transform.localPosition = placement;
                    var targetPhysics = target.GetComponent<Rigidbody>();
                    targetPhysics.isKinematic = false;
                }
            }
            tableWasFound = true;
        }
    }
}
