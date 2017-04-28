using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile {
    public bool active = false;
    public Tile bottomSibling;

    private static readonly byte[] cmyGen = { 1 << 0, 1 << 1, 1 << 2 };
    public static readonly byte color_c = 1 << 0;
    public static readonly byte color_m = 1 << 1;
    public static readonly byte color_y = 1 << 2;
    public static readonly byte color_k = (byte)(color_c | color_m | color_y);

    public static Tile empty = new Tile(-1, -1, 0);

    public byte color;
    public byte originalColor;

    public int x { get; protected set; }
    public int y { get; protected set; }

    public Tile(int x, int y, bool active = false) {
        this.active = active;
        this.x = x;
        this.y = y;
        this.color = cmyGen[Random.Range(0, 3)];
        this.originalColor = color;
    }

    public Tile(int x, int y, int c, bool active = false) {
        Debug.Assert(c < 256 && c >= 0, "New Tile Color overflow");
        this.active = active;
        this.x = x;
        this.y = y;
        this.color = (byte)c;
        this.originalColor = color;
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
        return new Tile(left.x, left.y, left.color | right.color, left.active || right.active);
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
