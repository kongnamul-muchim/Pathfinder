using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

namespace Pathfinder.Editor
{
    /// <summary>
    /// 2D 플랫포머 하수도/던전 생성기 - Medieval 타일셋 사용
    /// 사이드뷰 스타일의 플랫포머 맵 자동 생성
    /// </summary>
    public class SewerDungeonGenerator : EditorWindow
    {
        [Header("Map Settings")]
        public int width = 150;
        public int height = 60;
        public int floorHeight = 8; // 바닥 높이
        
        [Header("Generation Settings")]
        public int platformCount = 12;
        public int wallThickness = 2;
        public bool generateCeiling = true;
        public int ceilingHeight = 45;
        
        [Header("Tile Path")]
        public string tilePath = "Assets/Medieval_pixel_art_asset_FREE/Tiles_free";
        
        // 타일 캐시
        private Dictionary<string, TileBase> tileCache = new Dictionary<string, TileBase>();
        
        [MenuItem("Pathfinder/Generate Sewer Dungeon")]
        public static void ShowWindow()
        {
            GetWindow<SewerDungeonGenerator>("Sewer Generator");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("2D 하수도/던전 생성기", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("맵 크기", EditorStyles.boldLabel);
            width = EditorGUILayout.IntField("너비", width);
            height = EditorGUILayout.IntField("높이", height);
            floorHeight = EditorGUILayout.IntField("바닥 높이", floorHeight);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("생성 설정", EditorStyles.boldLabel);
            platformCount = EditorGUILayout.IntField("플랫폼 개수", platformCount);
            wallThickness = EditorGUILayout.IntField("벽 두께", wallThickness);
            generateCeiling = EditorGUILayout.Toggle("천장 생성", generateCeiling);
            if (generateCeiling)
                ceilingHeight = EditorGUILayout.IntField("천장 높이", ceilingHeight);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("하수도 던전 생성", GUILayout.Height(40)))
            {
                GenerateDungeon();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "생성 내용:\n" +
                "- 사이드뷰 플랫포머 구조\n" +
                "- 바닥 + 벽 + 천장\n" +
                "- 다양한 높이의 플랫폼\n" +
                "- 하수도 파이프/통로\n" +
                "- Medieval 타일셋 사용",
                MessageType.Info);
        }
        
        private void GenerateDungeon()
        {
            // 씬에서 VillageMap 찾기 (또는 새로 생성)
            GameObject dungeonMap = GameObject.Find("VillageMap") ?? GameObject.Find("DungeonMap");
            if (dungeonMap == null)
            {
                // 새로 생성
                dungeonMap = new GameObject("DungeonMap");
                
                // Grid 생성
                GameObject gridObj = new GameObject("Grid");
                gridObj.transform.SetParent(dungeonMap.transform);
                Grid grid = gridObj.AddComponent<Grid>();
                grid.cellSize = new Vector3(1, 1, 0);
                
                // Tilemap들 생성
                CreateTilemap(gridObj.transform, "Ground", 0);
                CreateTilemap(gridObj.transform, "Walls", 1);
                CreateTilemap(gridObj.transform, "Platforms", 2);
                CreateTilemap(gridObj.transform, "Decoration", 3);
                CreateTilemap(gridObj.transform, "Background", -1);
            }
            
            // 타일맵 찾기
            Transform gridTransform = dungeonMap.transform.Find("Grid");
            Tilemap groundTilemap = gridTransform.Find("Ground")?.GetComponent<Tilemap>();
            Tilemap wallsTilemap = gridTransform.Find("Walls")?.GetComponent<Tilemap>();
            Tilemap platformsTilemap = gridTransform.Find("Platforms")?.GetComponent<Tilemap>();
            Tilemap decorationTilemap = gridTransform.Find("Decoration")?.GetComponent<Tilemap>();
            Tilemap backgroundTilemap = gridTransform.Find("Background")?.GetComponent<Tilemap>();
            
            if (groundTilemap == null || wallsTilemap == null)
            {
                EditorUtility.DisplayDialog("오류", "Tilemap 오브젝트를 찾을 수 없습니다.", "확인");
                return;
            }
            
            // 기존 타일 지우기
            groundTilemap.ClearAllTiles();
            wallsTilemap.ClearAllTiles();
            platformsTilemap?.ClearAllTiles();
            decorationTilemap?.ClearAllTiles();
            backgroundTilemap?.ClearAllTiles();
            
            // 타일 로드
            LoadTiles();
            
            // 던전 생성
            DrawSewerDungeon(groundTilemap, wallsTilemap, platformsTilemap, decorationTilemap, backgroundTilemap);
            
            // WarpPoint 배치
            PlaceWarpPoints(dungeonMap.transform);
            
            Debug.Log($"[SewerDungeonGenerator] 하수도 던전 생성 완료! ({width}x{height})");
            EditorUtility.DisplayDialog("생성 완료", "2D 하수도 던전이 생성되었습니다!\nScene 뷰에서 확인하세요.", "확인");
        }
        
        private void CreateTilemap(Transform parent, string name, int sortingOrder)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            Tilemap tilemap = obj.AddComponent<Tilemap>();
            TilemapRenderer renderer = obj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            
            // 벽과 플랫폼은 콜라이더 추가
            if (name == "Walls" || name == "Ground" || name == "Platforms")
            {
                var collider = obj.AddComponent<TilemapCollider2D>();
                if (name == "Ground" || name == "Platforms")
                {
                    // CompositeCollider2D는 TilemapCollider2D와 함께 사용
                    obj.AddComponent<CompositeCollider2D>();
                }
            }
        }
        
        private void LoadTiles()
        {
            tileCache.Clear();
            
            // Medieval 타일 로드 (하수도/던전용)
            string[] tileNames = new string[]
            {
                // 바닥 타일 (어두운 돌/벽돌)
                "Medieval_tiles_free2_0",    // 검은 바닥
                "Medieval_tiles_free2_1",    // 어두운 바닥
                "Medieval_tiles_free2_2",    // 회색 바닥
                "Medieval_tiles_free2_3",    // 돌 바닥
                "Medieval_tiles_free2_4",    // 벽돌 바닥
                "Medieval_tiles_free2_5",    // 돌2
                "Medieval_tiles_free2_6",    // 돌3
                "Medieval_tiles_free2_7",    // 돌4
                
                // 벽 타일
                "Medieval_tiles_free2_10",   // 벽1
                "Medieval_tiles_free2_11",   // 벽2
                "Medieval_tiles_free2_12",   // 벽3
                "Medieval_tiles_free2_13",   // 벽4
                "Medieval_tiles_free2_20",   // 돌벽1
                "Medieval_tiles_free2_21",   // 돌벽2
                "Medieval_tiles_free2_22",   // 돌벽3
                "Medieval_tiles_free2_23",   // 돌벽4
                
                // 장식 (하수도용)
                "Medieval_tiles_free2_30",   // 파이프1
                "Medieval_tiles_free2_31",   // 파이프2
                "Medieval_tiles_free2_32",   // 파이프3
                "Medieval_tiles_free2_40",   // 물1
                "Medieval_tiles_free2_41",   // 물2
                "Medieval_tiles_free2_50",   // 철창
                "Medieval_tiles_free2_51",   // 사슬
                "Medieval_tiles_free2_60",   // 횃불
            };
            
            foreach (string tileName in tileNames)
            {
                string path = $"{tilePath}/{tileName}.asset";
                TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (tile != null)
                {
                    tileCache[tileName] = tile;
                }
            }
            
            // fallback: 폴더에서 모든 타일 로드
            if (tileCache.Count < 10)
            {
                string[] guids = AssetDatabase.FindAssets("t:Tile", new[] { tilePath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                    if (tile != null)
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension(path);
                        tileCache[name] = tile;
                    }
                }
            }
            
            Debug.Log($"[SewerDungeonGenerator] {tileCache.Count}개 타일 로드됨");
        }
        
        private void DrawSewerDungeon(Tilemap ground, Tilemap walls, Tilemap platforms, Tilemap decoration, Tilemap background)
        {
            // 1. 배경 (어두운 벽)
            DrawBackground(background);
            
            // 2. 바닥
            DrawFloor(ground);
            
            // 3. 양쪽 벽
            DrawWalls(walls);
            
            // 4. 천장 (선택적)
            if (generateCeiling)
            {
                DrawCeiling(walls);
            }
            
            // 5. 플랫폼 (점프해서 올라가는 곳)
            DrawPlatforms(platforms);
            
            // 6. 장식 (파이프, 물 등)
            DrawDecorations(decoration);
        }
        
        private void DrawBackground(Tilemap background)
        {
            if (background == null) return;
            
            TileBase bgTile = GetTile("Medieval_tiles_free2_1") ?? GetTile("Medieval_tiles_free2_0");
            if (bgTile == null) return;
            
            // 전체 배경 채우기
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    background.SetTile(new Vector3Int(x, y, 0), bgTile);
                }
            }
        }
        
        private void DrawFloor(Tilemap ground)
        {
            TileBase floorTile1 = GetTile("Medieval_tiles_free2_3");
            TileBase floorTile2 = GetTile("Medieval_tiles_free2_4");
            TileBase floorTile3 = GetTile("Medieval_tiles_free2_5");
            
            // 바닥 생성 (floorHeight 높이로)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < floorHeight; y++)
                {
                    TileBase tile = floorTile1;
                    float rand = Random.value;
                    if (rand > 0.6f && floorTile2 != null) tile = floorTile2;
                    else if (rand > 0.85f && floorTile3 != null) tile = floorTile3;
                    
                    if (tile != null)
                    {
                        ground.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
                
                // 바닥 표면 (더 다양한 패턴)
                if (floorTile1 != null)
                {
                    ground.SetTile(new Vector3Int(x, floorHeight, 0), floorTile1);
                }
            }
            
            // 바닥의 움푹한 부분 (구멍) - 통과 가능한 경로
            int holeCount = width / 30;
            for (int i = 0; i < holeCount; i++)
            {
                int holeX = Random.Range(10, width - 10);
                int holeWidth = Random.Range(2, 4);
                
                for (int x = holeX; x < holeX + holeWidth && x < width; x++)
                {
                    for (int y = floorHeight - 2; y <= floorHeight; y++)
                    {
                        ground.SetTile(new Vector3Int(x, y, 0), null);
                    }
                }
            }
        }
        
        private void DrawWalls(Tilemap walls)
        {
            TileBase wallTile1 = GetTile("Medieval_tiles_free2_10");
            TileBase wallTile2 = GetTile("Medieval_tiles_free2_11");
            TileBase wallTile3 = GetTile("Medieval_tiles_free2_20");
            
            // 왼쪽 벽
            for (int x = 0; x < wallThickness; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileBase tile = wallTile1;
                    float rand = Random.value;
                    if (rand > 0.5f && wallTile2 != null) tile = wallTile2;
                    else if (rand > 0.8f && wallTile3 != null) tile = wallTile3;
                    
                    if (tile != null)
                    {
                        walls.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }
            
            // 오른쪽 벽
            for (int x = width - wallThickness; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileBase tile = wallTile1;
                    float rand = Random.value;
                    if (rand > 0.5f && wallTile2 != null) tile = wallTile2;
                    else if (rand > 0.8f && wallTile3 != null) tile = wallTile3;
                    
                    if (tile != null)
                    {
                        walls.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }
        }
        
        private void DrawCeiling(Tilemap walls)
        {
            TileBase wallTile = GetTile("Medieval_tiles_free2_10") ?? GetTile("Medieval_tiles_free2_20");
            if (wallTile == null) return;
            
            // 천장 (전체 또는 부분)
            for (int x = wallThickness; x < width - wallThickness; x++)
            {
                for (int y = ceilingHeight; y < height; y++)
                {
                    walls.SetTile(new Vector3Int(x, y, 0), wallTile);
                }
            }
            
            // 천장의 돌출부 (낮은 부분)
            int section = width / 4;
            for (int i = 1; i < 4; i++)
            {
                int dropX = i * section;
                int dropHeight = Random.Range(3, 6);
                int dropWidth = Random.Range(4, 8);
                
                for (int x = dropX; x < dropX + dropWidth && x < width - wallThickness; x++)
                {
                    for (int y = ceilingHeight - dropHeight; y < ceilingHeight; y++)
                    {
                        walls.SetTile(new Vector3Int(x, y, 0), wallTile);
                    }
                }
            }
        }
        
        private void DrawPlatforms(Tilemap platforms)
        {
            if (platforms == null) return;
            
            TileBase platformTile1 = GetTile("Medieval_tiles_free2_4");
            TileBase platformTile2 = GetTile("Medieval_tiles_free2_5");
            TileBase platformTile3 = GetTile("Medieval_tiles_free2_6");
            
            int sectionWidth = width / (platformCount / 2);
            
            for (int i = 0; i < platformCount; i++)
            {
                // 플랫폼 위치 (floorHeight + 5 ~ ceilingHeight - 5 사이)
                int platformY = Random.Range(floorHeight + 6, generateCeiling ? ceilingHeight - 8 : height - 10);
                int platformX = Random.Range(wallThickness + 5, width - wallThickness - 15);
                int platformWidth = Random.Range(4, 10);
                
                // 플랫폼 생성
                for (int x = platformX; x < platformX + platformWidth && x < width - wallThickness; x++)
                {
                    TileBase tile = platformTile1;
                    float rand = Random.value;
                    if (rand > 0.5f && platformTile2 != null) tile = platformTile2;
                    else if (rand > 0.8f && platformTile3 != null) tile = platformTile3;
                    
                    if (tile != null)
                    {
                        platforms.SetTile(new Vector3Int(x, platformY, 0), tile);
                    }
                }
                
                // 계단식 플랫폼 (20% 확률)
                if (Random.value > 0.8f && i < platformCount - 1)
                {
                    int nextY = platformY + Random.Range(3, 6);
                    int nextX = platformX + platformWidth + Random.Range(2, 5);
                    
                    if (nextX < width - wallThickness - 5)
                    {
                        for (int step = 0; step < 3; step++)
                        {
                            int stepX = platformX + platformWidth + step;
                            int stepY = platformY + step;
                            
                            if (stepX < width - wallThickness)
                            {
                                platforms.SetTile(new Vector3Int(stepX, stepY, 0), platformTile2 ?? platformTile1);
                            }
                        }
                    }
                }
            }
            
            // 긴 통로 플랫폼 (상위 경로)
            int upperPathY = generateCeiling ? ceilingHeight - 5 : height - 8;
            int gapSize = 6;
            int currentX = wallThickness + 5;
            
            while (currentX < width - wallThickness - 5)
            {
                int pathLength = Random.Range(8, 20);
                
                for (int x = currentX; x < currentX + pathLength && x < width - wallThickness; x++)
                {
                    platforms.SetTile(new Vector3Int(x, upperPathY, 0), platformTile1);
                }
                
                currentX += pathLength + gapSize;
            }
        }
        
        private void DrawDecorations(Tilemap decoration)
        {
            if (decoration == null) return;
            
            TileBase pipeTile = GetTile("Medieval_tiles_free2_30");
            TileBase waterTile = GetTile("Medieval_tiles_free2_40");
            TileBase chainTile = GetTile("Medieval_tiles_free2_51");
            TileBase torchTile = GetTile("Medieval_tiles_free2_60");
            
            // 파이프 (벽에 붙어있는)
            if (pipeTile != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    int pipeX = Random.value > 0.5f ? wallThickness : width - wallThickness - 1;
                    int pipeY = Random.Range(floorHeight + 5, generateCeiling ? ceilingHeight - 5 : height - 5);
                    int pipeHeight = Random.Range(3, 8);
                    
                    for (int y = pipeY; y < pipeY + pipeHeight && y < height; y++)
                    {
                        decoration.SetTile(new Vector3Int(pipeX, y, 0), pipeTile);
                    }
                }
            }
            
            // 물웅덩이 (바닥에)
            if (waterTile != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    int waterX = Random.Range(wallThickness + 5, width - wallThickness - 5);
                    int waterWidth = Random.Range(2, 5);
                    
                    for (int x = waterX; x < waterX + waterWidth && x < width - wallThickness; x++)
                    {
                        decoration.SetTile(new Vector3Int(x, floorHeight + 1, 0), waterTile);
                    }
                }
            }
            
            // 사슬 (천장에서)
            if (chainTile != null && generateCeiling)
            {
                for (int i = 0; i < 5; i++)
                {
                    int chainX = Random.Range(wallThickness + 10, width - wallThickness - 10);
                    int chainLength = Random.Range(3, 8);
                    
                    for (int y = 0; y < chainLength && ceilingHeight - y > floorHeight + 5; y++)
                    {
                        decoration.SetTile(new Vector3Int(chainX, ceilingHeight - y, 0), chainTile);
                    }
                }
            }
            
            // 횃불 (벽에)
            if (torchTile != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    int torchX = Random.value > 0.5f ? wallThickness + 1 : width - wallThickness - 2;
                    int torchY = Random.Range(floorHeight + 3, generateCeiling ? ceilingHeight - 5 : height - 5);
                    
                    decoration.SetTile(new Vector3Int(torchX, torchY, 0), torchTile);
                }
            }
        }
        
        private void PlaceWarpPoints(Transform parent)
        {
            // 기존 워프 포인트 찾기/제거
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith("Warp_"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            
            // 시작 지점 (왼쪽)
            CreateWarpPoint(parent, "Dungeon_Start", new Vector3(wallThickness + 3, floorHeight + 2, 0), true);
            
            // 출구 (오른쪽)
            CreateWarpPoint(parent, "Dungeon_Exit", new Vector3(width - wallThickness - 3, floorHeight + 2, 0), false);
            
            // 중간 지점 (위쪽 플랫폼)
            int upperY = generateCeiling ? ceilingHeight - 5 : height - 8;
            CreateWarpPoint(parent, "Dungeon_Upper", new Vector3(width / 2, upperY + 2, 0), false);
        }
        
        private void CreateWarpPoint(Transform parent, string name, Vector3 position, bool isStart)
        {
            GameObject warpObj = new GameObject($"Warp_{name}");
            warpObj.transform.SetParent(parent);
            warpObj.transform.position = position;
            
            SpriteRenderer sr = warpObj.AddComponent<SpriteRenderer>();
            sr.color = isStart ? Color.green : Color.yellow;
            sr.sortingOrder = 10;
        }
        
        private TileBase GetTile(string name)
        {
            if (tileCache.ContainsKey(name))
                return tileCache[name];
            return null;
        }
    }
}
