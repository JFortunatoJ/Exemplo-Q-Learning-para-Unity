using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Action { Up, Down, Left, Right }

[System.Serializable]
public class Row
{
    public Tile[] Collumns;
}

[System.Serializable]
public class ActionQuality
{
    public Action Action;
    public float Quality;

    public ActionQuality(Action a, float q)
    {
        Action = a;
        Quality = q;
    }
}

[System.Serializable]
public class QCell
{
    public Tile State;
    public List<ActionQuality> Actions;
}

public class PersonagemIA : MonoBehaviour
{
    [SerializeField] private Row[] tileMap = new Row[4];
    [Space]
    [SerializeField] private Tile Alvo;

    [SerializeField] private QCell[,] TabelaQ = new QCell[4, 4];

    private int curCollumnIndex = 0;
    private int curRowIndex = 0;

    [SerializeField] private bool keyBoardTest = false;
    [SerializeField] private float speed = 0.2f;
    [Space]
    [SerializeField] private Text txtChapters;


    //Q-Learning
    private const float ALFA = 0.1f;
    public const float DESCONTO = 0.9f;

    private Action action;
    private Tile currentState;
    private Tile nextState;
    private float R;

    private float QPred;
    private float QTarget;

    private int chapter;
    private int Chapter
    {
        get { return chapter; }
        set
        {
            chapter = value;
            txtChapters.text = string.Concat("Episódio: ", chapter);
        }
    }

    bool finished = false;


    void Start()
    {
        Chapter = 0;
        BuildTable();
        StartCoroutine(QLearning());
    }

    private void InitChapter()
    {
        curCollumnIndex = 0;
        curRowIndex = 0;
        currentState = tileMap[curRowIndex].Collumns[curCollumnIndex];
        transform.position = currentState.transform.position;
        finished = false;
    }

    private void Update()
    {
        if (keyBoardTest)
        {
            if (Input.GetKeyDown(KeyCode.W))
                Reward(Action.Up);
            if (Input.GetKeyDown(KeyCode.S))
                Reward(Action.Down);
            if (Input.GetKeyDown(KeyCode.A))
                Reward(Action.Left);
            if (Input.GetKeyDown(KeyCode.D))
                Reward(Action.Right);
        }
    }

    private IEnumerator QLearning()
    {
        do
        {
            InitChapter();

            while (!finished)
            {
                yield return new WaitForSeconds(speed);

                action = ChooseAction();
                Reward(action);
                QPred = GetValueOfTable(action, currentState);
                if (!nextState.Equals(Alvo))
                {
                    QTarget = R + DESCONTO * GetMaxValueOfTable(nextState);
                }
                else
                {
                    QTarget = R;
                    finished = true;
                }

                InsertValueOnTable(ALFA * (QTarget - QPred), currentState, action);
                currentState = nextState;
                transform.position = currentState.transform.position;
            }

            ShowTabel();

            Chapter++;

            yield return new WaitForSeconds(1);
        } while (Chapter < 100);
        print("Terminou");
    }

    private void Reward(Action action)
    {
        switch (action)
        {
            case Action.Down:
                if (curRowIndex + 1 < 4)
                    curRowIndex++;
                break;
            case Action.Up:
                if (curRowIndex - 1 >= 0)
                    curRowIndex--;
                break;
            case Action.Left:
                if (curCollumnIndex - 1 >= 0)
                    curCollumnIndex--;
                break;
            case Action.Right:
                if (curCollumnIndex + 1 < 4)
                    curCollumnIndex++;
                break;
        }

        R = tileMap[curRowIndex].Collumns[curCollumnIndex].reward;
        nextState = tileMap[curRowIndex].Collumns[curCollumnIndex];

        if (R == -1)
            finished = true;
    }

    private Action ChooseAction()
    {
        List<Action> possiveisAcoes = new List<Action>();
        //Esquerda
        if (curCollumnIndex - 1 >= 0)
            possiveisAcoes.Add(Action.Left);

        //Direita
        if (curCollumnIndex + 1 < tileMap[0].Collumns.Length)
            possiveisAcoes.Add(Action.Right);

        //Cima
        if (curRowIndex - 1 >= 0)
            possiveisAcoes.Add(Action.Up);

        //Baixo
        if (curRowIndex + 1 < tileMap.Length)
            possiveisAcoes.Add(Action.Down);


        Action bestAction = possiveisAcoes[Random.Range(0, possiveisAcoes.Count)];
        float maxQuality = 0;
        foreach (ActionQuality item in TabelaQ[curRowIndex, curCollumnIndex].Actions)
        {
            if (item.Quality > maxQuality)
            {
                maxQuality = item.Quality;
                bestAction = item.Action;
            }
        }

        return bestAction;
    }

    private void InsertValueOnTable(float _quality, Tile _state, Action _action)
    {
        foreach (QCell item in TabelaQ)
        {
            if (item.State.Equals(_state))
            {
                for (int i = 0; i < item.Actions.Count; i++)
                {
                    if (item.Actions[i].Action.Equals(_action))
                    {
                        item.Actions[i].Quality += _quality;
                        break;
                    }
                }
                break;
            }
        }
    }

    private float GetValueOfTable(Action _action, Tile _state)
    {
        foreach (QCell item in TabelaQ)
        {
            if (item.State.Equals(_state))
            {
                foreach (ActionQuality action in item.Actions)
                {
                    if (action.Action.Equals(_action))
                    {
                        return action.Quality;
                    }
                }
                break;
            }
        }
        return float.NaN;
    }

    private float GetMaxValueOfTable(Tile _state)
    {
        float max = 0;
        foreach (QCell item in TabelaQ)
        {
            if (item.State.Equals(_state))
            {
                foreach (ActionQuality action in item.Actions)
                {
                    if (action.Quality > max)
                    {
                        max = action.Quality;
                    }
                }
                break;
            }
        }

        return max;
    }

    private void BuildTable()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                TabelaQ[i, j] = new QCell();
                TabelaQ[i, j].State = tileMap[i].Collumns[j];
                TabelaQ[i, j].Actions = new List<ActionQuality>();

                //Esquerda
                if (j - 1 >= 0)
                    TabelaQ[i, j].Actions.Add(new ActionQuality(Action.Left, 0));

                //Direita
                if (j + 1 < tileMap[0].Collumns.Length)
                    TabelaQ[i, j].Actions.Add(new ActionQuality(Action.Right, 0));

                //Cima
                if (i - 1 >= 0)
                    TabelaQ[i, j].Actions.Add(new ActionQuality(Action.Up, 0));

                //Baixo
                if (i + 1 < tileMap.Length)
                    TabelaQ[i, j].Actions.Add(new ActionQuality(Action.Down, 0));
            }
        }
    }

    private void ShowTabel()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                string s = TabelaQ[i, j].State.transform.name + "\n";
                for (int k = 0; k < TabelaQ[i, j].Actions.Count; k++)
                {
                    s += "Ação: " + TabelaQ[i, j].Actions[k].Action + "\n" + "Valor: " + TabelaQ[i, j].Actions[k].Quality + "\n";
                }
                print(s);
            }
        }
    }
}