namespace Snake
{
    public class Obstacle
    {
        public Position Position { get; private set; } // ตำแหน่งของอุปสรรค

        public Obstacle(Position position)
        {
            Position = position;
        }
    }
}
