using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Npc
{
    public enum Speaker
    {
        Villager,
        Owner,
    }

    // A villager of the café: their portrait model, what they say when they order, and how serving them builds friendship.
    [CreateAssetMenu(menuName = "Moonbrew/Npc/Villager", fileName = "SO_Villager_")]
    public class VillagerData : ScriptableObject
    {
        [Tooltip("The same ID as the villager's customer, e.g. villager_rose.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Model shown in the dialogue portrait, with its PortraitPoser.")]
        [SerializeField] private GameObject portraitModel;
        [Tooltip("The conversation when they order. {owner} is the owner's name, {dish} the dish they ordered.")]
        [SerializeField] private DialogueLine[] orderLines = new DialogueLine[0];
        [Tooltip("Friendship gained each time the owner serves them herself.")]
        [SerializeField, Min(0)] private int servedPoints = 1;

        public string Id => id;

        public string DisplayName => displayName;

        public GameObject PortraitModel => portraitModel;

        public IReadOnlyList<DialogueLine> OrderLines => orderLines;

        public int ServedPoints => servedPoints;

        // For villagers built from code.
        public void Init(string villagerId, string name, DialogueLine[] lines, int points = 1, GameObject portrait = null)
        {
            id = villagerId;
            displayName = name;
            orderLines = lines ?? new DialogueLine[0];
            servedPoints = points;
            portraitModel = portrait;
        }
    }

    [Serializable]
    public struct DialogueLine
    {
        [SerializeField] private Speaker speaker;
        [SerializeField, TextArea] private string text;
        [Tooltip("ID of the villager's face while this line shows (see SO_PortraitMoods).")]
        [SerializeField] private string expression;
        [Tooltip("ID of the villager's pose while this line shows (see SO_PortraitMoods).")]
        [SerializeField] private string pose;

        public DialogueLine(Speaker speaker, string text, string expression, string pose)
        {
            this.speaker = speaker;
            this.text = text;
            this.expression = expression;
            this.pose = pose;
        }

        public Speaker Speaker => speaker;

        public string Text => text;

        public string Expression => expression;

        public string Pose => pose;
    }
}
