using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

//Ações
public enum Action { Up, Down, Left, Right }

//Classe 'linha' utilizada para identificar o mapa (matriz)
[System.Serializable]
public class Row
{
    public Tile[] Collumns;
}

//Classe que controla as ação e seu valor na tabela Q
[System.Serializable]
public class ActionQuality
{
    public Action Action;
    public float Quality;

    //Construtor
    public ActionQuality(Action a, float q)
    {
        Action = a;
        Quality = q;
    }
}

//Célula da tabela Q, possui o tile (estado) e as possíveis ações com seus respectivos valores
[System.Serializable]
public class QCell
{
    public Tile State;
    public List<ActionQuality> Actions;
}

public class PersonagemIA : MonoBehaviour
{
    #region <Inspector>

    //Mapa
    [SerializeField] private Row[] tileMap = new Row[4];
    [Space]

    //Alvo
    //Para alterar o objetivo, basta arrastar o tile desejado para essa variável no inspector e alterar o valor de sua recompensa
    //para 1
    [SerializeField] private Tile Alvo;

    //Tabela Q (matriz 4x4)
    [SerializeField] private QCell[,] TabelaQ = new QCell[4, 4];

    //Usado para testar a movimentação
    [SerializeField] private bool keyBoardTest = false;
    //Velocidade de movimento do personagem
    [SerializeField] private float speed = 0.2f;
    [Space]
    //Texto que mostra o capítulo atual
    [SerializeField] private Text txtChapters;
    [SerializeField] private Text txtTargetReachTimes;

    #endregion

    #region <Variáveis Privadas>

    //Variáveis usadas para navegar pelo mapa
    private int curCollumnIndex = 0;
    private int curRowIndex = 0;

    //Q-Learning
    private const float ALFA = 0.1f;
    private const float DESCONTO = 0.9f;

    //Ação a ser tomada (A)
    private Action action;
    //Estado atual (S)
    private Tile currentState;
    //Próximo estado (S')
    private Tile nextState;
    //Último estado
    private Tile lastState;
    //Recompensa do estado (R)
    private float R;

    //QPred e QAlvo
    private float QPred;
    private float QTarget;

    //Episódio atual
    private int episode;
    private int Episode
    {
        get { return episode; }
        set
        {
            episode = value;
            txtChapters.text = string.Concat("Episódio: ", episode);
        }
    }

    private int timesReachTarget;
    private int TimesReachTarget
    {
        get { return timesReachTarget; }
        set
        {
            timesReachTarget = value;
            txtTargetReachTimes.text = string.Concat("Chegou ao objetivo: ", TimesReachTarget);
        }
    }

    //Terminou?
    private bool finished = false;

    #endregion

    #region <Métodos>

    void Start()
    {
        Episode = 0;
        TimesReachTarget = 0;

        //Constrói a tabela
        BuildTable();
        //Inicia o algoritimo
        StartCoroutine(QLearning());
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

    //Começa um novo episódio
    private void InitChapter()
    {
        curCollumnIndex = 0;
        curRowIndex = 0;
        currentState = tileMap[curRowIndex].Collumns[curCollumnIndex];
        transform.position = currentState.transform.position;
        finished = false;
    }

    private IEnumerator QLearning()
    {
        do
        {
            //Começa um novo capítulo
            InitChapter();

            //Enquanto não terminou
            while (!finished)
            {
                //Escolhe uma ação
                action = ChooseAction();
                //Pega a recompesa do estado baseado na ação
                R = Reward(action);
                //Pega o valor da tabela baseado na ação tomada
                QPred = GetValueOfTable(currentState, action);
                //Se o próximo estado for diferente do alvo
                if (!nextState.Equals(Alvo))
                {
                    //Calcula o QAlvo
                    QTarget = R + DESCONTO * GetMaxValueOfTable().Quality;
                }
                else
                {
                    QTarget = R;
                    finished = true;
                }

                InsertValueOnTable(ALFA * (QTarget - QPred), currentState, action);
                lastState = currentState;
                currentState = nextState;

                Tweener t = transform.DOMove(currentState.transform.position, speed);
                yield return t.WaitForCompletion();
            }

            ShowTabel();

            Episode++;

            yield return new WaitForSeconds(1);
        } while (Episode < 100);
        print("Terminou");
    }

    //Pega a recompensa
    private float Reward(Action action)
    {

        //'Realiza' a ação
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

        float r = tileMap[curRowIndex].Collumns[curCollumnIndex].reward;
        nextState = tileMap[curRowIndex].Collumns[curCollumnIndex];

        //Se chegou em um obstáculo = termina o episódio
        if (r == -1)
            finished = true;
        else if (r == 1)
            TimesReachTarget++;

        return r;
    }

    //Escolhe a ação
    private Action ChooseAction()
    {
        //Determina as possíveis ações baseado na posição atual
        //Ex: Não dá para ir para esquerda se a posição atual estiver na primeira coluna da matriz
        //Verifica também se o próximo estado for diferente do estado anterior, assim não volta para o estado que acabou de passar
        List<Action> possiveisAcoes = new List<Action>();

        //Esquerda
        if (curCollumnIndex - 1 >= 0 && TabelaQ[curRowIndex, curCollumnIndex - 1].State != lastState)
            possiveisAcoes.Add(Action.Left);

        //Direita
        if (curCollumnIndex + 1 < tileMap[0].Collumns.Length && TabelaQ[curRowIndex, curCollumnIndex + 1].State != lastState)
            possiveisAcoes.Add(Action.Right);

        //Cima
        if (curRowIndex - 1 >= 0 && TabelaQ[curRowIndex - 1, curCollumnIndex].State != lastState)
            possiveisAcoes.Add(Action.Up);

        //Baixo
        if (curRowIndex + 1 < tileMap.Length && TabelaQ[curRowIndex + 1, curCollumnIndex].State != lastState)
            possiveisAcoes.Add(Action.Down);

        //Escolhe uma ação aleatória provisóriamente
        Action bestAction = possiveisAcoes[Random.Range(0, possiveisAcoes.Count)];

        //Escolhe a ação com maior valor na tabela Q baseado na posição atual
        float maxQuality = 0;
        foreach (ActionQuality item in TabelaQ[curRowIndex, curCollumnIndex].Actions)
        {
            if (item.Quality > maxQuality /*&& possiveisAcoes.Contains(item.Action)*/)
            {
                maxQuality = item.Quality;
                bestAction = item.Action;
            }
        }

        return bestAction;
    }

    //Inseri valor na tabela Q
    private void InsertValueOnTable(float _quality, Tile _state, Action _action)
    {
        //Procura a célula na tabela Q com o estado e ação especificados
        foreach (QCell item in TabelaQ)
        {
            if (item.State.Equals(_state))
            {
                for (int i = 0; i < item.Actions.Count; i++)
                {
                    if (item.Actions[i].Action.Equals(_action))
                    {
                        //Incrementa o valor daquela célula
                        item.Actions[i].Quality += _quality;
                        break;
                    }
                }
                break;
            }
        }
    }

    //Retorna o valor da tabela procurando pelo estado e ação
    private float GetValueOfTable(Tile _state, Action _action)
    {
        foreach (QCell item in TabelaQ)
        {
            if (item.State.Equals(_state))
            {
                foreach (ActionQuality action in item.Actions)
                {
                    if (action.Action.Equals(_action))
                        return action.Quality;
                }
                break;
            }
        }
        return float.NaN;
    }

    //Retorna o maior valor da tabela
    private ActionQuality GetMaxValueOfTable()
    {
        float max = 0;
        ActionQuality bestAction = TabelaQ[curRowIndex, curCollumnIndex].Actions[0];

        foreach (ActionQuality action in TabelaQ[curRowIndex, curCollumnIndex].Actions)
        {
            if (action.Quality > max)
            {
                max = action.Quality;
                bestAction = action;
            }
        }
        return bestAction;
    }

    //Constrói a tabela, atribuindo 0 para todas as células
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

    #endregion
}