using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

public class Caro : MonoBehaviour
{
    //perfab danh co o vuong
    public GameObject cellPrefab;
    //ban co
    public Transform boardParent;
    //kich thuoc ban co
    public int boardSize;
    //luu tru trang thai cua cac o vuong
    private string[,] board;
    //danh sach cac o da duoc tao
    private List<Cell> cells = new List<Cell>();
    //do dai chuoi thang
    public int winLength = 5;
    //cac huong thang
    private readonly int[] dx = { 1, 0, 1, -1 };
    private readonly int[] dy = { 0, 1, 1, 1 };
    //bien de luu tru nguoi choi hien tai
    public bool isPlayerTurn = true;

    public void CreateBoard()
    {
        //khoi tao ban co
        board = new string[boardSize, boardSize];
        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                var position = new Vector3(col, -row, 0);
                var go = Instantiate(cellPrefab, position,
                    Quaternion.identity, boardParent);
                var cell = go.GetComponent<Cell>();
                cell.row = row;
                cell.col = col;
                cell.manager = this; 
                int rowIndex = row; // Store the row index in a local variable
                int colIndex = col; // Store the column index in a local variable
                cell.GetComponent<Button>().onClick.AddListener(() => HandlePlayerMove(rowIndex, colIndex));
                cells.Add(cell);
            }
        }
    }
    public void HandlePlayerMove(int row, int col)
    {
        //kiem tra nguoi choi co the di hay khong
        if(isPlayerTurn == false || board[row, col] != null) return;
        //cap nhat trang thai cua o 
        board[row, col] = "O";
        UpdateCellUI(row, col, "O");
        //kiem tra nguoi choi co thang hay khong
        if (CheckWin("O"))
        {
            Debug.Log("Player wins!");
        }
        else if (IsBoardFull(board))
        {
            Debug.Log("Draw!");
        }
        else
        {
            isPlayerTurn = false; //may choi se di
            Invoke(nameof(PlayerAIMove), 0.3f); //doi 0.3s
        }
    }
    // Updated Minimax method to fix CS0161 and IDE0060
    (Vector2Int move, int score) Minimax(string[,] b, int depth, bool isMax, int alpha, int beta)
    {
        // Check for win conditions
        if (CheckWin("X", b)) return (Vector2Int.zero, 10000 + depth);
        if (CheckWin("O", b)) return (Vector2Int.zero, -10000 + depth);
        if (depth == 0 || IsBoardFull(b)) return (Vector2Int.zero, EvaluateBoard(b));

        // Get potential moves
        List<Vector2Int> candidateMoves = GetSmartCandidateMoves(b);

        Vector2Int bestMove = candidateMoves.Count > 0 ? candidateMoves[0] : Vector2Int.zero;
        int bestScore = isMax ? int.MinValue : int.MaxValue;

        foreach (var move in candidateMoves)
        {
            // Simulate the move
            b[move.x, move.y] = isMax ? "X" : "O"; //gia lap nuoc di
            var score = Minimax(b, depth - 1, !isMax, alpha, beta).score;
            b[move.x, move.y] = null; //hoan tac nuoc di

            // Update best score and move
            if (isMax && score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                alpha = Mathf.Max(alpha, score);
            }
            else if (!isMax && score < bestScore)
            {
                bestScore = score;
                bestMove = move;
                beta = Mathf.Min(beta, bestScore);
            }

            // Alpha-beta pruning
            if (beta <= alpha) break;
        }

        return (bestMove, bestScore);
    }
    //ham may choi
    public void PlayerAIMove()
    {
        int stoneCount = CountStones(board);
        //tang do sau tim kiem
        int depth = stoneCount < 10 ? 4 : 3;
        //kiem tra ngay lap tuc
        Vector2Int immediateMove = FindmmediateMove();

        Vector2Int bestMove = new Vector2Int(-1, -1);
        if (immediateMove != Vector2Int.one * -1)
        {
            bestMove = immediateMove;
        }
        else {
            //tim nuco di tot nhat
            var (move,_) = Minimax(board, depth, true, int.MinValue, int.MaxValue);
            bestMove = move;
        }
        board[bestMove.x, bestMove.y] = "X";//cap nhat nuoc di
        UpdateCellUI(bestMove.x, bestMove.y, "X");

        //kiem tra may co thang hay khong
        if (CheckWin("X"))
        {
            Debug.Log("may thang");
        }
        else if(IsBoardFull(board)) 
        {
            Debug.Log("Hoa");
        }
        else
        {
            isPlayerTurn = true; //den luot nguoi choi
        }
    }

    //nuoc di tiem nang
    List<Vector2Int> GetSmartCandidateMoves(string[,] b)
    {
        List<Vector2Int> candidateMoves = new List<Vector2Int>();
        HashSet<Vector2Int> consideredCell = new HashSet<Vector2Int>();
        int searchRange = 2;//khoang cach tim kiem

        //tim tat ca cac o trong xung quanh
        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                if (b[row, col] != null)
                {
                    //xem xet cac o xung quanh
                    for (int i = -searchRange; i <= searchRange; i++)
                    {
                        for (int j = -searchRange; j <= searchRange; j++)
                        {
                            int newRow = row + i;
                            int newCol = col + j;
                            //kiem tra o nam trong ban co
                            if (newRow >= 0 && newRow < boardSize &&
                                newCol >= 0 && newCol < boardSize &&
                                    b[newRow, newCol] == null &&
                                    !consideredCell.Contains(new Vector2Int(newRow, newCol)))
                            {
                                //them o vao danh sach
                                candidateMoves.Add(new Vector2Int(newRow, newCol));
                                consideredCell.Add(new Vector2Int(newRow, newCol));
                                int value = EvaluateMove(b, newRow, newCol);
                            }
                        }
                    }
                }
            }
        }
        //neu khong co nuoc di nao, chon vi tri trung tam
        if(candidateMoves.Count == 0)
        {
            int center = boardSize / 2;
            if(b[center, center] != null)
            {
                candidateMoves.Add(new Vector2Int(center, center));
            }
            else
            {
                //tim o trong dau tien
                for(int row = 0; row < boardSize; row++)
                {
                    for (int col = 0; col < boardSize; col++)
                    {
                        if (b[row, col] == null)
                        {
                            candidateMoves.Add(new Vector2Int(row, col));
                            break;
                        }
                    }
                }
            }
        }
        //sap xep cac nuoc di theo gia tri
        candidateMoves.OrderByDescending(pos => EvaluateMove(b, pos.x, pos.y)).ToList();
        return candidateMoves;
    }
    //danh gia 1 nuoc di
    int EvaluateMove(string[,] b, int row, int col)
    {
        int value = 0;
        //thu dat quan vao o
        b[row, col] = "X"; //gia lap nuoc di
        value += CalculateStrength(b, row, col, "X"); //tinh suc manh cua nuoc di
        b[row,col] = null; //hoan thanh nuoc di

        //kiem tra xem nuoc di co the chan nguoi choi hay khong
        b[row, col] = "O"; //gia lap nuoc di
        value += CalculateStrength(b, row, col, "O"); //tinh suc manh cua nuoc di
        b[row, col] = null; //hoan thanh nuoc di

        //yeu to vi tri - uu tien giua cac o
        int centerRow = boardSize / 2;
        int centerCol = boardSize / 2;
        //tinh khoang cach tu o hien tai den o giua
        float distanceToCenter = Mathf.Abs(row - centerRow) + Mathf.Abs(col - centerCol);
        value += Mathf.Max(5 - (int)distanceToCenter, 0); //tinh diem dua tren khoang cach
        return value;
    }
    //tinh suc manh cua mot chuoi vi tri tu vi tri (row, col)
    int CalculateStrength(string[,] b, int row, int col, string symbol)
    {
        int strength = 0;
        //kiem tra 4 huong
        for(int d =0; d < 4; d++)
        {
            int count = 1; //dem quan so lien tiep
            int emtybefore = 0;// dem so o trong truoc chuoi
            int emtyafter = 0; //dem so o trong sau chuoi

            //dem ve phia truoc
            for(int i =0; i < winLength; i++)
            {
                int newRow = row + dy[d] * i;
                int newCol = col + dx[d] * i;
                if (newRow < 0 || newRow >= boardSize ||
                    newCol < 0 || newCol >= boardSize) break;

                if (b[newRow, newCol] == symbol) count++;
                else if (b[newRow, newCol] == null)
                {
                    emtyafter++;
                    break;
                }
                else break;
            }
            //dem ve phia sau
            for (int i = 0; i < winLength; i++)
            {
                int newRow = row + dx[d] * i;
                int newCol = col + dy[d] * i;
                if (newRow < 0 || newRow >= boardSize ||
                    newCol < 0 || newCol >= boardSize) break;

                if (b[newRow, newCol] == symbol) count++;
                else if (b[newRow, newCol] == null)
                {
                    emtybefore++;
                    break;
                }
                else break;
            }
            //tinh suc manh cua chuoi
            if(count+ emtybefore + emtyafter >= winLength)
            {
                if (count == 4) strength += 1000; // chuoi 4 rat nguy hiem
                else if (count == 3) strength += 100; //chuoi 3 rat can chu y
                else if (count == 2) strength += 10; //chuoi 2 it quan trong

                strength += (emtybefore + emtyafter) * 2;//them suc manh cho cac o trong
            }
        }
        return strength;
    }
    //danh gia gia tri ban co hien tai
    int EvaluateBoard(string[,] b)
    {
        int score = 0;
        //mang luu gia tri cua chuoi
        int[] aiValues = { 0, 1, 10, 100, 1000 };
        int[] playerValues = { 0, -1, -15, -150, -2000 };
        //Tinh diem theo tung huong(hang, cot, cheo)
        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                for (int d = 0; d < 4; d++)
                {
                    if (row + dx[d] * (winLength - 1) >= boardSize ||
                        col + dy[d] * (winLength - 1) >= boardSize ||
                        row + dx[d] * (winLength - 1) < 0 ||
                        col + dy[d] * (winLength - 1) < 0) continue;

                    int aiCount = 0, player = 0, emtyCount = 0;
                    for(int i = 0; i < winLength; i++)
                    {
                        int newRow = row + dx[d] * i;
                        int newCol = col + dy[d] * i;
                        if (b[newRow, newCol] == "X")
                        {
                            aiCount++;
                        }
                        else if(b[newRow, newCol] == "Y")
                        {
                            player++;
                        }
                        else
                        {
                            emtyCount++;
                        }
                    }
                    if(player == 0 && aiCount > 0)
                    {
                        score += aiValues[aiCount];
                    }
                    else if(player == 0 && player > 0)
                    {
                        score += playerValues[player];
                    }
                }
            }
        }
        //them yeu to vi tri - uu tien cac o giua
        for(int row = 0; row < winLength; row++)
        {
            for(int col = 0;col < winLength; col++)
            {
                if (b[row,col] == "X")
                {
                    int centerRow = boardSize / 2;
                    int centerCol = boardSize / 2;
                    //tinh khoang cach tu o hien tai den o giua
                    float distanceToCenter = Mathf.Sqrt(Mathf.Pow(row - centerRow, 2) + Mathf.Pow(col - centerCol, 2));
                    //tinh diem dua tren khoang cach
                    score += Mathf.Max(5 - (int)distanceToCenter, 0);
                }
            }
        }
        return score;
    }
    //tim nuoc di ngay lap tuc
    Vector2Int FindmmediateMove()
    {
        //kiem tra xem AI co the thang ngay lap tuc khong
        for(int row = 0; row < boardSize; row++)
        {
            for(int col = 0; col < boardSize; col++)
            {
                if (board[row, col] != null) continue;
                board[row, col] = "X";
                if(CheckWin("X", board))
                {
                    board[row, col] = null; // Reset the cell
                    //tra ve nuoc di ngay lap tuc
                    return new Vector2Int(row, col);
                }
                board[row, col] = null; //hoan thanh nuoc di
            }
        }
        //kiem tra xem nguoi choi co the thang ngay lap tuc hay khong
        for(int row = 0; row < boardSize; row++)
        {
            for(int col =0; col < boardSize; col++)
            {
                if (board[row, col] != null) continue;
                board[row, col] = "O";
                if (CheckWin("O", board))
                {
                    board[row, col] = null; // Reset the cell
                    //tra ve nuoc di ngay lap tuc
                    return new Vector2Int(row, col);
                }
                board[row, col] = null; //hoan thanh nuoc di
            }
        }
        return new Vector2Int(-1, -1); // khong tim thay nuoc di ngay lap tuc
    }
    //dem so quan hien tai tren ban co
    int CountStones(string[,] b)
    {
        int count = 0;
        foreach(var s in b)
        {
            if(s != null)
            {
                count++;
            }
        }
        return count;
    }
    public void UpdateCellUI(int row, int col, string symbol)
    {
        var cell = cells.Find(c => c.row == row && c.col == col);
        cell.SetSymbol(symbol);
        var image = cell.GetComponent<Image>();
        image.color = symbol == "O" ? Color.white : Color.red;
    }
    bool CheckWin(string symbol, string[,] b = null)
    {
        b ??= board;
        for(int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                if (b[row, col] != symbol) continue;
               //kiem tra 4 huong
               for(int d = 0; d < 4; d++)
                {
                    //bat dau dem tu 1
                    int count = 1;
                    for(int i = 0; i < winLength - 1; i++)
                    {
                        int newRow = row + dx[d] * (i + 1);
                        int newCol = col + dy[d] * (i + 1);
                        //kiem tra o nam trong ban co
                        if (newRow < 0 || newRow >= boardSize ||
                            newCol < 0 || newCol >= boardSize) break;
                        if (b[newRow, newCol] != symbol) break;
                        count++;
                    }
                    if(count >= winLength) return true;
                }
            }
        }
        return false;
    }
    bool IsBoardFull(string[,] board)
    {
        bool isFull = true;
        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                if (board[row, col] == null)
                    isFull = false;
                break;
            }
        }
        return isFull;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateBoard();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
