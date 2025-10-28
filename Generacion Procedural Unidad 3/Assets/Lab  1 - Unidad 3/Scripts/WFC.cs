﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WFC : MonoBehaviour
{
    [Header("Prefabs con TileInfo")]
    public GameObject[] prefabs; // cada prefab tiene un TileInfo
    public int width = 10;
    public int height = 10;

    private List<GameObject>[,] possibilities;
    private GameObject[,] placedTiles;

    void Start()
    {
        possibilities = new List<GameObject>[width, height];
        placedTiles = new GameObject[width, height];

        // Inicializamos todas las celdas con todos los prefabs posibles
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                possibilities[x, z] = new List<GameObject>(prefabs);

        // Empieza el proceso
        StartCoroutine(CollapseRoutine());
    }

    System.Collections.IEnumerator CollapseRoutine()
    {
        while (true)
        {
            // Buscar la celda con menor entropía (menos opciones)
            Vector2Int pos = GetCellWithLowestEntropy();
            if (pos.x == -1) yield break; // terminado

            // Escoger un prefab al azar
            GameObject chosenPrefab = possibilities[pos.x, pos.y][Random.Range(0, possibilities[pos.x, pos.y].Count)];
            wfcData chosenInfo = chosenPrefab.GetComponent<wfcData>();

            // Instanciarlo en la escena
            Vector3 position = new Vector3(pos.x, 0, pos.y);
            placedTiles[pos.x, pos.y] = Instantiate(chosenPrefab, position, chosenPrefab.transform.rotation);


            // Colapsar celda
            possibilities[pos.x, pos.y] = new List<GameObject> { chosenPrefab };

            // Propagar restricciones
            Propagate(pos.x, pos.y, chosenInfo);

            yield return new WaitForSeconds(0.05f);
        }
    }

    Vector2Int GetCellWithLowestEntropy()
    {
        int minCount = int.MaxValue;
        Vector2Int pos = new Vector2Int(-1, -1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int count = possibilities[x, z].Count;
                if (count > 1 && count < minCount)
                {
                    minCount = count;
                    pos = new Vector2Int(x, z);
                }
            }
        }
        return pos;
    }

    void Propagate(int x, int z, wfcData tile)
    {
        CheckNeighbor(x + 1, z, tile, "xplus");
        CheckNeighbor(x - 1, z, tile, "xless");
        CheckNeighbor(x, z + 1, tile, "zplus");
        CheckNeighbor(x, z - 1, tile, "zless");
    }

    void CheckNeighbor(int nx, int nz, wfcData origin, string dir)
    {
        if (nx < 0 || nz < 0 || nx >= width || nz >= height) return;

        List<GameObject> valid = new List<GameObject>();

        foreach (var candidate in possibilities[nx, nz])
        {
            wfcData info = candidate.GetComponent<wfcData>();
            bool compatible = false;

            switch (dir)
            {
                case "xplus": compatible = (origin.xplus == info.xless); break;
                case "xless": compatible = (origin.xless == info.xplus); break;
                case "zplus": compatible = (origin.zplus == info.zless); break;
                case "zless": compatible = (origin.zless == info.zplus); break;
            }

            if (compatible) valid.Add(candidate);
        }

        if (valid.Count != possibilities[nx, nz].Count)
        {
            possibilities[nx, nz] = valid;
            if (valid.Count == 1)
            {
                // Si el vecino colapsó a una sola opción, seguimos propagando
                var chosenInfo = valid[0].GetComponent<wfcData>();
                Propagate(nx, nz, chosenInfo);
            }
        }
    }
}