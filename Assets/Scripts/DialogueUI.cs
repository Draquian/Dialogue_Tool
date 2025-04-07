using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    public Text speakerText;
    public Text dialogueText;
    public GameObject choicePanel;
    public GameObject balloon;
    public GameObject npcSprite;
    public GameObject sound;
    public Button[] choiceButtons;

    private GraphData currentDialogue;
    private int currentNodeIndex = 0;

    [SerializeField] TextMeshProUGUI _textMeshPro;
    [SerializeField] float timeBtwnChars;
    [SerializeField] float timeBtwnWords;

    public string test;

    int i = 0;

    private string npcSpritePath;
    void Start()
    {
        // Cargar el diálogo (puedes hacerlo desde otro script)
        DialogueLoader loader = FindObjectOfType<DialogueLoader>();
        if (loader != null)
        {
            currentDialogue = loader.LoadDialogue("Assets/Dialogues-Save/Graph_Dialogue.json");
            npcSpritePath = "Assets/_Scripts/Dialogue Tool/";
            if (currentDialogue != null)
            {
                ShowNode(currentNodeIndex);
            }
        }
    }

    private void Update()
    {
       if(Input.GetKeyDown(KeyCode.Space))
       {
            ShowNode(currentNodeIndex + 1);
       }
    }

    public void ShowNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= currentDialogue.Nodes.Count)
        {
            Debug.LogError("Invalid node index");
            return;
        }

        NodeData node = currentDialogue.Nodes[nodeIndex];

        balloon.GetComponent<Image>().sprite = node.Balloon;
        balloon.GetComponent<Image>().material = node.Material;

        npcSprite.GetComponent<Image>().sprite = Resources.Load<Sprite>(node.Speaker);
        npcSprite.GetComponent<Image>().material = node.Material;

        if (node.Sound != null)
        {
            sound.GetComponent<AudioSource>().clip = node.Sound;
            sound.GetComponent<AudioSource>().Play();
        }

        if (node.Type == NodeType.Dialogue)
        {
            speakerText.text = node.Speaker;

            test = node.Dialogue;

            EndCheck();

            /*if(!node.RandomDialogue)
            {
                dialogueText.text = node.Dialogue;
            }
            else
            {
                int n = Random.Range(0, node.RandomDialogues.Count - 1);
                dialogueText.text = node.RandomDialogues[n];
            }*/

            //TODO::ROGER
            //lletra per lletra (+ accelerar aparcció) i al final salatar seguent amb un boto
            /*for (int i = 0; i < node.Dialogue.Length; i++)
            {
                dialogueText.text = dialogueText.text + node.Dialogue[i].ToString();
            }*/

            choicePanel.SetActive(false); // Ocultar opciones si es un diálogo
        }
        else if (node.Type == NodeType.Choice)
        {
            speakerText.text = "Choose an option:";
            dialogueText.text = "";
            choicePanel.SetActive(true); // Mostrar opciones

            // Configurar los botones de opciones
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < node.Choices.Count)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].GetComponentInChildren<Text>().text = node.Choices[i];
                    int choiceIndex = i; // Capturar el índice para el evento
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choiceIndex));
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        //ShowNode(nodeIndex + 1);
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        NodeData currentNode = currentDialogue.Nodes[currentNodeIndex];

        if (currentNode.Type == NodeType.Choice && choiceIndex < currentNode.ConnectedNodeIDs.Count)
        {
            int nextNodeIndex = currentNode.ConnectedNodeIDs[choiceIndex];
            if (nextNodeIndex >= 0 && nextNodeIndex < currentDialogue.Nodes.Count)
            {
                currentNodeIndex = nextNodeIndex;
                ShowNode(currentNodeIndex);
            }
        }
    }

    void EndCheck()
    {
        if(i<= test.Length - 1)
        {
            _textMeshPro.text = test;
            StartCoroutine(TextToScreen());
        }
    }

    private IEnumerator TextToScreen()
    {
        _textMeshPro.ForceMeshUpdate();
        int totalVisibleCharacters = _textMeshPro.textInfo.characterCount;
        int counter = 0;

        while (true)
        {
            int visibleCount = counter % (totalVisibleCharacters + 1);
            _textMeshPro.maxVisibleCharacters = visibleCount;

            if(visibleCount >= totalVisibleCharacters)
            {
                i += 1;
                Invoke("EndCheck", timeBtwnWords);
                break;
            }

            counter += 1;
            yield return new WaitForSeconds(timeBtwnChars);
        }
    }
}