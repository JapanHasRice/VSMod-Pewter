using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Pewter.Patches {
  [HarmonyPatch(typeof(ChildDepositGenerator))]
  [HarmonyPatch("GenDeposit")]
  public class Patch_WaypointMapLayer_OnCmdWayPoint {
    public static bool Prefix(ChildDepositGenerator __instance,
                              BlockPos ___targetPos,
                              int ___chunksize,
                              int ___worldheight,
                              Dictionary<int, ResolvedDepositBlock> ___placeBlockByInBlockId,
                              Dictionary<int, ResolvedDepositBlock> ___surfaceBlockByInBlockId,
                              DepositVariant ___variant,
                              DepositBlock ___PlaceBlock,
                              DepositBlock ___InBlock,
                              IBlockAccessor blockAccessor,
                              IServerChunk[] chunks,
                              int originChunkX,
                              int originChunkZ,
                              BlockPos pos,
                              ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace) {
      IMapChunk heremapchunk = chunks[0].MapChunk;

      int depositGradeIndex = __instance.PlaceBlock.AllowedVariants != null ? __instance.DepositRand.NextInt(__instance.PlaceBlock.AllowedVariants.Length) : 0;

      int radius = Math.Min(64, (int)__instance.Radius.nextFloat(1, __instance.DepositRand));
      if (radius <= 0) return false;
      radius++;

      bool shouldGenSurfaceDeposit = __instance.DepositRand.NextFloat() > 0.35f && __instance.SurfaceBlock != null;
      float tries = __instance.RandomTries.nextFloat(1, __instance.DepositRand);

      for (int i = 0; i < tries; i++) {
        ___targetPos.Set(
            pos.X + __instance.DepositRand.NextInt(2 * radius + 1) - radius,
            pos.Y + __instance.DepositRand.NextInt(2 * radius + 1) - radius,
            pos.Z + __instance.DepositRand.NextInt(2 * radius + 1) - radius
        );

        int lx = ___targetPos.X % ___chunksize;
        int lz = ___targetPos.Z % ___chunksize;

        if (___targetPos.Y <= 1 || ___targetPos.Y >= ___worldheight || lx < 0 || lz < 0 || lx >= ___chunksize || lz >= ___chunksize) continue;

        int index3d = ((___targetPos.Y % ___chunksize) * ___chunksize + lz) * ___chunksize + lx;
        int blockId = chunks[___targetPos.Y / ___chunksize].Blocks[index3d];

        ResolvedDepositBlock resolvedPlaceBlock;
        Block placeblock;

        if (___placeBlockByInBlockId.TryGetValue(blockId, out resolvedPlaceBlock)) {
          #region Modified code start
          // As written in vanilla code, attempting to place a child deposit ore in a rock type that is not available throws an index out of bounds error,
          // causing the entire deposit in that chunk to fail to place.
          // This change tells ore generation to ignore if it is unplaceable in that specific spot and continue trying elsewhere.
          if (depositGradeIndex < resolvedPlaceBlock.Blocks.Length) {
            placeblock = resolvedPlaceBlock.Blocks[depositGradeIndex];
            // __instance.Api.Logger.Warning("{0}", $"[RICE] oregen SUCCESS, blockId: {__instance.Api.World.BlockAccessor.GetBlock(blockId)?.Code}, PlaceBlock: {___PlaceBlock?.Code} at {___targetPos}");
          }
          else {
            // __instance.Api.Logger.Warning("{0}", $"[RICE] oregen error, blockId: {__instance.Api.World.BlockAccessor.GetBlock(blockId)?.Code}, PlaceBlock: {___PlaceBlock?.Code} at {___targetPos}");
            continue;
          }
          #endregion

          if (___variant.WithBlockCallback) {
            placeblock.TryPlaceBlockForWorldGen(blockAccessor, ___targetPos, BlockFacing.UP, __instance.DepositRand);
          }
          else {
            chunks[___targetPos.Y / ___chunksize].Blocks[index3d] = placeblock.BlockId;
          }

          if (shouldGenSurfaceDeposit) {
            int surfaceY = heremapchunk.RainHeightMap[lz * ___chunksize + lx];
            int depth = surfaceY - ___targetPos.Y;
            float chance = __instance.SurfaceBlockChance * Math.Max(0, 1 - depth / 8f);
            if (surfaceY < ___worldheight && __instance.DepositRand.NextFloat() < chance) {
              index3d = (((surfaceY + 1) % ___chunksize) * ___chunksize + lz) * ___chunksize + lx;

              Block belowBlock = __instance.Api.World.Blocks[chunks[surfaceY / ___chunksize].Blocks[((surfaceY % ___chunksize) * ___chunksize + lz) * ___chunksize + lx]];

              if (belowBlock.SideSolid[BlockFacing.UP.Index] && chunks[(surfaceY + 1) / ___chunksize].Blocks[index3d] == 0) {
                chunks[(surfaceY + 1) / ___chunksize].Blocks[index3d] = ___surfaceBlockByInBlockId[blockId].Blocks[0].BlockId;
              }
            }
          }
        }
      }
      return false;
    }
  }
}
