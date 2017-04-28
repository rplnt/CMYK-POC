using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Tile {
    private static readonly byte[] cmyGen = { 1 << 0, 1 << 1, 1 << 2 };
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
            (color == color_m || color == color_y || (color == (color_m | color_y))) ? 0.75f : 0.0f,
            (color == color_c || color == color_y || (color == (color_c | color_y))) ? 0.75f : 0.0f,
            (color == color_c || color == color_m || (color == (color_c | color_m))) ? 0.75f : 0.0f,
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
    public int tilePixelSize;

    [Header("Game Options")]
    public int width;
    public int height;
    public bool passThroughEnabled;

    public int score;
    public int total;

    int spawnerX;

    Tile[,] board;
    Tile activeTile;
    byte activeColor;

    float lastUpdate = 0.0f;
    bool gameOver = false;


	void Start () {
        Debug.Assert(width % 2 == 1, "Board width has to be odd");
        Debug.Assert(tilePixelSize % 2 == 1, "Tile size has to be odd");
        Debug.Assert(tilePixelSize > 2, "Tile size has to be greater than 2");

        board = new Tile[width, height];
        spawnerX = (width / 2);
        total = 0;

        /* create board */
        boardTexture = new Texture2D(width * tilePixelSize, height * tilePixelSize);
        boardTexture.filterMode = FilterMode.Point;
        boardSR.sprite = Sprite.Create(boardTexture, new Rect(0, 0, width * tilePixelSize, height * tilePixelSize), new Vector2(0.5f, 0.5f));
        PaintBoard();
	}
	

	void Update () {
        if (gameOver) return;
        bool redraw = false;

        if (Input.GetKeyDown(KeyCode.DownArrow) && activeTile.y > 0) {
            redraw = MoveTileDown(activeTile.x, activeTile.y);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
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
                Tile newTile = new Tile(spawnerX, height - 1);
                total++;

                if (total % 10 == 0) {
                    Debug.Log("Speed up");
                    updateDelay *= 0.9f;
                }

                if (board[spawnerX, height - 1] == null) {
                    activeTile = newTile;
                } else if (board[spawnerX, height - 1].Contains(newTile.color) == false) {
                    board[spawnerX, height - 1] += newTile.color;
                } else {
                    gameOver = true;
                    Debug.Log("Game Over");
                }            
                
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
        for (int i = 0; i < tilePixelSize * tilePixelSize; i++) {
            Color c;
            //if (t == activeTile && (i < tilePixelSize || i >= tilePixelSize * (tilePixelSize - 1) || i % tilePixelSize == 0 || i % tilePixelSize == tilePixelSize - 1)) { c = Color.white; }
            if (i == (tilePixelSize * tilePixelSize - tilePixelSize * (tilePixelSize / 3) - tilePixelSize / 2 - 1)) { c = t.GetSubcolor(Tile.color_c); }
            else if (i == (tilePixelSize / 2 + tilePixelSize * (tilePixelSize / 3) + 1)) { c = t.GetSubcolor(Tile.color_m); }
            else if (i == (tilePixelSize / 2 + tilePixelSize * (tilePixelSize / 3) - 1)) { c = t.GetSubcolor(Tile.color_y); } 
            else { c = t.GetColor(); }

            if (t == activeTile && (i < tilePixelSize || i >= tilePixelSize * (tilePixelSize - 1) || i % tilePixelSize == 0 || i % tilePixelSize == tilePixelSize - 1)) { c *= 1.1f; }

            boardTexture.SetPixel(x * tilePixelSize + i % tilePixelSize, y * tilePixelSize + i / tilePixelSize, c);
        }
    }


    bool MoveActiveTile(int direction) {
        int newX = activeTile.x + direction;

        if (board[newX, activeTile.y] == null) {
            if (activeTile.color == activeColor) {
                //Debug.Log("null -> [ ] -> null");
                board[activeTile.x, activeTile.y] = null;
                board[newX, activeTile.y] = activeTile.MoveSideways(direction);
            } else if (passThroughEnabled) {
                //Debug.Log("[ ] -> [ ] -> null ");
                board[activeTile.x, activeTile.y] -= activeColor;
                board[newX, activeTile.y] = new Tile(newX, activeTile.y, activeColor);
                activeTile = board[newX, activeTile.y];
            }
            return true;
        } else if (board[newX, activeTile.y].Contains(activeTile.color) == false) {
            if (activeTile.color == activeColor) {
                //Debug.Log("null -> [ ] -> [ ]");
                board[activeTile.x, activeTile.y] = null;
            } else if (passThroughEnabled) {
                //Debug.Log("[ ] -> [ ] -> [ ]");
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
        Debug.Assert(board[x, y].color != 0, "Moving empty tile " + board[x, y]);

        if (y == 0) return false;

        /* active tile can pass through */
        if (x == activeTile.x && y == activeTile.y) {
            //Debug.Log("Processing active tile " + board[x, y]);

            if (board[x, y - 1] == null) {
                if (board[x, y] == activeTile) {
                    board[x, y - 1] = board[x, y].MoveDown();
                    board[x, y] = null;
                } else {
                    // I have no idea what this is supposed to be
                    //board[x, y] -= activeColor;
                    //if (board[x, y].color == 0) board[x, y] = null;
                    //board[x, y - 1] = new Tile(x, y - 1, activeColor);
                    //activeTile = board[x, y - 1];
                }
                return true;
            }

            if (board[x, y - 1] != null && board[x, y - 1].Contains(activeColor) == false) {
                if (!passThroughEnabled && board[x, y].color != activeColor) return false;
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
        bool movedAnything = false;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (board[x, y] == null) continue;

                /* move tile */
                bool moved = MoveTileDown(x, y);
                movedAnything |= moved;

                if (!moved && board[x, y].color == Tile.color_k) {
                    board[x, y] = null;
                    score++;
                    continue;
                }
            }
        }

        return movedAnything;
    }
}
