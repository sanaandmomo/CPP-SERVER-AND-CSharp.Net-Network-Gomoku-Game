using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    public partial class MultiPlayForm : Form
    {
        private Thread thread; // 통신을 위한 쓰레드
        private TcpClient tcpClient; // TCP 클라이언트
        private NetworkStream stream; // 퉁신을 주고 받는 스트림

        private const int rectSize = 33; // 오목판의 셀 크기
        private const int edgeCount = 15; // 오목판의 선 개수

        // 15 X 15
        private enum Horse { none = 0, BLACK, WHITE };
        private Horse[,] board;
        private Horse nowPlayer;
        private bool nowTurn;
        private int roomID;

        private bool playing;
        private bool entered;
        private bool threading;

        public MultiPlayForm()
        {
            InitializeComponent();
            this.playButton.Enabled = false;
            playing = false;
            entered = false;
            threading = false;
            board = new Horse[edgeCount, edgeCount];
            nowTurn = false;
        }

        private bool judge(Horse player) // 승리 판정 함수
        {
            // 가로
            for (int x = 0; x < edgeCount; x++)
                for (int y = 4; y < edgeCount; y++)
                    if (board[x, y] == player && board[x, y - 1] == player && board[x, y - 2] == player &&
                        board[x, y - 3] == player && board[x, y - 4] == player)
                        return true;

            // 세로
            for (int y = 0; y < edgeCount; y++)
                for (int x = 4; x < edgeCount; x++)
                    if (board[x, y] == player && board[x - 1, y] == player && board[x - 2, y] == player &&
                        board[x - 3, y] == player && board[x - 4, y] == player)
                        return true;

            // y = x 대각선
            for (int x = 4; x < edgeCount; x++)
                for (int y = 0; y < edgeCount - 4; y++)
                    if (board[x, y] == player && board[x - 1, y + 1] == player && board[x - 2, y + 2] == player &&
                        board[x - 3, y + 3] == player && board[x - 4, y + 4] == player)
                        return true;

            // y = -x 대각선
            for (int x = 0; x < edgeCount - 4; x++)
                for (int y = 0; y < edgeCount - 4; y++)
                    if (board[x, y] == player && board[x + 1, y + 1] == player && board[x + 2, y + 2] == player &&
                       board[x + 3, y + 3] == player && board[x + 4, y + 4] == player)
                        return true;

            return false;
        }

        private void refresh()
        {
            this.boardPicture.Refresh();

            for (int i = 0; i < edgeCount; i++)
                for (int j = 0; j < edgeCount; j++)
                    board[i, j] = Horse.none;

            this.playButton.Enabled = false;
        }

        private void boardPicture_MouseDown(object sender, MouseEventArgs e)
        {
            if (!playing)
            {
                MessageBox.Show("게임을 실행해주세요.");
                return;
            }

            if (!nowTurn) return;

            int x = e.X / rectSize;
            int y = e.Y / rectSize;

            if (x < 0 || y < 0 || x >= edgeCount || y >= edgeCount)
            {
                MessageBox.Show("테두리를 벗어날 수 없습니다.");
                return;
            }

            if (board[x, y] != Horse.none) return;
            board[x, y] = nowPlayer;
            paintCoord(x, y, nowPlayer);

            // 놓은 바둑돌의 위치 보내기
            string message = label("Put") + roomID + "," + x + "," + y;
            SendMessage(message);

            if (judge(nowPlayer))
            {
                status.Text = "승리했습니다.";
                playing = false;
                playButton.Text = "재시작";
                playButton.Enabled = true;
            }
            else
            {
                status.Text = "상대방이 둘 차례입니다.";
                nowTurn = false;
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

        private void enterButton_Click(object sender, EventArgs e)
        {
            // tcp 연결 인스턴스와 연결 스트림 인스턴스 초기화
            tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", 9876);
            stream = tcpClient.GetStream();

            // 쓰레드 초기화
            thread = new Thread(new ThreadStart(read));
            thread.Start();
            threading = true;

            // 방 접속하기
            roomID = Int32.Parse(roomTextBox.Text);
            SendMessage(label("Enter") + roomID);
        }

        // 서버로부터 메세지를 전달 받는 함수
        private void read()
        {
            while (true)
            {
                // tcp stream으로부터 서버의 메세지를 받아서 buf 버퍼에 담기
                byte[] buf = new byte[1024];
                int bufBytes = stream.Read(buf, 0, buf.Length);
                string message = Encoding.ASCII.GetString(buf, 0, bufBytes);

                // 접속 성공
                if (message.Contains(label("Enter")))
                {
                    this.status.Text = "[" + roomID + "]번 방에 접속했습니다.";

                    // 게임 시작 처리
                    this.roomTextBox.Enabled = false;
                    this.enterButton.Enabled = false;
                    entered = true;
                }
                // 방이 가득 찬 경우
                else if (message.Contains(label("Full")))
                {
                    this.status.Text = "이미 가득 찬 방입니다.";
                    closeNetWork(); // 쓰레드와 tcp통신 인스턴스 초기화 (enterRoom 하면서 다시 생성하기 때문)
                }
                // 게임 시작
                else if (message.Contains(label("Play")))
                {
                    refresh();
                    string horse = message.Split(']')[1];

                    if (horse.Contains("Black"))
                    {
                        this.status.Text = "당신의 차례입니다.";
                        nowTurn = true;
                        nowPlayer = Horse.BLACK;
                    }
                    else
                    {
                        this.status.Text = "상대방의 차례입니다.";
                        nowTurn = false;
                        nowPlayer = Horse.WHITE;
                    }

                    playing = true; 
                }
                // 상대방이 나간 경우
                else if (message.Contains(label("Exit")))
                {
                    this.status.Text = "상대방이 나갔습니다.";
                    refresh();
                }
                // 상대방이 돌을 둔 경우
                else if (message.Contains(label("Put")))
                {
                    string position = message.Split(']')[1];
                    string[] coord = position.Split(',');
                    int x = Convert.ToInt32(coord[0]);
                    int y = Convert.ToInt32(coord[1]);

                    // 돌은 둔 위치에 이미 다른 돌이 있을 때 넘어감
                    if (board[x, y] != Horse.none) continue;

                    Horse enemyPlayer = nowPlayer == Horse.BLACK ? Horse.WHITE : Horse.BLACK;
                    paintCoord(x, y, enemyPlayer);

                    if (judge(enemyPlayer))
                    {
                        status.Text = "패배했습니다.";
                        playing = false;
                        playButton.Text = "재시작";
                        playButton.Enabled = true;
                    }
                    else
                    {
                        status.Text = "당신이 둘 차례입니다.";
                        nowTurn = true;
                    }
                }
            }
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            if (playing) return;

            refresh();
            playing = true;

            // 서버로 메세지 보내기
            SendMessage(label("Play") + roomID);

            this.status.Text = "상대 플레이어의 준비를 기다립니다.";
            this.playButton.Enabled = false;
        }

        private void MultiPlayForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            closeNetWork();
        }

        private void closeNetWork()
        {
            if (threading && thread.IsAlive) thread.Abort();

            if (entered) tcpClient.Close();
        }

        private string label(string message)
        {
            return "[" + message + "]";
        }

        private void SendMessage(string message)
        {
            byte[] buf = Encoding.ASCII.GetBytes(message);
            stream.Write(buf, 0, buf.Length); // 서버로 메세지 보내기
        }

        private void paintCoord(int x, int y, Horse player)
        {
            Graphics g = this.boardPicture.CreateGraphics();
            Color color = player == Horse.BLACK ? Color.Black : Color.White;
            SolidBrush brush = new SolidBrush(color);

            board[x, y] = player;
            g.FillEllipse(brush, x * rectSize, y * rectSize, rectSize, rectSize);
        }
    }
}
