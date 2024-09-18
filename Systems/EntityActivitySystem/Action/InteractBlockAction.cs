﻿using Newtonsoft.Json;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ActivateBlockAction : IEntityAction
    {
        public string Type => "activateblock";
        public bool ExecutionHasFailed { get; set; }

        EntityActivitySystem vas;
        [JsonProperty]
        AssetLocation targetBlockCode;
        [JsonProperty]
        float searchRange;
        [JsonProperty]
        string activateArgs;

        public ActivateBlockAction() { }

        public ActivateBlockAction(EntityActivitySystem vas, AssetLocation targetBlockCode, float searchRange, string activateArgs)
        {
            this.vas = vas;
            this.targetBlockCode = targetBlockCode;
            this.searchRange = searchRange;
            this.activateArgs = activateArgs;
        }


        public bool IsFinished()
        {
            return true;
        }

        public void Start(EntityActivity act)
        {
            BlockPos targetPos = getTarget();

            ExecutionHasFailed = targetPos == null;

            if (targetPos != null)
            {
                Vec3f targetVec = new Vec3f();

                targetVec.Set(
                    (float)(targetPos.X + 0.5 - vas.Entity.ServerPos.X),
                    (float)(targetPos.Y + 0.5 - vas.Entity.ServerPos.Y),
                    (float)(targetPos.Z + 0.5 - vas.Entity.ServerPos.Z)
                );

                vas.Entity.ServerPos.Yaw = (float)Math.Atan2(targetVec.X, targetVec.Z);

                var block = vas.Entity.Api.World.BlockAccessor.GetBlock(targetPos);

                var blockSel = new BlockSelection()
                {
                    Block = block,
                    Position = targetPos,
                    HitPosition = new Vec3d(0.5, 0.5, 0.5),
                    Face = BlockFacing.NORTH
                };

                var args = activateArgs == null ? null : TreeAttribute.FromJson(activateArgs) as ITreeAttribute;
                block.Activate(vas.Entity.World, new Caller() { Entity = vas.Entity, Type = EnumCallerType.Entity, Pos = vas.Entity.Pos.XYZ }, blockSel, args);
            }
        }

        private BlockPos getTarget()
        {
            var range = GameMath.Clamp(searchRange, -10, 10);
            var api = vas.Entity.Api;
            var minPos = vas.Entity.ServerPos.XYZ.Add(-range, -1, -range).AsBlockPos;
            var maxPos = vas.Entity.ServerPos.XYZ.Add(range, 1, range).AsBlockPos;

            BlockPos targetPos = null;
            api.World.BlockAccessor.WalkBlocks(minPos, maxPos, (block, x, y, z) =>
            {
                if (targetBlockCode == null) return;

                if (block.WildCardMatch(targetBlockCode))
                {
                    targetPos = new BlockPos(x, y, z);
                }
            }, true);
            return targetPos;
        }

        public void OnTick(float dt)
        {

        }

        public void Cancel()
        {

        }
        public void Finish() { }
        public void LoadState(ITreeAttribute tree) { }
        public void StoreState(ITreeAttribute tree) { }

        public override string ToString()
        {
            return "Activate nearest block " + targetBlockCode + " within " + searchRange + " blocks";
        }

        public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
        {
            var b = ElementBounds.Fixed(0, 0, 300, 25);
            singleComposer
                .AddStaticText("Search Range (capped to 10 blocks)", CairoFont.WhiteDetailText(), b)
                .AddNumberInput(b = b.BelowCopy(0, -5), null, CairoFont.WhiteDetailText(), "searchRange")

                .AddStaticText("Block Code", CairoFont.WhiteDetailText(), b = b.BelowCopy(0, 10))
                .AddTextInput(b = b.BelowCopy(0, -5), null, CairoFont.WhiteDetailText(), "targetBlockCode")

                .AddStaticText("Activation Arguments", CairoFont.WhiteDetailText(), b = b.BelowCopy(0, 10))
                .AddTextInput(b = b.BelowCopy(0, -5), null, CairoFont.WhiteDetailText(), "activateArgs")
            ;

            singleComposer.GetNumberInput("searchRange").SetValue(searchRange);
            singleComposer.GetTextInput("targetBlockCode").SetValue(targetBlockCode?.ToShortString());
            singleComposer.GetTextInput("activateArgs").SetValue(activateArgs);
        }

        public IEntityAction Clone()
        {
            return new ActivateBlockAction(vas, targetBlockCode, searchRange, activateArgs);
        }

        public bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
        {
            searchRange = singleComposer.GetTextInput("searchRange").GetText().ToFloat();
            targetBlockCode = new AssetLocation(singleComposer.GetTextInput("targetBlockCode").GetText());
            activateArgs = singleComposer.GetTextInput("activateArgs").GetText();
            return true;
        }

        public void OnVisualize(ActivityVisualizer visualizer)
        {
            var target = getTarget();
            if (target != null)
            {
                visualizer.LineTo(target.ToVec3d().Add(0.5, 0.5, 0.5));
            }
        }
        public void OnLoaded(EntityActivitySystem vas)
        {
            this.vas = vas;
        }
    }
}
