// Copyright (C) 2016-2018 madalien.com
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// bnetlauncher is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with bnetlauncher. If not, see <http://www.gnu.org/licenses/>.
//
//
// Contact:
// daf <daf@madalien.com>

using System;

namespace bnetlauncher
{
    class Game
    {

        public Game()
        {

        }

        public Game(string game_id, string client_id, string game_name)
        {
            this.Id = game_id;
            this.Name = game_name;
            this.Client = client_id;
        }

        /// <summary>
        /// The Id is the value used by the b.net client to launch the game
        /// using the uri handler battlenet://
        /// This key is case sensitive.
        /// </summary>
        public string Id
        {
            get
            {
                return game_id;
            }
            set
            {
                game_id = value;
            }
        }

        public string Client
        {
            get
            {
                return client_id;
            }
            set
            {
                client_id = value;
            }
        }

        /// <summary>
        /// The name of the game to which the key is associated to.
        /// </summary>
        public string Name
        {
            get
            {
                return game_name;
            }
            set
            {
                game_name = value;
            }
        }

        public string Cmd
        {
            get
            {
                return launch_cmd;
            }
            set
            {
                launch_cmd = value;
            }
        }

        public string Exe
        {
            get
            {
                return game_exe;
            }
            set
            {
                game_exe = value;
            }
        }

        public string Options
        {
            get
            {
                return options;
            }
            set
            {
                if (value is string)
                {
                    options = value.ToLower().Trim();
                }
                else
                {
                    options = "";
                }
            }
        }

        private string game_id;
        private string client_id;
        private string game_name;
        private string launch_cmd;
        private string game_exe;
        private string options;
    }
}
