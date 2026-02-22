using System;
using System.Linq;
using canmailbox.src.inventory;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace canmailbox.src.gui
{
    public class GuiDialogBlockEntityMailBoxInventory : GuiDialogBlockEntity
    {
        private int cols;

        private EnumPosFlag screenPos;
        public override double DrawOrder => 0.2;
        //bool likeNonOwner = false;
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            if (capi.Gui.GetDialogPosition(SingleComposer.DialogName) == null)
            {
                OccupyPos("smallblockgui", screenPos);
            }
        }
        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            FreePos("smallblockgui", screenPos);
        }
        protected void DoSendPacketMoveDown()
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.InternalY, BlockEntityPosition.Z, 2025);
        }
        public override void Recompose()
        {
            if (IsDuplicate)
            {
                return;
            }

            string ownerName = (Inventory as InventoryCANMailBox)?.be?.ownerUID;

            for (int i = 0; i < 2; i++)
            {
                this.Inventory[i].HexBackgroundColor = "#eb600b";
            }
            //Set slots colors
            /*if (ownerName.Equals(capi.World.Player.PlayerName))
            {
                this.Inventory[0].HexBackgroundColor = "#79E02E";
                this.Inventory[2].HexBackgroundColor = "#79E02E";
                this.Inventory[4].HexBackgroundColor = "#79E02E";
                this.Inventory[6].HexBackgroundColor = "#79E02E";
            }
            else
            {
                this.Inventory[0].HexBackgroundColor = "#855522";
                this.Inventory[2].HexBackgroundColor = "#855522";
                this.Inventory[4].HexBackgroundColor = "#855522";
                this.Inventory[6].HexBackgroundColor = "#855522";
            }*/
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds ownerNameBounds = ElementBounds.Fixed(0.0, 3.0, 150, 25).WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds closeButton = ElementBounds.Fixed(0, 30, 0, 0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);

            //ElementBounds leftText = ElementBounds.FixedSize(70, 25).FixedUnder(ownerNameBounds, 20);
            /*ElementBounds rightText = ElementBounds.FixedSize(70, 25).RightOf(leftText, 25);
            rightText.fixedY = leftText.fixedY;*/

            ElementBounds leftSlots = ElementBounds.FixedSize(120, 60).FixedUnder(ownerNameBounds, 15);
            ElementBounds buttonSend = leftSlots.RightCopy().WithFixedSize(60, 40);
            ElementBounds ownerSlots = ElementBounds.FixedSize(200, 230).FixedUnder(leftSlots, 15);
            //ElementBounds rightSlots = ElementBounds.FixedSize(60, 230).FixedRightOf(leftSlots);
            //rightSlots.fixedY = leftSlots.fixedY;

            ElementBounds InfiniteStocksTextBounds = ElementBounds.FixedSize(150, 25).FixedUnder(leftSlots, 15);
            ElementBounds InfiniteStocksButtonBounds = ElementBounds.FixedSize(50, 25).RightOf(InfiniteStocksTextBounds, 15);
            InfiniteStocksButtonBounds.fixedY = InfiniteStocksTextBounds.fixedY;

            ElementBounds StorePaymentTextBounds = ElementBounds.FixedSize(150, 25).FixedUnder(InfiniteStocksButtonBounds, 15);
            ElementBounds StorePaymentButtonBounds = ElementBounds.FixedSize(50, 25).RightOf(StorePaymentTextBounds, 15);
            StorePaymentButtonBounds.fixedY = StorePaymentTextBounds.fixedY;

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[]
            {
                closeButton
            });
            SingleComposer = this.capi.Gui
               .CreateCompo("canmailboxCompo", dialogBounds)
               .AddShadedDialogBG(bgBounds, true, 5.0, 0.75f)
               .AddDialogTitleBar(Lang.Get("canmailbox:gui-mailbox-name"), delegate
               {
                   TryClose();
               }, null, null)
               .BeginChildElements(bgBounds);

            

            //new int[] { 0, 1, 2, 3 }

            bool openedByOwner = (this.Inventory as InventoryCANMailBox).be.ownerUID.Equals(this.capi.World.Player.PlayerUID);
            if (openedByOwner /*&& !likeNonOwner*/)
            {
                SingleComposer.AddItemSlotGrid(Inventory, new Action<object>(DoSendPacket), 4, new int[] { 0, 1 }, leftSlots, "publicSlots");
                /*SingleComposer.AddIconButton("canmailbox:save-arrow", (bool t) =>
                {
                    DoSendPacketMoveDown();
                }, buttonSend, "sendButton");*/
                if ((Inventory as InventoryCANMailBox)?.be?.ownerName != null)
                {
                    int len = 0;
                    using (var paint = new SKPaint())
                    {
                        paint.Typeface = SKTypeface.FromFamilyName("Times New Roman");

                        paint.TextSize = 24f;
                        var skBounds = SKRect.Empty;
                        len = (int)paint.MeasureText((Inventory as InventoryCANMailBox).be.ownerName.AsSpan(), ref skBounds);

                    }
                    SingleComposer.AddHoverText((Inventory as InventoryCANMailBox).be?.ownerName, CairoFont.ButtonPressedText(), len + 30, buttonSend);
                }
                
                SingleComposer.AddItemSlotGrid(Inventory, new Action<object>(DoSendPacket), 4, Enumerable.Range(2, Inventory.Count - 2).ToArray(), ownerSlots, "privateSlots");
               /* SingleComposer.AddSwitch((t) =>
                {
                    this.likeNonOwner = t;
                    this.Recompose();
                }, ownerSlots.BelowCopy());*/
            }
            else
            {
                SingleComposer.AddItemSlotGrid(Inventory, new Action<object>(DoSendPacket), 4, new int[] { 0, 1 }, leftSlots, "publicSlots");
                SingleComposer.AddIconButton("canmailbox:save-arrow", (bool t) =>
                {
                    DoSendPacketMoveDown();
                }, buttonSend, "sendButton");
                if ((Inventory as InventoryCANMailBox)?.be?.ownerName != null)
                {
                    int len = 0;
                    using (var paint = new SKPaint())
                    {
                        paint.Typeface = SKTypeface.FromFamilyName("Times New Roman");

                        paint.TextSize = 24f;
                        var skBounds = SKRect.Empty;
                        len = (int)paint.MeasureText((Inventory as InventoryCANMailBox).be.ownerName.AsSpan(), ref skBounds);

                    }
                    SingleComposer.AddHoverText((Inventory as InventoryCANMailBox).be?.ownerName, CairoFont.ButtonPressedText(), len + 30, buttonSend);
                }
                //SingleComposer.AddItemSlotGrid(Inventory, new Action<object>(DoSendPacket), 4, Enumerable.Range(2, Inventory.Count - 2).ToArray(), ownerSlots, "privateSlots");
            }

            // SingleComposer.AddItemSlotGrid(ownerInventory, new Action<object>(DoSendPacketOwner), 4, Enumerable.Range(0, ownerInventory.Count).ToArray(), ownerSlots, "ownerSlots");
            //SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(this.DoSendPacket), 1, new int[] { 1, 3, 5, 7 }, rightSlots, "goodsSlots");

            /*if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                SingleComposer.AddStaticText(Lang.Get("canmarket:infinite-stocks-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), InfiniteStocksTextBounds);
                SingleComposer.AddSwitch(FlipInfiniteStocksState, InfiniteStocksButtonBounds, "infinitestockstoggle");
                SingleComposer.GetSwitch("infinitestockstoggle")?.SetValue(infiniteStocks);

                SingleComposer.AddStaticText(Lang.Get("canmarket:store-payment-info-gui"), CairoFont.WhiteDetailText().WithFontSize(20), StorePaymentTextBounds);
                SingleComposer.AddSwitch(FlipStorePaymentState, StorePaymentButtonBounds, "storepaymenttoggle");
                SingleComposer.GetSwitch("storepaymenttoggle")?.SetValue(storePayment);
            }*/

            var slotSize = GuiElementPassiveItemSlot.unscaledSlotSize;
            var slotPaddingSize = GuiElementItemSlotGridBase.unscaledSlotPadding;

            /*for (int i = 0; i < 4; i++)
            {
                ElementBounds tmpEB = ElementBounds.
                    FixedSize(35, 35).
                    RightOf(rightSlots);
                tmpEB.fixedY = rightSlots.fixedY + i * (slotSize + slotPaddingSize) + 28;

                if (infiniteStocks)
                {
                    SingleComposer.AddDynamicText("∞",
                    CairoFont.WhiteSmallText().WithFontSize(13), tmpEB, "stock" + i);
                }
                else
                {
                    SingleComposer.AddDynamicText((this.Inventory as InventoryCANMarketOnChest).stocks[i] < 999
                        ? (this.Inventory as InventoryCANMarketOnChest).stocks[i].ToString()
                        : "999+",
                        CairoFont.WhiteSmallText().WithFontSize(13), tmpEB, "stock" + i);
                }
            }*/


            SingleComposer.Compose(true);

        }
        public GuiDialogBlockEntityMailBoxInventory(string dialogTitle,
            InventoryBase inventory, BlockPos blockEntityPos, int cols, ICoreClientAPI capi)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            Recompose();
           /* if (IsDuplicate)
            {
                return;
            }

            string ownerName = (Inventory as InventoryCANMailBox)?.be?.ownerUID;

            for (int i = 0; i < 2; i++)
            {
                this.Inventory[i].HexBackgroundColor = "#eb600b";
            }

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds ownerNameBounds = ElementBounds.Fixed(0.0, 3.0, 150, 25).WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds closeButton = ElementBounds.Fixed(0, 30, 0, 0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);


            ElementBounds leftSlots = ElementBounds.FixedSize(120, 60).FixedUnder(ownerNameBounds, 15);
            ElementBounds buttonSend = leftSlots.RightCopy().WithFixedSize(60, 40);
            ElementBounds ownerSlots = ElementBounds.FixedSize(200, 230).FixedUnder(leftSlots, 15);
            //ElementBounds rightSlots = ElementBounds.FixedSize(60, 230).FixedRightOf(leftSlots);
            //rightSlots.fixedY = leftSlots.fixedY;

            ElementBounds InfiniteStocksTextBounds = ElementBounds.FixedSize(150, 25).FixedUnder(leftSlots, 15);
            ElementBounds InfiniteStocksButtonBounds = ElementBounds.FixedSize(50, 25).RightOf(InfiniteStocksTextBounds, 15);
            InfiniteStocksButtonBounds.fixedY = InfiniteStocksTextBounds.fixedY;

            ElementBounds StorePaymentTextBounds = ElementBounds.FixedSize(150, 25).FixedUnder(InfiniteStocksButtonBounds, 15);
            ElementBounds StorePaymentButtonBounds = ElementBounds.FixedSize(50, 25).RightOf(StorePaymentTextBounds, 15);
            StorePaymentButtonBounds.fixedY = StorePaymentTextBounds.fixedY;

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[]
            {
                closeButton
            });
            SingleComposer = this.capi.Gui
               .CreateCompo("canmailboxCompo", dialogBounds)
               .AddShadedDialogBG(bgBounds, true, 5.0, 0.75f)
               .AddDialogTitleBar(Lang.Get("canmailbox:gui-mailbox-name"), delegate
               {
                   TryClose();
               }, null, null)
               .BeginChildElements(bgBounds);


            //new int[] { 0, 1, 2, 3 }

            bool openedByOwner = (this.Inventory as InventoryCANMailBox).be.ownerUID.Equals(this.capi.World.Player.PlayerUID);
            if (openedByOwner && !likeNonOwner)
            {
                SingleComposer.AddItemSlotGrid(Inventory, new Action<object>(DoSendPacket), 4, new int[] { 0, 1 }, leftSlots, "publicSlots");
                SingleComposer.AddItemSlotGrid(Inventory, new Action<object>(DoSendPacket), 4, Enumerable.Range(2, Inventory.Count - 2).ToArray(), ownerSlots, "privateSlots");
                SingleComposer.AddSwitch((t) =>
                {
                    this.likeNonOwner = t;
                    this.Recompose();
                }, ownerSlots.BelowCopy());
            }
            else
            {
                SingleComposer.AddItemSlotGrid(Inventory, new Action<object>(DoSendPacket), 4, new int[] {0, 1}, leftSlots, "publicSlots");
                SingleComposer.AddIconButton("canmailbox:save-arrow", (bool t) =>
                {
                    DoSendPacketMoveDown();
                }, buttonSend, "sendButton");

                //SingleComposer.AddItemSlotGrid(Inventory, new Action<object>(DoSendPacket), 4, Enumerable.Range(2, Inventory.Count - 2).ToArray(), ownerSlots, "privateSlots");
            }
            
            // SingleComposer.AddItemSlotGrid(ownerInventory, new Action<object>(DoSendPacketOwner), 4, Enumerable.Range(0, ownerInventory.Count).ToArray(), ownerSlots, "ownerSlots");
            //SingleComposer.AddItemSlotGrid((IInventory)this.Inventory, new Action<object>(this.DoSendPacket), 1, new int[] { 1, 3, 5, 7 }, rightSlots, "goodsSlots");


            var slotSize = GuiElementPassiveItemSlot.unscaledSlotSize;
            var slotPaddingSize = GuiElementItemSlotGridBase.unscaledSlotPadding;



            SingleComposer.Compose(true);


          */
        }
    }
}
