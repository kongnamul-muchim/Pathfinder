using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace Pathfinder.Editor
{
    public class PlatformerVillageGenerator : EditorWindow
    {
        private const string TILEMAP_FOLDER = "Assets/2D Pixel Art Platformer - Plain/tilemap/tile assets";
        private const string SPRITES_GUID = "eaa42f295a599ce4cbe0f579a2e668ff";
        
        private int villageWidth = 40;
        private int villageHeight = 15;
        private int groundLevel = 5;
        
        private TileBase[] groundTiles;
        private TileBase[] fenceTiles;
        private TileBase[] bushTiles;
        private TileBase[] plantTiles;
        private TileBase treeStumpTile;
        private TileBase[] stoneTiles;
        private TileBase boxTile;
        private TileBase signTile;
        private TileBase monumentalStoneTile;
        private TileBase spikesTile;
        
        [MenuItem("Pathfinder/Generate Platformer Village")]
        public static void ShowWindow()
        {
            GetWindow<PlatformerVillageGenerator>("Platformer Village Generator");
        }
        
        private void OnEnable()
        {
            LoadTiles();
        }
        
        private void LoadTiles()
        {
            groundTiles = new TileBase[10];
            for (int i = 0; i < 10; i++)
            {
                groundTiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/ground_{i + 1}.asset");
            }
            
            fenceTiles = new TileBase[6];
            for (int i = 0; i < 6; i++)
            {
                fenceTiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/fence_{i + 1}.asset");
            }
            
            bushTiles = new TileBase[4];
            for (int i = 0; i < 4; i++)
            {
                bushTiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/bush_{i + 1}.asset");
            }
            
            plantTiles = new TileBase[6];
            for (int i = 0; i < 6; i++)
            {
                plantTiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/plant_{i + 1}.asset");
            }
            
            treeStumpTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/Tree_stump_1.asset");
            
            stoneTiles = new TileBase[2];
            stoneTiles[0] = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/stone_1.asset");
            stoneTiles[1] = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/stone_2.asset");
            
            boxTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/box_1.asset");
            signTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/sign_1.asset");
            monumentalStoneTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/monumental_stone_1.asset");
            spikesTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TILEMAP_FOLDER}/spikes_1.asset");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Platformer Village Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            villageWidth = EditorGUILayout.IntField("Village Width", villageWidth);
            villageHeight = EditorGUILayout.IntField("Village Height", villageHeight);
            groundLevel = EditorGUILayout.IntField("Ground Level", groundLevel);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Generate Village", GUILayout.Height(40)))
            {
                GenerateVillage();
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Clear Village", GUILayout.Height(30)))
            {
                ClearVillage();
            }
        }
        
        private void GenerateVillage()
        {
            GameObject gridObject = GameObject.Find("VillageGrid");
            if (gridObject == null)
            {
                gridObject = new GameObject("VillageGrid");
                gridObject.AddComponent<Grid>();
            }
            
            Grid grid = gridObject.GetComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);
            
            // Create or clear tilemaps
            Tilemap groundTilemap = GetOrCreateTilemap(gridObject, "Ground", 0);
            Tilemap decorationTilemap = GetOrCreateTilemap(gridObject, "Decorations", 1);
            Tilemap platformTilemap = GetOrCreateTilemap(gridObject, "Platforms", 2);
            
            groundTilemap.ClearAllTiles();
            decorationTilemap.ClearAllTiles();
            platformTilemap.ClearAllTiles();
            
            System.Random random = new System.Random();
            
            // Generate ground layer
            for (int x = -5; x < villageWidth + 5; x++)
            {
                // Create thick ground
                for (int y = groundLevel - 3; y <= groundLevel; y++)
                {
                    TileBase tile = groundTiles[random.Next(groundTiles.Length)];
                    if (y == groundLevel)
                    {
                        // Top layer - use grassier tiles
                        tile = groundTiles[random.Next(0, 4)];
                    }
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
            
            // Generate floating platforms
            int platformCount = villageWidth / 8;
            for (int i = 0; i < platformCount; i++)
            {
                int platformX = random.Next(5, villageWidth - 5);
                int platformY = groundLevel + random.Next(3, 7);
                int platformWidth = random.Next(3, 6);
                
                for (int x = 0; x < platformWidth; x++)
                {
                    TileBase tile = groundTiles[random.Next(0, 4)];
                    platformTilemap.SetTile(new Vector3Int(platformX + x, platformY, 0), tile);
                }
            }
            
            // Place houses (represented by groups of tiles)
            int houseCount = Mathf.Max(2, villageWidth / 12);
            List<int> housePositions = new List<int>();
            
            for (int i = 0; i < houseCount; i++)
            {
                int houseX = random.Next(8, villageWidth - 8);
                int houseWidth = random.Next(4, 7);
                int houseHeight = random.Next(3, 5);
                
                // Check if position is valid
                bool validPosition = true;
                foreach (int pos in housePositions)
                {
                    if (Mathf.Abs(pos - houseX) < 8)
                    {
                        validPosition = false;
                        break;
                    }
                }
                
                if (!validPosition) continue;
                housePositions.Add(houseX);
                
                // Build house base using ground tiles
                for (int x = 0; x < houseWidth; x++)
                {
                    for (int y = 0; y < houseHeight; y++)
                    {
                        int worldX = houseX + x;
                        int worldY = groundLevel + 1 + y;
                        
                        // House walls
                        if (x == 0 || x == houseWidth - 1 || y == houseHeight - 1)
                        {
                            TileBase tile = groundTiles[random.Next(4, 8)];
                            decorationTilemap.SetTile(new Vector3Int(worldX, worldY, 0), tile);
                        }
                        else
                        {
                            // House interior - empty or window
                            if (y == 1 && (x == 2 || x == houseWidth - 3))
                            {
                                // Window
                                decorationTilemap.SetTile(new Vector3Int(worldX, worldY, 0), boxTile);
                            }
                        }
                    }
                }
                
                // Door
                int doorX = houseX + houseWidth / 2;
                decorationTilemap.SetTile(new Vector3Int(doorX, groundLevel + 1, 0), null);
            }
            
            // Place fences
            for (int x = 0; x < villageWidth; x += random.Next(3, 6))
            {
                if (random.Next(2) == 0)
                {
                    TileBase fence = fenceTiles[random.Next(fenceTiles.Length)];
                    decorationTilemap.SetTile(new Vector3Int(x, groundLevel + 1, 0), fence);
                }
            }
            
            // Place trees (tree stumps)
            int treeCount = villageWidth / 6;
            for (int i = 0; i < treeCount; i++)
            {
                int treeX = random.Next(2, villageWidth - 2);
                bool nearHouse = false;
                foreach (int pos in housePositions)
                {
                    if (Mathf.Abs(pos - treeX) < 5)
                    {
                        nearHouse = true;
                        break;
                    }
                }
                
                if (!nearHouse && treeStumpTile != null)
                {
                    decorationTilemap.SetTile(new Vector3Int(treeX, groundLevel + 1, 0), treeStumpTile);
                }
            }
            
            // Place bushes
            int bushCount = villageWidth / 3;
            for (int i = 0; i < bushCount; i++)
            {
                int bushX = random.Next(0, villageWidth);
                TileBase bush = bushTiles[random.Next(bushTiles.Length)];
                decorationTilemap.SetTile(new Vector3Int(bushX, groundLevel + 1, 0), bush);
            }
            
            // Place plants
            int plantCount = villageWidth / 4;
            for (int i = 0; i < plantCount; i++)
            {
                int plantX = random.Next(0, villageWidth);
                TileBase plant = plantTiles[random.Next(plantTiles.Length)];
                decorationTilemap.SetTile(new Vector3Int(plantX, groundLevel + 1, 0), plant);
            }
            
            // Place stones
            int stoneCount = villageWidth / 8;
            for (int i = 0; i < stoneCount; i++)
            {
                int stoneX = random.Next(0, villageWidth);
                TileBase stone = stoneTiles[random.Next(stoneTiles.Length)];
                decorationTilemap.SetTile(new Vector3Int(stoneX, groundLevel + 1, 0), stone);
            }
            
            // Place boxes and signs
            for (int i = 0; i < 3; i++)
            {
                int boxX = random.Next(5, villageWidth - 5);
                if (boxTile != null)
                    decorationTilemap.SetTile(new Vector3Int(boxX, groundLevel + 1, 0), boxTile);
            }
            
            // Place sign near entrance
            if (signTile != null)
            {
                decorationTilemap.SetTile(new Vector3Int(2, groundLevel + 1, 0), signTile);
            }
            
            // Place monumental stone
            if (monumentalStoneTile != null)
            {
                int monumentX = villageWidth / 2;
                decorationTilemap.SetTile(new Vector3Int(monumentX, groundLevel + 1, 0), monumentalStoneTile);
            }
            
            EditorUtility.DisplayDialog("Success", "Platformer village generated successfully!", "OK");
        }
        
        private Tilemap GetOrCreateTilemap(GameObject gridObject, string name, int sortingOrder)
        {
            Transform tilemapTransform = gridObject.transform.Find(name);
            GameObject tilemapObject;
            
            if (tilemapTransform == null)
            {
                tilemapObject = new GameObject(name);
                tilemapObject.transform.SetParent(gridObject.transform);
                tilemapObject.transform.localPosition = Vector3.zero;
            }
            else
            {
                tilemapObject = tilemapTransform.gameObject;
            }
            
            Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                tilemap = tilemapObject.AddComponent<Tilemap>();
            }
            
            TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = tilemapObject.AddComponent<TilemapRenderer>();
            }
            renderer.sortingOrder = sortingOrder;
            
            return tilemap;
        }
        
        private void ClearVillage()
        {
            GameObject gridObject = GameObject.Find("VillageGrid");
            if (gridObject != null)
            {
                foreach (Transform child in gridObject.transform)
                {
                    Tilemap tilemap = child.GetComponent<Tilemap>();
                    if (tilemap != null)
                    {
                        tilemap.ClearAllTiles();
                    }
                }
                EditorUtility.DisplayDialog("Cleared", "Village cleared successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "No village found to clear.", "OK");
            }
        }
    }
}
