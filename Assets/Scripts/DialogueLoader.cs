using UnityEngine;
using System.IO;

public class DialogueLoader : MonoBehaviour
{
    public string dialogue_Name = "Graph_Dialogue";
    string general_Path = Application.dataPath + "/Dialogues-Save/";
    string extension = ".json";

    void Start()
    {
        // Cargar el diálogo al iniciar el juego
        GraphData graphData = LoadDialogue(general_Path + dialogue_Name + extension);
        if (graphData != null)
        {
            ProcessDialogue(graphData);
        }
    }

    public GraphData LoadDialogue(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            GraphData graphData = JsonUtility.FromJson<GraphData>(json);
            return graphData; // Devolver el objeto GraphData
        }
        else
        {
            Debug.LogError("File not found: " + path);
            return null; // Devolver null si el archivo no existe
        }
    }

    private void ProcessDialogue(GraphData graphData)
    {
        foreach (var nodeData in graphData.Nodes)
        {
            if (nodeData.Type == NodeType.Dialogue)
            {
                Debug.Log($"Speaker: {nodeData.Speaker}, Dialogue: {nodeData.Dialogue}");
            }
            else if (nodeData.Type == NodeType.Choice)
            {
                Debug.Log($"Choices: {string.Join(", ", nodeData.Choices)}");
            }
        }
    }
}