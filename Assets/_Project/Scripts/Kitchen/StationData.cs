using System.Collections.Generic;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Kitchen
{
    // A machine that turns items into others over time, like the mill.
    [CreateAssetMenu(menuName = "Moonbrew/Kitchen/Station", fileName = "SO_Station_")]
    public class StationData : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. station_mill. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ProcessData[] processes = new ProcessData[0];
        [Tooltip("Batches taken at once, e.g. 10 wheat for the mill.")]
        [SerializeField, Min(1)] private int capacity = 10;
        [Tooltip("A click this close to the station uses it.")]
        [SerializeField, Min(0.1f)] private float clickRadius = 0.9f;
        [Tooltip("The player must stand this close to the station.")]
        [SerializeField, Min(0.1f)] private float reach = 2f;
        [Tooltip("Bubble shown above the station: a child named Icon shows what is going on.")]
        [SerializeField] private GameObject bubblePrefab;
        [Tooltip("Icon in the bubble while a batch is being made; when it is done, the output's icon shows instead.")]
        [SerializeField] private Sprite workingIcon;
        [Tooltip("Gap between the top of the station model and the bottom of its bubble.")]
        [SerializeField, Min(0f)] private float bubbleGap = 0.1f;

        public string Id => id;

        public string DisplayName => displayName;

        public IReadOnlyList<ProcessData> Processes => processes;

        public int Capacity => capacity;

        public float ClickRadius => clickRadius;

        public float Reach => reach;

        public GameObject BubblePrefab => bubblePrefab;

        public Sprite WorkingIcon => workingIcon;

        public float BubbleGap => bubbleGap;

        // For stations built from code.
        public void Init(string stationId, ProcessData[] stationProcesses, int batches, GameObject bubble = null, Sprite working = null)
        {
            id = stationId;
            processes = stationProcesses ?? new ProcessData[0];
            capacity = batches;
            bubblePrefab = bubble;
            workingIcon = working;
        }

        // The process that takes this item, or null.
        public ProcessData ProcessFor(ItemData item)
        {
            if (item == null)
            {
                return null;
            }

            foreach (var process in processes)
            {
                if (process != null && process.Input == item)
                {
                    return process;
                }
            }

            return null;
        }

        public ProcessData FindProcess(string processId)
        {
            foreach (var process in processes)
            {
                if (process != null && !string.IsNullOrEmpty(processId) && process.Id == processId)
                {
                    return process;
                }
            }

            return null;
        }
    }
}
