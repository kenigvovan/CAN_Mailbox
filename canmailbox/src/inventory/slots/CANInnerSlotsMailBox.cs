using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace canmailbox.src.inventory.slots
{
    public class CANInnerSlotsMailBox: ItemSlotSurvival
    {
        public CANInnerSlotsMailBox(InventoryBase inventory) : base(inventory)
        {
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            return false;
        }
        public override bool CanTake()
        {
            return false;
        }
        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return false;
        }
        public override ItemStack TakeOut(int quantity)
        {
            return base.TakeOut(quantity);
            //return null;
        }
        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            var player = op.ActingPlayer;
            if(player == null)
            {
                return;
            }

            bool ownerHandling = player.PlayerUID.Equals((this.inventory as InventoryCANMailBox).be.ownerUID);

            bool inputSlots = this.inventory.GetSlotId(this) < 2;
            if(ownerHandling || inputSlots)
            {
                //take from
                if (sourceSlot.Empty)
                {
                    sourceSlot.Itemstack = this.itemstack;
                    this.itemstack = null;
                    sourceSlot.MarkDirty();
                    this.MarkDirty();
                    (this.inventory as InventoryCANMailBox).be.MarkDirty();
                    return;
                }
                else
                {
                    if(this.Empty)
                    {
                        itemstack = sourceSlot.Itemstack;
                        sourceSlot.Itemstack = null;
                        //just to be calm
                        itemstack.StackSize = Math.Min(itemstack.StackSize, itemstack.Collectible.MaxStackSize);
                        sourceSlot.MarkDirty();
                        this.MarkDirty();
                        (this.inventory as InventoryCANMailBox).be.MarkDirty();
                        return;
                    }
                }
            }
            
            if(!this.Empty)
            {
                return;
            }
        }
        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            var player = op.ActingPlayer;
            if (player == null)
            {
                return;
            }

            bool ownerHandling = player.PlayerUID.Equals((this.inventory as InventoryCANMailBox).be.ownerUID);

            bool inputSlots = this.inventory.GetSlotId(this) < 2;
            if (ownerHandling || inputSlots)
            {
                InventoryBasePlayer hotbar = (InventoryBasePlayer)player.InventoryManager.GetOwnInventory("hotbar");
                if (hotbar != null)
                {
                    for(int i = 0; i < 10; i++)
                    {
                        if (hotbar[i].Empty)
                        {
                            hotbar[i].Itemstack = this.itemstack;
                            this.itemstack = null;
                            hotbar[i].MarkDirty();
                            this.MarkDirty();
                            return;
                        }
                    }
                }
                InventoryPlayerBackpacks playerBackpacks = (InventoryPlayerBackpacks)player.InventoryManager.GetOwnInventory("backpack");
                if(playerBackpacks.Count < 5)
                {
                    return;
                }
                playerBackpacks.TakeLocked = true;
                playerBackpacks.PutLocked = true;
                for (int i = 4; i < playerBackpacks.Count; i++)
                {
                    if (playerBackpacks[i].Empty)
                    {
                        playerBackpacks[i].Itemstack = this.itemstack;
                        this.itemstack = null;
                        playerBackpacks[i].MarkDirty();
                        this.MarkDirty();
                        playerBackpacks.TakeLocked = false;
                        playerBackpacks.PutLocked = false;
                        return;
                    }
                }
                playerBackpacks.TakeLocked = false;
                playerBackpacks.PutLocked = false;
            }

            if (!this.Empty)
            {
                return;
            }
        }

    }
}
