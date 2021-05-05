using System.Collections.Generic;

namespace Systems
{
    public class MessageLog
    {
        public string[] Lines => _lines.ToArray();
        private Queue<string> _lines;
        private const int MAX_LINES = 10;
        public MessageLog()
        {
            _lines = new Queue<string>();
        }

        public void AddMessage(string message)
        {
            _lines.Enqueue(message);
            if (_lines.Count >= MAX_LINES)
                _lines.Dequeue();
        }
    }
}