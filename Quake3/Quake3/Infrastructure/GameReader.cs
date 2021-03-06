﻿using Quake3.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Quake3.Infrastructure
{
    public class GameReader
    {
        /// <summary>
        /// Função para leitura do aquivo quake3
        /// </summary>
        /// <returns></returns>
        public List<Game> LerArquivo()
        {
            var caminho = ConfigurationManager.AppSettings["ArquivoLog"];//Buscando o valor do caminho do arquivo de log do quake 3 no arquivo de configuração(Webconfig)
            var pathToFiles = HttpContext.Current.Server.MapPath(caminho);
            var actionsGames = new Regex("(InitGame|ClientConnect|ClientUserinfoChanged|Kill)");
            var games = new List<Game>();
            var currentGame = new Game();

            using (var readerLog = new StreamReader(pathToFiles, Encoding.UTF8))
            {
                string row = "";
                int game = 0;

                while ((row = readerLog.ReadLine()) != null)
                {
                    Match action = actionsGames.Match(row);

                    switch (action.Value)
                    {
                        //Iniciando o Game
                        case "InitGame":
                            game += 1;
                            currentGame = MapeandoGame(games, game);
                            break;
                        //Player Contectado
                        case "ClientConnect":
                            MapeandoPlayes(currentGame, row);
                            break;
                        //Alterando nome
                        case "ClientUserinfoChanged":
                            MapeandoAlteracaoNomeDoPlayer(currentGame, row);
                            break;
                        //morte
                        case "Kill":
                            MapeandoKillPlayer(currentGame, row);
                            break;
                        default:
                            break;
                    }
                }
            }

            return games;
        }
        private Game MapeandoGame(List<Game> games, int id)
        {
            Game newGame = new Game();
            newGame.Id = id;
            games.Add(newGame);

            return newGame;
        }
        private void MapeandoPlayes(Game currentGame, string row)
        {
            const string findText = " ClientConnect: ";

            string firstPart = row.Substring(row.IndexOf(findText) + findText.Length);
            int id = Int32.Parse(firstPart);

            currentGame.Add(new Player(id));
        }
        private void MapeandoAlteracaoNomeDoPlayer(Game currentGame, string row)
        {
            const string findText = " ClientUserinfoChanged: ";

            string firstPart = row.Substring(row.IndexOf(findText) + findText.Length);
            int id = Int32.Parse(firstPart.Substring(0, firstPart.IndexOf(@"n\")));

            firstPart = row.Substring(row.IndexOf(@"n\") + 2);
            string name = firstPart.Substring(0, firstPart.IndexOf(@"\t\"));

            currentGame.AlterarNome(new Player(id), name);
        }
        private void MapeandoKillPlayer(Game currentGame, string row)
        {
            const string findText = " Kill: ";

            string firstPart = row.Substring(row.IndexOf(findText) + findText.Length);
            firstPart = firstPart.Substring(0, firstPart.IndexOf(": "));
            string[] infor = firstPart.Split(' ');

            var idFirstPlayer = Int32.Parse(infor[0]);
            var idSecondPlayer = Int32.Parse(infor[1]);
            var meansOfDeath = (MeansOfDeath)Int32.Parse(infor[2]);

            Kill(currentGame, idFirstPlayer, idSecondPlayer, meansOfDeath);
        }
        private void Kill(Game currentGame, int idFirstPlayer, int idSecondPlayer, MeansOfDeath meansOfDeath)
        {
            if (idFirstPlayer == 1022)
            {
                var killer = new Player(idSecondPlayer);
                currentGame.KillByNaturalDeath(killer, meansOfDeath);
            }
            else
            {
                var killer = new Player(idFirstPlayer);
                var victim = new Player(idSecondPlayer);
                currentGame.KillForMurder(killer, victim, meansOfDeath);
            }
        }
    }
}