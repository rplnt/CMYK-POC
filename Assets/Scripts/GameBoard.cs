using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class GameBoard : MonoBehaviour {

    public SpriteRenderer boardSR;
    Texture2D boardTexture;

    [Header("Display Options")]
    public float updateDelay;
    public int tilePixelSize;

    [Header("Game Options")]
    public int width;
    public int height;
    public int spawnGroupSize;
    public int speedUpRate;
    public float speedUpMultiplier;

    bool slamming = false;

    [Header("Stats")]
    public int score;
    public int total;

    public event System.Action ScoreUpdated;

    int spawnerX;

    Tile[,] board;
    Tile[] activeTiles;

    float lastUpdate = 0.0f;
    bool gameOver = false;


	void Start () {
        Debug.Assert(width % 2 == 1, "Board width has to be odd");
        Debug.Assert(tilePixelSize % 2 == 1, "Tile size has to be odd");
        Debug.Assert(tilePixelSize > 2, "Tile size has to be greater than 2");
        activeTiles = new Tile[spawnGroupSize];

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

        if (slamming) {
            slamming = MoveActiveGroup(0, -1);
            if (!slamming) {
                ClearActiveTiles();
            }
            redraw = true;
        } else if (ProcessInput()) {
            redraw = true;
        }

        if (Time.time > lastUpdate + updateDelay) {
            // TODO make this comprehensible
            redraw = MoveBoard();
            if (!redraw) {
                redraw = MoveActiveGroup(0, -1);
                if (!redraw) {
                    /* piece landed */
                    ClearActiveTiles();
                    redraw = ClearK();
                } else {
                    lastUpdate = Time.time;
                }

                /* spawn new piece */
                if (!redraw) {
                    if (!SpawnNewTiles(spawnGroupSize)) {
                        gameOver = true;
                        Debug.Log("Game Over");
                    }

                    if ((total % (speedUpRate * spawnGroupSize)) == 0) {
                        Debug.Log("Speed up");
                        updateDelay *= speedUpMultiplier;
                    }

                    lastUpdate = Time.time;
                    redraw = true;
                }
            }
        }

        if (redraw) {
            PaintBoard();
        }
	}


    bool ProcessInput() {
        bool redraw = false;
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            redraw |= MoveActiveGroup(0, -1);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            redraw |= SwapActiveGroup();
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            slamming = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            redraw |= MoveActiveGroup(-1, 0);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            redraw |= MoveActiveGroup(+1, 0);
        }

        return redraw;
    }


    bool SpawnNewTiles(int size) {
        Debug.Assert(size >= 1, "At least one tile has to be spawned");

        /* prepare tiles */
        for (int i = size; i > 0; i--) {
            activeTiles[i - 1] = new Tile(spawnerX, height - i, true);

            /* check for game over */
            if (board[spawnerX, height - i] == null) continue;
            if (board[spawnerX, height - i].Contains(activeTiles[i - 1].color)) {
                //Debug.Log("Position at " + spawnerX + ", " + (height - i) + " doesn't seem to be good for " + activeTiles[i - 1]);
                return false;
            }
        }

        /* place tiles */
        for (int i = size; i > 0; i--) {
            if (board[spawnerX, height - i] == null) {
                board[spawnerX, height - i] = activeTiles[i - 1];
            } else {
                board[spawnerX, height - i] += activeTiles[i - 1].color;
                board[spawnerX, height - i].activeComponent = activeTiles[i - 1].color;
            }

            total++;
        }

        return true;
    }


    bool SwapActiveGroup() {
        if (activeTiles.Length != 2) return false;

        if (CanMoveActiveTile(activeTiles[0], 0, -1) && CanMoveActiveTile(activeTiles[1], 0, +1)) {
            Tile swapTile = activeTiles[1];
            activeTiles[1] = activeTiles[0].Move(0, -1);
            activeTiles[0] = swapTile.Move(0, +1);

            board[activeTiles[1].x, activeTiles[1].y] = activeTiles[1];
            board[activeTiles[0].x, activeTiles[0].y] = activeTiles[0];

            return true;
        }

        return false;
    }


    bool MoveActiveGroup(int dirX, int dirY) {
        Debug.Assert(dirY <= 0, "MoveActiveGroup: Can't move upwards");
        bool canMove = true;
        /* check if all subtiles can be moved */
        for (int i = activeTiles.Length - 1; i >= 0; i--) {
            if (activeTiles[i] == null) return false;
            canMove = canMove && CanMoveActiveTile(activeTiles[i], dirX, dirY);
        }

        if (!canMove) return false;

        for (int i = activeTiles.Length - 1; i >= 0; i--) {
            Tile tile = activeTiles[i];
            Debug.Assert(tile.active, "Operating with inactive subtile " + tile);

            if (tile.color == tile.activeComponent) {
                //Debug.Log(tile + " null -> [T]");
                board[tile.x, tile.y] = null;
            } else {
                //Debug.Log(tile + " [ ] -> [T]");
                board[tile.x, tile.y] = new Tile(tile.x, tile.y, tile.color - tile.activeComponent, false);
            }

            if (board[tile.x + dirX, tile.y + dirY] == null) {
                //Debug.Log(tile + " [T] -> null");
                board[tile.x + dirX, tile.y + dirY] = new Tile(tile.x + dirX, tile.y + dirY, tile.activeComponent, true);
            } else {
                //Debug.Log(tile + " [T] -> [ ]");
                board[tile.x + dirX, tile.y + dirY] += tile.activeComponent;
                board[tile.x + dirX, tile.y + dirY].activeComponent = tile.activeComponent;
            }

            activeTiles[i] = board[tile.x + dirX, tile.y + dirY];
        }

        return true;
    }


    bool CanMoveActiveTile(Tile tile, int dirX, int dirY) {
        if (tile.activeComponent == 0) return false;
        if (tile.x + dirX > width - 1 || tile.x + dirX < 0 || tile.y + dirY < 0 || tile.y + dirY > height -1) return false;
        if (board[tile.x + dirX, tile.y + dirY] == null) return true;

        //Debug.Log("CanMoveActiveTile: " + board[tile.x + dirX, tile.y + dirY].color + " contains " + tile.activeComponent + ": " + (!board[tile.x + dirX, tile.y + dirY].Contains(tile.activeComponent)));
        //if target tile doesn't contain our active component we can move; if its active component is equal to ours, we can move as well (as it is move out)
        return (!board[tile.x + dirX, tile.y + dirY].Contains(tile.activeComponent) || board[tile.x + dirX, tile.y + dirY].activeComponent == tile.activeComponent);
    }


    bool MoveRegularTile(int x, int y) {
        if (board[x, y].active) {
            return false;
        }

        if (y == 0) return false;

        if (board[x, y - 1] == null) {
            board[x, y - 1] = board[x, y].Move(0, -1);
            board[x, y] = null;
            return true;
        }

        return false;
    }


    void ClearActiveTiles() {
        foreach (Tile tile in activeTiles) {
            if (tile == null) return;
            tile.activeComponent = 0;
        }
    }


    bool ClearK() {
        int cleared = 0;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (board[x, y] == null) continue;
                if (board[x, y].color == Tile.color_k) {
                    board[x, y] = null;
                    cleared++;
                }
            }
        }

        if (cleared == 2) cleared = 3;

        score += cleared;
        if (cleared != 0 && ScoreUpdated != null) {
            ScoreUpdated();
        }
        return cleared != 0;
    }


    bool MoveBoard() {
        bool movedAnything = false;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (board[x, y] == null) continue;
                bool moved = MoveRegularTile(x, y);
                movedAnything |= moved;
            }
        }

        return movedAnything;
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
            if (i == (tilePixelSize * tilePixelSize - tilePixelSize * (tilePixelSize / 3) - tilePixelSize / 2 - 1)) { c = t.GetSubcolor(Tile.color_c); } else if (i == (tilePixelSize / 2 + tilePixelSize * (tilePixelSize / 3) + 1)) { c = t.GetSubcolor(Tile.color_m); } else if (i == (tilePixelSize / 2 + tilePixelSize * (tilePixelSize / 3) - 1)) { c = t.GetSubcolor(Tile.color_y); } else { c = t.GetColor(); }

            if (t.active) { c *= 2f; }

            boardTexture.SetPixel(x * tilePixelSize + i % tilePixelSize, y * tilePixelSize + i / tilePixelSize, c);
        }
    }
}
