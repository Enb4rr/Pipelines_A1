using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "Spell", menuName = "Rekindled/DevTools/Spell")]
    public class SpellFXData : ScriptableObject
    {
        [Header("Elements")]
        public ElementType elementA;
        public ElementType elementB;

        [Header("Spell Info")]
        public string spellName;
        [TextArea(3, 5)] public string description;

        [Header("Visuals")]
        public GameObject vfxPrefab;

        [Header("Audio")]
        public AudioClip castSFX;
        
        public bool Matches(ElementType a, ElementType b)
        {
            return (elementA == a && elementB == b) ||
                   (elementA == b && elementB == a);
        }
    }
    
    public enum ElementType
    {
        Flame,
        Feather,
        Ash,
    }
}
