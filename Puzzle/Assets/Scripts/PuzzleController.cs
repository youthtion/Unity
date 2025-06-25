using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleController : MonoBehaviour
{
    private const float CAMERA_Z = -10.0f;

    [SerializeField]
    private GameObject cellPrefab = null;
    [Range(0f, 1f)]
    [SerializeField]
    private float cellSaturation = 0f;
    [Range(0f, 1f)]
    [SerializeField]
    private float cellLightness = 0f;

    private List<int[]> fitCells = new List<int[]>(); //拼圖重疊棋盤格id
    private float[] placePuzzlePos = new float[2] { 0f, 0f }; //拼圖重疊棋盤格棋盤座標(拼圖吸附後位置)
    private GameObject dragPuzzle = null; //抓取中的拼圖
    private Vector3 dragStartPos = Vector3.zero; //抓取拼圖前游標位置(計算移動量)
    private Vector3 puzzleStartPos = Vector3.zero; //抓取拼圖前拼圖位置(計算移動量)
    private int puzzleLayer; //拼圖用顯示層
    private float curZ = -0.01f; //置頂z座標
    private bool eventActive = false; //事件開關
    private bool isDragging = false;

    private void Start()
    {
        puzzleLayer = LayerMask.NameToLayer("PUZZLE"); //設定拼圖用顯示層
    }

    private void Update()
    {
        if (!eventActive){
            return;
        }
        //拼圖抓取&旋轉事件, 傳入點擊的拼圖節點
        if (Input.GetMouseButtonDown(0)){
            Vector3 mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast((Vector2)mouse_pos, Vector2.zero);
            //以物件顯示層判斷是否點擊為拼圖物件
            if (hit.collider != null && hit.collider.gameObject.layer == puzzleLayer){
                onMouseDown(mouse_pos, hit.collider.gameObject.transform.parent.gameObject);
            }
        }
        //拼圖移動事件
        if (dragPuzzle && Input.GetMouseButton(0)){
            Vector3 mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            onMouseMove(mouse_pos);
        }
        //拼圖放下事件
        if (dragPuzzle && Input.GetMouseButtonUp(0)){
            onMouseUp();
        }
    }

    public void setEventsActive(bool _set)
    {
        eventActive = _set;
    }

    //生成拼圖實體
    public void initPuzzle()
    {
        if (cellPrefab == null){
            return;
        }
        List<List<int>> board = GameModel.ins.getBoard(); //從GameModel取得棋盤資訊
        List<List<int[]>> puzzles = GameModel.ins.getPuzzles().Select(v0 => v0.Select(v1 => (int[])v1.Clone()).ToList()).ToList(); //從GameModel取得拼圖資訊
        List<Color> colors = generateHueColors(puzzles.Count * (GameModel.ins.getBuff(EBuffType.BD_HUE) + 1));
        //清除拼圖節點下所有拼圖
        for (int i = transform.childCount - 1; i >= 0; i--){
            Destroy(transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < puzzles.Count; i++){
            GameObject puzzle = new GameObject(i.ToString()); //生成拼圖節點, 名稱i
            //拼圖第一個CELL的座標bx,by
            int bx = puzzles[i][0][0];
            int by = puzzles[i][0][1];
            for (int j = 0; j < puzzles[i].Count; j++){
                GameObject cell = Instantiate(cellPrefab, puzzle.transform); //生成CELL
                //該CELL的座標x,y
                int x = puzzles[i][j][0];
                int y = puzzles[i][j][1];
                cell.transform.localPosition = new Vector3((x - bx) * Consts.CELL_WIDTH, (y - by) * Consts.CELL_WIDTH, 0); //各CELL位置都減少第一座標bx,by距離, 讓拼圖整體移動到0,0附近
                SpriteRenderer cell_sprite = cell.GetComponent<SpriteRenderer>();
                if (cell_sprite != null){
                    cell_sprite.color = colors[i]; //修改顏色
                }
                cell.AddComponent<BoxCollider2D>();
            }
            //設定拼圖置頂
            puzzle.transform.localPosition = new Vector3(
                board.Count * Consts.CELL_WIDTH / puzzles.Count * i - (board.Count - 1) * Consts.CELL_WIDTH * 0.5f ,
                -(board.Count + 1) * Consts.CELL_WIDTH,
                curZ);
            curZ -= 0.01f;
            puzzle.transform.SetParent(transform, false);
        }
    }

    //拼圖抓取&旋轉事件
    public void onMouseDown(Vector3 _cur_pos, GameObject _puzzle)
    {
        if (_puzzle.transform.parent == null){
            return;
        }
        //拼圖置頂超過相機z範圍, 調整所有拼圖z
        if (curZ < CAMERA_Z){
            curZ = -0.01f;
            foreach (UnityEngine.Transform puzzle in transform){
                puzzle.transform.position = new Vector3(puzzle.transform.position.x, puzzle.transform.position.y, curZ);
                curZ -= 0.01f;
            }
        }
        getFitCell(_puzzle); //取得拼圖重疊棋盤格資訊
        //重疊棋盤格與拼圖大小一致, 處理拼圖移出棋盤
        if (fitCells.Count == _puzzle.transform.childCount){
            GameEvents.onPickPuzzle(fitCells, _puzzle.name);
        }
        //左鍵抓取拼圖紀錄puzzle開始位置設定, 游標位置, 拼圖節點
        if (dragPuzzle == null){
            _puzzle.transform.position = new Vector3(_puzzle.transform.position.x, _puzzle.transform.position.y, curZ); //設定點擊拼圖置頂
            puzzleStartPos = _puzzle.transform.position;
            dragStartPos = _cur_pos;
            dragPuzzle = _puzzle;
            isDragging = false;
        }
        curZ -= 0.01f;
    }

    public void onMouseMove(Vector3 _cur_pos)
    {
        if(isDragging == false && Vector2.Distance(dragStartPos, _cur_pos) > 10f){
            isDragging = true;
        }
        if(isDragging == true){
            //計算游標移動量, 設定puzzle開始位置差
            float dx = dragStartPos.x - _cur_pos.x;
            float dy = dragStartPos.y - _cur_pos.y;
            dragPuzzle.transform.position = new Vector3(puzzleStartPos.x - dx, puzzleStartPos.y - dy, dragPuzzle.transform.position.z);
        }
    }

    public void onMouseUp()
    {
        //點選旋轉
        if (isDragging == false && GameModel.ins.getBuff(EBuffType.BD_ROTATE) == 0){
            Vector3 puzzle_pos = dragPuzzle.transform.position; //游標在畫面上的座標轉換在PuzzleController中的座標
            Vector3 offset = dragStartPos - puzzle_pos; //座標與puzzle位置設定差
            float rad = Mathf.PI / -2f; //旋轉弧度(-90度)
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            //設定新位置以游標在PuzzleController中的座標為旋轉中心, 扣除旋轉後與puzzle位置設定差
            float rx = offset.x * cos - offset.y * sin;
            float ry = offset.x * sin + offset.y * cos;
            dragPuzzle.transform.position = new Vector3(dragStartPos.x - rx, dragStartPos.y - ry, dragPuzzle.transform.position.z);
            Vector3 euler = dragPuzzle.transform.eulerAngles;
            euler.z = (euler.z - 90f) % 360f;
            dragPuzzle.transform.eulerAngles = euler;
        }
        getFitCell(dragPuzzle); //取得拼圖重疊棋盤格資訊
        //重疊棋盤格與拼圖大小一致, 處理拼圖放入棋盤
        if (fitCells.Count == dragPuzzle.transform.childCount){
            GameEvents.onPlacePuzzle(fitCells, dragPuzzle.name);
        }
        else{
            dragPuzzle = null;
        }
        isDragging = false;
    }

    public void getFitCell(GameObject _puzzle)
    {
        float dx = 0f;
        float dy = 0f;
        fitCells.Clear();
        if (_puzzle.transform.parent == null){
            return;
        }
        List<List<int>> board = GameModel.ins.getBoard(); //從GameModel取得棋盤資訊
        for (int i = 0; i < _puzzle.transform.childCount; i++){
            //各CELL在畫面上的座標轉換在PuzzleController中的座標x,y
            UnityEngine.Transform cell = _puzzle.transform.GetChild(i);
            Vector3 cur_pos = cell.position;
            Vector3 pos_in_parent = _puzzle.transform.parent.InverseTransformPoint(cur_pos);
            float x = pos_in_parent.x;
            float y = pos_in_parent.y;
            //畫面棋盤座標為中央0, 上下左右各一半棋盤長度, 判斷x,y是否在棋盤範圍+吸附範圍(FIT_ALLOW)內
            if (Mathf.Abs(x) > Consts.CELL_WIDTH * (board.Count - 1) / 2f + Consts.FIT_ALLOW || Mathf.Abs(y) > Consts.CELL_WIDTH * (board.Count - 1) / 2f + Consts.FIT_ALLOW){
                break;
            }
            //計算CELL座標是否在棋盤格吸附範圍內, 偶數長度的棋盤中央非棋盤格調整x,y
            if (board.Count % 2 == 0){
                x += Consts.CELL_WIDTH * 0.5f;
                y += Consts.CELL_WIDTH * 0.5f;
            }
            //CELL座標除以棋盤格, 判斷餘數是否在一單位格的+-吸附範圍內
            if (Mathf.Abs(x % Consts.CELL_WIDTH) > Consts.FIT_ALLOW && Mathf.Abs(x % Consts.CELL_WIDTH) < Consts.CELL_WIDTH - Consts.FIT_ALLOW){
                break;
            }
            if (Mathf.Abs(y % Consts.CELL_WIDTH) > Consts.FIT_ALLOW && Mathf.Abs(y % Consts.CELL_WIDTH) < Consts.CELL_WIDTH - Consts.FIT_ALLOW){
                break;
            }
            //確認CELL在棋盤範圍內且在棋盤格吸附範圍, 計算實際偏差量
            if (dx == 0f && dy == 0f){
                dx = (x % Consts.CELL_WIDTH + Consts.CELL_WIDTH) % Consts.CELL_WIDTH;
                if (dx > Consts.FIT_ALLOW){
                    dx -= Consts.CELL_WIDTH;
                }
                dy = (y % Consts.CELL_WIDTH + Consts.CELL_WIDTH) % Consts.CELL_WIDTH;
                if (dy > Consts.FIT_ALLOW){
                    dy -= Consts.CELL_WIDTH;
                }
                //紀錄puzzle位置設定與扣除偏差量後的位置(吸附後的位置)
                Vector3 puzzle_pos = _puzzle.transform.localPosition;
                placePuzzlePos[0] = puzzle_pos.x - dx;
                placePuzzlePos[1] = puzzle_pos.y - dy;
            }
            //再將x,y扣除吸附偏差調整為吻合棋盤格的座標, 方便計算所屬棋盤格id
            x -= dx;
            y -= dy;
            //座標轉換為棋盤格id, 因當作陣列id用四捨五入防止浮點數誤差
            fitCells.Add(new int[2] { Mathf.RoundToInt(x / Consts.CELL_WIDTH + Mathf.Floor((board.Count - 1) / 2f)), Mathf.RoundToInt(y / Consts.CELL_WIDTH + Mathf.Floor((board.Count - 1) / 2f)) });
        }
    }

    //拼圖放上棋盤事件處理完成的回傳
    public void onPlacePuzzleCallback(int? _dy)
    {
        if (dragPuzzle && _dy != null){
            //判定成功調整為吸附後位置
            dragPuzzle.transform.localPosition = new Vector3(placePuzzlePos[0], placePuzzlePos[1] - _dy.Value * Consts.CELL_WIDTH, -0.01f);
            placePuzzlePos[0] = 0;
            placePuzzlePos[1] = 0;
        }
        dragPuzzle = null;
    }

    //HSL轉RGB公式
    public Color hslToRgb(float _h, float _s, float _l)
    {
        float r, g, b;
        if (_s == 0f){
            r = g = b = _l;
        }
        else{
            float hue2Rgb(float _p, float _q, float _t){
                if (_t < 0f) _t += 1f;
                if (_t > 1f) _t -= 1f;
                if (_t < 1f / 6f) return _p + (_q - _p) * 6f * _t;
                if (_t < 1f / 2f) return _q;
                if (_t < 2f / 3f) return _p + (_q - _p) * (2f / 3f - _t) * 6f;
                return _p;
            }
            float q = _l < 0.5f ? _l * (1f + _s) : _l + _s - _l * _s;
            float p = 2f * _l - q;
            r = hue2Rgb(p, q, _h + 1f / 3f);
            g = hue2Rgb(p, q, _h);
            b = hue2Rgb(p, q, _h - 1f / 3f);
        }
        return new Color(r, g, b);
    }

    //生成_n個色相等差的顏色
    public List<Color> generateHueColors(int _n)
    {
        List<Color> colors = new List<Color>();
        for (int i = 0; i < _n; i++){
            float h = (float)i / _n;
            Color rgb = hslToRgb(h, cellSaturation, cellLightness);
            colors.Add(rgb);
        }
        return colors;
    }
}
