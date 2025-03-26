using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading; // เพิ่ม using System.Threading; เพื่อจัดการการเวลาเปลี่ยนแปลงตำแหน่งสิ่งกีดขวาง
using System.Threading.Tasks;

namespace Snake
{
    public class GameState
    {
        public int Rows { get; }
        public int Cols { get; }
        public GridValue[,] Grid { get; }
        public Direction Dir { get; private set; }
        public int Score { get; private set; }
        public bool GameOver { get; private set; }

        private readonly LinkedList<Direction> dirChanges = new LinkedList<Direction>();
        private readonly LinkedList<Position> snakePositions = new LinkedList<Position>();
        private readonly Random random = new Random();

        public List<Obstacle> Obstacles { get; private set; } = new List<Obstacle>(); // ✅ เพิ่ม List สำหรับสิ่งกีดขวาง

        private Timer obstacleTimer; // ✅ ใช้ System.Threading.Timer

        public GameState(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Grid = new GridValue[rows, cols];
            Dir = Direction.Right;

            AddSnake();
            AddFood();
            GenerateObstacles(5); // ✅ สร้างสิ่งกีดขวางเริ่มต้น

            // ✅ ตั้งค่าตัวจับเวลาให้เปลี่ยนสิ่งกีดขวางทุกๆ 10 วินาที
            obstacleTimer = new Timer(_ => RegenerateObstacles(), null, 10000, 10000);
        }

        private void AddSnake()
        {
            int r = Rows / 2;

            for (int c = 0; c < 3; c++)
            {
                Grid[r, c] = GridValue.Snake;
                snakePositions.AddFirst(new Position(r, c));
            }
        }

        private IEnumerable<Position> EmptyPosition()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (Grid[r, c] == GridValue.Empty)
                    {
                        yield return new Position(r, c);
                    }
                }
            }
        }

        private void AddFood()
        {
            List<Position> empty = new List<Position>(EmptyPosition());
            if (empty.Count == 0)
            {
                return;
            }

            Position pos = empty[random.Next(empty.Count)];
            Grid[pos.Row, pos.Col] = GridValue.Food;
        }

        private void GenerateObstacles(int count)
        {
            Obstacles.Clear(); // ✅ ลบสิ่งกีดขวางเดิม
            for (int i = 0; i < count; i++)
            {
                Position pos;
                do
                {
                    pos = new Position(random.Next(Rows), random.Next(Cols));
                } while (Grid[pos.Row, pos.Col] != GridValue.Empty); // ✅ ตรวจสอบหาตำแหน่งว่าง

                Obstacle newObstacle = new Obstacle(pos);
                Obstacles.Add(newObstacle);
                Grid[pos.Row, pos.Col] = GridValue.Obstacle; // ✅ เพิ่มสิ่งกีดขวางลงในกริด
            }
        }

        // ✅ ฟังก์ชันเปลี่ยนตำแหน่งสิ่งกีดขวางทุก 10 วินาที
        private void RegenerateObstacles()
        {
            if (GameOver) return; // ✅ ถ้าเกมจบไม่ให้ทำงาน

            lock (this)
            {
                foreach (var obstacle in Obstacles)
                {
                    Grid[obstacle.Position.Row, obstacle.Position.Col] = GridValue.Empty; // ✅ ลบสิ่งกีดขวางเก่า
                }

                GenerateObstacles(5); // ✅ สร้างสิ่งกีดขวางใหม่
            }
        }

        public Position HeadPosition()
        {
            return snakePositions.First.Value;
        }

        public Position TailPosition()
        {
            return snakePositions.Last.Value;
        }

        public IEnumerable<Position> SnakePositions()
        {
            return snakePositions;
        }

        private void AddHead(Position pos)
        {
            snakePositions.AddFirst(pos);
            Grid[pos.Row, pos.Col] = GridValue.Snake;
        }

        private void RemoveTail()
        {
            Position tail = snakePositions.Last.Value;
            Grid[tail.Row, tail.Col] = GridValue.Empty;
            snakePositions.RemoveLast();
        }

        private Direction GetLastDirection()
        {
            if (dirChanges.Count == 0)
            {
                return Dir;
            }
            return dirChanges.Last.Value;
        }

        private bool CanChangeDirection(Direction newDir)
        {
            if (dirChanges.Count == 2)
            {
                return false;
            }
            Direction lastDir = GetLastDirection();
            return newDir != lastDir && newDir != lastDir.Opposite();
        }

        public void ChangeDirection(Direction dir)
        {
            if (CanChangeDirection(dir))
            {
                dirChanges.AddLast(dir);
            }
        }

        private bool OutsideGrid(Position pos)
        {
            return pos.Row < 0 || pos.Row >= Rows || pos.Col < 0 || pos.Col >= Cols;
        }

        private GridValue WillHit(Position newHeadPos)
        {
            if (OutsideGrid(newHeadPos))
            {
                return GridValue.Outside;
            }

            if (newHeadPos == TailPosition())
            {
                return GridValue.Empty;
            }

            return Grid[newHeadPos.Row, newHeadPos.Col];
        }

        public void Move()
        {
            if (dirChanges.Count > 0)
            {
                Dir = dirChanges.First.Value;
                dirChanges.RemoveFirst();
            }

            Position newHeadPos = HeadPosition().Translate(Dir);
            GridValue hit = WillHit(newHeadPos);

            if (hit == GridValue.Outside || hit == GridValue.Snake || hit == GridValue.Obstacle) // ✅ เพิ่มการตรวจสอบชนสิ่งกีดขวาง
            {
                GameOver = true;
                obstacleTimer.Dispose(); // ✅ หยุดเปลี่ยนสิ่งกีดขวางเมื่อเกมจบ
            }
            else if (hit == GridValue.Empty)
            {
                RemoveTail();
                AddHead(newHeadPos);
            }
            else if (hit == GridValue.Food)
            {
                AddHead(newHeadPos);
                Score++;
                AddFood();
            }
        }
    }
}
