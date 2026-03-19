using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Pathfinder.Editor
{
    /// <summary>
    /// 게임 씬 자동 생성기 - Unity Editor 메뉴에서 실행
    /// 마을맵과 초원맵의 기본 구조를 자동으로 생성
    /// </summary>
    public class MapSceneGenerator : EditorWindow
    {
        [Header("Map Settings")]
        public int villageWidth = 150;
        public int villageHeight = 80;
        public int meadowWidth = 150;
        public int meadowHeight = 80;
        
        [Header("Tile Assets")]
        public TileBase groundTile;
        public TileBase wallTile;
        public TileBase pathTile;
        public TileBase grassTile;
        
        private const string SCENE_NAME = "GameScene";
        private const string SCENE_PATH = "Assets/Scenes/GameScene.unity";
        
        [MenuItem("Pathfinder/Generate Game Scene")]
        public static void ShowWindow()
        {
            GetWindow<MapSceneGenerator>("Map Scene Generator");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("게임 씬 자동 생성기", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("맵 크기 설정", EditorStyles.boldLabel);
            villageWidth = EditorGUILayout.IntField("마을맵 너비", villageWidth);
            villageHeight = EditorGUILayout.IntField("마을맵 높이", villageHeight);
            meadowWidth = EditorGUILayout.IntField("초원맵 너비", meadowWidth);
            meadowHeight = EditorGUILayout.IntField("초원맵 높이", meadowHeight);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("타일 에셋 (선택적)", EditorStyles.boldLabel);
            groundTile = (TileBase)EditorGUILayout.ObjectField("땅 타일", groundTile, typeof(TileBase), false);
            wallTile = (TileBase)EditorGUILayout.ObjectField("벽 타일", wallTile, typeof(TileBase), false);
            pathTile = (TileBase)EditorGUILayout.ObjectField("길 타일", pathTile, typeof(TileBase), false);
            grassTile = (TileBase)EditorGUILayout.ObjectField("풀 타일", grassTile, typeof(TileBase), false);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("게임 씬 생성", GUILayout.Height(40)))
            {
                GenerateGameScene();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "이 도구는:\n" +
                "1. 새로운 GameScene 생성\n" +
                "2. 마을맵 기본 구조 생성 (150x80)\n" +
                "3. 초원맵 기본 구조 생성 (150x80)\n" +
                "4. WarpPoint 배치 위치 설정\n" +
                "5. MapManager 설정\n\n" +
                "생성 후 Unity에서 타일을 그리세요!",
                MessageType.Info);
        }
        
        private void GenerateGameScene()
        {
            // 씬 생성 또는 열기
            Scene scene;
            if (System.IO.File.Exists(SCENE_PATH))
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, SCENE_PATH);
            }
            
            // 기존 오브젝트 정리
            ClearExistingMaps();
            
            // MapManager 생성
            GameObject mapManagerObj = CreateMapManager();
            
            // Maps 폴더 생성
            GameObject mapsFolder = new GameObject("Maps");
            mapsFolder.transform.SetParent(null);
            
            // 마을맵 생성
            GameObject villageMap = CreateVillageMap(mapsFolder.transform);
            
            // 초원맵 생성
            GameObject meadowMap = CreateMeadowMap(mapsFolder.transform);
            
            // MapManager 설정
            SetupMapManager(mapManagerObj, villageMap, meadowMap);
            
            // 저장
            EditorSceneManager.SaveScene(scene);
            
            Debug.Log($"[MapSceneGenerator] 게임 씬 생성 완료!\n" +
                     $"- 마을맵: {villageWidth}x{villageHeight}\n" +
                     $"- 초원맵: {meadowWidth}x{meadowHeight}\n" +
                     $"씬 파일: {SCENE_PATH}");
            
            EditorUtility.DisplayDialog("생성 완료", 
                "게임 씬이 성공적으로 생성되었습니다!\n\n" +
                "마을맵과 초원맵의 기본 구조가 생성되었습니다.\n" +
                "Unity에서 타일을 그리고 WarpPoint를 설정하세요.", "확인");
        }
        
        private void ClearExistingMaps()
        {
            GameObject existingMaps = GameObject.Find("Maps");
            if (existingMaps != null)
            {
                Undo.DestroyObjectImmediate(existingMaps);
            }
        }
        
        private GameObject CreateMapManager()
        {
            GameObject existing = GameObject.Find("MapManager");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
            
            GameObject mapManager = new GameObject("MapManager");
            var mapManagerComp = mapManager.AddComponent<World.MapManager>();
            
            // Reflection으로 private 필드 접근 또는 public 메서드 사용
            // 여기서는 기본 생성만 함
            
            return mapManager;
        }
        
        private GameObject CreateVillageMap(Transform parent)
        {
            // 마을맵 루트
            GameObject villageMap = new GameObject("VillageMap");
            villageMap.transform.SetParent(parent);
            
            // Grid 생성
            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(villageMap.transform);
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);
            
            // Ground Tilemap
            GameObject groundObj = new GameObject("Ground");
            groundObj.transform.SetParent(gridObj.transform);
            Tilemap groundTilemap = groundObj.AddComponent<Tilemap>();
            TilemapRenderer groundRenderer = groundObj.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = 0;
            
            // Collision Tilemap (벽, 장애물)
            GameObject collisionObj = new GameObject("Collision");
            collisionObj.transform.SetParent(gridObj.transform);
            Tilemap collisionTilemap = collisionObj.AddComponent<Tilemap>();
            TilemapRenderer collisionRenderer = collisionObj.AddComponent<TilemapRenderer>();
            collisionRenderer.sortingOrder = 1;
            TilemapCollider2D collisionCollider = collisionObj.AddComponent<TilemapCollider2D>();
            
            // Decoration Tilemap (장식물)
            GameObject decoObj = new GameObject("Decoration");
            decoObj.transform.SetParent(gridObj.transform);
            Tilemap decoTilemap = decoObj.AddComponent<Tilemap>();
            TilemapRenderer decoRenderer = decoObj.AddComponent<TilemapRenderer>();
            decoRenderer.sortingOrder = 2;
            
            // 기본 땅 생성 (테두리만 벽으로)
            GenerateVillageLayout(groundTilemap, collisionTilemap);
            
            // WarpPoint 배치 위치 설정 (빈 오브젝트로 표시)
            CreateWarpPointPlaceholder(villageMap.transform, "Village_Start", new Vector3(10, 10, 0), true);
            CreateWarpPointPlaceholder(villageMap.transform, "Village_ToMeadow", new Vector3(140, 40, 0), false);
            CreateWarpPointPlaceholder(villageMap.transform, "Village_ToCliff", new Vector3(75, 75, 0), false);
            CreateWarpPointPlaceholder(villageMap.transform, "Village_ToGraveyard", new Vector3(10, 40, 0), false);
            
            return villageMap;
        }
        
        private GameObject CreateMeadowMap(Transform parent)
        {
            // 초원맵 루트 (비활성화)
            GameObject meadowMap = new GameObject("MeadowMap");
            meadowMap.transform.SetParent(parent);
            meadowMap.SetActive(false);
            
            // Grid 생성
            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(meadowMap.transform);
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);
            
            // Ground Tilemap
            GameObject groundObj = new GameObject("Ground");
            groundObj.transform.SetParent(gridObj.transform);
            Tilemap groundTilemap = groundObj.AddComponent<Tilemap>();
            TilemapRenderer groundRenderer = groundObj.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = 0;
            
            // Collision Tilemap
            GameObject collisionObj = new GameObject("Collision");
            collisionObj.transform.SetParent(gridObj.transform);
            Tilemap collisionTilemap = collisionObj.AddComponent<Tilemap>();
            TilemapRenderer collisionRenderer = collisionObj.AddComponent<TilemapRenderer>();
            collisionRenderer.sortingOrder = 1;
            TilemapCollider2D collisionCollider = collisionObj.AddComponent<TilemapCollider2D>();
            
            // Decoration Tilemap
            GameObject decoObj = new GameObject("Decoration");
            decoObj.transform.SetParent(gridObj.transform);
            Tilemap decoTilemap = decoObj.AddComponent<Tilemap>();
            TilemapRenderer decoRenderer = decoObj.AddComponent<TilemapRenderer>();
            decoRenderer.sortingOrder = 2;
            
            // 기본 초원 레이아웃
            GenerateMeadowLayout(groundTilemap, collisionTilemap);
            
            // WarpPoint 배치 위치
            CreateWarpPointPlaceholder(meadowMap.transform, "Meadow_Entrance", new Vector3(10, 40, 0), true);
            CreateWarpPointPlaceholder(meadowMap.transform, "Meadow_ToVillage", new Vector3(10, 10, 0), false);
            CreateWarpPointPlaceholder(meadowMap.transform, "Meadow_Exit", new Vector3(140, 40, 0), false);
            
            return meadowMap;
        }
        
        private void GenerateVillageLayout(Tilemap ground, Tilemap collision)
        {
            if (groundTile == null) return;
            
            // 기본 땅 생성
            for (int x = 0; x < villageWidth; x++)
            {
                for (int y = 0; y < villageHeight; y++)
                {
                    // 테두리는 벽으로
                    if (x == 0 || x == villageWidth - 1 || y == 0 || y == villageHeight - 1)
                    {
                        if (wallTile != null)
                            collision.SetTile(new Vector3Int(x, y, 0), wallTile);
                    }
                    else
                    {
                        // 내부는 길과 땅 혼합
                        if (pathTile != null && (x % 20 == 0 || y % 20 == 0))
                        {
                            ground.SetTile(new Vector3Int(x, y, 0), pathTile);
                        }
                        else
                        {
                            ground.SetTile(new Vector3Int(x, y, 0), groundTile);
                        }
                    }
                }
            }
            
            // 중앙 광장
            int centerX = villageWidth / 2;
            int centerY = villageHeight / 2;
            for (int x = centerX - 10; x <= centerX + 10; x++)
            {
                for (int y = centerY - 10; y <= centerY + 10; y++)
                {
                    if (pathTile != null)
                        ground.SetTile(new Vector3Int(x, y, 0), pathTile);
                }
            }
        }
        
        private void GenerateMeadowLayout(Tilemap ground, Tilemap collision)
        {
            if (grassTile == null) return;
            
            // 기본 풀 생성
            for (int x = 0; x < meadowWidth; x++)
            {
                for (int y = 0; y < meadowHeight; y++)
                {
                    // 테두리 벽
                    if (x == 0 || x == meadowWidth - 1 || y == 0 || y == meadowHeight - 1)
                    {
                        if (wallTile != null)
                            collision.SetTile(new Vector3Int(x, y, 0), wallTile);
                    }
                    else
                    {
                        ground.SetTile(new Vector3Int(x, y, 0), grassTile);
                    }
                }
            }
            
            // 중앙 길
            int centerY = meadowHeight / 2;
            for (int x = 10; x < meadowWidth - 10; x++)
            {
                if (pathTile != null)
                {
                    ground.SetTile(new Vector3Int(x, centerY, 0), pathTile);
                    ground.SetTile(new Vector3Int(x, centerY + 1, 0), pathTile);
                    ground.SetTile(new Vector3Int(x, centerY - 1, 0), pathTile);
                }
            }
        }
        
        private void CreateWarpPointPlaceholder(Transform parent, string name, Vector3 position, bool isStart)
        {
            GameObject warpObj = new GameObject($"Warp_{name}");
            warpObj.transform.SetParent(parent);
            warpObj.transform.position = position;
            
            // 아이콘 표시를 위한 SpriteRenderer (임시)
            SpriteRenderer sr = warpObj.AddComponent<SpriteRenderer>();
            sr.color = isStart ? Color.green : Color.yellow;
            sr.sortingOrder = 10;
            
            // 나중에 WarpPoint 컴포넌트를 여기에 추가하면 됨
        }
        
        private void SetupMapManager(GameObject mapManagerObj, GameObject villageMap, GameObject meadowMap)
        {
            var mapManager = mapManagerObj.GetComponent<World.MapManager>();
            if (mapManager == null) return;
            
            // Reflection으로 private 필드 설정
            var mapDataType = typeof(World.MapManager).GetNestedType("MapData", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (mapDataType != null)
            {
                // MapData 리스트 생성 및 설정
                // Note: 실제로는 public 메서드나 Inspector에서 설정하는 것이 더 좋음
            }
            
            Debug.Log("[MapSceneGenerator] MapManager 설정 완료. Inspector에서 MapData를 수동으로 설정하세요.");
        }
    }
}
