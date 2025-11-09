using System.Collections.Generic;
using UnityEngine;

public class Lab3 : MonoBehaviour
{
    public List<List<int>> matriz = new List<List<int>>();
    public float offsetX = 0f;
    public float offsetY = 0f;
    public GameObject[] prefabs;

    private Dictionary<int, Dictionary<int, int>> transiciones = new Dictionary<int, Dictionary<int, int>>();

    public int anchoGenerado = 10;
    public int altoGenerado = 10;
    public int cantidadLaberintos = 3;
    public float separacionLaberintos = 15f;

    void Start()
    {
        LeerArchivo("matriz");
        GenerarMapaInicial();
        AprenderTransiciones();

        for (int i = 0; i < cantidadLaberintos; i++)
        {
            int[,] nuevoMapa = GenerarNuevoMapa();
            InstanciarMapa(nuevoMapa, i);
        }
    }

    void LeerArchivo(string nombreArchivo)
    {
        TextAsset archivo = Resources.Load<TextAsset>(nombreArchivo);
        if (archivo == null)
        {
            Debug.LogError("No se encontró el archivo " + nombreArchivo);
            return;
        }

        string[] lineas = archivo.text.Split('\n');
        foreach (string linea in lineas)
        {
            if (string.IsNullOrWhiteSpace(linea)) continue;
            string[] valores = linea.Trim().Split(' ');

            List<int> fila = new List<int>();
            foreach (string v in valores)
                if (int.TryParse(v, out int numero))
                    fila.Add(numero);

            matriz.Add(fila);
        }
    }

    void AprenderTransiciones()
    {
        for (int y = 0; y < matriz.Count; y++)
        {
            for (int x = 0; x < matriz[y].Count; x++)
            {
                int actual = matriz[y][x];
                if (actual == 0) continue;

                // vecinos cardinales
                int[] dx = { 1, -1, 0, 0 };
                int[] dy = { 0, 0, 1, -1 };

                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d];
                    int ny = y + dy[d];
                    if (ny < 0 || ny >= matriz.Count || nx < 0 || nx >= matriz[ny].Count)
                        continue;

                    int vecino = matriz[ny][nx];
                    if (vecino == 0) continue;

                    if (!transiciones.ContainsKey(actual))
                        transiciones[actual] = new Dictionary<int, int>();

                    if (!transiciones[actual].ContainsKey(vecino))
                        transiciones[actual][vecino] = 0;

                    transiciones[actual][vecino]++;
                }
            }
        }

        Debug.Log($"Modelo de Markov aprendido con {transiciones.Count} patrones base.");
    }

    int[,] GenerarNuevoMapa()
    {
        int[,] nuevo = new int[altoGenerado, anchoGenerado];

        // Elegir tile inicial aleatorio
        List<int> claves = new List<int>(transiciones.Keys);
        int tileActual = claves[Random.Range(0, claves.Count)];
        nuevo[0, 0] = tileActual;

        for (int y = 0; y < altoGenerado; y++)
        {
            for (int x = 0; x < anchoGenerado; x++)
            {
                if (x == 0 && y == 0) continue;

                int vecinoIzq = x > 0 ? nuevo[y, x - 1] : 0;
                int vecinoArr = y > 0 ? nuevo[y - 1, x] : 0;

                int siguiente = ObtenerSiguientePorProbabilidad(vecinoIzq, vecinoArr);
                nuevo[y, x] = siguiente;
            }
        }

        return nuevo;
    }

    int ObtenerSiguientePorProbabilidad(int izq, int arr)
    {
        // Si ambos son válidos, combinamos sus transiciones
        Dictionary<int, float> pesos = new Dictionary<int, float>();

        AgregarPesosDesdeVecino(izq, ref pesos, 1.0f);
        AgregarPesosDesdeVecino(arr, ref pesos, 0.8f);

        if (pesos.Count == 0)
        {
            List<int> claves = new List<int>(transiciones.Keys);
            return claves[Random.Range(0, claves.Count)];
        }

        // selección ponderada
        float total = 0;
        foreach (var kv in pesos)
            total += kv.Value;

        float r = Random.Range(0f, total);
        float acum = 0;
        foreach (var kv in pesos)
        {
            acum += kv.Value;
            if (r <= acum)
                return kv.Key;
        }

        return 0;
    }

    void AgregarPesosDesdeVecino(int vecino, ref Dictionary<int, float> acumulado, float factor)
    {
        if (vecino == 0 || !transiciones.ContainsKey(vecino)) return;

        foreach (var kv in transiciones[vecino])
        {
            // Aumentamos el peso (para marcar más los patrones)
            float peso = Mathf.Pow(kv.Value, 1.5f) * factor;

            if (!acumulado.ContainsKey(kv.Key))
                acumulado[kv.Key] = 0;

            acumulado[kv.Key] += peso;
        }
    }

    void InstanciarMapa(int[,] mapa, int indice)
    {
        float desplazamientoX = indice * (anchoGenerado + separacionLaberintos);

        for (int y = 0; y < mapa.GetLength(0); y++)
        {
            for (int x = 0; x < mapa.GetLength(1); x++)
            {
                int id = mapa[y, x];
                if (id == 0) continue;

                GameObject prefab = BuscarPrefabPorID(id);
                if (prefab == null) continue;

                Vector3 pos = new Vector3((x * 1f) + offsetX + desplazamientoX, 0, (-y * 1f) + offsetY);
                Instantiate(prefab, pos, prefab.transform.rotation, transform);
            }
        }

        Debug.Log($"Laberinto #{indice + 1} generado.");
    }

    GameObject BuscarPrefabPorID(int id)
    {
        foreach (GameObject obj in prefabs)
        {
            if (obj == null) continue;
            wfcData data = obj.GetComponent<wfcData>();
            if (data != null && data.ID == id)
                return obj;
        }
        return null;
    }

    void GenerarMapaInicial()
    {
        for (int y = 0; y < matriz.Count; y++)
        {
            for (int x = 0; x < matriz[y].Count; x++)
            {
                int idBuscada = matriz[y][x];

                // Buscar prefab que tenga ese ID
                GameObject prefab = BuscarPrefabPorID(idBuscada);
                if (prefab == null)
                {
                    Debug.LogWarning($"No se encontró prefab con ID {idBuscada} en posición [{x},{y}]");
                    continue;
                }

                Vector3 posicion = new Vector3((x * 1f) + offsetX, 0, (-y * 1f) + (offsetY+20));
                Instantiate(prefab, posicion, prefab.transform.rotation, transform);
            }
        }

        Debug.Log("Mapa generado correctamente");
    }

}
