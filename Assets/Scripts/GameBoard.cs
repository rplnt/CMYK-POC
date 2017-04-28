using System.Collections;
using System.Collections.Generic;
using UnityEngine;




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
                SpawnNewTiles(1);

                if (total % 10 == 0) {
                    Debug.Log("Speed up");
                    updateDelay *= 0.9f;
                }
            }
            redraw = true;
        }

        if (redraw) {
            PaintBoard();
        }
	}


    void SpawnNewTiles(int size) {
        Debug.Assert(size >= 1, "At least one tile has to be spawned");
        Tile topTile = null;
        Tile prevTile = null;
        for (int i = 1; i <= size; i++) {
            topTile = new Tile(spawnerX, height - size, true);
            if (prevTile != null) {
                topTile.bottomSibling = prevTile;
            }
            prevTile = topTile;
            total++;
        }

        if (board[spawnerX, height - 1] == null) {
            activeTile = topTile;
        } else if (board[spawnerX, height - 1].Contains(topTile.color) == false) {
            board[spawnerX, height - 1] += topTile.color;
        } else {
            gameOver = true;
            Debug.Log("Game Over");
        }

        activeTile.originalColor = activeTile.color;
        board[spawnerX, height - 1] = activeTile;
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

            //if (t.active && (i < tilePixelSize || i >= tilePixelSize * (tilePixelSize - 1) || i % tilePixelSize == 0 || i % tilePixelSize == tilePixelSize - 1)) { c *= 1.5f; }
            if (t.active) { c *= 2f; }

            boardTexture.SetPixel(x * tilePixelSize + i % tilePixelSize, y * tilePixelSize + i / tilePixelSize, c);
        }
    }


    bool MoveActiveTile(int direction) {
        int newX = activeTile.x + direction;

        if (board[newX, activeTile.y] == null) {
            if (activeTile.color == activeTile.originalColor) {
                //Debug.Log("null -> [ ] -> null");
                board[activeTile.x, activeTile.y] = null;
                board[newX, activeTile.y] = activeTile.MoveSideways(direction);
            } else if (passThroughEnabled) {
                //Debug.Log("[ ] -> [ ] -> null ");
                board[activeTile.x, activeTile.y] -= activeTile.originalColor;
                board[activeTile.x, activeTile.y].active = false;
                board[newX, activeTile.y] = new Tile(newX, activeTile.y, activeTile.originalColor, true);
                activeTile = board[newX, activeTile.y];
            }
            return true;
        } else if (board[newX, activeTile.y].Contains(activeTile.color) == false) {
            if (activeTile.color == activeTile.originalColor) {
                //Debug.Log("null -> [ ] -> [ ]");
                board[activeTile.x, activeTile.y] = null;
            } else if (passThroughEnabled) {
                //Debug.Log("[ ] -> [ ] -> [ ]");
                board[activeTile.x, activeTile.y] -= activeTile.originalColor;
                board[activeTile.x, activeTile.y].active = false;
            }
            board[newX, activeTile.y] += activeTile.color;
            board[newX, activeTile.y].active = true;
            activeTile = board[newX, activeTile.y];
            return true;
        }
        
        return false;
    }


    bool MoveTileDown(int x, int y) {
        Debug.Assert(board[x, y] != null, "Moving nonexisting tile");
        Debug.Assert(board[x, y].color != 0, "Moving empty tile " + board[x, y]);

        /* active tile can pass through */
        if (board[x, y].active) {
            //Debug.Log("Processing active tile " + board[x, y]);
            if (y == 0) {
                board[x, y].active = false;
                return false;
            }

            if (board[x, y - 1] == null) {
                board[x, y - 1] = board[x, y].MoveDown();
                board[x, y] = null;
                return true;
            } else if (board[x, y - 1].Contains(activeTile.originalColor) == false) {
                if (!passThroughEnabled && board[x, y].color != activeTile.originalColor) return false;
                board[x, y] -= activeTile.originalColor;
                if (board[x, y].color == 0) {
                    board[x, y] = null;
                } else {
                    board[x, y].active = false;
                }
                board[x, y - 1] += activeTile.originalColor;
                board[x, y - 1].active = true;
                activeTile = board[x, y - 1];
                return true;
            }

            board[x, y].active = false;
            return false;
        }

        if (y == 0) return false;
        if (board[x, y].Contains(board[x, y - 1]) == false) {
            board[x, y - 1] += board[x, y].MoveDown();
            //board[x, y - 1] += board[x, y].color;
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
