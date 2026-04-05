using System.IO;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class SpellCombinationEditorWindow : EditorWindow
    {
        // Spell Elements
        private ElementType selectedElementA;
        private ElementType selectedElementB;
        // Selected Spell Data
        private SpellFXData selectedFXData;
        
        // Utilities
        private Vector2 scrollPos;
        private string saveFolderPath = "Assets/Data/Spells";

        // Draw Window
        [MenuItem("Tools/Rekindled/Spell Combination Tool")]
        public static void ShowWindow()
        {
            GetWindow<SpellCombinationEditorWindow>("Spell Combo Tool");
        }

        private void OnGUI()
        {
            // Titles
            EditorGUILayout.LabelField("Rekindled Spell Combination Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create and manage spell combinations", MessageType.Info);

            GUILayout.Space(10);

            // Spell Element Selection
            EditorGUILayout.LabelField("Elements", EditorStyles.boldLabel);
            selectedElementA = (ElementType)EditorGUILayout.EnumPopup("Element A", selectedElementA);
            selectedElementB = (ElementType)EditorGUILayout.EnumPopup("Element B", selectedElementB);
            
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            // Find Spell Combination SO
            if (GUILayout.Button("Find Combination", GUILayout.Height(30)))
            {
                FindCombination();
            }

            // Create new Combination SO
            if (GUILayout.Button("Create New", GUILayout.Height(30)))
            {
                CreateNewCombination();
            }

            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);

            // Current element selected
            EditorGUILayout.LabelField("Current Selection", EditorStyles.boldLabel);

            if (selectedFXData != null)
            {
                EditorGUILayout.ObjectField("Loaded Data", selectedFXData, typeof(SpellFXData), false);

                if (HasDuplicate(selectedElementA, selectedElementB, selectedFXData))
                {
                    EditorGUILayout.HelpBox("Warning: Another combination with the same elements already exists.", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.LabelField("Loaded Data", "None");
            }
            
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (selectedFXData != null)
            {
                DrawSpellEditor();
            }
            else
            {
                EditorGUILayout.HelpBox("No spell combination selected. Search for one or create a new one.", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        // Draw Spell Data
        private void DrawSpellEditor()
        {
            EditorGUILayout.LabelField("Spell Data", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // Check if changes have been made in the GUI content
            EditorGUI.BeginChangeCheck();

            // Fill data
            selectedFXData.elementA = selectedElementA;
            selectedFXData.elementB = selectedElementB;

            selectedFXData.spellName = EditorGUILayout.TextField("Spell Name", selectedFXData.spellName);
            selectedFXData.description = EditorGUILayout.TextArea(selectedFXData.description, GUILayout.MinHeight(60));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);

            selectedFXData.vfxPrefab = (GameObject)EditorGUILayout.ObjectField("VFX Prefab", selectedFXData.vfxPrefab, typeof(GameObject), false);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);

            selectedFXData.castSFX = (AudioClip)EditorGUILayout.ObjectField("Cast SFX", selectedFXData.castSFX, typeof(AudioClip), false);

            GUILayout.Space(15);

            DrawActionButtons();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedFXData);
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
            {
                SaveChanges();
            }

            if (GUILayout.Button("Preview Spell", GUILayout.Height(30)))
            {
                PreviewSpell();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button("Ping Asset"))
            {
                EditorGUIUtility.PingObject(selectedFXData);
                Selection.activeObject = selectedFXData;
            }
        }

        /// <summary>
        /// Finds a Spell SO
        /// </summary>
        private void FindCombination()
        {
            // Data String
            string[] guids = AssetDatabase.FindAssets("t:SpellFXData");
            SpellFXData found = null;

            foreach (string guid in guids)
            {
                // Find asset in data
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SpellFXData fxData = AssetDatabase.LoadAssetAtPath<SpellFXData>(path);

                // If data, save it and break loop
                if (fxData != null && fxData.Matches(selectedElementA, selectedElementB))
                {
                    found = fxData;
                    break;
                }
            }

            // If not, debug
            if (found != null)
            {
                selectedFXData = found;
                Debug.Log($"Found combination: {found.spellName}");
            }
            else
            {
                // If not data found, debug
                selectedFXData = null;
                Debug.Log("No combination found.");
            }
        }

        // Create a new Combination Scriptable Object
        private void CreateNewCombination()
        {
            // Check if SO already exists
            if (HasDuplicate(selectedElementA, selectedElementB))
            {
                Debug.LogWarning($"A combination for {selectedElementA} + {selectedElementB} already exists.");
                return;
            }

            // Check if folder exists
            EnsureFolderExists(saveFolderPath);

            // Create SO and fill data
            SpellFXData newFXData = ScriptableObject.CreateInstance<SpellFXData>();
            newFXData.elementA = selectedElementA;
            newFXData.elementB = selectedElementB;
            newFXData.spellName = $"{selectedElementA}_{selectedElementB}_Spell";

            // Save Spell
            string assetName = $"Spell_{selectedElementA}_{selectedElementB}.asset";
            string fullPath = Path.Combine(saveFolderPath, assetName).Replace("\\", "/");
            AssetDatabase.CreateAsset(newFXData, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Update Window Content
            selectedFXData = newFXData;
            EditorGUIUtility.PingObject(newFXData);
            Selection.activeObject = newFXData;

            Debug.Log($"Created new combination asset at: {fullPath}");
        }

        /// <summary>
        /// Create an instance of the Spell FX (works only on play mode)
        /// </summary>
        private void PreviewSpell()
        {
            // Check if data selected
            if (selectedFXData == null)
            {
                Debug.LogWarning("No spell data selected.");
                return;
            }

            // Check if Unity is in Play Mode
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Preview works only in Play Mode.");
            }

            // Spawn VFX
            if (selectedFXData.vfxPrefab != null)
            {
                GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(selectedFXData.vfxPrefab);

                if (spawned != null)
                {
                    spawned.transform.position = Vector3.zero;
                    spawned.transform.localScale = Vector3.one;

                    if (Application.isPlaying)
                    {
                        Destroy(spawned, 8f);
                    }
                }
            }

            // Play SFX
            if (selectedFXData.castSFX != null)
            {
                if (Application.isPlaying)
                {
                    GameObject tempAudio = new GameObject("TempSpellAudio");
                    AudioSource source = tempAudio.AddComponent<AudioSource>();
                    source.clip = selectedFXData.castSFX;
                    source.Play();

                    Destroy(tempAudio, selectedFXData.castSFX.length + 0.1f);
                }
                else
                {
                    Debug.Log("Audio preview requires Play Mode.");
                }
            }

            Debug.Log($"Previewed spell: {selectedFXData.spellName}");
        }
        
        // Save SO Changes
        private void SaveChanges()
        {
            if (selectedFXData == null) return;

            if (HasDuplicate(selectedFXData.elementA, selectedFXData.elementB, selectedFXData))
            {
                Debug.LogWarning("Cannot save because another combination with the same elements already exists.");
                return;
            }

            EditorUtility.SetDirty(selectedFXData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Saved changes to: {selectedFXData.name}");
        }
        
        // Check if SO already exists
        private bool HasDuplicate(ElementType a, ElementType b, SpellFXData ignoreFXData = null)
        {
            string[] guids = AssetDatabase.FindAssets("t:SpellFXData");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SpellFXData fxData = AssetDatabase.LoadAssetAtPath<SpellFXData>(path);

                if (fxData == null || fxData == ignoreFXData)
                    continue;

                if (fxData.Matches(a, b))
                    return true;
            }

            return false;
        }

        // Ensures the folder that contains the data actually exists and is valid
        private void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}