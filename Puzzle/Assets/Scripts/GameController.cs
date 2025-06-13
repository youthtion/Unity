using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private const string DOMAIN = "https://youthtion.github.io/cocos_web/puzzle";
    private static readonly Color BOARD_COLOR = new Color32(64, 64, 64, 255); //�ѽL�C��
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
        strikeCamera.gameObject.SetActive(false); //��l�T�w�}�u�۾�
        strikeCamera.gameObject.SetActive(true);  //��ʭ��}�~�u�@
        GameEvents.preloaded += onPreloadData; //Ū��query string��cookie
        GameEvents.addBuff += onStartButtonClicked;
    }

    public void changeGameState(EGameState _state)
    {
        switch (_state)
        {
            //�������q
            case EGameState.GS_BUFF:
                GameModel.ins.addStage();
                GameModel.ins.removeBuff(EBuffType.BD_ROTATE);  //�M�����buff
                GameModel.ins.removeBuff(EBuffType.BD_GRAVITY); //�P�W
                buffCtrl?.generateBuffList();
                buffCtrl?.showBuffBtn();
                puzzleCtrl?.setEventsActive(false);
                setEventsActive(false);
                break;
            //�U���}�l
            case EGameState.GS_START:
                //���ͷs�@����T�ê�l�Ƴ槽��T
                initBoard();
                generatePuzzle();
                puzzleCtrl?.initPuzzle();
                puzzleCtrl?.setEventsActive(true);
                preloadCtrl?.setQueryString();
                preloadCtrl?.saveCookie();
                setEventsActive(true);
                break;
            //�䴩�Ҧ�
            case EGameState.GS_HELP:
                //�wŪ�����T, �q�ͦ�����}�l��l�Ƴ槽��T
                puzzleCtrl?.initPuzzle();
                puzzleCtrl?.setEventsActive(true);
                preloadCtrl?.setQueryString();
                setEventsActive(true);
                GameModel.ins.setHelpMode(true);
                break;
            //Ū���i��
            case EGameState.GS_CONTINUE:
                //�wŪ�����T, �q�ͦ�����}�l��l�Ƴ槽��T
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
            GameEvents.placePuzzle += onPlacePuzzle; //�������ϩ�W�ѽL�ƥ�
            GameEvents.pickPuzzle += onPickPuzzle;   //�������ϲ��X�ѽL�ƥ�
        }
        else{
            GameEvents.placePuzzle -= onPlacePuzzle;
            GameEvents.pickPuzzle -= onPickPuzzle;
        }
    }

    //��l�ƴѽL��T
    public void initBoard(HashSet<int> _obstacle_set = null)
    {
        if (cellPrefab == null){
            return;
        }
        if (_obstacle_set == null){
            _obstacle_set = new HashSet<int>();
        }
        int board_length = GameModel.ins.getBoardLength(); //�ѽL�j�p
        List<List<int>> board = new List<List<int>>(); //�ŴѽL��T
        //�M���ѽL�`�I
        for (int i = transform.childCount - 1; i >= 0; i--){
            Destroy(transform.GetChild(i).gameObject);
        }
        //�ھگʮ�buff�ƥͦ����O�ʮ��m
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
                //�ͦ�ixj��cell�[�J�ѽL�`�I
                GameObject cell = Instantiate(cellPrefab, transform);
                cell.transform.localPosition = new Vector3(i * Consts.CELL_WIDTH, j * Consts.CELL_WIDTH, 0);
                SpriteRenderer cell_sprite = cell.GetComponent<SpriteRenderer>();
                if (_obstacle_set.Contains(i * board_length + j)){
                    if (cell_sprite != null){
                        cell_sprite.color = OBSTACLE_COLOR;
                    }
                    line.Add(int.MaxValue); //�ʮ��J�L��
                }
                else{
                    if (cell_sprite != null){
                        cell_sprite.color = BOARD_COLOR;
                    }
                    line.Add(-1); //��l�ѽL��T�C���J-1
                }
            }
            board.Add(line);
        }
        GameModel.ins.setBoard(board); //��s�ѽL��T��GameModel
        //�ѽL�`�I�m����e��
        float pos = board_length * Consts.CELL_WIDTH * -0.5f + Consts.CELL_WIDTH * 0.5f;
        transform.localPosition = new Vector3(pos, pos, 0);
    }

    //�ͦ����ϸ�T
    public void generatePuzzle()
    {
        List<List<int>> board = GameModel.ins.getBoard().Select(v => new List<int>(v)).ToList();
        List<List<int[]>> puzzles = new List<List<int[]>>(); //�ū��ϸ�T
        int onecell_buff = GameModel.ins.getBuff(EBuffType.BD_ONECELL);
        for (int i = 0; i < board.Count; i++){
            for (int j = 0; j < board.Count; j++){
                //�M���ѽL��T�M���l��-1
                if (board[i][j] == -1){
                    int puzzle_id = puzzles.Count;
                    //�Ni,j��]�w�����ϲĤ@��CELL
                    puzzles.Add(new List<int[]> { new int[] { i, j } });
                    //���ͦ��T�w���(1x1 buff)
                    if (onecell_buff > 0){
                        board[i][j] = int.MaxValue;
                        onecell_buff--;
                    }
                    else{
                        board[i][j] = puzzle_id;
                        this.addNearCell(puzzles, board); //���j�ͦ��F��CELL
                        //�ͦ����G�Y�u��1CELL, �[�i�F�����
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
        GameModel.ins.setPuzzles(puzzles); //��s���ϸ�T��GameModel
    }

    //���j�ͦ��F��CELL
    public void addNearCell(List<List<int[]>> _puzzles, List<List<int>> _board)
    {
        int puzzle_id = _puzzles.Count - 1;
        var puzzle = _puzzles[puzzle_id];
        List<List<int>> board = _board.Select(v => new List<int>(v)).ToList(); //�ƻs�ѽL��T�Ȯɭק�
        List<int[]> new_cell = new List<int[]>(); //�Կ�ͦ�CELL
        int[][] direct = new int[][] { new int[] { 1, 0 }, new int[] { 0, 1 } }; //���k�U�M��i�ͦ�CELL(�קK�Ӯe�����Y��L���ϥͦ��Ŷ��������W�ͦ�)
        //�M���{��CELL
        foreach (var cell in puzzle){
            //�j�M�k�P�U�ѽL
            foreach (var dir in direct){
                //�k�P�UCELL��x,y
                int x = cell[0] + dir[0];
                int y = cell[1] + dir[1];
                //�Y�b�ѽL�d�򤺥B���Q�e��(-1)�h�[�J�Կ�
                if (x < board.Count && y < board[x].Count && board[x][y] == -1){
                    new_cell.Add(new int[] { x, y });
                    board[x][y] = 0; //��JCELL��ѽL�W�]�w������
                }
            }
        }
        //�Y���Կ�BCELL�Ʀb���v����פ�
        if (new_cell.Count > 0 && puzzle.Count < Consts.ADD_NEAR_RATE.Length){
            //���v����O�_�ͦ�
            float[] add_near_rate = buffCtrl.getBuffAddNearRate();
            if (UnityEngine.Random.Range(0f, 1f) < add_near_rate[puzzle.Count]){
                //�ͦ����\�ѭԿ��H��1��CELL�[�J
                int rand = UnityEngine.Random.Range(0, new_cell.Count);
                int x = new_cell[rand][0];
                int y = new_cell[rand][1];
                _puzzles[puzzle_id].Add(new int[] { x, y });
                _board[x][y] = puzzle_id; //�]�w�ѽL�W����
                addNearCell(_puzzles, _board); //�~��ͦ��F��CELL
            }
        }
    }

    //�M��F�����
    public int findNearPuzzle(List<List<int>> _board, int _x, int _y)
    {
        int[][] direct = new int[][]{ new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, 1 }, new int[] { 0, -1 }}; //4��V
        List<int[]> near = new List<int[]>();
        //�M��ѽL�d�򤺥B�w�Q���Ϊ��F��CELL
        for (int i = 0; i < direct.Length; i++){
            int dx = direct[i][0];
            int dy = direct[i][1];
            int x = _x + dx;
            int y =_y + dy;
            if (x >= 0 && y >= 0 && x < _board.Count && y < _board[x].Count && _board[x][y] > -1 && _board[x][y] != int.MaxValue){
                near.Add(new int[] { x, y });
            }
        }
        //���䤤�@�ӾF��CELL�����Ϋ���ID�^��
        if (near.Count > 0){
            int rand = UnityEngine.Random.Range(0, near.Count);
            int nx = near[rand][0];
            int ny = near[rand][1];
            return _board[nx][ny];
        }
        return 0;
    }

    //���ϩ�W�ѽL�ƥ�
    public void onPlacePuzzle(List<int[]> _fit_cell, string _name)
    {
        int gravity = GameModel.ins.getBuff(EBuffType.BD_GRAVITY);
        //�����̰��M�̧Cy�y��
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
        List<List<int>> board = GameModel.ins.getBoard().Select(v => new List<int>(v)).ToList(); //�ƻs�ѽL��T
        int? dy = null; //�p���B�~�ק�ؼ�y�b���ʮ��(���Obuff)
        //���Obuff���ؼ�y�b�I(���O�����O��, �B�O�����O��)
        int tar_y = 0;
        if (gravity > 0){
            tar_y = min_y - 0;
        }
        else if (gravity < 0){
            tar_y = board.Count - 1 - max_y;
        }
        //�q��m�쩹�W(�U)���i��m���Ϫ���m
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
            //�M�����ϦUcell�i��m���i��m�I, ����dy
            if (fit_cnt == _fit_cell.Count){
                dy = i * gravity;
            }
            else{
                break;
            }
        }
        //�S���i�H��W���ѽL��, �^�ǵ��G����s�ѽL��T
        if (dy == null){
            puzzleCtrl?.onPlacePuzzleCallback(null);
            return;
        }
        int puzzle_id = int.Parse(_name); //�ǤJ����id�r��אּ�Ʀr
        //��W���Ϧ��\�^�ǵ��G, ��s�ѽL��T
        foreach (var cell in _fit_cell){
            int x = cell[0];
            int y = cell[1];
            board[x][y - dy.Value] = puzzle_id;
        }
        GameModel.ins.setBoard(board);
        puzzleCtrl?.onPlacePuzzleCallback(dy);
        checkResult(board); //�ӧQ�P�w
    }

    //���ϲ��X�ѽL�ƥ�
    public void onPickPuzzle(List<int[]> _fit_cell, string _name)
    {
        int puzzle_id = int.Parse(_name); //�ǤJ����id�r��אּ�Ʀr
        List<List<int>> board = GameModel.ins.getBoard().Select(v => new List<int>(v)).ToList(); //�ƻs�ѽL��T�Ȯɭק�(���G�P�w�����ѴN����s)
        //�P�w�C��CELL���ѽL��T���O����id, ��J-1
        foreach (var cell in _fit_cell){
            int x = cell[0];
            int y = cell[1];
            if (board[x][y] == puzzle_id){
                board[x][y] = -1;
            }
            //�J��D���Ϯ�, ����s�ѽL��T
            else{
                return;
            }
        }
        GameModel.ins.setBoard(board); //���X���Ϧ��\, ��s�ѽL��T
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

    //�ƻsquery string
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
        GameEvents.preloaded -= onPreloadData; //Ū�ɦ��\�u�n�@��
    }

    //�ӧQ�P�w
    public void checkResult(List<List<int>> _board)
    {
        if (GameModel.ins.getHelpMode()){
            return;
        }
        //�ѽL��T�񺡬��ӧQ
        for (int i = 0; i < _board.Count; i++){
            for (int j = 0; j < _board[i].Count; j++){
                if (_board[i][j] == -1){
                    return;
                }
            }
        }
        changeGameState(EGameState.GS_BUFF);
    }

    //�]�w�Ӫ��A�U�����
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