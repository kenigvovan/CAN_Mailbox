using canmailbox.src.be;
using canmailbox.src.inventory.slots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace canmailbox.src.inventory
{
    public class InventoryCANMailBox : InventoryBase, ISlotProvider
    {
        private ItemSlot[] slots;
        public int[] stocks;
        public ItemSlot[] Slots => this.slots;
        public BEMailBox be;
        public int slotsCount;
        public InventoryCANMailBox(string inventoryID, ICoreAPI api, int slotsAmount = 8)
          : base(inventoryID, api)
        {
           // base.GenEmptySlots(this.Count);
            this.slots = this.GenEmptySlotsInner(slotsAmount);
            stocks = new int[slotsAmount / 2];
            this.SlotModified += SlotModifiedFunc;
        }
        public void SlotModifiedFunc(int slotId)
        {
            /*if (this.Api.Side == EnumAppSide.Client)
            {
                if (!this.Empty)
                {
                    if (!this.be.flagUp)
                    {
                        this.be.flagUp = true;
                        this.be.ownMesh = this.be.GenMesh((this.be.Api as ICoreClientAPI).Tesselator, true);
                        //this.be.MarkDirty();
                    }               
                }
                else
                {
                    if (this.be.flagUp)
                    {
                        this.be.flagUp = false;
                        this.be.ownMesh = this.be.GenMesh((this.be.Api as ICoreClientAPI).Tesselator, true);
                        //this.be.MarkDirty();
                    }
                }
            }*/
        }
        public ItemSlot[] GenEmptySlotsInner(int quantity)
        {
            ItemSlot[] array = new ItemSlot[quantity];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = NewSlotInner(i);
            }

            return array;
        }
        protected ItemSlot NewSlotInner(int i)
        {
             return (ItemSlot)new CANInnerSlotsMailBox((InventoryBase)this);       
        }
        public override int Count => slots.Length;

        public override ItemSlot this[int slotId]
        {
            get => slotId < 0 || slotId >= this.Count ? (ItemSlot)null : this.slots[slotId];
            set
            {
                if (slotId < 0 || slotId >= this.Count)
                    throw new ArgumentOutOfRangeException(nameof(slotId));
                this.slots[slotId] = value != null ? value : throw new ArgumentNullException(nameof(value));
            }
        }
        public virtual void LateInitialize(string inventoryID, ICoreAPI api, BEMailBox be)
        {
            base.LateInitialize(inventoryID, api);
            this.be = be;
        }
        public override void FromTreeAttributes(ITreeAttribute tree) => this.slots = this.SlotsFromTreeAttributes(tree, this.slots);

        public override void ToTreeAttributes(ITreeAttribute tree) => this.SlotsToTreeAttributes(this.slots, tree);

        protected override ItemSlot NewSlot(int i) => (ItemSlot)new ItemSlotSurvival((InventoryBase)this);

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge) => targetSlot == this.slots[0] && sourceSlot.Itemstack.Collectible.GrindingProps != null ? 4f : base.GetSuitability(sourceSlot, targetSlot, isMerge);

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return null;
        }
        public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            return null;
        }
        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
        {
            if (sourceSlot.Itemstack == null)
            {
                return false;
            }
            return base.CanContain(sinkSlot, sourceSlot);
        }
    }
}
