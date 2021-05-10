using System;
using Actors;
using Common.Components;
using Enums;

namespace Common
{
    public class PlayerStatsMenu : Menu
    {
        private Player _player;
        private Stats _stats => _player.Stats;
        public PlayerStatsMenu(Player player) : base($"Player Stats", string.Empty)
        {
            _player = player;
            SummaryPrintFlags = RB.ALIGN_V_TOP | RB.ALIGN_H_CENTER;
            Header = _player.Name.EndsWith("s") ? $"{_player.Name}' Stats" : $"{_player.Name}'s Stats";
        }

        public override void Render()
        {
            Summary = string.Empty;
            Summary += $"@FF4400Level:@FFFFFF {_stats[StatTypes.LEVEL]}\n";
            Summary += $"@FF4400Experience:@FFFFFF {_stats[StatTypes.EXPERIENCE]}\n";
            Summary += $"@FF4400Attack:@FFFFFF {_stats[StatTypes.ATTACK]} ({_stats[StatTypes.ATTACK_CHANCE]}%)\n";
            Summary += $"@FF4400Defense:@FFFFFF {_stats[StatTypes.DEFENSE]} ({_stats[StatTypes.DEFENSE_CHANCE]}%)\n";
            Summary += $"@FF4400Awareness:@FFFFFF {_stats[StatTypes.AWARENESS]}\n";
            Summary += $"@D4AF37Gold:@FFFFFF {_stats[StatTypes.GOLD]}";
            base.Render();
        }
    }
}