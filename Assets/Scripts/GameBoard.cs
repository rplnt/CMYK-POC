using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Tile {
    static readonly byte[] cmy = { 1, 2, 4 };
    public static readonly byte c = 1 << 0;
    public static readonly byte m = 1 << 1;
    public static readonly byte y = 1 << 2;
    public static readonly byte k = (byte)(c | m | y);

    public static Tile empty = new Tile(0);

    public byte color;

    public Tile() {
        color = cmy[Random.Range(0, 3)];
    }

    public Tile(int c) {
        Debug.Assert(c < 256 && c >= 0, "New Tile Color overflow");
        color = (byte)c;
    }

    public Color getColor() {
        return new Color(
            (color == m || color == y || (color == (m | y))) ? 1 : 0,
            (color == c || color == y || (color == (c | y))) ? 1 : 0,
            (color == c || color == m || (color == (c | m))) ? 1 : 0,
            1
            );
    }

    public static Tile operator |(Tile a, Tile b) {
        if (a == null) return b;
        if (b == null) return a;
        return new Tile(a.color | b.color);
    }

    public void Add(Tile other) {
        if (other == null) return;
        this.color |= other.color;
    }

    public void Add(byte other) {
        this.color |= other;
    }

    public static Tile operator &(Tile a, Tile b) {
        if (a == null || b == null) {
            return empty;
        } else {
            return new Tile(a.color & b.color);
        }
    }

    public override string ToString() {
        return base.ToString() + "(" + this.color + ")";
    }
}


public class GameBoard : MonoBehaviour {

    public SpriteRenderer boardSR;
    Texture2D boardTexture;

    [Header("Game Options")]
    public int width;
    public int height;

    public int score;

    int spawnerX;

    Tile[,] board;
    Tile activeTile;

	// Use this for initialization
	void Start () {
        Debug.Assert(width%2 == 1);

        board = new Tile[width, height];
        spawnerX = (width / 2);

        boardTexture = new Texture2D(width, height);
        boardTexture.filterMode = FilterMode.Point;
        PaintBoard();
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (!MoveBoard()) {
                if (board[spawnerX, height - 1] != null) {
                    /* Game Over */
                    Debug.Log("Game over");
                    return;
                }
                activeTile = new Tile();
                board[spawnerX, height - 1] = activeTile;
            }
            PaintBoard();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            // TODO: tiles should have info about their position
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            //
        }
	}


    void PaintBoard() {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Color c;
                if (board[x, y] == null) {
                    c = Color.white;
                } else {
                    c = board[x, y].getColor();
                }

                boardTexture.SetPixel(x, y, c);
                
            }
        }

        boardTexture.Apply();
        boardSR.sprite = Sprite.Create(boardTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }


    bool MoveBoard() {
        // move all pieces, if nothing was moved return false
        bool moved = false;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (board[x, y] == null) continue;
                if (board[x, y].color == Tile.k) {
                    Debug.Log("Cleared " + x + ", " + y);
                    board[x, y] = null;
                    score++;
                }

                if (y == 0) continue;

                /* move tile */
                if ((board[x, y] & board[x, y - 1]).color == 0) {
                    //Debug.Log(board[x, y]);
                    board[x, y - 1] |= board[x, y];
                    //Debug.Log(board[x, y - 1]);
                    board[x, y] = null;
                    moved = true;
                } else {
                    // should the falling black fall through?
                    Debug.Log(x + "," + y + ": " + board[x, y] + " & " + board[x, y - 1] + " = " + (board[x, y] & board[x, y - 1]));
                }
            }
        }

        return moved;
    }
}
