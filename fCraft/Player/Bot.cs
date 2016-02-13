/*Copyright (c) <2014> <LeChosenOne, DingusBungus>
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace fCraft {
    public class Bot {

        /// <summary>
        /// Name of the bot. 
        /// </summary>
        public String Name;

        /// <summary>
        /// Name of the bot. 
        /// </summary>
        public String SkinName;
        public String oldSkinName;

        /// <summary>
        /// Current world the bot is on.
        /// </summary>
        public World World;

        /// <summary>
        /// Position of bot.
        /// </summary>
        public Position Position;

        /// <summary>
        /// Entity ID of the bot (-1 = default)
        /// </summary>
        public sbyte ID = -1;

        /// <summary>
        /// Current model of the bot
        /// </summary>
        public String oldModel = "humanoid";
        public String Model = "humanoid";


        #region Public Methods

        /// <summary>
        /// Sets a bot, as well as the bot values. Must be called before any other bot classes.
        /// </summary>
        public void setBot(String botName, String skinName, String modelName, World botWorld, Position pos, sbyte entityID) {
            Name = botName;
            SkinName =  (skinName ?? SkinName);
            Model =  (modelName ?? Model);
            World = botWorld;
            Position = pos;
            ID = entityID;

            World.Bots.Add(this);
            Server.SaveEntity(this);
        }

        /// <summary>
        /// Creates only the bot entity, not the bot data. Bot data is created from setBot.
        /// </summary>
        public void createBot() {
            foreach (Player sendTo in World.Players) {
                if (sendTo.Supports(CpeExt.ExtPlayerList2)) {
					sendTo.Send(Packet.MakeExtAddEntity2(ID, Name, (SkinName == "" ? Name : SkinName),
                        new Position(Position.X, Position.Y, Position.Z, Position.R, Position.L), sendTo));
                } else {
                    sendTo.Send(Packet.MakeAddEntity(ID, Name,
                        new Position(Position.X, Position.Y, Position.Z, Position.R, Position.L)));
                }
                if (sendTo.Supports(CpeExt.ChangeModel)) {
                    sendTo.Send(Packet.MakeChangeModel((byte)ID, Model));
                }
            }
            Server.SaveEntity(this);
        }

        /// <summary>
        /// Teleports the bot to a specific location
        /// </summary>
        public void teleportBot(Position p) {
            World.Players.Send(Packet.MakeTeleport(ID, p));
            Position = p;
            Server.SaveEntity(this);
        }

        /// <summary>
        /// Completely removes the entity and data of the bot.
        /// </summary>
        public void removeBot() {
            World.Players.Send(Packet.MakeRemoveEntity(ID));
            World.Bots.Remove(this);
            if (File.Exists("./Entities/" + Name.ToLower() + ".txt")) {
                File.Delete("./Entities/" + Name.ToLower() + ".txt");
            }
        }

        /// <summary>
        /// Changes the model of the bot
        /// </summary>
        public void changeBotModel(String botModel, String skinName) {
            Block blockModel;
            if (!CpeCommands.validEntities.Contains(botModel)) {
                if (Map.GetBlockByName(botModel, false, out blockModel)) {
                    botModel = blockModel.GetHashCode().ToString();
                } else {
                    return; //something went wrong, model does not exist
                }
            }

            World.Players.Where(p => p.Supports(CpeExt.ChangeModel)).Send(Packet.MakeChangeModel((byte) ID, botModel));
            Model = botModel;
            SkinName = skinName;
            Server.SaveEntity(this);
        }

        /// <summary>
        /// Changes the skin of the bot
        /// </summary>
        public void changeBotSkin(String skinName) {
            SkinName = skinName;
            Server.SaveEntity(this);
        }

        #endregion

    }
}