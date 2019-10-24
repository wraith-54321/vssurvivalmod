﻿using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent
{
    public class BlockEntityBerryBush : BlockEntity
    {
        static Random rand = new Random();

        // Total game hours from where on it can enter the next growth stage
        double totalDaysForNextStage;

        long growListenerId;


        public BlockEntityBerryBush() : base()
        {

        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api is ICoreServerAPI)
            {
                if (totalDaysForNextStage == 0)
                {
                    totalDaysForNextStage = api.World.Calendar.TotalDays + GetDaysForNextStage();
                }

                growListenerId = RegisterGameTickListener(CheckGrow, 8000);
            }
        }


        private void CheckGrow(float dt)
        {
            bool didGrow = Api.World.Calendar.TotalDays > totalDaysForNextStage;

            while (Api.World.Calendar.TotalDays > totalDaysForNextStage)
            {
                DoGrow();
                totalDaysForNextStage += GetDaysForNextStage();
            }
        }

        public double GetDaysForNextStage()
        {
            if (IsRipe()) return 4 * (5 + rand.NextDouble()) * 0.8;

            return (5 + rand.NextDouble()) * 0.8;
        }

        public bool IsRipe()
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            return block.LastCodePart() == "ripe";
        }

        void DoGrow()
        { 
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            string nowCodePart = block.LastCodePart();
            string nextCodePart = (nowCodePart == "empty") ? "flowering" : ((nowCodePart == "flowering") ? "ripe" : "empty");


            AssetLocation loc = block.CodeWithParts(nextCodePart);
            if (!loc.Valid)
            {
                Api.World.BlockAccessor.RemoveBlockEntity(Pos);
                return;
            }

            Block nextBlock = Api.World.GetBlock(loc);
            if (nextBlock?.Code == null) return;

            Api.World.BlockAccessor.ExchangeBlock(nextBlock.BlockId, Pos);
            MarkDirty(true);
        }



        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);
            totalDaysForNextStage = tree.GetDouble("totalDaysForNextStage");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("totalDaysForNextStage", totalDaysForNextStage);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            double daysleft = totalDaysForNextStage - Api.World.Calendar.TotalDays;

            /*if (forPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return "" + daysleft;
            }*/

            if (block.LastCodePart() == "ripe")
            {
                return;
            }

            string code = (block.LastCodePart() == "empty") ? "flowering" : "ripen";

            if (daysleft < 1)
            {
                sb.AppendLine(Lang.Get("berrybush-"+ code + "-1day"));
            }
            else
            {
                sb.AppendLine(Lang.Get("berrybush-" + code + "-xdays", (int)daysleft));
            }

        }
    }
}
