using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Tile {
    private static readonly byte[] cmyGen = { 1, 2, 4 };
    public static readonly byte color_c = 1 << 0;
    public static readonly byte color_m = 1 << 1;
    public static readonly byte color_y = 1 << 2;
    public static readonly byte color_k = (byte)(color_c | color_m | color_y);

    public static Tile empty = new Tile(-1, -1, 0);

    public byte color;

    public int x { get; protected set; }
    public int y { get; protected set; }

    public Tile(int x, int y) {
        this.color = cmyGen[Random.Range(0, 3)];
        this.x = x;
        this.y = y;
    }

    public Tile(int x, int y, int c) {
        Debug.Assert(c < 256 && c >= 0, "New Tile Color overflow");
        color = (byte)c;
    }

    public Color GetColor() {
        return new Color(
            (color == color_m || color == color_y || (color == (color_m | color_y))) ? 0.9f : 0.0f,
            (color == color_c || color == color_y || (color == (color_c | color_y))) ? 0.9f : 0.0f,
            (color == color_c || color == color_m || (color == (color_c | color_m))) ? 0.9f : 0.0f,
            1
            );
    }

    public static Tile operator +(Tile left, Tile right) {
        if (left == null) return right;
        if (right == null) return left;
        return new Tile(left.x, left.y, left.color | right.color);
    }

    public static Tile operator +(Tile left, byte c) {
        left.color |= c;
        return left;
    }

    public static Tile operator -(Tile left, byte c) {
        left.color ^= c;
        return left;
    }

    public void Add(Tile other) {
        if (other == null) return;
        this.color |= other.color;
    }

    public void Add(byte other) {
        this.color |= other;
    }

    public bool Contains(byte otherColor) {
        return (this.color & otherColor) != 0;
    }

    public bool Contains(Tile other) {
        if (other == null) return false;
        return (this.color & other.color) != 0;
    }

    public Color GetSubcolor(byte c) {
        if (this.Contains(c)) return new Tile(-1, -1, c).GetColor();
        else return Color.black;
    }

    public override string ToString() {
        return base.ToString() + "(" + this.color + ") @[" + x + "," + y + "]";
    }

    public Tile MoveSideways(int x) {
        this.x += x;
        return this;
    }

    public Tile MoveDown() {
        y--;
        return this;
    }


}


public class GameBoard : MonoBehaviour {

    public SpriteRenderer boardSR;
    Texture2D boardTexture;

    public float updateDelay;

    [Header("Game Options")]
    public int width;
    public int height;

    public int score;

    int spawnerX;

    Tile[,] board;
    Tile activeTile;
    byte activeColor;

    float lastUpdate = 0.0f;


	void Start () {
        Debug.Assert(width%2 == 1);

        board = new Tile[width, height];
        spawnerX = (width / 2);

        /* create board */
        boardTexture = new Texture2D(5 * width, 5 * height);
        boardTexture.filterMode = FilterMode.Point;
        boardSR.sprite = Sprite.Create(boardTexture, new Rect(0, 0, 5 * width, 5 * height), new Vector2(0.5f, 0.5f));
        PaintBoard();
	}
	

	void Update () {
        bool redraw = false;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.DownArrow)) {
            while (activeTile.y > 0 && MoveTileDown(activeTile.x, activeTile.y)) {
                redraw = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            if (activeTile.x > 0) {
                redraw |= MoveActiveTile(-1);
            }
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            if (activeTile.x < width - 1) {
                redraw |= MoveActiveTile(+1);
            }
        }

        /* update borad */
        if (Time.time > lastUpdate + updateDelay) {
            lastUpdate = Time.time;
            if (!MoveBoard()) {
                if (board[spawnerX, height - 1] != null) {
                    Debug.Log("Game over");
                    return;
                }
                activeTile = new Tile(spawnerX, height - 1);
                activeColor = activeTile.color;
                board[spawnerX, height - 1] = activeTile;
            }
            redraw = true;
        }

        if (redraw) {
            PaintBoard();
        }
	}


    void PaintBoard() {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (board[x, y] == null) {
                    PaintTile(x, y, Tile.empty);
                } else {
                    PaintTile(x, y, board[x, y]);
                }
                
            }
        }

        boardTexture.Apply();
    }

    void PaintTile(int x, int y, Tile t) {
        for (int i = 0; i < 25; i++) {
            Color c;
            switch (i) {
                case 17:
                    c = t.GetSubcolor(Tile.color_c);
                    break;
                case 8:
                    c = t.GetSubcolor(Tile.color_m);
                    break;
                case 6:
                    c = t.GetSubcolor(Tile.color_y);
                    break;
                default:
                    c = t.GetColor();
                    break;
            }

            boardTexture.SetPixel(x * 5 + i % 5, y * 5 + i / 5, c);
        }
    }


    bool MoveActiveTile(int direction) {
        int newX = activeTile.x + direction;

        if (board[newX, activeTile.y] == null) {
            if (activeTile.color == activeColor) {
                Debug.Log("null -> [ ] -> null");
                board[activeTile.x, activeTile.y] = null;
                board[newX, activeTile.y] = activeTile.MoveSideways(direction);
            } else {
                Debug.Log("[ ] -> [ ] -> null ");
                board[activeTile.x, activeTile.y] -= activeColor;
                board[newX, activeTile.y] = new Tile(newX, activeTile.y, activeColor);
                activeTile = board[newX, activeTile.y];
            }
            return true;
        } else if (board[newX, activeTile.y].Contains(activeTile.color) == false) {
            if (activeTile.color == activeColor) {
                Debug.Log("null -> [ ] -> [ ]");
                board[activeTile.x, activeTile.y] = null;
            } else {
                Debug.Log("[ ] -> [ ] -> [ ]");
                board[activeTile.x, activeTile.y] -= activeColor;
            }
            board[newX, activeTile.y] += activeTile.color;
            activeTile = board[newX, activeTile.y];
            return true;
        }
        
        return false;
    }


    bool MoveTileDown(int x, int y) {
        Debug.Assert(board[x, y] != null, "Moving nonexisting tile");
        //Debug.Assert(board[x, y].color == 0, "Moving empty tile " + board[x, y]);

        /* active tile can pass through */
        if (x == activeTile.x && y == activeTile.y) {
            //Debug.Log("Processing active tile " + board[x, y]);

            if (board[x, y - 1] == null) {
                if (board[x, y] == activeTile) {
                    board[x, y - 1] = board[x, y].MoveDown();
                    board[x, y] = null;
                } else {
                    board[x, y] -= activeColor;
                    if (board[x, y].color == 0) board[x, y] = null;
                    board[x, y - 1] = new Tile(x, y - 1, activeColor);
                    activeTile = board[x, y - 1];
                }
                return true;
            }

            if (board[x, y - 1] != null && board[x, y - 1].Contains(activeColor) == false) {
                board[x, y] -= activeColor;
                if (board[x, y].color == 0) board[x, y] = null;
                board[x, y - 1] += activeColor;
                activeTile = board[x, y - 1];
                return true;
            }
            
        }

        if (board[x, y].Contains(board[x, y - 1]) == false) {
            board[x, y - 1] += board[x, y].MoveDown();
            board[x, y] = null;
            return true;
        }

        return false;
    }


    bool MoveBoard() {
        // move all pieces, if nothing was moved return false
        bool moved = false;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (board[x, y] == null) continue;
                if (board[x, y].color == Tile.color_k) {
                    board[x, y] = null;
                    score++;
                    continue;
                }

                if (y == 0) continue;

                /* move tile */
                moved = MoveTileDown(x, y);
            }
        }

        return moved;
    }
}
