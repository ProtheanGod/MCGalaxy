﻿/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
        
    Dual-licensed under the    Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using MCGalaxy.Generator.Foliage;

namespace MCGalaxy.Blocks.Physics {

    public static class OtherPhysics {
        
        public static void DoFalling(Level lvl, ref Check C) {
            ushort x, y, z;
            lvl.IntToPos(C.b, out x, out y, out z);
            int index = C.b;
            bool movedDown = false;
            byte block = lvl.blocks[C.b];
            
            do {
                index = lvl.IntOffset(index, 0, -1, 0); //Get block below each loop
                if (lvl.GetTile(index) == Block.Invalid) break;
                bool hitBlock = false;
                
                switch (lvl.blocks[index]) {
                    case Block.Air:
                    case Block.Water:
                    case Block.Lava:
                        movedDown = true;
                        break;
                        //Adv physics crushes plants with sand
                    case Block.Sapling:
                    case Block.Dandelion:
                    case Block.Rose:
                    case Block.Mushroom:
                    case Block.RedMushroom:
                        if (lvl.physics > 1) movedDown = true;
                        break;
                    default:
                        hitBlock = true;
                        break;
                }
                if (hitBlock || lvl.physics > 1) break;
            } while (true);

            if (movedDown) {
                lvl.AddUpdate(C.b, Block.Air);
                if (lvl.physics > 1)
                    lvl.AddUpdate(index, block);
                else
                    lvl.AddUpdate(lvl.IntOffset(index, 0, 1, 0), block);
                
                ActivateablePhysics.CheckNeighbours(lvl, x, y, z);
            }
            C.data.Data = PhysicsArgs.RemoveFromChecks;
        }
        
        public static void DoFloatwood(Level lvl, ref Check C) {
            int index = lvl.IntOffset(C.b, 0, -1, 0);
            if (lvl.GetTile(index) == Block.Air) {
                lvl.AddUpdate(C.b, Block.Air);
                lvl.AddUpdate(index, Block.FloatWood);
            } else {
                index = lvl.IntOffset(C.b, 0, 1, 0);
                byte above = lvl.GetTile(index);
                if (above == Block.StillWater || Block.Convert(above) == Block.Water) {
                    lvl.AddUpdate(C.b, lvl.blocks[index]);
                    lvl.AddUpdate(index, Block.FloatWood);
                }
            }
            C.data.Data = PhysicsArgs.RemoveFromChecks;
        }
        
        public static void DoShrub(Level lvl, ref Check C) {
            Random rand = lvl.physRandom;            
            ushort x, y, z;
            lvl.IntToPos(C.b, out x, out y, out z);
            if (lvl.physics > 1) { //Adv physics kills flowers and mushroos in water/lava
                ActivateablePhysics.CheckNeighbours(lvl, x, y, z);
            }

            if (!lvl.Config.GrowTrees) { C.data.Data = PhysicsArgs.RemoveFromChecks; return; }
            if (C.data.Data < 20) {
                if (rand.Next(20) == 0) C.data.Data++;
                return;
            }
            
            lvl.SetTile(x, y, z, Block.Air);        
            Tree tree = Tree.Find(lvl.Config.TreeType);
            if (tree == null) tree = new NormalTree();
            
            tree.SetData(rand, tree.DefaultSize(rand));
            tree.Generate(x, y, z, (xT, yT, zT, bT) =>
                        {
                            if (!lvl.IsAirAt(xT, yT, zT)) return;
                            lvl.Blockchange(xT, yT, zT, (ExtBlock)bT);
                        });
            
            C.data.Data = PhysicsArgs.RemoveFromChecks;
        }
        
        public static void DoDirtGrow(Level lvl, ref Check C) {
            if (!lvl.Config.GrassGrow) { C.data.Data = PhysicsArgs.RemoveFromChecks; return; }
            ushort x, y, z;
            lvl.IntToPos(C.b, out x, out y, out z);
            
            if (C.data.Data > 20) {                
                ExtBlock above = lvl.GetBlock(x, (ushort)(y + 1), z);
                if (lvl.LightPasses(above)) {
                    ExtBlock block = lvl.GetBlock(x, y, z);
                    ExtBlock grass = ExtBlock.FromIndex(lvl.Props[block.Index].GrassIndex);
                    lvl.AddUpdate(C.b, grass);
                }
                C.data.Data = PhysicsArgs.RemoveFromChecks;
            } else {
                C.data.Data++;
            }
        }
        
        public static void DoGrassDie(Level lvl, ref Check C) {
            if (!lvl.Config.GrassGrow) { C.data.Data = PhysicsArgs.RemoveFromChecks; return; }
            ushort x, y, z;
            lvl.IntToPos(C.b, out x, out y, out z);
            
            if (C.data.Data > 20) {
                ExtBlock above = lvl.GetBlock(x, (ushort)(y + 1), z);
                if (!lvl.LightPasses(above)) {
                    ExtBlock block = lvl.GetBlock(x, y, z);
                    ExtBlock dirt = ExtBlock.FromIndex(lvl.Props[block.Index].DirtIndex);
                    lvl.AddUpdate(C.b, dirt);
                }
                C.data.Data = PhysicsArgs.RemoveFromChecks;
            } else {
                C.data.Data++;
            }
        }
        
        
        public static void DoSponge(Level lvl, ref Check C, bool lava) {
            byte target = lava ? Block.Lava : Block.Water;
            byte alt    = lava ? Block.StillLava : Block.StillWater;
            
            for (int y = -2; y <= +2; ++y)
                for (int z = -2; z <= +2; ++z)
                    for (int x = -2; x <= +2; ++x)
            {
                int index = lvl.IntOffset(C.b, x, y, z);
                byte block = lvl.GetTile(index);
                if (block == Block.Invalid) continue;
                
                if (Block.Convert(block) == target || Block.Convert(block) == alt)
                    lvl.AddUpdate(index, Block.Air);
            }
            C.data.Data = PhysicsArgs.RemoveFromChecks;
        }
        
        public static void DoSpongeRemoved(Level lvl, int b, bool lava) {
            byte target = lava ? Block.Lava : Block.Water;
            byte alt    = lava ? Block.StillLava : Block.StillWater;
            
            for (int y = -3; y <= +3; ++y)
                for (int z = -3; z <= +3; ++z)
                    for (int x = -3; x <= +3; ++x)
            {
                if (Math.Abs(x) == 3 || Math.Abs(y) == 3 || Math.Abs(z) == 3) //Calc only edge
                {
                    int index = lvl.IntOffset(b, x, y, z);
                    byte block = lvl.GetTile(index);
                    if (block == Block.Invalid) continue;
                    
                    if (Block.Convert(block) == target || Block.Convert(block) == alt)
                        lvl.AddCheck(index);
                }
            }
        }
        
        public static void DoOther(Level lvl, ref Check C) {
            if (lvl.physics <= 1) { C.data.Data = PhysicsArgs.RemoveFromChecks; return; }
            ushort x, y, z;
            lvl.IntToPos(C.b, out x, out y, out z);
            
            //Adv physics kills flowers and mushroos in water/lava
            ActivateablePhysics.CheckNeighbours(lvl, x, y, z);
            C.data.Data = PhysicsArgs.RemoveFromChecks;
        }
    }
}
