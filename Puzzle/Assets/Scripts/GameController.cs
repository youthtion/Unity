using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private const string DOMAIN = "https://youthtion.github.io/cocos_web/puzzle";
    private static readonly Color BOARD_COLOR = new Color32(64, 64, 64, 255); //棋盤顏色
    private static readonly Color OBSTACLE_COLOR = new Color32(0, 0, 0, 255);

    [SerializeField]
    private GameObject cellPrefab;
    [SerializeField]
    private PuzzleController puzzleCtrl;
    [SerializeField]
    private BuffController buffCtrl;
    [SerializeField]
    private PreloadController preloadCtrl;
    [SerializeField]
    private GameObject buffMenu;
    [SerializeField]
    private GameObject btnStart;
    [SerializeField]
    private TextMeshProUGUI titleLabel;
    [SerializeField]
    private TextMeshProUGUI stageLabel;
    [SerializeField]
    private GameObject shareBtn;
    [SerializeField]
    private GameObject shareMenu;
    [SerializeField]
    private InputField shareEdit;
    [SerializeField]
    private GameObject screenshotBtn;
    [SerializeField]
    private Camera strikeCamera;

    private void Start()
    {
        strikeCamera.gameObject.SetActive(false); //初始固定罷工相機
        strikeCamera.gameObject.SetActive(true);  //手動重開才工作
        GameEvents.preloaded += onPreloadData; //讀到query string或cookie
        GameEvents.addBuff += onStartButtonClicked;
    }

    public void changeGameState(EGameState _state)
    {
        switch (_state)
        {
            //場間階段
            case EGameState.GS_BUFF:
                GameModel.ins.addStage();
                GameModel.ins.removeBuff(EBuffType.BD_ROTATE);  //清除單場buff
                GameModel.ins.removeBuff(EBuffType.BD_GRAVITY); //同上
                buffCtrl?.generateBuffList();
                buffCtrl?.showBuffBtn();
                puzzleCtrl?.setEventsActive(false);
                setEventsActive(false);
                break;
            //各場開始
            case EGameState.GS_START:
                //產生新一場資訊並初始化單局資訊
                initBoard();
                generatePuzzle();
                puzzleCtrl?.initPuzzle();
                puzzleCtrl?.setEventsActive(true);
                preloadCtrl?.setQueryString();
                preloadCtrl?.saveCookie();
                setEventsActive(true);
                break;
            //支援模式
            case EGameState.GS_HELP:
                //預讀單場資訊, 從生成實體開始初始化單局資訊
                puzzleCtrl?.initPuzzle();
                puzzleCtrl?.setEventsActive(true);
                preloadCtrl?.setQueryString();
                setEventsActive(true);
                GameModel.ins.setHelpMode(true);
                break;
            //讀取進度
            case EGameState.GS_CONTINUE:
                //預讀單場資訊, 從生成實體開始初始化單局資訊
                puzzleCtrl?.initPuzzle();
                puzzleCtrl?.setEventsActive(true);
                preloadCtrl?.setQueryString();
                setEventsActive(true);
                break;
        }
        setMenuVisible(_state);
    }

    public void setEventsActive(bool _set)
    {
        if (_set){
            GameEvents.placePuzzle += onPlacePuzzle; //接收拼圖放上棋盤事件
            GameEvents.pickPuzzle += onPickPuzzle;   //接收拼圖移出棋盤事件
        }
        else{
            GameEvents.placePuzzle -= onPlacePuzzle;
            GameEvents.pickPuzzle -= onPickPuzzle;
        }
    }

    //初始化棋盤資訊
    public void initBoard(HashSet<int> _obstacle_set = null)
    {
        if (cellPrefab == null){
            return;
        }
        if (_obstacle_set == null){
            _obstacle_set = new HashSet<int>();
        }
        int board_length = GameModel.ins.getBoardLength(); //棋盤大小
        List<List<int>> board = new List<List<int>>(); //空棋盤資訊
        //清除棋盤節點
        for (int i = transform.childCount - 1; i >= 0; i--){
            Destroy(transform.GetChild(i).gameObject);
        }
        //根據缺格buff數生成底板缺格位置
        if (_obstacle_set.Count == 0 && GameModel.ins.getBuff(EBuffType.BD_OBSTACLE) > 0){
            int obstacle_buff = GameModel.ins.getBuff(EBuffType.BD_OBSTACLE);
            int part_len = (board_length * board_length) / obstacle_buff;
            for (int i = 0; i < obstacle_buff; i++){
                _obstacle_set.Add(UnityEngine.Random.Range(0, part_len) + part_len * i);
            }
        }
        for (int i = 0; i < board_length; i++){
            List<int> line = new List<int>();
            for (int j = 0; j < board_length; j++){
                //生成ixj個cell加入棋盤節點
                GameObject cell = Instantiate(cellPrefab, transform);
                cell.transform.localPosition = new Vector3(i * Consts.CELL_WIDTH, j * Consts.CELL_WIDTH, 0);
                SpriteRenderer cell_sprite = cell.GetComponent<SpriteRenderer>();
                if (_obstacle_set.Contains(i * board_length + j)){
                    if (cell_sprite != null){
                        cell_sprite.color = OBSTACLE_COLOR;
                    }
                    line.Add(int.MaxValue); //缺格填入無限
                }
                else{
                    if (cell_sprite != null){
                        cell_sprite.color = BOARD_COLOR;
                    }
                    line.Add(-1); //初始棋盤資訊每格填入-1
                }
            }
            board.Add(line);
        }
        GameModel.ins.setBoard(board); //更新棋盤資訊給GameModel
        //棋盤節點置中於畫面
        float pos = board_length * Consts.CELL_WIDTH * -0.5f + Consts.CELL_WIDTH * 0.5f;
        transform.localPosition = new Vector3(pos, pos, 0);
    }

    //生成拼圖資訊
    public void generatePuzzle()
    {
        List<List<int>> board = GameModel.ins.getBoard().Select(v => new List<int>(v)).ToList();
        List<List<int[]>> puzzles = new List<List<int[]>>(); //空拼圖資訊
        int onecell_buff = GameModel.ins.getBuff(EBuffType.BD_ONECELL);
        for (int i = 0; i < board.Count; i++){
            for (int j = 0; j < board.Count; j++){
                //遍歷棋盤資訊尋找初始格-1
                if (board[i][j] == -1){
                    int puzzle_id = puzzles.Count;
                    //將i,j格設定為拼圖第一個CELL
                    puzzles.Add(new List<int[]> { new int[] { i, j } });
                    //先生成固定方塊(1x1 buff)
                    if (onecell_buff > 0){
                        board[i][j] = int.MaxValue;
                        onecell_buff--;
                    }
                    else{
                        board[i][j] = puzzle_id;
                        this.addNearCell(puzzles, board); //遞迴生成鄰近CELL
                        //生成結果若只有1CELL, 加進鄰近拼圖
                        if (puzzles[puzzle_id].Count <= 1){
                            int x = puzzles[puzzle_id][0][0];
                            int y = puzzles[puzzle_id][0][1];
                            int near_puzzle_id = findNearPuzzle(board, x, y);
                            puzzles[near_puzzle_id].Add(puzzles[puzzle_id][0]);
                            puzzles.RemoveAt(puzzle_id);
                            board[x][y] = near_puzzle_id;
                        }
                    }
                }
            }
        }
        GameModel.ins.setPuzzles(puzzles); //更新拼圖資訊給GameModel
    }

    //遞迴生成鄰近CELL
    public void addNearCell(List<List<int[]>> _puzzles, List<List<int>> _board)
    {
        int puzzle_id = _puzzles.Count - 1;
        var puzzle = _puzzles[puzzle_id];
        List<List<int>> board = _board.Select(v => new List<int>(v)).ToList(); //複製棋盤資訊暫時修改
        List<int[]> new_cell = new List<int[]>(); //候選生成CELL
        int[][] direct = new int[][] { new int[] { 1, 0 }, new int[] { 0, 1 } }; //往右下尋找可生成CELL(避免太容易壓縮其他拼圖生成空間不往左上生成)
        //遍歷現有CELL
        foreach (var cell in puzzle){
            //搜尋右與下棋盤
            foreach (var dir in direct){
                //右與下CELL的x,y
                int x = cell[0] + dir[0];
                int y = cell[1] + dir[1];
                //若在棋盤範圍內且未被占用(-1)則加入候選
                if (x < board.Count && y < board[x].Count && board[x][y] == -1){
                    new_cell.Add(new int[] { x, y });
                    board[x][y] = 0; //選入CELL後棋盤上設定為佔用
                }
            }
        }
        //若有候選且CELL數在機率表長度內
        if (new_cell.Count > 0 && puzzle.Count < Consts.ADD_NEAR_RATE.Length){
            //機率表抽選是否生成
            float[] add_near_rate = buffCtrl.getBuffAddNearRate();
            if (UnityEngine.Random.Range(0f, 1f) < add_near_rate[puzzle.Count]){
                //生成成功由候選隨機1個CELL加入
                int rand = UnityEngine.Random.Range(0, new_cell.Count);
                int x = new_cell[rand][0];
                int y = new_cell[rand][1];
                _puzzles[puzzle_id].Add(new int[] { x, y });
                _board[x][y] = puzzle_id; //設定棋盤上佔用
                addNearCell(_puzzles, _board); //繼續生成鄰近CELL
            }
        }
    }

    //尋找鄰近拼圖
    public int findNearPuzzle(List<List<int>> _board, int _x, int _y)
    {
        int[][] direct = new int[][]{ new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, 1 }, new int[] { 0, -1 }}; //4方向
        List<int[]> near = new List<int[]>();
        //尋找棋盤範圍內且已被佔用的鄰近CELL
        for (int i = 0; i < direct.Length; i++){
            int dx = direct[i][0];
            int dy = direct[i][1];
            int x = _x + dx;
            int y =_y + dy;
            if (x >= 0 && y >= 0 && x < _board.Count && y < _board[x].Count && _board[x][y] > -1 && _board[x][y] != int.MaxValue){
                near.Add(new int[] { x, y });
            }
        }
        //抽選其中一個鄰近CELL的佔用拼圖ID回傳
        if (near.Count > 0){
            int rand = UnityEngine.Random.Range(0, near.Count);
            int nx = near[rand][0];
            int ny = near[rand][1];
            return _board[nx][ny];
        }
        return 0;
    }

    //拼圖放上棋盤事件
    public void onPlacePuzzle(List<int[]> _fit_cell, string _name)
    {
        int gravity = GameModel.ins.getBuff(EBuffType.BD_GRAVITY);
        //找方塊最高和最低y座標
        int min_y = int.MaxValue;
        int max_y = -1;
        foreach (var cell in _fit_cell){
            int y = cell[1];
            if (y < min_y){
                min_y = y;
            }
            if (y > max_y){
                max_y = y;
            }
        }
        List<List<int>> board = GameModel.ins.getBoard().Select(v => new List<int>(v)).ToList(); //複製棋盤資訊
        int? dy = null; //計算額外修改目標y軸移動格數(重力buff)
        //重力buff的目標y軸點(重力為底板底, 浮力為底板頂)
        int tar_y = 0;
        if (gravity > 0){
            tar_y = min_y - 0;
        }
        else if (gravity < 0){
            tar_y = board.Count - 1 - max_y;
        }
        //從放置位往上(下)找到可放置拼圖的位置
        for (int i = 0; i <= tar_y; i++){
            int fit_cnt = 0;
            foreach (var cell in _fit_cell){
                int x = cell[0];
                int y = cell[1];
                if (board[x][y - i * gravity] == -1){
                    fit_cnt++;
                }
                else{
                    break;
                }
            }
            //遍歷拼圖各cell可放置為可放置點, 紀錄dy
            if (fit_cnt == _fit_cell.Count){
                dy = i * gravity;
            }
            else{
                break;
            }
        }
        //沒有可以放上的棋盤格, 回傳結果不更新棋盤資訊
        if (dy == null){
            puzzleCtrl?.onPlacePuzzleCallback(null);
            return;
        }
        int puzzle_id = int.Parse(_name); //傳入拼圖id字串改為數字
        //放上拼圖成功回傳結果, 更新棋盤資訊
        foreach (var cell in _fit_cell){
            int x = cell[0];
            int y = cell[1];
            board[x][y - dy.Value] = puzzle_id;
        }
        GameModel.ins.setBoard(board);
        puzzleCtrl?.onPlacePuzzleCallback(dy);
        checkResult(board); //勝利判定
    }

    //拼圖移出棋盤事件
    public void onPickPuzzle(List<int[]> _fit_cell, string _name)
    {
        int puzzle_id = int.Parse(_name); //傳入拼圖id字串改為數字
        List<List<int>> board = GameModel.ins.getBoard().Select(v => new List<int>(v)).ToList(); //複製棋盤資訊暫時修改(結果判定為失敗就不更新)
        //判定每個CELL的棋盤資訊都是拼圖id, 填入-1
        foreach (var cell in _fit_cell){
            int x = cell[0];
            int y = cell[1];
            if (board[x][y] == puzzle_id){
                board[x][y] = -1;
            }
            //遇到非拼圖格, 不更新棋盤資訊
            else{
                return;
            }
        }
        GameModel.ins.setBoard(board); //移出拼圖成功, 更新棋盤資訊
    }

    public void onStartButtonClicked()
    {
        changeGameState(EGameState.GS_START);
    }

    public void onShareBtnClick()
    {
        if (shareMenu != null){
            shareMenu.SetActive(!shareMenu.activeSelf);
        }
    }

    //複製query string
    public void onCopyBtnClick()
    {
        if (shareEdit != null && !string.IsNullOrEmpty(shareEdit.text)){
            GUIUtility.systemCopyBuffer = shareEdit.text;
        }
    }

    public void onPreloadData(HashSet<int> _obstacle_set, EGameState _state)
    {
        initBoard(_obstacle_set);
        changeGameState(_state);
        GameEvents.preloaded -= onPreloadData; //讀檔成功只要一次
    }

    //勝利判定
    public void checkResult(List<List<int>> _board)
    {
        if (GameModel.ins.getHelpMode()){
            return;
        }
        //棋盤資訊填滿為勝利
        for (int i = 0; i < _board.Count; i++){
            for (int j = 0; j < _board[i].Count; j++){
                if (_board[i][j] == -1){
                    return;
                }
            }
        }
        changeGameState(EGameState.GS_BUFF);
    }

    //設定個狀態各類選單
    public void setMenuVisible(EGameState _state)
    {
        if (stageLabel != null){
            stageLabel.text = _state == EGameState.GS_START || _state == EGameState.GS_CONTINUE ? "Stage" + GameModel.ins.getStage() : "";
        }
        if (buffMenu != null){
            buffMenu.SetActive(_state == EGameState.GS_BUFF);
        }
        if (shareBtn != null){
            shareBtn.SetActive(_state != EGameState.GS_BUFF);
        }
        if (shareMenu != null){
            shareMenu.SetActive(false);
        }
        if (btnStart != null){
            btnStart.SetActive(false);
        }
        if (titleLabel != null){
            titleLabel.text = "";
        }
        if (screenshotBtn != null){
            screenshotBtn.SetActive(_state == EGameState.GS_HELP);
        }
        if (shareEdit != null){
            shareEdit.text = DOMAIN + "?" + GameModel.ins.getQueryString();
        }
    }
}