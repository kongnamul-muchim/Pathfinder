using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

namespace Pathfinder.Editor
{
    /// <summary>
    /// 중세 마을 자동 생성기 - Cainos 타일셋 사용
    /// </summary>
    public class MedievalVillageGenerator : EditorWindow
    {
        [Header("Map Settings")]
        public int width = 150;
        public int height = 80;
        
        [Header("Tile Paths")]
        public string tilePath = "Assets/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground";
        
        // 타일 캐시
        private Dictionary<string, TileBase> tileCache = new Dictionary<string, TileBase>();
        
        [MenuItem("Pathfinder/Generate Medieval Village")]
        public static void ShowWindow()
        {
            GetWindow<MedievalVillageGenerator>("Village Generator");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("중세 마을 자동 생성기", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            width = EditorGUILayout.IntField("맵 너비", width);
            height = EditorGUILayout.IntField("맵 높이", height);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("현재 씬에 마을 생성", GUILayout.Height(40)))
            {
                GenerateVillage();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "생성 내용:\n" +
                "- 중앙 광장 (돌바닥)\n" +
                "- 집 8개 (나무/돌 벽)\n" +
                "- 연결 길 (흙길)\n" +
                "- 울타리 및 장식\n" +
                "- 테두리 벽",
                MessageType.Info);
        }
        
        private void GenerateVillage()
        {
            // 씬에서 VillageMap 찾기
            GameObject villageMap = GameObject.Find("VillageMap");
            if (villageMap == null)
            {
                EditorUtility.DisplayDialog("오류", "VillageMap 오브젝트를 찾을 수 없습니다.\n먼저 MapSceneGenerator로 씬을 생성하세요.", "확인");
                return;
            }
            
            // 타일맵 찾기
            Transform gridTransform = villageMap.transform.Find("Grid");
            if (gridTransform == null)
            {
                EditorUtility.DisplayDialog("오류", "Grid 오브젝트를 찾을 수 없습니다.", "확인");
                return;
            }
            
            Tilemap groundTilemap = gridTransform.Find("Ground")?.GetComponent<Tilemap>();
            Tilemap collisionTilemap = gridTransform.Find("Collision")?.GetComponent<Tilemap>();
            Tilemap decorationTilemap = gridTransform.Find("Decoration")?.GetComponent<Tilemap>();
            
            if (groundTilemap == null || collisionTilemap == null)
            {
                EditorUtility.DisplayDialog("오류", "Tilemap 오브젝트를 찾을 수 없습니다.", "확인");
                return;
            }
            
            // 기존 타일 지우기
            groundTilemap.ClearAllTiles();
            collisionTilemap.ClearAllTiles();
            decorationTilemap?.ClearAllTiles();
            
            // 타일 로드
            LoadTiles();
            
            // 마을 생성
            DrawVillage(groundTilemap, collisionTilemap, decorationTilemap);
            
            Debug.Log($"[MedievalVillageGenerator] 중세 마을 생성 완료! ({width}x{height})");
            EditorUtility.DisplayDialog("생성 완료", "중세 마을이 생성되었습니다!\nScene 뷰에서 확인하세요.", "확인");
        }
        
        private void LoadTiles()
        {
            tileCache.Clear();
            
            // Cainos 타일 로드 (일반적인 타일 번호 사용)
            string[] tileNames = new string[]
            {
                // 땅/길 타일
                "TX Tileset Ground_0",   // 잔디
                "TX Tileset Ground_1",   // 흙
                "TX Tileset Ground_2",   // 돌1
                "TX Tileset Ground_3",   // 돌2
                "TX Tileset Ground_4",   // 돌3
                "TX Tileset Ground_5",   // 길 모서리
                "TX Tileset Ground_6",   // 길 직선
                "TX Tileset Ground_7",   // 길 교차
                
                // 벽 타일
                "TX Tileset Ground_10",  // 나무 벽1
                "TX Tileset Ground_11",  // 나무 벽2
                "TX Tileset Ground_20",  // 돌 벽1
                "TX Tileset Ground_21",  // 돌 벽2
                
                // 장식
                "TX Tileset Ground_30",  // 울타리
                "TX Tileset Ground_31",  // 나무
                "TX Tileset Ground_32",  // 풀
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
            if (tileCache.Count == 0)
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
            
            Debug.Log($"[MedievalVillageGenerator] {tileCache.Count}개 타일 로드됨");
        }
        
        private void DrawVillage(Tilemap ground, Tilemap collision, Tilemap decoration)
        {
            int centerX = width / 2;
            int centerY = height / 2;
            
            // 1. 기본 바닥 (잔디/흙)
            DrawBaseGround(ground);
            
            // 2. 중앙 광장 (돌바닥)
            DrawTownSquare(ground, centerX, centerY);
            
            // 3. 집들
            DrawHouses(collision, decoration, centerX, centerY);
            
            // 4. 길 연결
            DrawRoads(ground, centerX, centerY);
            
            // 5. 울타리 및 장식
            DrawDecorations(decoration, centerX, centerY);
            
            // 6. 테두리 벽
            DrawBorderWalls(collision);
        }
        
        private void DrawBaseGround(Tilemap ground)
        {
            TileBase grassTile = GetTile("TX Tileset Ground_0") ?? GetTile("TX Tileset Ground_1");
            TileBase dirtTile = GetTile("TX Tileset Ground_1");
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 랜덤하게 잔디와 흙 배치
                    TileBase tile = (Random.value > 0.7f && dirtTile != null) ? dirtTile : grassTile;
                    if (tile != null)
                    {
                        ground.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }
        }
        
        private void DrawTownSquare(Tilemap ground, int centerX, int centerY)
        {
            int squareSize = 12;
            
            TileBase stoneTile1 = GetTile("TX Tileset Ground_2");
            TileBase stoneTile2 = GetTile("TX Tileset Ground_3");
            TileBase stoneTile3 = GetTile("TX Tileset Ground_4");
            
            for (int x = centerX - squareSize; x <= centerX + squareSize; x++)
            {
                for (int y = centerY - squareSize; y <= centerY + squareSize; y++)
                {
                    // 돌바닥 패턴
                    TileBase tile = stoneTile1;
                    float rand = Random.value;
                    if (rand > 0.6f && stoneTile2 != null) tile = stoneTile2;
                    else if (rand > 0.85f && stoneTile3 != null) tile = stoneTile3;
                    
                    if (tile != null)
                    {
                        ground.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }
        }
        
        private void DrawHouses(Tilemap collision, Tilemap decoration, int centerX, int centerY)
        {
            // 집 위치들 (광장 주변)
            Vector2Int[] housePositions = new Vector2Int[]
            {
                new Vector2Int(centerX - 25, centerY + 20),
                new Vector2Int(centerX + 25, centerY + 20),
                new Vector2Int(centerX - 25, centerY - 20),
                new Vector2Int(centerX + 25, centerY - 20),
                new Vector2Int(centerX - 35, centerY),
                new Vector2Int(centerX + 35, centerY),
                new Vector2Int(centerX, centerY + 35),
                new Vector2Int(centerX, centerY - 35),
            };
            
            TileBase woodWall1 = GetTile("TX Tileset Ground_10");
            TileBase woodWall2 = GetTile("TX Tileset Ground_11");
            TileBase stoneWall1 = GetTile("TX Tileset Ground_20");
            TileBase stoneWall2 = GetTile("TX Tileset Ground_21");
            
            foreach (Vector2Int pos in housePositions)
            {
                DrawHouse(collision, decoration, pos.x, pos.y, 
                    Random.value > 0.5f ? woodWall1 : stoneWall1);
            }
        }
        
        private void DrawHouse(Tilemap collision, Tilemap decoration, int x, int y, TileBase wallTile)
        {
            if (wallTile == null) return;
            
            int houseWidth = 6 + Random.Range(0, 3);
            int houseHeight = 5 + Random.Range(0, 2);
            
            // 집 벽
            for (int dx = 0; dx < houseWidth; dx++)
            {
                for (int dy = 0; dy < houseHeight; dy++)
                {
                    // 테두리만 벽으로
                    if (dx == 0 || dx == houseWidth - 1 || dy == 0 || dy == houseHeight - 1)
                    {
                        collision.SetTile(new Vector3Int(x + dx, y + dy, 0), wallTile);
                    }
                }
            }
            
            // 문
            int doorX = x + houseWidth / 2;
            int doorY = y;
            collision.SetTile(new Vector3Int(doorX, doorY, 0), null);
            
            // 지붕 (간단한 표현)
            TileBase roofTile = GetTile("TX Tileset Ground_20");
            if (roofTile != null)
            {
                for (int dx = -1; dx <= houseWidth; dx++)
                {
                    collision.SetTile(new Vector3Int(x + dx, y + houseHeight, 0), roofTile);
                }
            }
        }
        
        private void DrawRoads(Tilemap ground, int centerX, int centerY)
        {
            TileBase roadTile = GetTile("TX Tileset Ground_6") ?? GetTile("TX Tileset Ground_1");
            if (roadTile == null) return;
            
            // 4방향 길
            int roadWidth = 3;
            
            // 동쪽 길
            for (int x = centerX + 12; x < width - 5; x++)
            {
                for (int y = centerY - roadWidth/2; y <= centerY + roadWidth/2; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), roadTile);
                }
            }
            
            // 서쪽 길
            for (int x = 5; x < centerX - 12; x++)
            {
                for (int y = centerY - roadWidth/2; y <= centerY + roadWidth/2; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), roadTile);
                }
            }
            
            // 북쪽 길
            for (int y = centerY + 12; y < height - 5; y++)
            {
                for (int x = centerX - roadWidth/2; x <= centerX + roadWidth/2; x++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), roadTile);
                }
            }
            
            // 남쪽 길
            for (int y = 5; y < centerY - 12; y++)
            {
                for (int x = centerX - roadWidth/2; x <= centerX + roadWidth/2; x++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), roadTile);
                }
            }
        }
        
        private void DrawDecorations(Tilemap decoration, int centerX, int centerY)
        {
            TileBase fenceTile = GetTile("TX Tileset Ground_30");
            TileBase treeTile = GetTile("TX Tileset Ground_31");
            TileBase grassTile = GetTile("TX Tileset Ground_32");
            
            if (decoration == null) return;
            
            // 울타리 (광장 주변)
            if (fenceTile != null)
            {
                int fenceOffset = 14;
                for (int x = centerX - fenceOffset; x <= centerX + fenceOffset; x += 2)
                {
                    decoration.SetTile(new Vector3Int(x, centerY + fenceOffset, 0), fenceTile);
                    decoration.SetTile(new Vector3Int(x, centerY - fenceOffset, 0), fenceTile);
                }
                for (int y = centerY - fenceOffset; y <= centerY + fenceOffset; y += 2)
                {
                    decoration.SetTile(new Vector3Int(centerX + fenceOffset, y, 0), fenceTile);
                    decoration.SetTile(new Vector3Int(centerX - fenceOffset, y, 0), fenceTile);
                }
            }
            
            // 나무 (랜덤 배치)
            if (treeTile != null)
            {
                for (int i = 0; i < 20; i++)
                {
                    int x = Random.Range(5, width - 5);
                    int y = Random.Range(5, height - 5);
                    
                    // 광장과 길 피하기
                    if (Vector2Int.Distance(new Vector2Int(x, y), new Vector2Int(centerX, centerY)) > 20)
                    {
                        decoration.SetTile(new Vector3Int(x, y, 0), treeTile);
                    }
                }
            }
            
            // 풀 (랜덤)
            if (grassTile != null)
            {
                for (int i = 0; i < 50; i++)
                {
                    int x = Random.Range(5, width - 5);
                    int y = Random.Range(5, height - 5);
                    decoration.SetTile(new Vector3Int(x, y, 0), grassTile);
                }
            }
        }
        
        private void DrawBorderWalls(Tilemap collision)
        {
            TileBase wallTile = GetTile("TX Tileset Ground_20") ?? GetTile("TX Tileset Ground_10");
            if (wallTile == null) return;
            
            // 테두리 벽
            for (int x = 0; x < width; x++)
            {
                collision.SetTile(new Vector3Int(x, 0, 0), wallTile);
                collision.SetTile(new Vector3Int(x, height - 1, 0), wallTile);
            }
            
            for (int y = 0; y < height; y++)
            {
                collision.SetTile(new Vector3Int(0, y, 0), wallTile);
                collision.SetTile(new Vector3Int(width - 1, y, 0), wallTile);
            }
        }
        
        private TileBase GetTile(string name)
        {
            if (tileCache.ContainsKey(name))
                return tileCache[name];
            return null;
        }
    }
}
