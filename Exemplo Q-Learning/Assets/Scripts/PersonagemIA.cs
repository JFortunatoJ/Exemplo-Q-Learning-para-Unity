using UnityEngine;

public enum Action { Up, Down, Left, Right }

[System.Serializable]
public class Row
{
    public Tile[] Collumns;
}

public class QTable
{
    public Action Action { get; set; }
    public Tile State { get; set; }
}

public class PersonagemIA : MonoBehaviour
{
    [SerializeField] private Row[] tileMap = new Row[4];

    private int curCollumnIndex = 0;
    private int curRowIndex = 0;

    [SerializeField] private bool keyBoardTest = true;

    void Start()
    {
        transform.position = tileMap[curRowIndex].Collumns[curCollumnIndex].transform.position;
    }

    private void Update()
    {
        if (keyBoardTest)
        {
            if (Input.GetKeyDown(KeyCode.W))
                Move(Action.Up);
            if (Input.GetKeyDown(KeyCode.S))
                Move(Action.Down);
            if (Input.GetKeyDown(KeyCode.A))
                Move(Action.Left);
            if (Input.GetKeyDown(KeyCode.D))
                Move(Action.Right);
        }
    }

    private void IA()
    {
        bool terminou = false;
        while (!terminou)
        {

        }
    }

    private bool Move(Action action)
    {
        bool r = false;
        switch (action)
        {
            case Action.Down:
                if (curRowIndex + 1 < 4)
                {
                    curRowIndex++;
                    r = true;
                }
                break;
            case Action.Up:
                if (curRowIndex - 1 >= 0)
                {
                    curRowIndex--;
                    r = true;
                }
                break;
            case Action.Left:
                if (curCollumnIndex - 1 >= 0)
                {
                    curCollumnIndex--;
                    r = true;
                }
                break;
            case Action.Right:
                if (curCollumnIndex + 1 < 4)
                {
                    curCollumnIndex++;
                    r = true;
                }
                break;
        }
        transform.position = tileMap[curRowIndex].Collumns[curCollumnIndex].transform.position;

        return r;
    }
}
