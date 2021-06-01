using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
Application.SetHighDpiMode(HighDpiMode.SystemAware);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Rectangle screenBound = Screen.PrimaryScreen.Bounds;
Form minefieldForm = new()
{
    Size = new Size(screenBound.Height / 10 * 9, screenBound.Height / 10 * 9),
    StartPosition = FormStartPosition.CenterScreen,
    Font = new Font("Consolas", 6),
    WindowState = FormWindowState.Normal,
    ShowIcon = false,
    ShowInTaskbar = true,
    HelpButton = false,
    BackColor = Color.White,
    ControlBox = false,
    FormBorderStyle = FormBorderStyle.FixedSingle,
    ForeColor = Color.Black
};
bool debugging = false;
string header = "Minefield";
int defSize = 15,
    minSize = 10,
    maxSize = 50,
    fieldSize = 0,
    mineCount = 0;
GameState state = GameState.Building;
List<List<Cell>> table;
Panel gameAreaPanel = new()
{
    Parent = minefieldForm,
    Dock = DockStyle.Bottom,
    BackColor = Color.AliceBlue,
    Height = minefieldForm.Height / 10 * 9,
};
Panel gameInfoPanel = new()
{
    Parent = minefieldForm,
    Dock = DockStyle.Top,
    BackColor = Color.LightGreen,
    Height = minefieldForm.Height - gameAreaPanel.Height
};
minefieldForm.Shown += (s, e) =>
{
    Work();
};
Application.Run(minefieldForm);
void Work()
{
    while (!CheckState(GameState.Abort) && !CheckState(GameState.FirstClicking))
    {
        switch (state)
        {
            case GameState.Building:
                Building();
                break;
            case GameState.End:
                End();
                break;
        }
    }
}
void SetState(GameState gameState) => state = gameState;
bool CheckState(GameState gameState) => state == gameState;
void Building()
{
    gameAreaPanel.Enabled = true;

    string inputText = Interaction.InputBox("Field Size ?", header, defSize.ToString());

    if (string.IsNullOrEmpty(inputText))
        SetState(GameState.Abort);
    else if (int.TryParse(inputText, out fieldSize) && fieldSize >= minSize && fieldSize <= maxSize)
    {
        mineCount = fieldSize * 2;
        table = new();

        for (int a = 0; a < fieldSize; a++)
        {
            List<Cell> row = new();
            Size CellSize = new(gameAreaPanel.Width / fieldSize, gameAreaPanel.Height / fieldSize);

            for (int b = 0; b < fieldSize; b++)
            {
                Cell cell = new()
                {
                    Text = debugging ? $"{a},{b}" : "",
                    Row = a,
                    Column = b,
                    Parent = gameAreaPanel,
                    Size = CellSize,
                    Location = new(CellSize.Width * a, CellSize.Height * b),
                    Font = new Font(minefieldForm.Font.FontFamily, minefieldForm.Font.Size + 6, FontStyle.Bold)
                };
                cell.Click += CellClick;
                cell.Paint += CellPaint;
                cell.MouseUp += CellFlag;
                row.Add(cell);
            }

            table.Add(row);
        }
        SetState(GameState.FirstClicking);
    }
    else
        MessageBox.Show($"input Require number (Range : {minSize}-{maxSize})", header);
}
void CellFlag(object s, MouseEventArgs e)
{
    Cell cell = ((Cell)s);
    if (e.Button == MouseButtons.Right)
    {
        cell.Text = cell.Text == "" ? "X" : "";
        cell.Invalidate();
    }
}
void CellPaint(object s, PaintEventArgs e)
{
    Cell cell = ((Cell)s);
    dynamic fillBrush = new SolidBrush(cell.BackColor);
    dynamic drawBrush = new SolidBrush(cell.ForeColor);
    dynamic sf = new StringFormat
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
    Rectangle rectangle = cell.ClientRectangle;
    rectangle.Width -= 10;
    rectangle.Height -= 10;
    rectangle.X += 5;
    rectangle.Y += 5;
    e.Graphics.FillRectangle(fillBrush, rectangle);
    e.Graphics.DrawString(cell.Text, cell.Font, drawBrush, cell.ClientRectangle, sf);
    drawBrush.Dispose();
    sf.Dispose();
}
void CellClick(object s, EventArgs e)
{
    Cell current = ((Cell)s);
    if (CheckState(GameState.FirstClicking))
    {
        current.IsFirstClick = true;
        MineSetting(current);
        Bloom(current);
        return;
    }
    if (CheckState(GameState.Playing))
    {
        if (current.IsMine)
        {
            SetState(GameState.End);
            Work();
        }
        else
            Bloom(current);
    }
}
void MineSetting(Cell clicking)
{
    Random random1 = new(DateTime.Now.Millisecond);
    Thread.Sleep(10);
    Random random2 = new(DateTime.Now.Millisecond);
    Thread.Sleep(10);

    int i = 0;

    do
    {
        int a = random1.Next(0, fieldSize);
        int b = random2.Next(0, fieldSize);

        Cell current = table[a][b];

        if (!current.IsMine && !current.IsFirstClick)
        {
            List<Side> sides = Enum.GetValues(typeof(Side)).Cast<Side>().ToList();

            bool isFriendFirstClick = false;

            foreach (Side side in sides)
            {
                Cell friend = GetOrNull(current, side, true);
                if (friend != null && friend.IsFirstClick)
                {
                    isFriendFirstClick = true;
                    break;
                }
            }

            if (isFriendFirstClick)
                continue;

            ++i;

            current.IsMine = true;
            current.Text = debugging ? "W" : "";

            current.Invalidate();
        }
    } while (i < mineCount);

    SetState(GameState.Playing);
}
void Bloom(Cell cell)
{
    if (cell is null || cell.IsMine || cell.IsBloom)
        return;

    cell.IsBloom = true;
    cell.Enabled = false;
    cell.BackColor = Color.LightGray;
    cell.Invalidate();

    List<Side> sides = Enum.GetValues(typeof(Side)).Cast<Side>().ToList();

    int friendMines = 0;

    foreach (Side side in sides)
    {
        Cell friendCell = GetOrNull(cell, side);
        if (friendCell != null && friendCell.IsMine)
            ++friendMines;
    }

    if (friendMines > 0)
    {
        cell.Text = friendMines.ToString();
        cell.ForeColor = ChooseColor(friendMines);
        cell.Invalidate();
        return;
    }

    foreach (Side side in sides)
        Bloom(GetOrNull(cell, side));
}
Color ChooseColor(int count) => count switch
{
    1 => Color.Blue,
    2 => Color.Green,
    3 => Color.Orange,
    4 => Color.Purple,
    5 => Color.Brown,
    6 => Color.Red,
    7 => Color.DarkRed,
    8 => Color.DarkGray,
    _ => Color.Black,
};
Cell GetOrNull(Cell cell, Side side, bool getCross = false)
{
    int row = cell.Row + side switch
    {
        Side.Top => -1,
        Side.TopLeft => getCross ? -1 : 0,
        Side.TopRight => getCross ? -1 : 0,
        Side.Bottom => +1,
        Side.BottomLeft => getCross ? +1 : 0,
        Side.BottomRight => getCross ? +1 : 0,
        _ => 0
    };
    int column = cell.Column + side switch
    {
        Side.Left => -1,
        Side.TopLeft => getCross ? -1 : 0,
        Side.BottomLeft => getCross ? -1 : 0,
        Side.Right => +1,
        Side.TopRight => getCross ? +1 : 0,
        Side.BottomRight => getCross ? +1 : 0,
        _ => 0
    };
    if (
        (row < fieldSize && row > -1)
        &&
        (column < fieldSize && column > -1)
        )
        return table[row][column];

    return null;
}
void End()
{
    gameAreaPanel.Enabled = false;
    MessageBox.Show("End Game!", header);
    SetState(GameState.Abort);
}
class Cell : Button
{
    public int Row { get; set; }
    public int Column { get; set; }
    public bool IsBloom { get; set; }
    public bool IsMine { get; set; }
    public bool IsFirstClick { get; set; }
}
enum Side
{
    Left,
    Top,
    TopLeft,
    TopRight,
    Right,
    Bottom,
    BottomLeft,
    BottomRight
}
enum GameState
{
    Building,
    FirstClicking,
    MineSetting,
    Playing,
    End,
    Abort
}