using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CrossChessServer.MessageClasses
{
    internal static class RoundManager
    {
        /// <summary>
        /// 工程目录
        /// </summary>
        public static string projectDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");

        /// <summary>
        /// 把战局信息保存到txt中
        /// </summary>
        /// <param name="round">战局</param>
        public static void SaveRoundInfo(Round round)
        {
            string filePath = Path.Combine(projectDirectory, "Save", "Rounds.txt");
            string roundInfo = $"RoundID: {round.roundID}, Player1: {round.player1}, Player2: {round.player2}, Result: {round.result}, Steps: ";
            for (int i = 0; i < 9; i++)
            {
                roundInfo += round.steps[i] + (i < 8 ? "," : "");
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.AppendAllText(filePath, roundInfo + Environment.NewLine);
                Console.WriteLine("战局信息已保存: " + roundInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("保存战局信息出错: " + e.Message);
            }
        }

        /// <summary>
        /// 从txt中加载战局信息
        /// </summary>
        /// <returns>战局信息数组</returns>
        public static Round[] GetRoundList()
        {
            string filePath = Path.Combine(projectDirectory, "Save", "Rounds.txt");
            if (!File.Exists(filePath))
            {
                Console.WriteLine("文件不存在: " + filePath);
                return Array.Empty<Round>();
            }

            string[] lines = File.ReadAllLines(filePath);
            Round[] rounds = new Round[lines.Length];
            Regex regex = new Regex(@"RoundID: (\d+), Player1: (.*?), Player2: (.*?), Result: (\d+), Steps: (.*)");

            for (int i = 0; i < lines.Length; i++)
            {
                Match match = regex.Match(lines[i]);
                if (match.Success)
                {
                    Round round = new Round
                    {
                        roundID = int.Parse(match.Groups[1].Value),
                        player1 = match.Groups[2].Value,
                        player2 = match.Groups[3].Value,
                        result = int.Parse(match.Groups[4].Value)
                    };

                    string[] steps = match.Groups[5].Value.Split(',');
                    for (int j = 0; j < steps.Length; j++)
                    {
                        round.steps[j] = int.Parse(steps[j]);
                    }

                    rounds[i] = round;
                }
            }

            return rounds;
        }
    }
}
