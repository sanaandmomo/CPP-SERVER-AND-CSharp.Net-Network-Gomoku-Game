using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class SinglePlayForm : Form
    {
        private const int rectSize = 33; // 오목판의 셀 크기
        private const int edgeCount = 15; // 오목판의 선 개수

        // 15 X 15
        private enum Horse {  none = 0, BLACK, WHITE };
        private Horse[,] board = new Horse[edgeCount, edgeCount];
        private Horse nowPlayer = Horse.BLACK;

        private bool playing = false;

        public SinglePlayForm()
        {
            InitializeComponent();
        }

        private bool judge() // 승리 판정 함수
        {
            // 가로
            for (int x = 0; x < edgeCount; x++)
                for (int y = 4; y < edgeCount; y++)
                    if (board[x, y] == nowPlayer && board[x, y - 1] == nowPlayer && board[x, y - 2] == nowPlayer &&
                        board[x, y - 3] == nowPlayer && board[x, y - 4] == nowPlayer)
                        return true;

            // 세로
            for (int y = 0; y < edgeCount; y++)
                for (int x = 4; x < edgeCount; x++)
                    if (board[x, y] == nowPlayer && board[x - 1, y] == nowPlayer && board[x - 2, y] == nowPlayer &&
                        board[x - 3, y] == nowPlayer && board[x - 4, y] == nowPlayer)
                        return true;

            // y = x 대각선
            for (int x = 4; x < edgeCount; x++)
                for (int y = 0; y < edgeCount - 4; y++)
                    if (board[x, y] == nowPlayer && board[x - 1, y + 1] == nowPlayer && board[x - 2, y + 2] == nowPlayer &&
                        board[x - 3, y + 3] == nowPlayer && board[x - 4, y + 4] == nowPlayer)
                        return true;

            // y = -x 대각선
            for (int x = 0; x < edgeCount - 4; x++)
                for (int y = 0; y < edgeCount - 4; y++)
                    if (board[x, y] == nowPlayer && board[x + 1, y + 1] == nowPlayer && board[x + 2, y + 2] == nowPlayer &&
                       board[x + 3, y + 3] == nowPlayer && board[x + 4, y + 4] == nowPlayer)
                        return true;

            return false;
        }

        private void refresh()
        {
            this.boardPicture.Refresh();

            for (int i = 0; i < edgeCount; i++)
                for (int j = 0; j < edgeCount; j++)
                    board[i, j] = Horse.none;
        }

        private void boardPicture_MouseDown(object sender, MouseEventArgs e)
        {
            if (!playing)
            {
                MessageBox.Show("게임을 실행해주세요.");
                return;
            }

            Graphics g = this.boardPicture.CreateGraphics();
            int x = e.X / rectSize;
            int y = e.Y / rectSize;

            if (x < 0 || y < 0 || x >= edgeCount || y >= edgeCount)
            {
                MessageBox.Show("테두리를 벗어날 수 없습니다.");
                return;
            }

            if (board[x, y] != Horse.none) return;
            board[x, y] = nowPlayer;

            Color color = nowPlayer == Horse.BLACK ? Color.Black : Color.White;
            SolidBrush brush = new SolidBrush(color);
            g.FillEllipse(brush, x * rectSize, y * rectSize, rectSize, rectSize);

            if (judge())
            {
                status.Text = nowPlayer.ToString() + "플레이어가 승리했습니다.";
                playing = false;
                playButton.Text = "게임 시작";
            }
            else
            {
                nowPlayer = nowPlayer == Horse.BLACK ? Horse.WHITE : Horse.BLACK;
                status.Text = nowPlayer.ToString() + " 플레이어의 차례입니다.";
            }
        }

        private void boardPicture_Paint(object sender, PaintEventArgs e)
        {
            Graphics gp = e.Graphics;
            Color lineColor = Color.Black; // 오목판의 선 색깔
            Pen p = new Pen(lineColor, 2);

            float leftX = rectSize / 2;
            float rightX = rectSize * edgeCount - leftX;
            float topY = rectSize / 2;
            float bottomY = rectSize * edgeCount - topY;

            gp.DrawLine(p, leftX, topY, leftX, bottomY); // 좌측
            gp.DrawLine(p, leftX, topY, rightX, topY); // 상측
            gp.DrawLine(p, leftX, bottomY, rightX, bottomY); // 하측
            gp.DrawLine(p, rightX, topY, rightX, bottomY); // 우측

            p = new Pen(lineColor, 1);

            // 대각선 방향으로 이동하면서 십자가 모양의 선 그리기
            for (float i = rectSize + leftX; i < rightX; i += rectSize)
            {
                gp.DrawLine(p, leftX, i, rightX, i); // 가로
                gp.DrawLine(p, i, topY, i, bottomY); // 세로
            }
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            refresh();

            if (playing)
            {
                status.Text = "게임이 재시작되었습니다.";
            }
            else
            {
                playing = true;
                playButton.Text = "재시작";
                status.Text = nowPlayer.ToString() + " 플레이어의 차례입니다.";
            }
        }

        
    }
}
