using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;
using System.IO;
using System.Linq;

public class DialogueTool : EditorWindow
{
    private List<NodeBase> nodes = new List<NodeBase>();
    private NodeBase selectedNode = null;
    private NodeBase selectedOutputNode = null;

    private GUIStyle dialogueNodeStyle;
    private GUIStyle choiceNodeStyle;
    private GUIStyle conditionNodeStyle;
    private GUIStyle scriptStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle rightPanelStyle; // Estilo para el panel derecho

    private Vector2 dragOffset;
    private bool isConnecting = false; // Bandera para controlar la conexión en progreso.

    private float panelWidth = 300f; // Ancho del panel derecho

    public string file = "Graph_Dialogue";
    public string path = Application.dataPath + "/Dialogues-Save/";

    [MenuItem("Tool/Dialogue")]
    public static void ShowWindow()
    {
        GetWindow<DialogueTool>("DialogueTOOL");
    }

    private void OnEnable()
    {
        // Estilos para los nodos
        dialogueNodeStyle = CreateNodeStyle(Color.cyan);
        choiceNodeStyle = CreateNodeStyle(Color.yellow);
        conditionNodeStyle = CreateNodeStyle(Color.magenta);
        scriptStyle = CreateNodeStyle(Color.red);
        selectedNodeStyle = CreateNodeStyle(Color.green);

        // Estilo personalizado para el panel derecho
        rightPanelStyle = new GUIStyle();
        rightPanelStyle.normal.background = MakeColorTexture(1, 1, new Color(0.5f, 0.5f, 0.5f));

        rightPanelStyle.padding = new RectOffset(10, 10, 10, 10);
    }

    private GUIStyle CreateNodeStyle(Color color)
    {
        var style = new GUIStyle();
        style.normal.background = MakeColorTexture(1, 1, color);
        style.border = new RectOffset(12, 12, 12, 12);
        return style;
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawNodes();
        DrawConnections();
        DrawMainArea();
        DrawRightPanel();
        ProcessEvents(Event.current);
    }

    private void DrawToolbar()
    {
        file = GUILayout.TextField(file, GUILayout.Width(position.width - panelWidth));

        if (GUILayout.Button("Export Dialogue", GUILayout.Width(position.width - panelWidth)))
            ExportToJSON(file);

        GUILayout.Space(15f);

        path = GUILayout.TextField(path, GUILayout.Width(position.width - panelWidth));

        if (GUILayout.Button("Import Dialogue", GUILayout.Width(position.width - panelWidth)))
            ImporFromJSON(path + ".json");
    }

    private void ExportToJSON(string fileName)
    {
        GraphData graphData = new GraphData();

        // Convert nodes into serializable format
        foreach (var node in nodes)
        {
            NodeData nodeData = new NodeData
            {
                Type = node.Type,
                Position = node.Position,
                Title = node.Title,
                Balloon = (Sprite)node.balloon,
                NPCSprite = (Sprite)node.npcSprite,
                Material = (Material)node.material,
                Sound = (AudioClip)node.sound,
                Speaker = (node as DialogueNode)?.Speaker,
                Dialogue = (node as DialogueNode)?.Dialogue,
                RandomDialogue = (node as DialogueNode)?.randomDialogue ?? false,
                RandomDialogues = (node as DialogueNode)?.randomDialogues,
                Choices = (node as ChoiceNode)?.Choices,
                Condition = (node as ConditionNode)?.Condition,
                ConnectedNodeIDs = node.ConnectedNodes.Select(n => nodes.IndexOf(n)).ToList()
            };

            graphData.Nodes.Add(nodeData);
        }

        string json = JsonUtility.ToJson(graphData, true);

        if (!Directory.Exists(Application.dataPath + "/Dialogues-Save"))
            Directory.CreateDirectory(Application.dataPath + "/Dialogues-Save");

        File.WriteAllText(Application.dataPath + "/Dialogues-Save/" + fileName + ".json", json);
        Debug.Log("Dialogue exported to JSON!");
    }

    private void ImporFromJSON(string loadPath)
    {
        if (!File.Exists(loadPath))
        {
            Debug.LogError("File not found: " + loadPath);
            return;
        }

        string json = File.ReadAllText(loadPath);
        GraphData graphData = JsonUtility.FromJson<GraphData>(json);

        nodes.Clear();

        // Recreate nodes
        foreach (var nodeInfo in graphData.Nodes)
        {
            NodeBase newNode = CreateNodeFromData(nodeInfo);
            nodes.Add(newNode);
        }

        // Restore connections
        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            var nodeInfo = graphData.Nodes[i];
            var node = nodes[i];

            foreach (int connectedIndex in nodeInfo.ConnectedNodeIDs)
            {
                if (connectedIndex >= 0 && connectedIndex < nodes.Count)
                {
                    node.ConnectedNodes.Add(nodes[connectedIndex]);
                }
            }
        }

        Debug.Log("Graph inportet from JSON!");
    }

    private NodeBase CreateNodeFromData(NodeData nodeData)
    {
        NodeBase node = null;

        //TODO: Set the variables of each type of node

        switch (nodeData.Type)
        {
            case NodeType.Dialogue:
                node = new DialogueNode(nodeData.Position);
                (node as DialogueNode).Dialogue = nodeData.Dialogue;
                (node as DialogueNode).randomDialogue = nodeData.RandomDialogue;
                (node as DialogueNode).randomDialogues = nodeData.RandomDialogues;
                break;

            case NodeType.Choice:
                node = new ChoiceNode(nodeData.Position);
                (node as ChoiceNode).Choices = nodeData.Choices;
                break;
            
            case NodeType.Condition:
                node = new ConditionNode(nodeData.Position);
                (node as ConditionNode).Condition = nodeData.Condition;
                break;
            
            case NodeType.Script:
                node = new ScriptNode(nodeData.Position);
                break;
        }

        node.Speaker = nodeData.Speaker;
        node.Title = nodeData.Title;
        node.balloon = nodeData.Balloon;
        node.npcSprite = nodeData.NPCSprite;
        node.material = nodeData.Material;
        node.sound = nodeData.Sound;

        return node;
    }

    private void DrawMainArea()
    {
        // Área principal (izquierda)
        Rect mainAreaRect = new Rect(0, 0, position.width - panelWidth, position.height);
        GUILayout.BeginArea(mainAreaRect);
        DrawNodes();
        DrawConnections();
        GUILayout.EndArea();
    }

    private void DrawRightPanel()
    {
        // Área derecha (panel de información)
        Rect panelRect = new Rect(position.width - panelWidth, 0, panelWidth, position.height);
        GUILayout.BeginArea(panelRect, rightPanelStyle);
        EditorGUILayout.LabelField("Node Information", EditorStyles.boldLabel);

        if (selectedNode != null)
        {
            selectedNode.DisplayInfo();
        }
        else
        {
            EditorGUILayout.LabelField("No node selected.");
        }

        GUILayout.EndArea();
    }

    private void DrawConnections()
    {
        foreach (var node in nodes)
        {
            foreach (var connectedNode in node.ConnectedNodes)
            {
                Vector2 start = node.OutputPort;
                Vector2 end = connectedNode.InputPort;
                Vector2 direction = (end - start).normalized; //Line direcction

                // Draw line connection
                Handles.color = Color.red;
                Handles.DrawLine(start, end);

                // Draw the arrow at the end of the line
                DrawArrow(end, direction);


                // Detectar clics en la conexion
                if (HandleUtility.DistancePointBezier(Event.current.mousePosition, node.OutputPort, connectedNode.InputPort,
                    node.OutputPort + Vector2.down * 50f, connectedNode.InputPort + Vector2.up * 50f) < 10f)
                {
                    if (Event.current.type == EventType.ContextClick) // Click derecho
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Delete Connection"), false, () =>
                        {
                            node.ConnectedNodes.Remove(connectedNode); // Eliminar la conexi n
                            GUI.changed = true;
                        });
                        menu.ShowAsContext();
                        Event.current.Use(); // Consumir el evento
                    }
                }

            }
        }

        // Dibujar línea temporal para la conexión en progreso
        if (isConnecting && selectedOutputNode != null)
        {

            Vector2 start = selectedOutputNode.OutputPort;
            Vector2 end = Event.current.mousePosition;
            Vector2 direction = (end - start).normalized; //Line direcction

            Handles.color = Color.red;
            Handles.DrawLine(selectedOutputNode.OutputPort, Event.current.mousePosition);

            DrawArrow(end, direction);

        }
    }

    //Funcion that draws the arrow
    private void DrawArrow(Vector2 position, Vector2 direction)
    {
        float arrowSize = 10f;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x); //90  Rotation

        Vector2 point1 = position;
        Vector2 point2 = position - direction * arrowSize + perpendicular * (arrowSize / 2);
        Vector2 point3 = position - direction * arrowSize - perpendicular * (arrowSize / 2);

        Handles.color = Color.red;
        Handles.DrawAAPolyLine(3, point1, point2, point3, point1);
    }

    private void DrawNodes()
    {
        foreach (var node in nodes)
        {
            var style = node.Type switch
            {
                NodeType.Dialogue => dialogueNodeStyle,
                NodeType.Choice => choiceNodeStyle,
                NodeType.Condition => conditionNodeStyle,
                NodeType.Script => scriptStyle,
                _ => GUIStyle.none
            };

            node.Draw(selectedNode == node ? selectedNodeStyle : style);
        }
    }

    private void ProcessEvents(Event e)
    {
        // Verify if the click is in the right panel
        Rect panelRect = new Rect(position.width - panelWidth, 0, panelWidth, position.height);
        bool isClickInsideRightPanel = panelRect.Contains(e.mousePosition);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && !isClickInsideRightPanel) HandleLeftClick(e);
                if (e.button == 1 && !isClickInsideRightPanel) HandleRightClick(e);
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && !isClickInsideRightPanel)
                {
                    if (isConnecting) HandleConnectionDrag(e);
                    else if (selectedNode != null) DragNode(e);
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0 && !isClickInsideRightPanel) HandleMouseUp(e);
                break;
        }
    }

    private void HandleLeftClick(Event e)
    {
        NodeBase clickedNode = GetNodeAtPosition(e.mousePosition);

        if (clickedNode != null)
        {
            // Detectar clic en el puerto de salida
            if (Vector2.Distance(e.mousePosition, clickedNode.OutputPort) < 20f)
            {
                selectedOutputNode = clickedNode;
                isConnecting = true;
            }
            // Detectar clic en el nodo
            else
            {
                if (selectedNode != clickedNode && selectedNode != null)
                {

                }
                selectedNode = clickedNode;
                dragOffset = e.mousePosition - clickedNode.Position;
                Repaint();
            }
        }
        else
        {
            selectedNode = null;

            Repaint();
        }
    }

    private void HandleRightClick(Event e)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Add Dialogue Node"), false, () => AddNode(e.mousePosition, NodeType.Dialogue));
        menu.AddItem(new GUIContent("Add Choice Node"), false, () => AddNode(e.mousePosition, NodeType.Choice));
        menu.AddItem(new GUIContent("Add Condition Node"), false, () => AddNode(e.mousePosition, NodeType.Condition));
        menu.AddItem(new GUIContent("Add Script Node"), false, () => AddNode(e.mousePosition, NodeType.Script));

        NodeBase clickedNode = GetNodeAtPosition(e.mousePosition);
        if (clickedNode != null)
        {
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Node"), false, () => nodes.Remove(clickedNode));
        }

        foreach (var node in nodes)
        {
            foreach (var connectedNode in node.ConnectedNodes)
            {
                if (HandleUtility.DistancePointBezier(
                        e.mousePosition,
                        node.OutputPort,
                        connectedNode.InputPort,
                        node.OutputPort + Vector2.down * 50f,
                        connectedNode.InputPort + Vector2.up * 50f
                    ) < 10f)
                {
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete Connection"), false, () =>
                    {
                        if (EditorUtility.DisplayDialog("Delete Connection", "Are you sure you want to delete this connection?", "Yes", "No"))
                        {
                            node.ConnectedNodes.Remove(connectedNode);
                            GUI.changed = true;
                        }
                    });
                }
            }
        }

        menu.ShowAsContext();
    }
    private void HandleMouseUp(Event e)
    {
        if (isConnecting)
        {
            NodeBase targetNode = GetNodeAtPosition(e.mousePosition);

            if (targetNode != null && Vector2.Distance(e.mousePosition, targetNode.InputPort) < 10f)
            {
                // Completar conexión
                selectedOutputNode.ConnectedNodes.Add(targetNode);
            }

            isConnecting = false;
            selectedOutputNode = null;
        }
        else
        {
            //selectedNode = null;
        }
    }
    private void HandleConnectionDrag(Event e)
    {
        // Actualizar la conexión temporal mientras se arrastra el mouse
        Repaint();
    }
    private void DragNode(Event e)
    {
        selectedNode.Position = e.mousePosition - dragOffset;
        GUI.changed = true;
    }

    private void AddNode(Vector2 position, NodeType type)
    {
        switch (type)
        {
            case NodeType.Dialogue:
                nodes.Add(new DialogueNode(position));
                break;
            case NodeType.Choice:
                nodes.Add(new ChoiceNode(position));
                break;
            case NodeType.Condition:
                nodes.Add(new ConditionNode(position));
                break;
            case NodeType.Script:
                nodes.Add(new ScriptNode(position));
                break;
        }
    }

    private NodeBase GetNodeAtPosition(Vector2 position)
    {
        foreach (var node in nodes)
        {
            if (node.Rect.Contains(position)) return node;
        }
        return null;
    }

    private Texture2D MakeColorTexture(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}

public enum NodeType
{
    Dialogue, Choice, Condition, Script, Base
}

[System.Serializable]
public class GraphData
{
    public List<NodeData> Nodes = new List<NodeData>();

    public void AddDialogue(string speaker, string line)
    {
        //Dialogues.Add(new NodeData { Speaker = speaker, Line = line });
    }
}

[System.Serializable]
public class NodeData
{
    public NodeType Type;
    public Vector2 Position;
    public string Title;
    public string Speaker;
    public string Dialogue;
    public bool RandomDialogue;
    public List<string> RandomDialogues;
    public List<string> Choices;
    public List<string> Condition;
    public List<int> ConnectedNodeIDs; // Store connections by node index
    public Sprite Balloon;
    public Sprite NPCSprite;
    public Material Material;
    public AudioClip Sound;
}

public class NodeBase
{
    public Rect Rect;
    public string Title;
    public string Speaker;
    public NodeType Type;

    public Object balloon;
    public Object npcSprite;
    public Object material;
    public Object sound;

    public List<NodeBase> ConnectedNodes = new List<NodeBase>();

    public Vector2 Position
    {
        get => new Vector2(Rect.x, Rect.y);
        set
        {
            Rect.x = value.x;
            Rect.y = value.y;
        }
    }

    public Vector2 InputPort => new Vector2(Rect.x + Rect.width / 2, Rect.y + 5f);
    public Vector2 OutputPort => new Vector2(Rect.x + Rect.width / 2, Rect.y + Rect.height - 5f);

    public NodeBase(Vector2 position, float width, float height, string title, NodeType type)
    {
        Rect = new Rect(position.x, position.y, width, height);
        Title = title;
        Type = type;
    }

    public virtual void Draw(GUIStyle style)
    {
        GUI.Box(Rect, "", style);
        GUI.contentColor = Color.black;

        GUILayout.BeginArea(Rect);
        GUILayout.FlexibleSpace();
        GUIStyle titleNode = new GUIStyle();

        titleNode.alignment = TextAnchor.MiddleCenter;
        
        titleNode.fontSize = 35;

        GUILayout.Label(Title, titleNode, GUILayout.ExpandWidth(true));

        titleNode.fontSize = 15;

        EditorGUILayout.LabelField(Type.ToString(), titleNode);

        GUILayout.Space(25);

        GUILayout.EndArea();

        DrawPort(InputPort, Color.green);
        DrawPort(OutputPort, Color.red);
    }

    public virtual void DisplayInfo()
    {
        //EditorGUILayout.LabelField(Title);
        EditorGUILayout.LabelField("Title Node:");
        GUI.contentColor = Color.white;
        Title = EditorGUILayout.TextField(Title);

        GUI.contentColor = Color.black;
        EditorGUILayout.LabelField("Speaker:");
        GUI.contentColor = Color.white;
        Speaker = EditorGUILayout.TextField(Speaker);

        GUI.contentColor = Color.black;
        EditorGUILayout.LabelField("Balloon shape:");
        GUI.contentColor = Color.white;
        balloon = EditorGUILayout.ObjectField(balloon, typeof(Sprite), true); //Script = MonoScript

        //GUI.contentColor = Color.black;
        //EditorGUILayout.LabelField("Npc Sprite:");
        //GUI.contentColor = Color.white;
        //npcSprite = EditorGUILayout.ObjectField(npcSprite, typeof(Sprite), true); 

        GUI.contentColor = Color.black;
        EditorGUILayout.LabelField("Material:");
        GUI.contentColor = Color.white;
        material = EditorGUILayout.ObjectField(material, typeof(Material), true);

        GUI.contentColor = Color.black;
        EditorGUILayout.LabelField("Play sound:");
        GUI.contentColor = Color.white;
        sound = EditorGUILayout.ObjectField(sound, typeof(AudioClip), true);
    }

    private void DrawPort(Vector2 position, Color color)
    {
        Handles.color = color;
        Handles.DrawSolidDisc(position, Vector3.forward, 8f);
    }
}

public class DialogueNode : NodeBase
{
    public string Dialogue;
    private GUIStyle textStyle;
    public bool randomDialogue;
    public List<string> randomDialogues = new List<string>();

    //private void OnEnable()
    //{
    //   //Create and configure the text
    //   textStyle = new GUIStyle(EditorStyles.label);
    //   textStyle.normal.textColor = Color.black; // Change the desired color
    //}

    public DialogueNode(Vector2 position) : base(position, 200, 100, "Dialogue Node", NodeType.Dialogue)
    {
        GUI.contentColor = Color.white;
        Speaker = "Speaker";
        Dialogue = "Dialogue text here...";
        randomDialogue = false;
    }

    public override void Draw(GUIStyle style)
    {
        base.Draw(style);
        GUILayout.BeginArea(Rect);
        EditorGUILayout.Space(15f);
        GUILayout.EndArea();
    }

    public override void DisplayInfo()
    {
        base.DisplayInfo();

        EditorGUILayout.BeginHorizontal();
        GUI.contentColor = Color.black;
        EditorGUILayout.LabelField("Random --> " + (randomDialogue == true ? "Yes" : "No"));
        GUI.contentColor = Color.white;
        randomDialogue = EditorGUILayout.Toggle(randomDialogue);
        EditorGUILayout.EndHorizontal();

        if (!randomDialogue)
        {
            GUI.contentColor = Color.black;
            EditorGUILayout.LabelField("Dialogue:");
            GUI.contentColor = Color.white;
            Dialogue = EditorGUILayout.TextArea(Dialogue, GUILayout.Height(50));
        }
        else
        {
            for (int i = 0; i < randomDialogues.Count; i++)
            {
                randomDialogues[i] = EditorGUILayout.TextField(randomDialogues[i]);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add")) randomDialogues.Add($"Option {randomDialogues.Count + 1}");
            if (GUILayout.Button("Remove") && randomDialogues.Count > 0) randomDialogues.RemoveAt(randomDialogues.Count - 1);
            if (GUILayout.Button("Remove All") && randomDialogues.Count > 0) randomDialogues.Clear();
            EditorGUILayout.EndHorizontal();
        }
    }
}

public class ChoiceNode : NodeBase
{
    public List<string> Choices;

    public ChoiceNode(Vector2 position) : base(position, 200, 100, "Choices Node", NodeType.Choice)
    {
        Speaker = "Speaker";

        Choices = new List<string> { "Option 1", "Option 2" };
    }

    public override void Draw(GUIStyle style)
    {
        base.Draw(style);
        GUILayout.BeginArea(Rect);
        EditorGUILayout.Space(15f);
        GUILayout.EndArea();
    }

    public override void DisplayInfo()
    {
        base.DisplayInfo();

        EditorGUILayout.LabelField("Choices:");

        for (int i = 0; i < Choices.Count; i++)
        {
            Choices[i] = EditorGUILayout.TextField($"Choice {i + 1}", Choices[i]);
        }

        if (GUILayout.Button("Add Choice")) Choices.Add($"Option {Choices.Count + 1}");
    }
}

public class ConditionNode : NodeBase
{
    public List<bool> selectCondition;
    public List<string> Condition;
    public bool randomCondition;
    public string tag;

    bool test;
    string status = "Select a GameObject";

    public ConditionNode(Vector2 position) : base(position, 200, 100, "Condition Node", NodeType.Condition)
    {
        Condition = new List<string> { "Condition 1", "Condition 2" };
        selectCondition = new List<bool> { false, false };
    }

    public override void Draw(GUIStyle style)
    {
        base.Draw(style);
        GUILayout.BeginArea(Rect);
        EditorGUILayout.Space(15f);
        GUILayout.EndArea();
    }

    public override void DisplayInfo()
    {
        base.DisplayInfo();

        EditorGUILayout.LabelField("Conditions:");

        randomCondition = EditorGUILayout.Toggle(randomCondition);

        test = EditorGUILayout.Foldout(test, status);

        if(test)
        {

        }

        for (int i = 0; i < Condition.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            selectCondition[i] = EditorGUILayout.Toggle(selectCondition[i]);
            Condition[i] = EditorGUILayout.TextField($"Condition {i + 1}", Condition[i]);
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Condition")) Condition.Add($"Condition {Condition.Count + 1}");
    }


    /*
     Dialeg amb condicions, llista de condicions, llista de outputs
     */
}

//Can't use an array in the function of the script called
public class ScriptNode : NodeBase
{
    public MonoBehaviour triggerScript;
    public MonoBehaviour preScript;
    private string[] functionNames;
    private int selectedFunctionIndex = 0;
    private int preIndex = -1;

    //array of types
    System.Reflection.ParameterInfo[] parameters;

    List<object> parametersValue = new List<object>();

    public ScriptNode(Vector2 position) : base(position, 200, 100, "Script Node", NodeType.Script)
    {
        triggerScript = null;
    }

    public override void Draw(GUIStyle style)
    {
        base.Draw(style);
        GUILayout.BeginArea(Rect);
        EditorGUILayout.Space(15f);
        GUILayout.EndArea();
    }

    public override void DisplayInfo()
    {
        GUI.contentColor = Color.black;
        EditorGUILayout.LabelField("Script:");
        GUI.contentColor = Color.white;

        triggerScript = (MonoBehaviour)EditorGUILayout.ObjectField(triggerScript, typeof(MonoBehaviour), true);

        if (triggerScript != null)
        {
            // Get all public methods from the selected script
            if (functionNames == null || functionNames.Length == 0)
            {
                GetFunctionNames();
            }

            if (preScript != triggerScript)
            {
                GetFunctionNames();

                preScript = triggerScript;
            }

            // Display a dropdown to select a function
            if (functionNames != null && functionNames.Length > 0)
            {
                GUI.contentColor = Color.black;
                selectedFunctionIndex = EditorGUILayout.Popup("Select Function", selectedFunctionIndex, functionNames);
                GUI.contentColor = Color.white;

                if (preIndex != selectedFunctionIndex)
                {
                    parametersValue.Clear();

                    preIndex = selectedFunctionIndex;
                }

                // Check if the selected function requires parameters
                var methodName = functionNames[selectedFunctionIndex];
                var method = triggerScript.GetType().GetMethod(methodName);

                if (method != null && HasParameters(method))
                {
                    parameters = method.GetParameters();

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUI.contentColor = Color.black;
                        EditorGUILayout.LabelField($"- {parameters[i].Name} ({parameters[i].ParameterType.Name})");
                        GUI.contentColor = Color.white;

                        if (parameters[i].ParameterType == typeof(int))
                        {
                            int aux = 0;

                            if (parametersValue.Count == 0)
                            {
                                parametersValue.Add(aux);
                            }
                            else if (parametersValue.Count < parameters.Length)
                            {
                                parametersValue.Add(aux);
                            }

                            aux = EditorGUILayout.IntField((int)parametersValue[i]);
                            parametersValue[i] = aux;
                        }
                        else if (parameters[i].ParameterType == typeof(string))
                        {
                            string aux = "";

                            if (parametersValue.Count == 0)
                            {
                                parametersValue.Add(aux);
                            }
                            else if(parametersValue.Count < parameters.Length)
                            {
                                parametersValue.Add(aux);
                            }

                            aux = EditorGUILayout.TextField((string)parametersValue[i]);
                            parametersValue[i] = aux;
                        }
                        else if (parameters[i].ParameterType == typeof(bool))
                        {
                            bool aux = false;

                            if (parametersValue.Count == 0)
                            {
                                parametersValue.Add(aux);
                            }
                            else if (parametersValue.Count < parameters.Length)
                            {
                                parametersValue.Add(aux);
                            }

                            aux = EditorGUILayout.Toggle((bool)parametersValue[i]);
                            parametersValue[i] = aux;
                        }
                        else if (parameters[i].ParameterType.IsSubclassOf(typeof(Object)))
                        {
                            Object aux = null;

                            if (parametersValue.Count == 0)
                            {
                                parametersValue.Add(aux);
                            }
                            else if (parametersValue.Count < parameters.Length)
                            {
                                parametersValue.Add(aux);
                            }

                            aux = EditorGUILayout.ObjectField((Object)parametersValue[i], parameters[i].ParameterType, true);
                            parametersValue[i] = aux;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No public methods found in the selected script.");
            }
        }

        // Button to trigger the selected function
        if (GUILayout.Button("Trigger Function") && triggerScript != null && functionNames.Length > 0)
        {
            TriggerSelectedFunction();
        }
    }

    private bool HasParameters(System.Reflection.MethodInfo method)
    {
        return method.GetParameters().Length > 0;
    }

    private void GetFunctionNames()
    {
        // Use reflection to get all public methods from the selected script
        var methods = triggerScript.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        functionNames = new string[methods.Length];

        for (int i = 0; i < methods.Length; i++)
        {
            functionNames[i] = methods[i].Name;
        }
    }

    private void TriggerSelectedFunction()
    {
        if (triggerScript != null && functionNames.Length > 0)
        {
            var methodName = functionNames[selectedFunctionIndex];
            var method = triggerScript.GetType().GetMethod(methodName);

            if (method != null)
            {
                method.Invoke(triggerScript, parametersValue.ToArray());
                Debug.Log($"Function '{methodName}' triggered successfully.");
            }
            else
            {
                Debug.LogError($"Function '{methodName}' not found in the selected script.");
            }
        }
    }
}