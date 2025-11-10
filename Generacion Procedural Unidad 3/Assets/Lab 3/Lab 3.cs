using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Lab3 : MonoBehaviour
{
    public List<List<int>> matriz = new List<List<int>>();
    public List<List<int>> columnas = new List<List<int>>();
    public float offsetX = 0f;
    public float offsetY = 0f;
    public GameObject[] prefabs;



    public int numColumnasGeneradas = 30; 
    public float separacionColumnas = 1f;
    public int repeticiones = 3;

    public int ordenMarkov = 2;
    Dictionary<string, List<List<int>>> modeloMarkov = new Dictionary<string, List<List<int>>>();


    void Start()
    {
        LeerArchivo("matriz");
        GuardarColumnas(); 
        MostrarColumnasPorConsola(); 
        GenerarMapaInicial();



        for (int i = 0; i < repeticiones; i++)
        {
            offsetX += 40;

            ConstruirModeloMarkov();
            var columnasGeneradas = GenerarMapaNuevoMarkov();
            if (columnasGeneradas.Count == 0) break;

            var nuevaMatriz = TransponerMatriz(columnasGeneradas);
            ActualizarMatriz(nuevaMatriz);
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


    void GuardarColumnas()
    {
        if (matriz.Count == 0) return;

        int numColumnas = matriz[0].Count;
        columnas.Clear();

        for (int x = 0; x < numColumnas; x++)
        {
            List<int> columna = new List<int>();
            for (int y = 0; y < matriz.Count; y++)
            {
                if (x < matriz[y].Count)
                    columna.Add(matriz[y][x]);
            }
            columnas.Add(columna);
        }

        Debug.Log($"Se guardaron {columnas.Count} columnas");
    }

    void MostrarColumnasPorConsola()
    {
        for (int i = 0; i < columnas.Count; i++)
        {
            string contenido = string.Join(", ", columnas[i]);
            Debug.Log($"Columna {i}: [{contenido}]");
        }
    }


    List<List<int>> GenerarMapaNuevoMarkov()
    {
        if (modeloMarkov.Count == 0)
        {
            Debug.LogWarning("El modelo de Markov no está construido.");
            return new List<List<int>>();
        }

        List<List<int>> nuevoMapa = new List<List<int>>();

        // Semilla inicial: tomamos las primeras N columnas del mapa original
        for (int i = 0; i < ordenMarkov && i < columnas.Count; i++)
            nuevoMapa.Add(new List<int>(columnas[i]));

        for (int i = 0; i < numColumnasGeneradas; i++)
        {
            // Obtenemos el contexto actual
            if (nuevoMapa.Count < ordenMarkov) break;
            var contexto = nuevoMapa.Skip(nuevoMapa.Count - ordenMarkov).Take(ordenMarkov).ToList();

            string clave = string.Join("|", contexto.Select(c => string.Join(",", c)));

            if (!modeloMarkov.ContainsKey(clave))
            {
                // Si no hay transición, reiniciamos con un contexto aleatorio
                var entradaAleatoria = modeloMarkov.ElementAt(Random.Range(0, modeloMarkov.Count));
                clave = entradaAleatoria.Key;
                Debug.Log($"Transición no encontrada, se eligió un nuevo contexto aleatorio: {clave}");
            }


            var posibles = modeloMarkov[clave];

            // Escogemos aleatoriamente entre las opciones aprendidas
            var siguiente = posibles[Random.Range(0, posibles.Count)];
            nuevoMapa.Add(new List<int>(siguiente));
        }

        // --- Instanciación ---
        float inicioX = matriz[0].Count * separacionColumnas + offsetX + 2f;
        for (int x = 0; x < nuevoMapa.Count; x++)
        {
            List<int> columna = nuevoMapa[x];
            for (int y = 0; y < columna.Count; y++)
            {
                int id = columna[y];
                GameObject prefab = BuscarPrefabPorID(id);
                if (prefab == null) continue;

                Vector3 pos = new Vector3(inicioX + x * separacionColumnas, 0, (-y * 1f) + (offsetY + 20));
                Instantiate(prefab, pos, prefab.transform.rotation, transform);
            }
        }

        Debug.Log("Mapa nuevo generado con modelo de Markov.");
        return nuevoMapa;
    }


    void ActualizarMatriz(List<List<int>> nuevaMatriz)
    {
        matriz = nuevaMatriz;
        columnas.Clear();

        int numColumnas = matriz[0].Count;
        for (int x = 0; x < numColumnas; x++)
        {
            List<int> columna = new List<int>();
            for (int y = 0; y < matriz.Count; y++)
            {
                if (x < matriz[y].Count)
                    columna.Add(matriz[y][x]);
            }
            columnas.Add(columna);
        }

        Debug.Log("Matriz y columnas actualizadas para la siguiente iteración.");
    }

    List<List<int>> TransponerMatriz(List<List<int>> columnas)
    {
        if (columnas == null || columnas.Count == 0)
            return new List<List<int>>();

        int alto = columnas[0].Count;
        int ancho = columnas.Count;

        List<List<int>> nueva = new List<List<int>>();

        for (int y = 0; y < alto; y++)
        {
            List<int> fila = new List<int>();
            for (int x = 0; x < ancho; x++)
            {
                fila.Add(columnas[x][y]);
            }
            nueva.Add(fila);
        }

        return nueva;
    }

    void ConstruirModeloMarkov()
    {
        modeloMarkov.Clear();
        int n = ordenMarkov;

        for (int i = 0; i < columnas.Count - n; i++)
        {
            // contexto: las últimas n columnas
            string clave = string.Join("|", columnas.Skip(i).Take(n).Select(c => string.Join(",", c)));

            // columna siguiente
            var siguiente = columnas[i + n];

            if (!modeloMarkov.ContainsKey(clave))
                modeloMarkov[clave] = new List<List<int>>();

            modeloMarkov[clave].Add(siguiente);
        }

        Debug.Log($"Modelo de Markov construido con {modeloMarkov.Count} estados (orden {ordenMarkov}).");
    }

}
