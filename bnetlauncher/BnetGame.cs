// Copyright (C) 2016 madalien.com
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
    class BnetGame
    {
        public BnetGame(string key, string name, string abbr = "")
        {
            this.Key = key;
            this.Name = name;
            this.Alias = abbr;
        }

        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                key = value;
            }
        }

        public string Alias
        {
            get
            {
                return abbr;
            }
            set
            {
                abbr = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        private string key;
        private string name;
        private string abbr;
    }
}
